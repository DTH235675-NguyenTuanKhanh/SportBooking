"""QR acceptance through real forms on a clean, disposable database."""
import concurrent.futures
from datetime import timedelta
import html
import os
import time
import unittest
from urllib.parse import urlsplit

from playwright.sync_api import expect, sync_playwright
import requests

import regression
from regression import ROOT, Regression, sample_png
from sb09_e2e import CleanDatabaseJourney


def upload(variant=1):
    return {'name': 'payment-test.png', 'mimeType': 'image/png', 'buffer': sample_png(variant)}


class QrJourney(CleanDatabaseJourney):
    evidence_folder = ROOT / 'docs/qr-transfer/evidence'

    def make_inventory(self, admin, owner):
        self.go(admin, '/Admin/SportTypeManagement/Create')
        self.fill(admin, Name='Cầu lông QR', DisplayOrder=1)
        admin.locator('#IsActive').check()
        self.submit(admin, '/Admin/SportTypeManagement/Create', 'Lưu loại sân')
        sport = self.rows('SELECT Id FROM SportTypes')[0]['Id']
        self.go(owner, '/Owner/Complexes/Create')
        self.fill(owner, Name='Cụm sân QR', Address='10 Đường Kiểm Thử', Province='TP. Hồ Chí Minh',
                  District='Quận 5', OpeningTime='07:00', ClosingTime='22:00')
        self.submit(owner, '/Owner/Complexes/Create', 'Lưu cụm sân')
        complex_id = self.rows('SELECT Id FROM SportComplexes')[0]['Id']
        self.go(owner, f'/Owner/Courts/Create?complexId={complex_id}')
        self.fill(owner, Name='Sân mẫu QR')
        owner.locator('#SportTypeId').select_option(str(sport))
        self.submit(owner, '/Owner/Courts/Create', 'Lưu sân')
        court = self.rows('SELECT Id FROM Courts')[0]['Id']
        for start, end, price in (('18:00', '19:00', 180000), ('19:00', '20:00', 220000)):
            self.go(owner, f'/Owner/TimeSlots/Create?courtId={court}')
            self.fill(owner, StartTime=start, EndTime=end, PriceWeekday=price, PriceWeekend=price)
            self.submit(owner, '/Owner/TimeSlots/Create', 'Lưu khung giờ')
        return court

    def checkout(self, customer, court, day, method='payOnline', count=2):
        self.go(customer, f'/Booking/Create?courtId={court}&bookingDate={day}')
        slots = customer.locator('#timeSlotGrid input[type=checkbox]:not(:disabled)')
        for index in range(count):
            slots.nth(index).check()
        customer.locator(f'label[for={method}]').click()
        self.submit(customer, '/Booking/Create', 'Xác nhận đặt sân')
        path = urlsplit(customer.url)
        self.assertEqual('/BankTransfer/Details' if method == 'payOnline' else '/Booking/Confirmation', path.path)
        return path.path + '?' + path.query

    def review_request(self, page, code, report_id, decision, note=''):
        return self.protected_post(page, '/BankTransfer/Review', {
            'code': code, 'reportId': str(report_id), 'decision': decision, 'note': note,
        })

    def payment_rows(self, code):
        return self.rows("SELECT b.Id, b.Status AS BookingStatus, b.PaymentStatus, b.PaymentDeadline, p.Amount, "
                         "p.Status AS PaymentRowStatus, p.ManualRefundedAt, p.ManualRefundedById "
                         "FROM Bookings b JOIN Payments p ON p.BookingId=b.Id "
                         f"WHERE b.BookingGroupCode='{code}' ORDER BY b.StartDateTime")

    def review_concurrently(self, page, code, report_id):
        cookie_values = {cookie['name']: cookie['value'] for cookie in page.context.cookies()}
        csrf = page.locator('form[action="/BankTransfer/Review"] input[name=__RequestVerificationToken]').input_value()

        def submit(_):
            with requests.Session() as client:
                client.trust_env = False
                return client.post(self.base + '/BankTransfer/Review', cookies=cookie_values, data={
                    'code': code, 'reportId': report_id, 'decision': 'received',
                    '__RequestVerificationToken': csrf}, timeout=25, allow_redirects=False).status_code

        with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
            self.assertEqual([302, 302], list(pool.map(submit, range(2))))

    def expire_and_review(self, owner, code, report_id):
        complex_id = self.rows(f"SELECT ComplexId FROM BankTransfers WHERE GroupCode='{code}'")[0]['ComplexId']
        resource = f'SportBooking:Complex:{complex_id}'
        # Hold the same lock while the real worker (5-second scan) and the owner request queue.
        with concurrent.futures.ThreadPoolExecutor(max_workers=1) as pool:
            holder = pool.submit(Regression.sql, f"""
                BEGIN TRANSACTION;
                DECLARE @got int;
                EXEC @got=sys.sp_getapplock @Resource=N'{resource}', @LockMode='Exclusive', @LockOwner='Transaction';
                IF @got < 0 THROW 51000, 'Test could not hold the schedule lock.', 1;
                WAITFOR DELAY '00:00:08';
                COMMIT;
            """, self.database)
            until = time.monotonic() + 5
            while self.rows(f"SELECT APPLOCK_TEST('public', '{resource}', 'Exclusive', 'Session') AS Available")[0]['Available']:
                self.assertLess(time.monotonic(), until, 'Test did not acquire the schedule lock')
                time.sleep(.1)
            Regression.sql("UPDATE Bookings SET PaymentDeadline=DATEADD(MINUTE,419,SYSUTCDATETIME()) "
                           f"WHERE BookingGroupCode='{code}';", self.database)
            self.review_concurrently(owner, code, report_id)
            holder.result()

    def test_qr_transfer(self):
        self.base, _ = self.start(bootstrap=True, email='admin@sb09.example.test',
                                  password=self.password, scan_seconds=5)
        self.assertEqual([], self.rows('SELECT Id FROM Courts'))
        with sync_playwright() as playwright:
            browser = playwright.chromium.launch(channel='msedge', headless=True)
            admin, owner, other_owner, customer, other_customer = [self.new_page(browser) for _ in range(5)]
            self.login(admin, 'admin', '/Admin')
            owner.set_viewport_size({'width': 375, 'height': 880})
            owner_user = self.register(owner, 'qr-owner', 'Chủ sân QR', owner=True, qr_image=upload())
            self.assertEqual(1, owner_user['ApprovalStatus'])
            self.assertEqual(1, len(self.rows('SELECT OwnerId FROM OwnerTransferProfiles')))
            self.login(owner, 'qr-owner', '/Account/Login', pending=True)
            self.approve(admin, owner_user['Id'])
            self.login(owner, 'qr-owner', '/Owner')
            other = self.register(other_owner, 'qr-other-owner', 'Chủ sân khác', owner=True)
            self.approve(admin, other['Id'])
            self.login(other_owner, 'qr-other-owner', '/Owner')
            self.register(customer, 'qr-customer', 'Khách QR')
            self.login(customer, 'qr-customer', '/')
            self.register(other_customer, 'qr-other-customer', 'Khách khác')
            self.login(other_customer, 'qr-other-customer', '/')
            court = self.make_inventory(admin, owner)
            settings = '/OwnerTransfer/Edit'

            owner.set_viewport_size({'width': 375, 'height': 880})
            self.go(owner, settings)
            expect(owner.locator('[data-qr-preview]')).to_be_visible()
            self.fits(owner)
            self.record('owner-payment-settings-mobile', {'registered_with_qr': True}, owner)
            owner.locator('#Enabled').uncheck()
            self.submit(owner, settings, 'Lưu thông tin chuyển khoản')
            create_path = f'/Booking/Create?courtId={court}&bookingDate={self.day}'
            self.go(customer, create_path)
            expect(customer.locator('#payOnline')).to_be_disabled()
            self.go(other_owner, settings)
            other_owner.locator('#Enabled').check()
            self.fill(other_owner, BankName='Ngân hàng khác', AccountNumber='99990000', AccountName='CHU SAN KHAC')
            other_owner.locator('#Confirmed').check()
            self.submit(other_owner, settings, 'Lưu thông tin chuyển khoản', status=200)
            expect(other_owner.locator('main')).to_contain_text('Chọn ảnh PNG hoặc JPEG hợp lệ')
            self.assertEqual(1, len(self.rows('SELECT OwnerId FROM OwnerTransferProfiles')))
            other_owner.locator('#Qr').set_input_files(upload(4))
            other_owner.locator('#Confirmed').check()
            self.submit(other_owner, settings, 'Lưu thông tin chuyển khoản')
            self.assertEqual(2, len(self.rows('SELECT OwnerId FROM OwnerTransferProfiles')))
            grid = customer.request.get(self.base + f'/Booking/GetTimeSlots?courtId={court}&date={self.day}').json()['slots']
            refused = self.protected_post(customer, '/Booking/Create', {
                'CourtId': str(court), 'BookingDate': str(self.day), 'SelectedTimeSlotIds': str(grid[0]['id']),
                'TotalAmount': str(grid[0]['price']), 'PaymentMethod': 'BankTransfer',
                f"SlotQuotes[{grid[0]['id']}]": grid[0]['quote'],
            })
            self.assertEqual(200, refused.status)
            self.assertIn('Chủ sân chưa bật chuyển khoản', html.unescape(refused.text()))
            self.assertEqual([], self.bookings())

            owner.set_viewport_size({'width': 1440, 'height': 900})
            self.go(owner, settings)
            owner.locator('#Enabled').check()
            owner.locator('#Confirmed').check()
            self.submit(owner, settings, 'Lưu thông tin chuyển khoản')
            self.fits(owner)
            customer.set_viewport_size({'width': 375, 'height': 880})
            detail = self.checkout(customer, court, self.day)
            transfer = self.rows('SELECT GroupCode, Reference, Amount, AccountNumber, QrImageId, State FROM BankTransfers')[0]
            code = transfer['GroupCode']
            self.assertEqual((400000, '0123456789', 0), (transfer['Amount'], transfer['AccountNumber'], transfer['State']))
            day_rows = self.payment_rows(code)
            self.assertEqual([(0, 0, 180000), (0, 0, 220000)],
                             [(r['BookingStatus'], r['PaymentRowStatus'], r['Amount']) for r in day_rows])
            qr = f'/BankTransfer/Qr?code={code}'
            original_qr = customer.request.get(self.base + qr)
            self.assertEqual(('image/png', 'nosniff'),
                             (original_qr.headers['content-type'], original_qr.headers['x-content-type-options']))
            self.assertIn('no-store', original_qr.headers['cache-control'])
            qr_download = customer.request.get(self.base + qr + '&download=true')
            self.assertIn('attachment', qr_download.headers['content-disposition'])
            self.assertEqual(original_qr.body(), qr_download.body())
            expect(customer.locator('main')).to_contain_text(transfer['Reference'])
            expect(customer.locator('[data-transfer-qr]')).to_be_visible()
            expect(customer.get_by_role('button', name='Sao chép số tài khoản')).to_be_visible()
            self.fits(customer)
            self.record('customer-transfer-mobile', {'method': 'BankTransfer', 'amount': transfer['Amount'],
                                                      'before': day_rows}, customer)
            customer.set_viewport_size({'width': 1440, 'height': 900})
            self.fits(customer)
            self.record('customer-transfer-desktop', {'copy_controls': True}, customer)
            customer.bring_to_front()
            customer.context.grant_permissions(['clipboard-read', 'clipboard-write'])
            for label, value in (('Sao chép số tài khoản', transfer['AccountNumber']),
                                 ('Sao chép nội dung chuyển khoản', transfer['Reference'])):
                customer.get_by_role('button', name=label).click()
                expect(customer.locator('[data-copy-result]')).to_have_text('Đã sao chép.')
                self.assertEqual(value, customer.evaluate('navigator.clipboard.readText()'))
            customer.set_viewport_size({'width': 375, 'height': 880})

            receipt = customer.locator('#transferReceipt')
            receipt.set_input_files({'name': 'invalid.png', 'mimeType': 'image/png', 'buffer': b'<svg>fake</svg>'})
            self.submit(customer, '/BankTransfer/Report', 'Tôi đã chuyển khoản')
            expect(customer.get_by_text('Chọn ảnh PNG hoặc JPEG hợp lệ', exact=False)).to_be_visible()
            self.assertEqual([], self.rows('SELECT Id FROM TransferReports'))
            customer.locator('#transferReceipt').set_input_files(upload(3))
            self.submit(customer, '/BankTransfer/Report', 'Tôi đã chuyển khoản')
            expect(customer.locator('main')).to_contain_text('Chờ chủ sân xác nhận')
            self.assertEqual([0, 0], [r['PaymentStatus'] for r in self.payment_rows(code)])
            first_report = self.rows('SELECT Id FROM TransferReports')[0]['Id']
            self.assertEqual(302, self.protected_post(customer, '/BankTransfer/Report', {'code': code}).status)
            self.assertEqual([first_report], [r['Id'] for r in self.rows('SELECT Id FROM TransferReports')])
            receipt_path = f'/BankTransfer/Receipt?code={code}&reportId={first_report}'
            self.assertEqual(200, owner.request.get(self.base + receipt_path).status)
            self.assertEqual(200, customer.request.get(self.base + receipt_path).status)
            for stranger in (other_owner, other_customer):
                for path in (detail, qr, receipt_path):
                    self.assertEqual(404, stranger.request.get(self.base + path, max_redirects=0).status)
            anonymous = browser.new_context()
            self.assertEqual(302, anonymous.request.get(self.base + receipt_path, max_redirects=0).status)
            anonymous.close()
            self.assertEqual(404, self.review_request(other_owner, code, first_report, 'received').status)
            self.assertEqual(404, self.protected_post(other_customer, '/BankTransfer/Report', {'code': code}).status)
            self.assertEqual(400, owner.request.post(self.base + '/BankTransfer/Review',
                                                    form={'code': code, 'decision': 'received'}).status)

            queue = f'/Owner/Bookings?TransferReview=true&Keyword={code}'
            self.go(owner, queue)
            owner.get_by_role('link', name='Chờ chủ sân xác nhận').first.click()
            expect(owner.locator('main')).to_contain_text('Khách QR')
            self.fits(owner)
            self.record('owner-reconcile-desktop', {'receipt_id': first_report, 'queue': True}, owner)
            owner.set_viewport_size({'width': 375, 'height': 880})
            self.fits(owner)
            self.record('owner-reconcile-mobile', {'receipt_id': first_report}, owner)
            owner.set_viewport_size({'width': 1440, 'height': 900})
            self.fill(owner, **{'review-note': 'Chưa tìm thấy đúng số tiền'})
            self.submit(owner, '/BankTransfer/Review', 'Chưa tìm thấy / cần bổ sung')
            previous_deadlines = [r['PaymentDeadline'] for r in self.payment_rows(code)]
            self.go(customer, detail)
            expect(customer.locator('main')).to_contain_text('Chưa tìm thấy giao dịch')
            self.submit(customer, '/BankTransfer/Report', 'Tôi đã chuyển khoản')
            current_report = self.rows('SELECT Id FROM TransferReports ORDER BY Id')[-1]['Id']
            self.assertNotEqual(first_report, current_report)
            self.assertEqual(previous_deadlines, [r['PaymentDeadline'] for r in self.payment_rows(code)])
            self.assertEqual(302, self.review_request(owner, code, first_report, 'received').status)
            self.assertEqual([0, 0], [r['PaymentStatus'] for r in self.payment_rows(code)])
            self.go(owner, detail)
            self.review_concurrently(owner, code, current_report)
            confirmed = self.payment_rows(code)
            self.assertEqual([(1, 1, 1), (1, 1, 1)],
                             [(r['BookingStatus'], r['PaymentStatus'], r['PaymentRowStatus']) for r in confirmed])
            self.go(customer, detail)
            expect(customer.locator('main')).to_contain_text('Đã xác nhận nhận tiền')
            self.record('customer-paid-mobile', {'after': confirmed, 'reports': 2}, customer)

            self.go(owner, settings)
            self.fill(owner, AccountNumber='9876500012')
            owner.locator('#Qr').set_input_files(upload(2))
            owner.locator('#Confirmed').check()
            self.submit(owner, settings, 'Lưu thông tin chuyển khoản')
            self.go(customer, detail)
            expect(customer.locator('main')).to_contain_text('0123456789')
            self.assertEqual(original_qr.body(), customer.request.get(self.base + qr).body())

            cancel_path = f"/Booking/Cancel/{confirmed[0]['Id']}"
            self.go(customer, cancel_path)
            self.assertEqual(302, self.protected_post(customer, '/Booking/Cancel', {
                'BookingId': str(confirmed[0]['Id']), 'CancellationReason': 'Tôi có việc đột xuất nên xin hủy.'}).status)
            self.assertEqual(4, self.payment_rows(code)[0]['PaymentStatus'])
            self.go(owner, detail)
            refund = {'code': code, 'bookingId': str(confirmed[0]['Id']), 'note': 'Đã đối chiếu và hoàn thủ công thử'}
            self.assertEqual(302, self.protected_post(owner, '/BankTransfer/Refund', refund).status)
            refunded = self.payment_rows(code)
            self.assertEqual((3, 2, owner_user['Id']), (refunded[0]['BookingStatus'],
                            refunded[0]['PaymentRowStatus'], refunded[0]['ManualRefundedById']))
            self.assertEqual(302, self.protected_post(owner, '/BankTransfer/Refund', refund).status)
            self.assertEqual(refunded, self.payment_rows(code))
            day = self.before.date().isoformat()
            self.go(owner, f'/Owner/Revenue?From={day}&To={day}&CourtId={court}')
            for selector, amount in (('#received', 400000), ('#refunded', 180000), ('#net', 220000)):
                expect(owner.locator(selector + ' dd')).to_have_text(f"{amount:,}".replace(',', '.') + ' ₫')

            second_day = self.day + timedelta(days=1)
            late_path = self.checkout(customer, court, second_day, count=1)
            late_code = self.rows(f"SELECT GroupCode FROM BankTransfers WHERE GroupCode<>'{code}'")[0]['GroupCode']
            expect(customer.locator('main')).to_contain_text('9876500012')
            new_qr = customer.request.get(self.base + f'/BankTransfer/Qr?code={late_code}').body()
            self.assertNotEqual(original_qr.body(), new_qr)
            self.submit(customer, '/BankTransfer/Report', 'Tôi đã chuyển khoản')
            self.go(owner, late_path)
            late_report = self.rows(f"SELECT Id FROM TransferReports WHERE GroupCode='{late_code}'")[0]['Id']
            self.expire_and_review(owner, late_code, late_report)
            late = self.payment_rows(late_code)
            self.assertEqual((4, 4, 4), (late[0]['BookingStatus'], late[0]['PaymentStatus'], late[0]['PaymentRowStatus']))
            self.checkout(other_customer, court, second_day, method='payCounter', count=1)
            self.assertEqual(302, self.review_request(owner, late_code, late_report, 'received').status)
            self.assertEqual(late, self.payment_rows(late_code))
            self.go(customer, late_path)
            expect(customer.locator('main')).to_contain_text('Booking đã hết hạn')
            self.record('late-transfer-rebooked', {'expired_paid': late, 'new_booking_kept': True}, customer)
            self.assertEqual([], self.page_errors)
            self.flow_passed = True
            browser.close()


if __name__ == '__main__':
    regression.APP_DLL = regression.APP_DLL if os.environ.get('SPORTBOOKING_TEST_DLL') else ROOT / 'bin/Release/net10.0/SportBooking.dll'
    unittest.main(argv=[__file__, 'QrJourney.test_qr_transfer'], verbosity=2)
