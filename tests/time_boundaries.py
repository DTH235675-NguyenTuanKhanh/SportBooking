"""Exercise the real HTTP and background expiry paths with fixed UTC instants."""
import html
import re
import time
import unittest
from urllib.parse import urlencode

import regression
from regression import ROOT, StartupRegression, sample_png, token


class VietnamTimeRegression(StartupRegression):
    def at(self, instant, seed=False):
        self.base, self.client = self.start(demo=seed, test_utc_now=instant)
        self.assertEqual(302, self.submit('/Account/Login', {
            'Email': 'customer1@sportbooking.vn', 'Password': 'Sport@123'}).status_code)

    def get(self, path):
        return self.client.get(self.base + path, timeout=20, allow_redirects=False)

    def submit(self, path, fields, source=None):
        return self.client.post(self.base + path, timeout=20, allow_redirects=False,
                                data={**fields, '__RequestVerificationToken': token(self.get(source or path))})

    def slots(self, day):
        return self.get(f'/Booking/GetTimeSlots?courtId=1&date={day}').json()['slots']

    def book(self, day, slots, payment, note):
        return self.submit('/Booking/Create', {
            'CourtId': 1, 'BookingDate': day, 'SelectedTimeSlotIds': [s['id'] for s in slots],
            'TotalAmount': sum(int(s['price']) for s in slots), 'PaymentMethod': payment, 'Notes': note,
            **{f"SlotQuotes[{s['id']}]": s['quote'] for s in slots}
        }, source='/Booking/MyBookings')

    def state(self, note):
        return self.rows(f"SELECT Id,Status,PaymentStatus FROM Bookings WHERE Note='{note}' ORDER BY Id")

    def expired(self, note, payment_status, size):
        deadline = time.monotonic() + 12
        while time.monotonic() < deadline:
            rows = self.state(note)
            if len(rows) == size and all(row['Status'] == 4 for row in rows):
                self.assertTrue(all(row['PaymentStatus'] == payment_status for row in rows), rows)
                return
            time.sleep(.25)
        self.fail(f'Background worker did not expire {note}: {self.state(note)}')

    def test_utc_midnight_weekend_prices_and_exact_expiry(self):
        # UTC Friday 16:45 = Vietnam Friday 23:45. 1 May is a fixed-date holiday.
        friday, saturday = '2026-05-01', '2026-05-02'
        self.at('2026-05-01T16:45:00Z', seed=True)
        owner = self.client
        self.assertEqual(302, self.submit('/Account/Logout', {}, source='/Booking/MyBookings').status_code)
        self.assertEqual(302, self.submit('/Account/Login', {
            'Email': 'owner1@sportbooking.vn', 'Password': 'Sport@123'}).status_code)
        bank = owner.post(self.base + '/OwnerTransfer/Edit', timeout=20, allow_redirects=False, data={
            'Enabled': 'true', 'Confirmed': 'true', 'BankName': 'Ngân hàng thử',
            'AccountNumber': '0123456789', 'AccountName': 'CHU SAN THU',
            '__RequestVerificationToken': token(self.get('/OwnerTransfer/Edit')),
        }, files={'Qr': ('qr.png', sample_png(), 'image/png')})
        self.assertEqual(302, bank.status_code)
        self.assertTrue(self.rows('SELECT Enabled FROM OwnerTransferProfiles')[0]['Enabled'])
        self.assertEqual(302, self.submit('/Account/Logout', {}, source='/OwnerTransfer/Edit').status_code)
        self.assertEqual(302, self.submit('/Account/Login', {
            'Email': 'customer1@sportbooking.vn', 'Password': 'Sport@123'}).status_code)
        home = html.unescape(self.get('/').text)
        self.assertIn(f'min="{friday}"', home)
        weekday = self.slots(friday)
        weekend = self.slots(saturday)
        rates = self.rows(f"SELECT PriceWeekday,PriceWeekend,PriceHoliday FROM TimeSlots WHERE Id={weekend[0]['id']}")[0]
        self.assertNotEqual(0, rates['PriceHoliday'])
        self.assertNotEqual(rates['PriceHoliday'], rates['PriceWeekday'])
        self.assertEqual(rates['PriceWeekday'], weekday[0]['price'])
        self.assertEqual(rates['PriceWeekend'], weekend[0]['price'])
        self.assertFalse(any(slot['selectable'] for slot in weekday))
        self.assertNotIn('/Court/Detail/1', self.get(f'/Court/Search?BookingDate={friday}&TimeSlotId=360').text)
        self.assertIn('/Court/Detail/1', self.get(f'/Court/Search?BookingDate={saturday}&TimeSlotId=360').text)
        past = self.book(friday, weekday[:1], 'Counter', 'past-time')
        self.assertEqual(200, past.status_code)
        self.assertIn('đã trôi qua', html.unescape(past.text))
        self.assertEqual([], self.state('past-time'))

        counter = self.book(saturday, weekend[:2], 'Counter', 'boundary-counter')
        if counter.status_code != 302:
            failure = html.unescape(counter.text)
            main = re.search(r'<main\b[^>]*>(.*?)</main>', failure, re.S)
            visible = re.sub(r'\s+', ' ', re.sub(r'<[^>]*>', ' ', main.group(1) if main else failure))
            self.fail(f'Booking POST HTTP {counter.status_code}: {visible[:1800]}')
        online = self.book(saturday, weekend[2:4], 'BankTransfer', 'boundary-online')
        self.assertEqual(302, online.status_code)
        saved = self.rows("SELECT PaymentDeadline,TotalAmount FROM Bookings WHERE Note='boundary-online' ORDER BY Id")
        self.assertEqual([('2026-05-02T00:00:00', slot['price']) for slot in weekend[2:4]],
                         [(row['PaymentDeadline'][:19], row['TotalAmount']) for row in saved])
        self.stop()

        self.at('2026-05-01T16:59:59Z')
        self.assertEqual([0, 0], [r['Status'] for r in self.state('boundary-online')])
        self.assertIn(f'min="{friday}"', self.get('/').text)
        self.stop()

        # UTC is still Friday; all business dates and prices must already be Saturday.
        self.at('2026-05-01T17:00:00Z')
        self.expired('boundary-online', payment_status=3, size=2)
        self.assertEqual([3, 3], [r['Status'] for r in self.rows(
            "SELECT p.Status FROM Payments p JOIN Bookings b ON p.BookingId=b.Id WHERE b.Note='boundary-online'")])
        self.assertEqual([0, 0], [r['Status'] for r in self.state('boundary-counter')])
        self.assertIn(f'min="{saturday}"', self.get('/').text)
        self.assertTrue(all(slot['selectable'] for slot in self.slots(saturday)[2:4]))
        self.assertNotIn('/Court/Detail/1', self.get(f'/Court/Search?BookingDate={friday}').text)
        self.stop()

        self.at('2026-05-01T22:59:59Z')
        self.assertEqual([0, 0], [r['Status'] for r in self.state('boundary-counter')])
        self.stop()
        self.at('2026-05-01T23:00:00Z')
        self.expired('boundary-counter', payment_status=0, size=2)
        current = self.slots(saturday)
        self.assertFalse(current[0]['selectable'])  # 06:00 just started in Vietnam.
        self.assertTrue(current[1]['selectable'])  # 07:00 is released with the group.
        self.assertNotIn('/Court/Detail/1', self.get(f'/Court/Search?BookingDate={saturday}&TimeSlotId=360').text)
        search = self.get(f'/Court/Search?BookingDate={saturday}&TimeSlotId=420').text
        self.assertIn('/Court/Detail/1', search)
        court = self.rows('SELECT Name FROM Courts WHERE Id=1')[0]['Name']
        exact_search = self.get('/Court/Search?' + urlencode({
            'Keyword': court, 'BookingDate': saturday, 'TimeSlotId': 420}))
        self.assertIn(f"{sum(slot['selectable'] for slot in current)} khung giờ trống",
                      html.unescape(exact_search.text))
        self.assertEqual(302, self.book(saturday, current[1:2], 'Counter', 'boundary-rebook').status_code)
        self.assertEqual(4, self.state('boundary-counter')[0]['Status'])
        self.assertEqual(1, len(self.state('boundary-rebook')))

        self.assertEqual(302, self.submit('/Account/Logout', {}, source='/Booking/MyBookings').status_code)
        self.assertEqual(302, self.submit('/Account/Login', {
            'Email': 'owner1@sportbooking.vn', 'Password': 'Sport@123'}).status_code)
        self.assertIn('02/05/2026', self.get('/Owner').text)


if __name__ == '__main__':
    regression.APP_DLL = ROOT / 'bin/SB09/net10.0/SportBooking.dll'
    unittest.main(argv=[__file__, 'VietnamTimeRegression.test_utc_midnight_weekend_prices_and_exact_expiry'], verbosity=2)
