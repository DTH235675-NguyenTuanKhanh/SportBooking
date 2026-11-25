"""HTTP/SQL regression tests. Runs against a disposable LocalDB database only."""
import concurrent.futures
from datetime import datetime, timedelta, timezone
import hashlib
import hmac
import html
from html.parser import HTMLParser
import json
import os
from pathlib import Path
import re
import socket
import subprocess
import struct
import tempfile
import threading
import time
import unittest
from urllib.parse import parse_qs, urlencode, urlparse
import uuid
import zlib

import requests

ROOT = Path(__file__).resolve().parents[1]
APP_DLL = Path(os.environ.get('SPORTBOOKING_TEST_DLL', str(ROOT / 'bin/Debug/net10.0/SportBooking.dll')))
SECRET = 'regression-only-secret'
MERCHANT = 'AUDIT123'
VIETNAM = timezone(timedelta(hours=7))


def vietnam_day(offset):
    return (datetime.now(VIETNAM).date() + timedelta(days=offset)).isoformat()


def sample_png(variant=1):
    """A synthetic image for checking uploads and snapshots. No payment data."""
    def chunk(name, data):
        return struct.pack('!I', len(data)) + name + data + struct.pack('!I', zlib.crc32(name + data))

    size = 32
    pixels = b''.join(b'\0' + bytes(
        0 if 2 < x < 29 and 2 < y < 29 and (x * variant + y) % 5 < 2 else 255
        for x in range(size)) for y in range(size))
    return (b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('!IIBBBBB', size, size, 8, 0, 0, 0, 0))
            + chunk(b'IDAT', zlib.compress(pixels)) + chunk(b'IEND', b''))


class Inputs(HTMLParser):
    def __init__(self, value):
        super().__init__()
        self.items = []
        self.feed(value)

    def handle_starttag(self, tag, attrs):
        if tag == 'input':
            self.items.append(dict(attrs))


def token(response):
    return next(x['value'] for x in Inputs(response.text).items
                if x.get('name') == '__RequestVerificationToken')


def sign(fields):
    data = urlencode(sorted((k, str(v)) for k, v in fields.items()
                            if k.startswith('vnp_') and k not in ('vnp_SecureHash', 'vnp_SecureHashType') and str(v)))
    return hmac.new(SECRET.encode(), data.encode(), hashlib.sha512).hexdigest()


class Regression(unittest.TestCase):
    sequence = 0

    @classmethod
    def sql(cls, query, database=None):
        result = subprocess.run([
            'sqlcmd', '-S', r'(localdb)\MSSQLLocalDB', '-d', database or cls.database,
            '-E', '-C', '-I', '-b', '-l', '5', '-y', '0', '-w', '65535',
            '-Q', 'SET NOCOUNT ON; ' + query
        ], capture_output=True, encoding='utf-8', errors='replace', timeout=30)
        if result.returncode:
            raise RuntimeError(result.stdout + result.stderr)
        return result.stdout.strip()

    @classmethod
    def rows(cls, query):
        return json.loads(cls.sql(query + ' FOR JSON PATH') or '[]')

    @classmethod
    def prepare_fixture_views(cls, database):
        # Keep the existing SQL fixture queries and their JSON keys. The app itself
        # always uses the physical Vietnamese tables through EF Core.
        if not re.fullmatch(r'SportBookingRegression_[0-9a-f]{12}', database):
            raise RuntimeError('Fixture views require an isolated regression database')
        migration = next((ROOT / 'Data/Migrations').glob('*_VietHoaBangVaCot.cs'))
        up = migration.read_text(encoding='utf-8-sig').split('protected override void Down', 1)[0]
        tables = re.findall(r'RenameTable\(\s*name: "(\w+)",\s*newName: "(\w+)"\)', up)
        columns = re.findall(r'RenameColumn\(\s*name: "(\w+)",\s*table: "(\w+)",\s*newName: "(\w+)"\)', up)
        commands = []
        for english, vietnamese in tables:
            projection = ', '.join(f'[{localized}] AS [{original}]'
                                   for original, table, localized in columns if table == vietnamese)
            if not projection:
                raise RuntimeError(f'Missing fixture mapping for {vietnamese}')
            commands.append(f"IF OBJECT_ID(N'dbo.[{vietnamese}]', N'U') IS NOT NULL "
                            f"AND OBJECT_ID(N'dbo.[{english}]') IS NULL "
                            f"EXEC(N'CREATE VIEW dbo.[{english}] AS SELECT {projection} FROM dbo.[{vietnamese}]');")
        # sqlcmd limits the maximum command argument size on Windows.
        for offset in range(0, len(commands), 8):
            cls.sql(' '.join(commands[offset:offset + 8]), database=database)

    @classmethod
    def setUpClass(cls):
        cls.database = 'SportBookingRegression_' + uuid.uuid4().hex[:12]
        with socket.socket() as sock:
            sock.bind(('127.0.0.1', 0))
            cls.port = sock.getsockname()[1]
        cls.base = f'http://127.0.0.1:{cls.port}'
        cls.temp = tempfile.TemporaryDirectory(prefix='sportbooking-regression-')
        cls.log_path = Path(cls.temp.name) / 'app.log'
        cls.log = cls.log_path.open('w', encoding='utf-8')
        environment = os.environ.copy()
        environment.update({
            'ASPNETCORE_ENVIRONMENT': 'Development',
            'ASPNETCORE_URLS': cls.base,
            'ConnectionStrings__DefaultConnection':
                f'Server=(localdb)\\MSSQLLocalDB;Database={cls.database};Trusted_Connection=True;'
                'MultipleActiveResultSets=true;TrustServerCertificate=True',
            'DatabaseInitialization__ApplyMigrations': 'true',
            'DatabaseInitialization__SeedDemoData': 'true',
            'DatabaseInitialization__CreateInitialAdmin': 'false',
            'VnPaySettings__TmnCode': MERCHANT,
            'VnPaySettings__HashSecret': SECRET,
            'EmailSettings__UseConsoleSender': 'true',
            'BookingSettings__ExpirationScanIntervalSeconds': '5',
            'Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command': 'Warning',
            'Logging__LogLevel__SportBooking': 'Information'
        })
        cls.app = subprocess.Popen(
            ['dotnet', str(APP_DLL)], cwd=ROOT,
            env=environment, stdout=cls.log, stderr=subprocess.STDOUT,
            creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
        cls.addClassCleanup(cls.cleanup_app)
        client = requests.Session()
        client.trust_env = False
        deadline = time.monotonic() + 45
        while time.monotonic() < deadline:
            try:
                if client.get(cls.base + '/Account/Login', timeout=1).status_code == 200:
                    cls.prepare_fixture_views(cls.database)
                    cls.rows('SELECT TOP 1 Id FROM Courts')
                    return
            except (requests.RequestException, RuntimeError):
                pass
            if cls.app.poll() is not None:
                break
            time.sleep(.2)
        raise RuntimeError('Test application did not start: '
                           + cls.log_path.read_text(encoding='utf-8', errors='replace')[-4000:])

    @classmethod
    def cleanup_app(cls):
        if cls.app.poll() is None:
            cls.app.terminate()
            try:
                cls.app.wait(timeout=10)
            except subprocess.TimeoutExpired:
                cls.app.kill()
                cls.app.wait(timeout=5)
        cls.log.close()
        # Only a database generated by this test process can be deleted.
        if not re.fullmatch(r'SportBookingRegression_[0-9a-f]{12}', cls.database):
            raise RuntimeError('Refusing to clean up an unexpected database name')
        cls.sql(f"IF DB_ID(N'{cls.database}') IS NOT NULL BEGIN "
                f'ALTER DATABASE [{cls.database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; '
                f'DROP DATABASE [{cls.database}]; END', database='master')
        cls.temp.cleanup()

    def session(self):
        client = requests.Session()
        client.trust_env = False
        self.addCleanup(client.close)
        return client

    def get(self, client, path):
        return client.get(self.base + path, timeout=20, allow_redirects=False)

    def post(self, client, path, data, source=None):
        page = self.get(client, source or path)
        self.assertEqual(200, page.status_code, page.text[:200])
        if path == '/Booking/Create' and 'TotalAmount' not in data:
            data = {**data, 'TotalAmount': self.quote_total(data['SelectedTimeSlotIds'], data['BookingDate'])}
        if path == '/Booking/Create':
            data = dict(data)
            for key, value in self.slot_quotes(client, data['CourtId'], data['BookingDate']).items():
                data.setdefault(key, value)
        return client.post(self.base + path, data={**data, '__RequestVerificationToken': token(page)},
                           timeout=25, allow_redirects=False)

    def quote_total(self, slots, date):
        ids = ','.join(str(int(x)) for x in slots)
        return int(self.rows(f"SELECT COALESCE(SUM(CASE WHEN DATEDIFF(day,'19000101','{date}')%7 IN (5,6) "
                         f"THEN PriceWeekend ELSE PriceWeekday END),0) AS price FROM TimeSlots WHERE Id IN ({ids})")[0]['price'])

    def slot_quotes(self, client, court, date):
        response = self.get(client, f'/Booking/GetTimeSlots?courtId={court}&date={date}')
        return {f'SlotQuotes[{s["id"]}]': s['quote'] for s in response.json().get('slots', [])}

    def login(self, email, password='Sport@123'):
        client = self.session()
        response = self.post(client, '/Account/Login', {'Email': email, 'Password': password})
        self.assertEqual(302, response.status_code, response.text[:200])
        return client

    def setUp(self):
        type(self).sequence += 1
        # Each test gets its own date within the 30-day booking window.
        self.days = type(self).sequence + 2
        self.target = vietnam_day(self.days)
        self.note = 'regression ' + self._testMethodName
        self.customer = self.login('customer1@sportbooking.vn')
        self.owner = self.login('owner1@sportbooking.vn')
        self.admin = self.login('admin@sportbooking.vn')

    def test_z_overlap_after_schedule_change(self):
        # Simulate an older schedule that was changed outside the Owner workflow.
        # The UI and booking POST must still respect the saved booking interval.
        date = vietnam_day(2)
        slots = self.rows('SELECT TOP 2 Id, StartTime, EndTime FROM TimeSlots WHERE CourtId=1 ORDER BY StartTime')
        first, second = slots
        booked = self.post(self.customer, '/Booking/Create', {'CourtId': 1, 'BookingDate': date,
            'SelectedTimeSlotIds': [first['Id']], 'PaymentMethod': 'Counter'}, source='/Booking/Create?courtId=1')
        self.assertEqual(302, booked.status_code)
        original = self.rows(f"SELECT Id, StartDateTime, EndDateTime, TotalAmount FROM Bookings WHERE CourtId=1 AND TimeSlotId={first['Id']} AND BookingDate='{date}'")[0]
        self.sql(f'UPDATE TimeSlots SET IsActive=0 WHERE Id={first["Id"]}')
        self.sql(f"UPDATE TimeSlots SET StartTime=CONVERT(time,DATEADD(minute,30,'{str(first['StartTime'])[:8]}')), "
                 f"EndTime=CONVERT(time,DATEADD(minute,30,'{str(first['EndTime'])[:8]}')) WHERE Id={second['Id']}")
        shifted = self.rows(f'SELECT CONVERT(varchar(5),StartTime,108) AS h FROM TimeSlots WHERE Id={second["Id"]}')[0]['h']
        minutes = int(shifted[:2]) * 60 + int(shifted[3:])
        availability = self.get(self.customer, f'/Booking/GetTimeSlots?courtId=1&date={date}').json()['slots']
        self.assertFalse(next(s for s in availability if s['id'] == second['Id'])['selectable'])
        search = html.unescape(self.get(self.customer,
            f'/Court/Search?BookingDate={date}&TimeSlotId={minutes}').text)
        court_name = self.rows('SELECT Name FROM Courts WHERE Id=1')[0]['Name']
        self.assertNotIn(court_name, search)
        rejected = self.post(self.customer, '/Booking/Create', {'CourtId': 1, 'BookingDate': date,
            'SelectedTimeSlotIds': [second['Id']], 'PaymentMethod': 'Counter'}, source='/Booking/MyBookings')
        self.assertEqual(200, rejected.status_code)
        self.assertIn('vừa được người khác đặt', html.unescape(rejected.text))
        self.assertEqual(original, self.rows(f'SELECT Id, StartDateTime, EndDateTime, TotalAmount FROM Bookings WHERE Id={original["Id"]}')[0])
        detail = html.unescape(self.get(self.customer, f'/Booking/Detail/{original["Id"]}').text)
        self.assertIn(str(original['StartDateTime'])[11:16], detail)

    def test_zz_booking_post_rejects_slot_count_duplicates_and_other_court(self):
        date = vietnam_day(15)
        slots = [row['Id'] for row in self.rows('SELECT TOP 7 Id FROM TimeSlots WHERE CourtId=1 ORDER BY StartTime')]
        foreign = self.rows('SELECT TOP 1 Id FROM TimeSlots WHERE CourtId=2 ORDER BY StartTime')[0]['Id']
        source = '/Booking/Create?courtId=1'
        source_token = token(self.get(self.customer, source))
        base = {'CourtId': 1, 'BookingDate': date, 'PaymentMethod': 'Counter',
                'Notes': self.note, '__RequestVerificationToken': source_token, 'TotalAmount': 100}
        for selected, expected in ((slots, 'tối đa 6 khung giờ'),
                                   ([slots[0], slots[0]], 'không hợp lệ hoặc không thuộc sân'),
                                   ([slots[0], foreign], 'không hợp lệ hoặc không thuộc sân'),
                                   ([slots[0]], 'ngày đã qua')):
            with self.subTest(selected=selected, expected=expected):
                fields = {**base, 'SelectedTimeSlotIds': selected}
                if len(selected) == 1:
                    fields['BookingDate'] = vietnam_day(-1)
                result = self.customer.post(self.base + '/Booking/Create', data=fields,
                                            allow_redirects=False, timeout=25)
                self.assertEqual(200, result.status_code)
                self.assertIn(expected, html.unescape(result.text))
                self.assertEqual([], self.bookings())

    def test_zz_two_customers_race_overlapping_intervals_and_replay(self):
        complex_id, court_id, first, _ = self.make_deletable_court('overlapping race')
        self.assertEqual(302, self.post(self.owner, f'/Owner/TimeSlots/Create?courtId={court_id}', {
            'StartTime': '17:00', 'EndTime': '18:00', 'PriceWeekday': '180000',
            'PriceWeekend': '200000', 'IsActive': 'true'}).status_code)
        second = self.rows(f'SELECT Id FROM TimeSlots WHERE CourtId={court_id} AND Id<>{first}')[0]['Id']
        # Fixture for a previously inconsistent schedule. Normal Owner edits forbid this overlap.
        self.sql(f"UPDATE TimeSlots SET StartTime='16:30', EndTime='17:30' WHERE Id={second}")
        date = vietnam_day(5)
        other = self.login('customer2@sportbooking.vn')
        requests_to_race = []
        for client, slot in ((self.customer, first), (other, second)):
            fields = {'CourtId': court_id, 'BookingDate': date, 'PaymentMethod': 'Counter',
                      'SelectedTimeSlotIds': [slot], 'TotalAmount': self.quote_total([slot], date),
                      'Notes': self.note, '__RequestVerificationToken': token(
                          self.get(client, f'/Booking/Create?courtId={court_id}'))}
            fields.update(self.slot_quotes(client, court_id, date))
            requests_to_race.append((client, fields))
        barrier = threading.Barrier(2)

        def book(item):
            client, fields = item
            barrier.wait(timeout=10)
            return client.post(self.base + '/Booking/Create', data=fields, timeout=25, allow_redirects=False)

        with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
            responses = list(pool.map(book, requests_to_race))
        self.assertEqual([200, 302], sorted(response.status_code for response in responses))
        winner = next(index for index, response in enumerate(responses) if response.status_code == 302)
        selected = requests_to_race[winner][1]['SelectedTimeSlotIds'][0]
        email = f'customer{winner + 1}@sportbooking.vn'
        rows = self.rows(f'SELECT b.Id,b.TimeSlotId,b.TotalAmount,u.Email FROM Bookings b '
                         f'JOIN Users u ON b.CustomerId=u.Id WHERE b.CourtId={court_id}')
        self.assertEqual([(selected, self.quote_total([selected], date), email)],
                         [(row['TimeSlotId'], row['TotalAmount'], row['Email']) for row in rows])
        client, fields = requests_to_race[winner]
        replay = client.post(self.base + '/Booking/Create', data=fields, timeout=25, allow_redirects=False)
        self.assertEqual(200, replay.status_code)
        self.assertEqual([{'Id': rows[0]['Id']}], self.rows(f'SELECT Id FROM Bookings WHERE CourtId={court_id}'))
        self.assertEqual([], self.rows(f'SELECT p.Id FROM Payments p JOIN Bookings b ON p.BookingId=b.Id WHERE b.CourtId={court_id}'))

    def test_zz_owner_court_stop_races_booking(self):
        complex_id, court_id, slot_id, name = self.make_deletable_court('inactive race')
        edit = f'/Owner/Courts/Edit/{court_id}?complexId={complex_id}'
        sport = self.rows(f'SELECT SportTypeId FROM Courts WHERE Id={court_id}')[0]['SportTypeId']
        source = f'/Booking/Create?courtId={court_id}'
        date = vietnam_day(5)
        csrf = token(self.get(self.customer, source))
        fields = {'CourtId': court_id, 'BookingDate': date, 'SelectedTimeSlotIds': [slot_id],
                  'TotalAmount': self.quote_total([slot_id], date), 'PaymentMethod': 'Counter',
                  '__RequestVerificationToken': csrf, **self.slot_quotes(self.customer, court_id, date)}
        barrier = threading.Barrier(2)

        def stop_court():
            barrier.wait(timeout=10)
            return self.post(self.owner, edit, {'Name': name, 'SportTypeId': sport, 'Status': 'Inactive'})

        def book():
            barrier.wait(timeout=10)
            return self.customer.post(self.base + '/Booking/Create', data=fields, timeout=25, allow_redirects=False)

        with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
            stop_task, book_task = pool.submit(stop_court), pool.submit(book)
            stopped, booked = stop_task.result(), book_task.result()
        self.assertEqual(302, stopped.status_code)
        self.assertIn(booked.status_code, (200, 302))
        self.assertEqual([{'Status': 2, 'IsDeleted': False}],
                         self.rows(f'SELECT Status, IsDeleted FROM Courts WHERE Id={court_id}'))
        rows = self.rows(f'SELECT Id, Status, TotalAmount FROM Bookings WHERE CourtId={court_id}')
        self.assertEqual(int(booked.status_code == 302), len(rows))
        if rows:
            self.assertEqual((0, fields['TotalAmount']), (rows[0]['Status'], rows[0]['TotalAmount']))
            self.assertEqual(200, self.get(self.customer, f"/Booking/Detail/{rows[0]['Id']}").status_code)
        self.assertEqual(200, self.customer.post(self.base + '/Booking/Create', data=fields,
                                              timeout=25, allow_redirects=False).status_code)
        self.assertEqual(rows, self.rows(f'SELECT Id, Status, TotalAmount FROM Bookings WHERE CourtId={court_id}'))

    def test_owner_time_slots_and_prices(self):
        owner_id = self.rows("SELECT Id FROM Users WHERE Email='owner1@sportbooking.vn'")[0]['Id']
        self.assertEqual(302, self.post(self.owner, '/Owner/Complexes/Create', {
            'Name': 'Slot test complex', 'Address': 'Test', 'Province': 'Test', 'District': 'Test',
            'OpeningTime': '06:00', 'ClosingTime': '23:00', 'IsActive': 'true'}).status_code)
        complex_id = self.rows("SELECT Id FROM SportComplexes WHERE Name=N'Slot test complex'")[0]['Id']
        type_id = self.rows('SELECT TOP 1 Id FROM SportTypes WHERE IsActive=1')[0]['Id']
        self.assertEqual(302, self.post(self.owner, f'/Owner/Courts/Create?complexId={complex_id}', {
            'Name': 'Slot test court', 'SportTypeId': type_id, 'Status': 'Active'}).status_code)
        court_id = self.rows("SELECT Id FROM Courts WHERE Name=N'Slot test court'")[0]['Id']
        path = '/Owner/TimeSlots'
        listing = path + f'?courtId={court_id}'
        create = path + f'/Create?courtId={court_id}'
        fields = {'StartTime': '08:00', 'EndTime': '09:00', 'PriceWeekday': '200000', 'PriceWeekend': '250000', 'IsActive': 'true'}
        for client in (self.customer, self.admin, self.session()):
            self.assertIn(client.get(self.base + listing, allow_redirects=False, timeout=20).status_code, (302, 403))
        self.assertEqual(400, self.owner.post(self.base + create, data=fields, timeout=20).status_code)
        other = self.login('owner2@sportbooking.vn')
        self.assertEqual(404, self.get(other, listing).status_code)
        self.assertEqual(404, self.post(other, create, fields, source='/Owner/Complexes').status_code)
        for invalid in ({'StartTime': '23:00', 'EndTime': '01:00'}, {'StartTime': '23:00', 'EndTime': '00:00'},
                        {'EndTime': '08:00'}, {'EndTime': '24:00'}, {'StartTime': '08:00:01'},
                        {'StartTime': '05:00'}, {'PriceWeekday': '0'}, {'PriceWeekend': '1.5'}):
            self.assertEqual(200, self.post(self.owner, create, {**fields, **invalid}).status_code, invalid)
        self.assertEqual(302, self.post(self.owner, create, fields).status_code)
        first = self.rows(f'SELECT Id,DurationMinutes FROM TimeSlots WHERE CourtId={court_id}')[0]
        self.assertEqual(60, first['DurationMinutes'])
        form = self.get(self.owner, path + f'/Edit/{first["Id"]}?courtId={court_id}')
        form_fields = {x['name']: x.get('value', '') for x in Inputs(form.text).items if x.get('name') and x.get('type') != 'checkbox'}
        self.assertEqual('200000', form_fields['PriceWeekday'])
        self.assertEqual('250000', form_fields['PriceWeekend'])
        self.assertEqual(302, self.post(self.owner, path + f'/Edit/{first["Id"]}?courtId={court_id}', {**form_fields, 'IsActive': 'true'}).status_code)
        self.assertEqual(200, self.post(self.owner, create, {**fields, 'StartTime': '08:30', 'EndTime': '09:30'}).status_code)
        self.assertEqual(302, self.post(self.owner, create, {**fields, 'StartTime': '09:00', 'EndTime': '10:00'}).status_code)
        self.assertEqual(302, self.post(self.owner, create, {**fields, 'StartTime': '11:00', 'EndTime': '12:00'}).status_code)
        ids = [x['Id'] for x in self.rows(f'SELECT Id FROM TimeSlots WHERE CourtId={court_id} ORDER BY StartTime')]
        book = {'CourtId': court_id, 'BookingDate': self.target, 'SelectedTimeSlotIds': [ids[0], ids[2]], 'PaymentMethod': 'Counter', 'Notes': self.note}
        source = f'/Booking/Create?courtId={court_id}'
        self.assertEqual(200, self.post(self.customer, '/Booking/Create', book, source=source).status_code)
        created = self.post(self.customer, '/Booking/Create', {**book, 'SelectedTimeSlotIds': ids[:2]}, source=source)
        self.assertEqual(302, created.status_code)
        old = self.rows(f'SELECT Id, TotalAmount, StartDateTime, EndDateTime FROM Bookings WHERE CourtId={court_id} ORDER BY Id')
        edit = path + f'/Edit/{ids[0]}?courtId={court_id}'
        self.assertEqual(404, self.get(other, edit).status_code)
        self.assertEqual(404, self.post(other, edit, fields, source='/Owner/Complexes').status_code)
        self.assertEqual(200, self.post(self.owner, edit, {**fields, 'StartTime': '07:00', 'EndTime': '08:00'}).status_code)
        stale_times = self.slot_quotes(self.customer, court_id, self.target)
        self.assertEqual(302, self.post(self.owner, edit, {**fields, 'PriceWeekday': '300000', 'PriceWeekend': '350000', 'IsActive': 'false'}).status_code)
        retired = self.post(self.customer, '/Booking/Create', {**book, 'BookingDate': vietnam_day(18),
            'SelectedTimeSlotIds': [ids[0]]}, source=source)
        self.assertEqual(200, retired.status_code)
        self.assertIn('không hợp lệ hoặc không thuộc sân', html.unescape(retired.text))
        self.assertEqual(old, self.rows(f'SELECT Id, TotalAmount, StartDateTime, EndDateTime FROM Bookings WHERE CourtId={court_id} ORDER BY Id'))
        self.assertEqual(200, self.get(self.customer, f'/Booking/Detail/{old[0]["Id"]}').status_code)
        # Retired windows with future bookings cannot be replaced by overlapping new slots.
        self.assertEqual(200, self.post(self.owner, create, {**fields, 'StartTime': '07:30', 'EndTime': '08:30'}).status_code)
        self.assertEqual(200, self.post(self.owner, f'/Owner/Complexes/Edit/{complex_id}', {
            'Name': 'Slot test complex', 'Address': 'Test', 'Province': 'Test', 'District': 'Test',
            'OpeningTime': '10:00', 'ClosingTime': '23:00', 'IsActive': 'true'}).status_code)
        second_edit = path + f'/Edit/{ids[1]}?courtId={court_id}'
        self.assertEqual(302, self.post(self.owner, second_edit, {**fields, 'StartTime': '09:00',
            'EndTime': '10:00', 'IsActive': 'false'}).status_code)
        blocked_hours = self.post(self.owner, f'/Owner/Complexes/Edit/{complex_id}', {
            'Name': 'Slot test complex', 'Address': 'Test', 'Province': 'Test', 'District': 'Test',
            'OpeningTime': '10:00', 'ClosingTime': '23:00', 'IsActive': 'true'})
        self.assertEqual(200, blocked_hours.status_code)
        self.assertIn('booking chưa kết thúc', html.unescape(blocked_hours.text))
        self.assertEqual('06:00', self.rows(f'SELECT CONVERT(varchar(5), OpeningTime, 108) AS h FROM SportComplexes WHERE Id={complex_id}')[0]['h'])
        third_edit = path + f'/Edit/{ids[2]}?courtId={court_id}'
        old_quote = self.quote_total([ids[2]], self.target)
        self.assertEqual(302, self.post(self.owner, third_edit, {**fields, 'StartTime': '11:00', 'EndTime': '12:00', 'PriceWeekday': '400000', 'PriceWeekend': '450000'}).status_code)
        self.assertEqual(200, self.post(self.customer, '/Booking/Create', {**book, 'SelectedTimeSlotIds': [ids[2]], 'TotalAmount': old_quote}, source=source).status_code)
        self.assertEqual(302, self.post(self.customer, '/Booking/Create', {**book, 'SelectedTimeSlotIds': [ids[2]]}, source=source).status_code)
        # Once reservations no longer hold the window, changing hours preserves historical snapshots.
        self.sql(f'UPDATE Bookings SET Status=3 WHERE CourtId={court_id}')
        self.assertEqual(302, self.post(self.owner, edit, {**fields, 'StartTime': '07:00', 'EndTime': '08:00'}).status_code)
        self.assertEqual(200, self.post(self.customer, '/Booking/Create', {**book, 'SelectedTimeSlotIds': [ids[0]], **stale_times}, source=source).status_code)
        for client, page in ((self.customer, f'/Booking/Detail/{old[0]["Id"]}'),
                             (self.customer, created.headers['Location']),
                             (self.owner, '/Owner/Bookings?Keyword=Slot+test+court')):
            response = self.get(client, page)
            self.assertEqual(200, response.status_code)
            display = html.unescape(response.text)
            for value in ('08:00', '09:00', f'{int(old[0]["TotalAmount"]):,}'.replace(',', '.')):
                self.assertIn(value, display)
        self.assertEqual(old, self.rows(f'SELECT Id, TotalAmount, StartDateTime, EndDateTime FROM Bookings WHERE CourtId={court_id} AND Id IN ({old[0]["Id"]},{old[1]["Id"]}) ORDER BY Id'))
        # Booking creation and an Owner time edit must observe one serialized schedule.
        race_date = self.rows(f"SELECT CONVERT(varchar(10),DATEADD(day,-1,'{self.target}'),23) AS d")[0]['d']
        barrier = threading.Barrier(2)
        def race_booking():
            barrier.wait(timeout=10)
            return self.post(self.customer, '/Booking/Create', {**book, 'BookingDate': race_date, 'SelectedTimeSlotIds': [ids[0]]}, source=source).status_code
        def race_edit():
            barrier.wait(timeout=10)
            return self.post(self.owner, edit, {**fields, 'StartTime': '06:00', 'EndTime': '07:00'}).status_code
        with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
            a, b = pool.submit(race_booking), pool.submit(race_edit)
            booking_status, edit_status = a.result(), b.result()
        self.assertIn(booking_status, (200, 302))
        if booking_status == 200:
            self.assertEqual(302, edit_status)
            self.assertEqual(302, self.post(self.customer, '/Booking/Create', {**book, 'BookingDate': race_date, 'SelectedTimeSlotIds': [ids[0]]}, source=source).status_code)
        slot = self.rows(f'SELECT CONVERT(varchar(5),StartTime,108) AS h FROM TimeSlots WHERE Id={ids[0]}')[0]['h']
        reserved = self.rows(f"SELECT CONVERT(varchar(5),StartDateTime,108) AS h FROM Bookings WHERE CourtId={court_id} AND BookingDate='{race_date}'")[0]['h']
        self.assertEqual(slot, reserved)
        self.assertIn(edit_status, (200, 302))

    def test_owner_courts(self):
        path = '/Owner/Courts'
        owner = self.rows("SELECT Id, ApprovalStatus FROM Users WHERE Email='owner1@sportbooking.vn'")[0]
        own = self.rows(f"SELECT TOP 1 Id FROM SportComplexes WHERE OwnerId='{owner['Id']}'")[0]['Id']
        foreign = self.rows(f"SELECT TOP 1 c.Id, c.SportComplexId, c.Name FROM Courts c JOIN SportComplexes s ON s.Id=c.SportComplexId WHERE s.OwnerId<>'{owner['Id']}'")[0]
        type_id = self.rows('SELECT TOP 1 Id FROM SportTypes WHERE IsActive=1')[0]['Id']
        listing = path + f'?complexId={own}'
        create = path + f'/Create?complexId={own}'
        fields = {'Name': '  Regression child court  ', 'SportTypeId': type_id, 'Status': 'Active',
                  'Description': 'Test court', 'ImageUrl': '/uploads/courts/test.jpg',
                  'SportComplexId': foreign['SportComplexId'], 'IsDeleted': 'true'}
        self.assertIn('Quản lý sân', html.unescape(self.get(self.owner, '/Owner/Complexes').text))
        for client in (self.customer, self.admin, self.session()):
            self.assertIn(client.get(self.base + listing, timeout=20, allow_redirects=False).status_code, (302, 403))
        self.assertEqual(400, self.owner.post(self.base + create, data=fields, timeout=20).status_code)
        self.assertEqual(302, self.post(self.owner, create, fields).status_code)
        court = self.rows("SELECT Id, SportComplexId, IsDeleted, Name FROM Courts WHERE Name=N'Regression child court'")[0]
        ident = court['Id']
        self.assertEqual(own, court['SportComplexId'])
        self.assertFalse(court['IsDeleted'])
        edit = path + f'/Edit/{ident}?complexId={own}'
        self.assertEqual(200, self.get(self.owner, edit).status_code)
        for invalid in ({'Name': '   '}, {'SportTypeId': 999999}, {'Status': '99'}, {'ImageUrl': 'javascript:alert(1)'}, {'Name': 'x' * 151}):
            self.assertEqual(200, self.post(self.owner, edit, {**fields, **invalid}).status_code)
        self.assertEqual('Regression child court', self.rows(f'SELECT Name FROM Courts WHERE Id={ident}')[0]['Name'])
        for endpoint in (path + f'?complexId={foreign["SportComplexId"]}',
                         path + f'/Create?complexId={foreign["SportComplexId"]}',
                         path + f'/Edit/{foreign["Id"]}?complexId={own}'):
            self.assertEqual(404, self.get(self.owner, endpoint).status_code)
        denied = self.post(self.owner, path + f'/Edit/{foreign["Id"]}?complexId={own}', fields, source=edit)
        self.assertEqual(404, denied.status_code)
        self.assertEqual(404, self.post(self.owner, path + f'/Create?complexId={foreign["SportComplexId"]}', fields, source=create).status_code)
        self.assertEqual(foreign['Name'], self.rows(f'SELECT Name FROM Courts WHERE Id={foreign["Id"]}')[0]['Name'])
        for status in ('Maintenance', 'Inactive', 'Active'):
            self.assertEqual(302, self.post(self.owner, edit, {**fields, 'Status': status}).status_code)
            filtered = self.get(self.owner, listing + f'&keyword=Regression&status={status}&page=999')
            self.assertEqual(200, filtered.status_code)
            self.assertIn('Regression child court', filtered.text)
        self.sql(f'UPDATE SportTypes SET IsActive=0 WHERE Id={type_id}')
        try:
            self.assertEqual(200, self.post(self.owner, create, fields).status_code)
            self.assertEqual(302, self.post(self.owner, edit, {**fields, 'Description': 'Keep current inactive type'}).status_code)
        finally:
            self.sql(f'UPDATE SportTypes SET IsActive=1 WHERE Id={type_id}')
        self.sql(f'UPDATE Courts SET IsDeleted=1 WHERE Id={ident}')
        self.assertEqual(404, self.get(self.owner, edit).status_code)
        self.assertEqual(404, self.post(self.owner, edit, fields, source=create).status_code)
        self.assertNotIn('Regression child court', self.get(self.owner, listing).text)
        self.sql(f"UPDATE Users SET ApprovalStatus=1 WHERE Id='{owner['Id']}'")
        try:
            self.assertIn(self.owner.get(self.base + listing, timeout=20, allow_redirects=False).status_code, (302, 403))
        finally:
            self.sql(f"UPDATE Users SET ApprovalStatus={owner['ApprovalStatus']} WHERE Id='{owner['Id']}'")

    def test_owner_complex_management(self):
        path = '/Owner/Complexes'
        owner_id = self.rows("SELECT Id FROM Users WHERE Email='owner1@sportbooking.vn'")[0]['Id']
        foreign = self.rows(f"SELECT TOP 1 Id, Name FROM SportComplexes WHERE OwnerId <> '{owner_id}'")[0]
        for client in (self.customer, self.admin, self.session()):
            self.assertIn(client.get(self.base + path, allow_redirects=False, timeout=20).status_code, (302, 403))
        self.assertEqual(400, self.owner.post(self.base + path + '/Create', data={'Name': 'Missing token'}, timeout=20).status_code)
        fields = {'Name': '  Regression complex  ', 'Address': '123 Test Street', 'Province': 'Test Province',
                  'District': 'Test District', 'OpeningTime': '06:00', 'ClosingTime': '23:00',
                  'PhoneNumber': '0901234567', 'Email': 'owner@example.test', 'IsActive': 'true',
                  'ImageUrl': 'https://example.test/court.jpg', 'OwnerId': 'forged-owner'}
        self.assertEqual(302, self.post(self.owner, path + '/Create', fields).status_code)
        item = self.rows("SELECT Id, OwnerId, Name FROM SportComplexes WHERE Name=N'Regression complex'")[0]
        self.assertEqual(owner_id, item['OwnerId'])
        ident = item['Id']
        edit = path + f'/Edit/{ident}'
        response = self.get(self.owner, edit)
        self.assertEqual(200, response.status_code)
        self.assertIn('value="06:00"', response.text)
        for invalid in ({'Name': '   '}, {'Address': '  '}, {'Province': '  '},
                        {'Name': 'x' * 201}, {'Address': 'x' * 301}, {'Email': 'bad-email'},
                        {'ImageUrl': 'javascript:alert(1)'}, {'OpeningTime': '23:00', 'ClosingTime': '06:00'},
                        {'ClosingTime': ''}, {'Province': ''}, {'OpeningTime': '06:00:30'}):
            response = self.post(self.owner, edit, {**fields, **invalid})
            self.assertEqual(200, response.status_code, invalid)
            self.assertIn('Test District', html.unescape(response.text))
        self.assertEqual('Regression complex', self.rows(f'SELECT Name FROM SportComplexes WHERE Id={ident}')[0]['Name'])
        listing = html.unescape(self.get(self.owner, path).text)
        self.assertIn('Regression complex', listing)
        self.assertNotIn(f'/Owner/Complexes/Edit/{foreign["Id"]}"', listing)
        self.assertEqual(404, self.get(self.owner, path + f'/Edit/{foreign["Id"]}').status_code)
        denied = self.post(self.owner, path + f'/Edit/{foreign["Id"]}', fields, source=edit)
        self.assertEqual(404, denied.status_code)
        self.assertEqual(404, self.post(self.owner, path + f'/Edit/{foreign["Id"]}',
            {**fields, 'OwnerId': owner_id}, source=edit).status_code)
        self.assertEqual(400, self.owner.post(self.base + edit, data=fields, timeout=20).status_code)
        self.assertEqual(foreign['Name'], self.rows(f'SELECT Name FROM SportComplexes WHERE Id={foreign["Id"]}')[0]['Name'])
        self.assertEqual(302, self.post(self.owner, edit, {**fields, 'Name': 'Updated complex', 'IsActive': 'false'}).status_code)
        item = self.rows(f'SELECT Name, IsActive, OwnerId, UpdatedAt FROM SportComplexes WHERE Id={ident}')[0]
        self.assertEqual('Updated complex', item['Name'])
        self.assertFalse(item['IsActive'])
        self.assertEqual(owner_id, item['OwnerId'])
        self.assertTrue(item['UpdatedAt'])
        self.assertIn('Updated complex', html.unescape(self.get(self.owner, path + '?keyword=Updated&isActive=false&page=999').text))
        self.assertNotIn('Updated complex', html.unescape(self.get(self.owner, path + '?isActive=true').text))
        self.assertIn('owner@example.test', html.unescape(self.get(self.owner, path).text))
        other = self.login('owner2@sportbooking.vn')
        self.assertNotIn('Updated complex', html.unescape(self.get(other, path).text))
        self.assertEqual(404, self.get(other, edit).status_code)
        self.assertEqual(404, self.post(other, edit, fields, source=path).status_code)
        self.assertIn('Updated complex', html.unescape(self.get(self.owner, path + '?keyword=123%20Test').text))
        for index in range(11):
            self.assertEqual(302, self.post(self.owner, path + '/Create', {**fields,
                'Name': f'Paged complex {index:02d}'}).status_code)
        first_page = html.unescape(self.get(self.owner, path + '?keyword=Paged%20complex&page=1').text)
        second_page = html.unescape(self.get(self.owner, path + '?keyword=Paged%20complex&page=2').text)
        self.assertIn('Paged complex 10', first_page)
        self.assertNotIn('Paged complex 00', first_page)
        self.assertIn('Paged complex 00', second_page)
        self.assertIn('Quản lý sân', first_page)
        valid_token = token(self.get(self.owner, path + '/Create'))
        original = self.rows(f"SELECT ApprovalStatus FROM Users WHERE Id='{owner_id}'")[0]['ApprovalStatus']
        self.sql(f"UPDATE Users SET ApprovalStatus=1 WHERE Id='{owner_id}'")
        try:
            for endpoint in (path, path + '/Create', edit):
                self.assertIn(self.owner.get(self.base + endpoint, allow_redirects=False, timeout=20).status_code, (302, 403))
            self.assertIn(self.owner.post(self.base + path + '/Create',
                data={**fields, '__RequestVerificationToken': valid_token},
                allow_redirects=False, timeout=20).status_code, (302, 403))
        finally:
            self.sql(f"UPDATE Users SET ApprovalStatus={original} WHERE Id='{owner_id}'")

    def test_owner_complex_concurrent_pause_and_booking(self):
        complex_id = self.rows('SELECT SportComplexId FROM Courts WHERE Id=1')[0]['SportComplexId']
        complex_row = self.rows(f'SELECT Name, Address, Province, District, OpeningTime, ClosingTime FROM SportComplexes WHERE Id={complex_id}')[0]
        edit = f'/Owner/Complexes/Edit/{complex_id}'
        fields = {key: complex_row[key] for key in ('Name', 'Address', 'Province', 'District')}
        for key in ('OpeningTime', 'ClosingTime'):
            if key in complex_row:
                fields[key] = complex_row[key][:5]
        booking = {'CourtId': 1, 'BookingDate': self.target, 'SelectedTimeSlotIds': [1],
                   'PaymentMethod': 'Counter', 'TotalAmount': self.quote_total([1], self.target)}
        booking.update(self.slot_quotes(self.customer, 1, self.target))
        booking['__RequestVerificationToken'] = token(self.get(self.customer, '/Booking/Create?courtId=1'))
        fields['IsActive'] = 'false'
        fields['__RequestVerificationToken'] = token(self.get(self.owner, edit))
        barrier = threading.Barrier(2)
        def pause():
            barrier.wait(timeout=10)
            return self.owner.post(self.base + edit, data=fields, timeout=25, allow_redirects=False).status_code
        def book():
            barrier.wait(timeout=10)
            return self.customer.post(self.base + '/Booking/Create', data=booking, timeout=25,
                                      allow_redirects=False).status_code
        with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
            a, b = pool.submit(pause), pool.submit(book)
            paused, booked = a.result(), b.result()
        self.assertEqual(302, paused)
        self.assertIn(booked, (200, 302))
        self.assertFalse(self.rows(f'SELECT IsActive FROM SportComplexes WHERE Id={complex_id}')[0]['IsActive'])
        existing = self.rows(f"SELECT Id, TotalAmount, StartDateTime FROM Bookings WHERE CourtId=1 AND BookingDate='{self.target}'")
        self.assertEqual(1 if booked == 302 else 0, len(existing))
        if existing:
            self.assertEqual(200, self.get(self.customer, f'/Booking/Detail/{existing[0]["Id"]}').status_code)
            code = self.rows(f'SELECT BookingCode FROM Bookings WHERE Id={existing[0]["Id"]}')[0]['BookingCode']
            self.assertIn(code, html.unescape(self.get(self.owner, '/Owner/Bookings').text))
        self.assertEqual(200, self.post(self.customer, '/Booking/Create', {
            'CourtId': 1, 'BookingDate': self.target, 'SelectedTimeSlotIds': [2],
            'PaymentMethod': 'Counter'}, source='/Booking/MyBookings').status_code)
        self.assertEqual(existing, self.rows(f"SELECT Id, TotalAmount, StartDateTime FROM Bookings WHERE CourtId=1 AND BookingDate='{self.target}'"))
        fields['IsActive'] = 'true'
        self.assertEqual(302, self.post(self.owner, edit, fields).status_code)
        self.assertEqual(302, self.post(self.customer, '/Booking/Create', {
            'CourtId': 1, 'BookingDate': self.target, 'SelectedTimeSlotIds': [2],
            'PaymentMethod': 'Counter'}, source='/Booking/Create?courtId=1').status_code)
        # A concurrent change that would exclude active slots must fail, while the booking uses the old hours.
        hours_fields = {**fields, 'OpeningTime': '23:30', 'ClosingTime': '23:59', 'IsActive': 'true'}
        hours_fields['__RequestVerificationToken'] = token(self.get(self.owner, edit))
        third = {'CourtId': 1, 'BookingDate': self.target, 'SelectedTimeSlotIds': [3],
                 'PaymentMethod': 'Counter', 'TotalAmount': self.quote_total([3], self.target)}
        third.update(self.slot_quotes(self.customer, 1, self.target))
        third['__RequestVerificationToken'] = token(self.get(self.customer, '/Booking/Create?courtId=1'))
        barrier = threading.Barrier(2)
        def change_hours():
            barrier.wait(timeout=10)
            return self.owner.post(self.base + edit, data=hours_fields, timeout=25, allow_redirects=False)
        def book_third():
            barrier.wait(timeout=10)
            return self.customer.post(self.base + '/Booking/Create', data=third, timeout=25, allow_redirects=False)
        with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
            a, b = pool.submit(change_hours), pool.submit(book_third)
            hours_result, third_result = a.result(), b.result()
        self.assertEqual(200, hours_result.status_code)
        self.assertIn('khung đang bán', html.unescape(hours_result.text))
        self.assertEqual(302, third_result.status_code)
        group = self.rows(f"SELECT TOP 1 Id, BookingGroupCode, TotalAmount FROM Bookings WHERE CourtId=1 AND TimeSlotId=2 AND BookingDate='{self.target}'")[0]
        self.assertEqual(302, self.post(self.owner, edit, {**fields, 'IsActive': 'false'}).status_code)
        self.assertEqual(302, self.post(self.owner, '/Owner/Bookings/ConfirmCounterPayment',
            {'bookingGroupCode': group['BookingGroupCode']}, source='/Owner/Bookings').status_code)
        confirmed = self.rows(f'SELECT Status, PaymentStatus, TotalAmount FROM Bookings WHERE Id={group["Id"]}')[0]
        self.assertEqual(1, confirmed['Status'])
        self.assertEqual(1, confirmed['PaymentStatus'])
        self.assertEqual(group['TotalAmount'], confirmed['TotalAmount'])
        self.assertEqual(302, self.post(self.owner, edit, {**fields, 'IsActive': 'true'}).status_code)

    def make_deletable_court(self, suffix):
        prefix = f'Soft delete {suffix} {uuid.uuid4().hex[:6]}'
        self.assertEqual(302, self.post(self.owner, '/Owner/Complexes/Create', {
            'Name': prefix, 'Address': '1 Test Street', 'Province': 'Test', 'District': 'Test',
            'IsActive': 'true'}).status_code)
        complex_id = self.rows(f"SELECT Id FROM SportComplexes WHERE Name='{prefix}'")[0]['Id']
        type_id = self.rows('SELECT TOP 1 Id FROM SportTypes WHERE IsActive=1')[0]['Id']
        self.assertEqual(302, self.post(self.owner, f'/Owner/Courts/Create?complexId={complex_id}', {
            'Name': prefix, 'SportTypeId': type_id, 'Status': 'Active'}).status_code)
        court_id = self.rows(f"SELECT Id FROM Courts WHERE Name='{prefix}'")[0]['Id']
        slot_path = f'/Owner/TimeSlots/Create?courtId={court_id}'
        self.assertEqual(302, self.post(self.owner, slot_path, {'StartTime': '16:00', 'EndTime': '17:00',
            'PriceWeekday': '200000', 'PriceWeekend': '250000', 'IsActive': 'true'}).status_code)
        slot_id = self.rows(f'SELECT Id FROM TimeSlots WHERE CourtId={court_id}')[0]['Id']
        return complex_id, court_id, slot_id, prefix

    def test_z_owner_soft_delete_keeps_booking_history(self):
        complex_id, court_id, slot_id, name = self.make_deletable_court('history')
        other_owner = self.login('owner2@sportbooking.vn')
        other_customer = self.login('customer2@sportbooking.vn')
        path = f'/Owner/Courts/Delete/{court_id}?complexId={complex_id}'
        listing = f'/Owner/Courts?complexId={complex_id}'
        date = vietnam_day(5)
        self.assertIn('Xóa sân', html.unescape(self.get(self.owner, listing).text))
        self.assertEqual(400, self.owner.post(self.base + path, timeout=20, allow_redirects=False).status_code)
        self.assertEqual(404, self.post(other_owner, path, {}, source='/Owner/Complexes').status_code)
        self.assertEqual(404, self.post(self.owner, f'/Owner/Courts/Delete/{court_id}?complexId=999999', {}, source=listing).status_code)
        self.assertEqual(404, self.post(self.owner, f'/Owner/Courts/Delete/999999?complexId={complex_id}', {}, source=listing).status_code)
        owner_id = self.rows("SELECT Id FROM Users WHERE Email='owner1@sportbooking.vn'")[0]['Id']
        delete_token = token(self.get(self.owner, listing))
        for change in ('ApprovalStatus=0', 'IsActive=0'):
            original = self.rows(f'SELECT ApprovalStatus, IsActive FROM Users WHERE Id=\'{owner_id}\'')[0]
            self.sql(f"UPDATE Users SET {change} WHERE Id='{owner_id}'")
            try:
                denied = self.owner.post(self.base + path,
                    data={'__RequestVerificationToken': delete_token}, timeout=20, allow_redirects=False)
                self.assertIn(denied.status_code, (302, 403))
                self.assertFalse(self.rows(f'SELECT IsDeleted FROM Courts WHERE Id={court_id}')[0]['IsDeleted'])
            finally:
                self.sql(f"UPDATE Users SET ApprovalStatus={original['ApprovalStatus']}, "
                         f"IsActive={int(original['IsActive'])} WHERE Id='{owner_id}'")
        booking_path = f'/Booking/Create?courtId={court_id}'
        booking = self.post(self.customer, '/Booking/Create', {'CourtId': court_id, 'BookingDate': date,
            'SelectedTimeSlotIds': [slot_id], 'PaymentMethod': 'Counter'}, source=booking_path)
        self.assertEqual(302, booking.status_code)
        snapshot = self.rows(f'SELECT Id, BookingCode, BookingGroupCode, StartDateTime, EndDateTime, TotalAmount FROM Bookings WHERE CourtId={court_id}')[0]
        blocked = self.post(self.owner, path, {}, source=listing)
        self.assertEqual(302, blocked.status_code)
        self.assertIn('booking chờ', html.unescape(self.get(self.owner, listing).text))
        self.assertFalse(self.rows(f'SELECT IsDeleted FROM Courts WHERE Id={court_id}')[0]['IsDeleted'])
        # Completed bookings are historical; they no longer block retirement.
        self.sql(f'UPDATE Bookings SET Status=2, PaymentStatus=1 WHERE Id={snapshot["Id"]}')
        self.assertEqual(302, self.post(self.owner, path, {}, source=listing).status_code)
        self.assertTrue(self.rows(f'SELECT IsDeleted FROM Courts WHERE Id={court_id}')[0]['IsDeleted'])
        self.assertEqual(302, self.post(self.owner, path, {}, source=listing).status_code)
        self.assertEqual(1, len(self.rows(f'SELECT Id FROM Courts WHERE Id={court_id}')))
        self.assertNotIn(f'Sửa {name}', html.unescape(self.get(self.owner, listing).text))
        self.assertNotIn(name, html.unescape(self.get(self.customer, '/Court/Search').text))
        self.assertEqual(302, self.get(self.customer, booking_path).status_code)
        self.assertEqual(200, self.post(self.customer, '/Booking/Create', {'CourtId': court_id,
            'BookingDate': date, 'SelectedTimeSlotIds': [slot_id], 'PaymentMethod': 'Counter'},
            source='/Booking/MyBookings').status_code)
        self.assertIn(snapshot['BookingCode'], html.unescape(self.get(self.customer,
            f'/Booking/MyBookings?Keyword={snapshot["BookingCode"]}').text))
        self.assertIn(snapshot['BookingCode'], html.unescape(self.get(self.owner,
            f'/Owner/Bookings?Keyword={snapshot["BookingCode"]}').text))
        self.assertIn(snapshot['BookingCode'], html.unescape(self.get(self.customer,
            f'/Booking/Confirmation?groupCode={snapshot["BookingGroupCode"]}').text))
        detail = html.unescape(self.get(self.customer, f'/Booking/Detail/{snapshot["Id"]}').text)
        self.assertIn(str(snapshot['StartDateTime'])[11:16], detail)
        self.assertIn(str(snapshot['EndDateTime'])[11:16], detail)
        self.assertIn(f"{int(snapshot['TotalAmount']):,}".replace(',', '.'), detail)
        self.assertNotEqual(200, self.get(other_customer, f'/Booking/Detail/{snapshot["Id"]}').status_code)
        self.assertNotIn(snapshot['BookingCode'], html.unescape(self.get(other_customer, '/Booking/MyBookings').text))
        review = self.get(self.customer, f'/Booking/Review/{snapshot["Id"]}')
        self.assertEqual(200, review.status_code)
        self.assertEqual(302, self.post(self.customer, '/Booking/Review', {'BookingId': snapshot['Id'],
            'Rating': '5', 'Comment': 'Sân tốt sau khi đóng cửa'}, source=f'/Booking/Review/{snapshot["Id"]}').status_code)
        self.assertTrue(self.rows(f'SELECT Id FROM Reviews WHERE BookingId={snapshot["Id"]}'))
        self.assertEqual(snapshot, self.rows(f'SELECT Id, BookingCode, BookingGroupCode, StartDateTime, EndDateTime, TotalAmount FROM Bookings WHERE Id={snapshot["Id"]}')[0])

    def test_z_owner_delete_races_booking_creation(self):
        complex_id, court_id, slot_id, _ = self.make_deletable_court('race')
        path = f'/Owner/Courts/Delete/{court_id}?complexId={complex_id}'
        booking_path = f'/Booking/Create?courtId={court_id}'
        date = vietnam_day(6)
        delete_token = token(self.get(self.owner, f'/Owner/Courts?complexId={complex_id}'))
        booking_token = token(self.get(self.customer, booking_path))
        fields = {'CourtId': court_id, 'BookingDate': date, 'SelectedTimeSlotIds': [slot_id],
                  'PaymentMethod': 'Counter', 'TotalAmount': self.quote_total([slot_id], date),
                  '__RequestVerificationToken': booking_token}
        fields.update(self.slot_quotes(self.customer, court_id, date))
        barrier = threading.Barrier(2)

        def delete():
            barrier.wait(timeout=10)
            return self.owner.post(self.base + path, data={'__RequestVerificationToken': delete_token},
                                   timeout=25, allow_redirects=False)

        def book():
            barrier.wait(timeout=10)
            return self.customer.post(self.base + '/Booking/Create', data=fields,
                                      timeout=25, allow_redirects=False)

        with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
            a, b = pool.submit(delete), pool.submit(book)
            deleted, booked = a.result(), b.result()
        self.assertEqual(302, deleted.status_code)
        self.assertIn(booked.status_code, (200, 302))
        booking = self.rows(f"SELECT Id, BookingCode FROM Bookings WHERE CourtId={court_id} AND BookingDate='{date}'")
        is_deleted = self.rows(f'SELECT IsDeleted FROM Courts WHERE Id={court_id}')[0]['IsDeleted']
        self.assertEqual(0 if is_deleted else 1, len(booking))
        if booking:
            self.assertIn(booking[0]['BookingCode'], html.unescape(self.get(self.customer,
                f'/Booking/MyBookings?Keyword={booking[0]["BookingCode"]}').text))
            self.assertIn(booking[0]['BookingCode'], html.unescape(self.get(self.owner,
                f'/Owner/Bookings?Keyword={booking[0]["BookingCode"]}').text))
            self.assertEqual(200, self.get(self.customer, f'/Booking/Detail/{booking[0]["Id"]}').status_code)

    def test_z_deleted_court_keeps_refund_workflow(self):
        complex_id, court_id, slot_id, _ = self.make_deletable_court('refund')
        path = f'/Owner/Courts/Delete/{court_id}?complexId={complex_id}'
        listing = f'/Owner/Courts?complexId={complex_id}'
        date = vietnam_day(8)
        self.assertEqual(302, self.post(self.customer, '/Booking/Create', {
            'CourtId': court_id, 'BookingDate': date, 'SelectedTimeSlotIds': [slot_id],
            'PaymentMethod': 'Counter'}, source=f'/Booking/Create?courtId={court_id}').status_code)
        booking = self.rows(f'SELECT Id, BookingGroupCode, TotalAmount FROM Bookings WHERE CourtId={court_id}')[0]
        self.assertEqual(302, self.post(self.owner, '/Owner/Bookings/ConfirmCounterPayment',
            {'bookingGroupCode': booking['BookingGroupCode']}, source='/Owner/Bookings').status_code)
        self.assertEqual(302, self.post(self.owner, path, {}, source=listing).status_code)
        self.assertFalse(self.rows(f'SELECT IsDeleted FROM Courts WHERE Id={court_id}')[0]['IsDeleted'])
        self.assertEqual(302, self.post(self.customer, '/Booking/Cancel', {
            'BookingId': booking['Id'], 'CancellationReason': 'Thay doi lich thi dau, xin hoan tien'},
            source=f'/Booking/Cancel/{booking["Id"]}').status_code)
        self.assertEqual(302, self.post(self.owner, path, {}, source=listing).status_code)
        self.assertTrue(self.rows(f'SELECT IsDeleted FROM Courts WHERE Id={court_id}')[0]['IsDeleted'])
        other_owner = self.login('owner2@sportbooking.vn')
        self.assertEqual(302, self.post(other_owner, '/Owner/Bookings/ConfirmCounterRefund',
            {'bookingId': booking['Id']}, source='/Owner/Bookings').status_code)
        self.assertEqual(4, self.rows(f'SELECT PaymentStatus FROM Bookings WHERE Id={booking["Id"]}')[0]['PaymentStatus'])
        self.assertEqual(302, self.post(self.owner, '/Owner/Bookings/ConfirmCounterRefund',
            {'bookingId': booking['Id']}, source='/Owner/Bookings').status_code)
        paid = self.rows(f'SELECT PaymentStatus, TotalAmount FROM Bookings WHERE Id={booking["Id"]}')[0]
        self.assertEqual((2, booking['TotalAmount']), (paid['PaymentStatus'], paid['TotalAmount']))
        self.assertEqual(200, self.get(self.customer, f'/Booking/Detail/{booking["Id"]}').status_code)

    def test_owner_dashboard_and_menu(self):
        path = '/Owner/Dashboard'
        response = self.get(self.owner, path)
        self.assertEqual(200, response.status_code)
        content = html.unescape(response.text)
        for link in ('/Owner', '/Owner/Bookings', '/Profile', '/Account/Logout'):
            self.assertIn('"' + link + '"', content)
        self.assertNotIn('/Owner/Profile', content)
        self.assertIn('Tổng quan chủ sân', content)
        self.assertIn('Lịch đặt hôm nay', content)
        self.assertIn('Menu chủ sân', html.unescape(self.get(self.owner, '/Owner/Bookings').text))
        for client in (self.customer, self.admin, self.session()):
            denied = client.get(self.base + path, allow_redirects=False, timeout=20)
            self.assertIn(denied.status_code, (302, 403))
        client = self.session()
        login = self.post(client, '/Account/Login', {'Email': 'owner1@sportbooking.vn', 'Password': 'Sport@123'})
        self.assertEqual('/Owner', login.headers['Location'])
        original_owner = self.rows("SELECT Id, ApprovalStatus FROM Users WHERE Email='owner1@sportbooking.vn'")[0]
        owner_id = original_owner['Id']
        self.sql(f"UPDATE Users SET ApprovalStatus=0 WHERE Id='{owner_id}'")
        try:
            denied = self.owner.get(self.base + path, allow_redirects=False, timeout=20)
            self.assertIn(denied.status_code, (302, 403))
        finally:
            self.sql(f"UPDATE Users SET ApprovalStatus={original_owner['ApprovalStatus']} WHERE Id='{owner_id}'")

    def test_admin_sport_types(self):
        path = '/Admin/SportTypeManagement'
        for client in (self.customer, self.owner, self.session()):
            response = client.get(self.base + path, allow_redirects=False, timeout=20)
            self.assertIn(response.status_code, (302, 403))
        self.assertEqual(400, self.admin.post(self.base + path + '/Create', data={'Name': 'No token'}, timeout=20).status_code)
        fields = {'Name': '  Regression sport  ', 'Description': 'Description',
                  'IconClass': 'bi-trophy-fill', 'DisplayOrder': '3', 'IsActive': 'true'}
        self.assertEqual(302, self.post(self.admin, path + '/Create', fields).status_code)
        item = self.rows("SELECT Id, Name FROM SportTypes WHERE Name=N'Regression sport'")[0]
        ident = item['Id']
        self.assertEqual('Regression sport', item['Name'])
        self.assertEqual(200, self.post(self.admin, path + '/Create', fields).status_code)
        self.assertEqual(1, len(self.rows("SELECT Id FROM SportTypes WHERE Name=N'Regression sport'")))
        self.assertEqual(200, self.post(self.admin, path + '/Create', {**fields, 'Name': '   '}).status_code)
        self.assertEqual(200, self.post(self.admin, path + '/Create', {**fields, 'Name': 'Invalid order', 'DisplayOrder': '-1'}).status_code)
        edit = path + f'/Edit/{ident}'
        self.assertEqual(302, self.post(self.admin, edit, {**fields, 'Name': 'Updated sport', 'IsActive': 'false'}).status_code)
        item = self.rows(f'SELECT Name, IsActive FROM SportTypes WHERE Id={ident}')[0]
        self.assertEqual('Updated sport', item['Name'])
        self.assertFalse(item['IsActive'])
        page = self.get(self.admin, path + '?keyword=Updated&isActive=false')
        self.assertEqual(200, page.status_code)
        self.assertIn('Updated sport', page.text)
        referenced = self.rows('SELECT TOP 1 SportTypeId FROM Courts')[0]['SportTypeId']
        self.assertEqual(302, self.post(self.admin, path + f'/Delete/{referenced}', {}, source=path).status_code)
        self.assertTrue(self.rows(f'SELECT Id FROM SportTypes WHERE Id={referenced}'))
        self.assertEqual(302, self.post(self.admin, path + f'/Delete/{ident}', {}, source=path).status_code)
        self.assertFalse(self.rows(f'SELECT Id FROM SportTypes WHERE Id={ident}'))

    def register(self):
        client = self.session()
        email = uuid.uuid4().hex + '@example.test'
        response = self.post(client, '/Account/Register', {
            'Email': email, 'Password': 'Audit@123', 'ConfirmPassword': 'Audit@123',
            'FullName': 'Regression Customer', 'PhoneNumber': '0901234567',
            'AccountType': 'Customer', 'AgreeToTerms': 'true'
        })
        self.assertEqual(302, response.status_code)
        uid = self.rows(f"SELECT Id FROM Users WHERE Email='{email}'")[0]['Id']
        return email, uid

    def create(self, slots=(1,), method='Counter', client=None, *, court=1):
        response = self.post(client or self.customer, '/Booking/Create', {
            'CourtId': court, 'BookingDate': self.target, 'SelectedTimeSlotIds': list(slots),
            'PaymentMethod': method, 'Notes': self.note
        }, source=f'/Booking/Create?courtId={court}')
        self.assertEqual(302, response.status_code, response.text[-500:])
        return response

    def bookings(self):
        return self.rows(f"SELECT Id,BookingGroupCode,Status,PaymentStatus,PaymentMethod,TotalAmount "
                         f"FROM Bookings WHERE Note='{self.note}' ORDER BY Id")

    def payments(self):
        return self.rows(f"SELECT p.Id, p.BookingId, p.Amount, p.Status, p.ResponseCode, p.GatewayTransactionId "
                         f"FROM Payments p JOIN Bookings b ON b.Id=p.BookingId WHERE b.Note='{self.note}' ORDER BY p.Id")

    def confirm(self, booking):
        response = self.post(self.owner, '/Owner/Bookings/ConfirmCounterPayment', {
            'bookingGroupCode': booking['BookingGroupCode']}, source='/Owner/Bookings')
        self.assertEqual(302, response.status_code)

    def create_legacy_vnpay(self, slots=(1,), *, court=1):
        # Reproduce transactions stored before checkout was removed. Only the isolated DB is changed.
        self.create(slots, court=court)
        reference = uuid.uuid4().hex
        for row in self.bookings():
            booking = row['Id']
            self.sql(f'UPDATE Bookings SET PaymentMethod=1, PaymentDeadline=DATEADD(minute,435,SYSUTCDATETIME()) '
                     f'WHERE Id={booking}; INSERT Payments (BookingId,TransactionCode,Method,Amount,Status,CreatedAt) '
                     f"SELECT Id,'{reference}-{booking}',1,TotalAmount,0,SYSUTCDATETIME() FROM Bookings WHERE Id={booking};")
        return {'vnp_TxnRef': reference, 'vnp_Amount': str(int(sum(p['Amount'] for p in self.payments()) * 100)),
                'vnp_OrderInfo': 'Thanh toan dat san lich su'}

    def callback(self, legacy, **changes):
        fields = {
            **legacy, 'vnp_TmnCode': MERCHANT, 'vnp_ResponseCode': '00',
            'vnp_TransactionStatus': '00', 'vnp_TransactionNo': '12345678', 'vnp_BankCode': 'NCB'
        }
        fields.update(changes)
        fields['vnp_SecureHash'] = sign(fields)
        return fields

    def ipn(self, fields):
        response = self.session().get(self.base + '/Payment/VnPayIpn', params=fields, timeout=25)
        self.assertEqual(200, response.status_code)
        return response.json()

    def test_01_admin_lock_unlock_invalidates_session(self):
        email, uid = self.register()
        existing = self.login(email, 'Audit@123')
        response = self.post(self.admin, '/Admin/UserManagement/SetActive', {'userId': uid, 'isActive': 'false'},
                             source='/Admin/UserManagement/Detail/' + uid)
        self.assertEqual(302, response.status_code)
        self.assertFalse(self.rows(f"SELECT IsActive FROM Users WHERE Id='{uid}'")[0]['IsActive'])
        self.assertEqual(302, self.get(existing, '/Profile').status_code)
        denied = self.post(self.session(), '/Account/Login', {'Email': email, 'Password': 'Audit@123'})
        self.assertEqual(200, denied.status_code)
        response = self.post(self.admin, '/Admin/UserManagement/SetActive', {'userId': uid, 'isActive': 'true'},
                             source='/Admin/UserManagement/Detail/' + uid)
        self.assertEqual(302, response.status_code)
        self.assertEqual(200, self.get(self.login(email, 'Audit@123'), '/Profile').status_code)

    def test_02_fifth_failure_message_and_admin_unlock(self):
        email, uid = self.register()
        client = self.session()
        for _ in range(5):
            response = self.post(client, '/Account/Login', {'Email': email, 'Password': 'Wrong@123'})
        self.assertNotIn('còn 5 lần thử', html.unescape(response.text))
        self.assertIn('đã bị khóa', html.unescape(response.text))
        self.post(self.admin, '/Admin/UserManagement/SetActive', {'userId': uid, 'isActive': 'true'},
                  source='/Admin/UserManagement/Detail/' + uid)
        self.assertEqual(200, self.get(self.login(email, 'Audit@123'), '/Profile').status_code)

    def test_03_actual_reset_email_link(self):
        email, _ = self.register()
        client = self.session()
        self.post(client, '/Account/ForgotPassword', {'Email': email})
        log = self.log_path.read_text(encoding='utf-8', errors='replace')
        links = re.findall(re.escape(self.base) + r'/Account/ResetPassword\?[^\s<>"\']+', log)
        self.assertTrue(links)
        page = client.get(html.unescape(links[-1]), timeout=20)
        fields = {x['name']: x.get('value', '') for x in Inputs(page.text).items if x.get('name') in ('Email', 'Token')}
        self.assertEqual(email, fields['Email'])
        response = client.post(self.base + '/Account/ResetPassword', data={
            **fields, 'NewPassword': 'Reset@123', 'ConfirmPassword': 'Reset@123', '__RequestVerificationToken': token(page)
        }, timeout=20, allow_redirects=False)
        self.assertEqual(302, response.status_code)
        self.login(email, 'Reset@123')
        replay = client.post(self.base + '/Account/ResetPassword', data={
            **fields, 'NewPassword': 'Replay@123', 'ConfirmPassword': 'Replay@123', '__RequestVerificationToken': token(page)
        }, timeout=20, allow_redirects=False)
        self.assertEqual(200, replay.status_code)

    def test_04_change_password_refreshes_current_session(self):
        email, _ = self.register()
        current = self.login(email, 'Audit@123')
        other = self.login(email, 'Audit@123')
        response = self.post(current, '/Account/ChangePassword', {
            'CurrentPassword': 'Audit@123', 'NewPassword': 'Changed@123', 'ConfirmPassword': 'Changed@123'})
        self.assertEqual(302, response.status_code)
        self.assertEqual(200, self.get(current, '/Profile').status_code)
        self.assertEqual(302, self.get(other, '/Profile').status_code)
        self.login(email, 'Changed@123')

    def test_05_search_matches_time_instead_of_slot_id(self):
        response = self.get(self.session(), f'/Court/Search?PageSize=48&BookingDate={self.target}&TimeSlotId=360')
        actual = {int(x) for x in re.findall(r'/Court/Detail/(\d+)', response.text)}
        expected = {x['Id'] for x in self.rows(
            "SELECT c.Id FROM Courts c JOIN SportComplexes sc ON sc.Id=c.SportComplexId "
            "JOIN Users u ON u.Id=sc.OwnerId JOIN SportTypes st ON st.Id=c.SportTypeId "
            "WHERE c.Status=0 AND c.IsDeleted=0 AND sc.IsActive=1 AND u.IsActive=1 AND st.IsActive=1 "
            "AND u.ApprovalStatus=2 AND EXISTS (SELECT 1 FROM TimeSlots t WHERE t.CourtId=c.Id "
            "AND t.StartTime=CAST('06:00' AS time) AND t.IsActive=1 AND NOT EXISTS "
            f"(SELECT 1 FROM Bookings b WHERE b.CourtId=c.Id AND b.TimeSlotId=t.Id AND b.BookingDate='{self.target}' "
            "AND b.Status IN (0,1,2)))")}
        self.assertTrue(expected)
        self.assertEqual(expected, actual)

    def concurrent_create(self, slots):
        barrier = threading.Barrier(len(slots))
        prepared = []
        for slot in slots:
            client = self.session()
            client.cookies.update(self.customer.cookies)
            prepared.append((client, token(self.get(client, '/Booking/Create?courtId=1')), slot, self.quote_total([slot], self.target), self.slot_quotes(client, 1, self.target)))
        def submit(item):
            client, csrf, slot, amount, quotes = item
            barrier.wait(timeout=10)
            return client.post(self.base + '/Booking/Create', data={
                'CourtId': 1, 'BookingDate': self.target, 'SelectedTimeSlotIds': [slot], 'PaymentMethod': 'Counter',
                'Notes': self.note, 'TotalAmount': amount, '__RequestVerificationToken': csrf, **quotes
            }, timeout=30, allow_redirects=False).status_code
        with concurrent.futures.ThreadPoolExecutor(max_workers=len(slots)) as executor:
            return list(executor.map(submit, prepared))

    def test_06_same_slot_has_one_winner(self):
        self.assertEqual(1, self.concurrent_create([1] * 6).count(302))
        self.assertEqual(1, len(self.bookings()))

    def test_07_different_slots_all_succeed(self):
        self.assertEqual([302] * 8, self.concurrent_create(list(range(1, 9))))
        codes = self.rows(f"SELECT BookingCode FROM Bookings WHERE Note='{self.note}'")
        self.assertEqual(8, len({x['BookingCode'] for x in codes}))
        self.assertTrue(all(len(x['BookingCode']) <= 30 for x in codes))

    def test_08_counter_refund_requires_confirmation_and_ownership(self):
        self.create()
        booking = self.bookings()[0]
        self.confirm(booking)
        response = self.post(self.customer, '/Booking/Cancel', {'BookingId': booking['Id'],
                             'CancellationReason': 'Khong the tham gia do thay doi lich'},
                             source=f"/Booking/Cancel/{booking['Id']}")
        self.assertEqual(302, response.status_code)
        self.assertEqual((3, 4), (self.bookings()[0]['Status'], self.bookings()[0]['PaymentStatus']))
        self.assertEqual(4, self.rows(f"SELECT Status FROM Payments WHERE BookingId={booking['Id']}")[0]['Status'])
        other = self.login('owner2@sportbooking.vn')
        self.post(other, '/Owner/Bookings/ConfirmCounterRefund', {'bookingId': booking['Id']}, source='/Owner/Bookings')
        self.assertEqual(4, self.bookings()[0]['PaymentStatus'])
        self.post(self.owner, '/Owner/Bookings/ConfirmCounterRefund', {'bookingId': booking['Id']}, source='/Owner/Bookings')
        self.assertEqual(2, self.bookings()[0]['PaymentStatus'])
        self.post(self.owner, '/Owner/Bookings/ConfirmCounterRefund', {'bookingId': booking['Id']}, source='/Owner/Bookings')
        self.assertEqual(1, len(self.rows(f"SELECT Id FROM Payments WHERE BookingId={booking['Id']}")))

    def test_09_cancellation_cutoff_unchanged(self):
        self.create()
        booking = self.bookings()[0]
        self.confirm(booking)
        self.sql(f"UPDATE Bookings SET StartDateTime=DATEADD(hour,8,SYSUTCDATETIME()) WHERE Id={booking['Id']}")
        response = self.post(self.customer, '/Booking/Cancel', {'BookingId': booking['Id'],
                             'CancellationReason': 'Khong the tham gia do thay doi lich'}, source='/Booking/MyBookings')
        self.assertEqual(200, response.status_code)
        self.assertEqual(1, self.bookings()[0]['Status'])

    def test_10_retired_vnpay_cannot_create_or_resume_payments(self):
        source = '/Booking/Create?courtId=1'
        page = self.get(self.customer, source)
        self.assertNotIn('Online qua VNPay', html.unescape(page.text))
        rejected = self.post(self.customer, '/Booking/Create', {
            'CourtId': 1, 'BookingDate': self.target, 'SelectedTimeSlotIds': [1],
            'PaymentMethod': 'Online', 'Notes': self.note,
        }, source=source)
        self.assertEqual(200, rejected.status_code)
        self.assertEqual(([], []), (self.bookings(), self.payments()))
        self.assertEqual(404, self.post(self.customer, '/Payment/Pay', {'groupCode': 'retired'},
                                       source='/Booking/MyBookings').status_code)

    def test_11_ipn_success_and_duplicate(self):
        response = self.create_legacy_vnpay((1, 2))
        fields = self.callback(response)
        before = self.payments()
        booking_before = self.bookings()
        self.assertEqual([0, 0], [p['Status'] for p in before])
        self.assertEqual([(0, 0), (0, 0)], [(b['Status'], b['PaymentStatus']) for b in booking_before])
        # The browser never calls Return in this test. IPN must finalize both payments by itself.
        self.assertEqual('00', self.ipn(fields)['RspCode'])
        after = self.payments()
        bookings = self.bookings()
        self.assertEqual([(b['Id'], b['TotalAmount'], 1, '00', '12345678') for b in bookings],
                         [(p['BookingId'], p['Amount'], p['Status'], p['ResponseCode'], p['GatewayTransactionId']) for p in after])
        self.assertTrue(all(x['Status'] == 1 and x['PaymentStatus'] == 1 for x in bookings))
        detail = html.unescape(self.get(self.customer, f"/Booking/Detail/{bookings[0]['Id']}").text)
        owner_list = html.unescape(self.get(self.owner, '/Owner/Bookings?keyword=' + bookings[0]['BookingGroupCode']).text)
        for page in (detail, owner_list):
            self.assertIn('Đã thanh toán', page)
        self.assertEqual('02', self.ipn(fields)['RspCode'])
        self.assertEqual(after, self.payments())
        if evidence := os.environ.get('SPORTBOOKING_VNPAY_TEST_EVIDENCE'):
            Path(evidence).write_text(json.dumps({
                'source': 'simulated IPN on disposable LocalDB; no Return request',
                'checkedAtUtc': datetime.now(timezone.utc).isoformat(),
                'before': {'bookings': booking_before, 'payments': before},
                'after': {'bookings': bookings, 'payments': after},
                'customerAndOwnerDisplayPaid': True,
                'duplicateRspCode': '02',
            }, indent=2) + '\n', encoding='utf-8')

    def test_12_ipn_bad_signature_does_not_change_bookings(self):
        response = self.create_legacy_vnpay()
        fields = self.callback(response)
        fields['vnp_SecureHash'] = '0' * 128
        self.assertEqual('97', self.ipn(fields)['RspCode'])
        self.assertEqual(0, self.bookings()[0]['PaymentStatus'])

    def test_13_ipn_wrong_amount_does_not_change_bookings(self):
        response = self.create_legacy_vnpay()
        self.assertEqual('04', self.ipn(self.callback(response, vnp_Amount='100'))['RspCode'])
        self.assertEqual(0, self.bookings()[0]['PaymentStatus'])

    def test_zzz_vnpay_signed_ipn_validates_required_fields_and_reference(self):
        self.target = vietnam_day(3)
        _, court, slot, _ = self.make_deletable_court('vnpay required fields')
        response = self.create_legacy_vnpay((slot,), court=court)
        before = self.bookings(), self.payments()
        for changes, code in (
            ({'vnp_TmnCode': 'OTHER123'}, '97'),
            ({'vnp_TxnRef': uuid.uuid4().hex}, '01'),
            ({'vnp_Amount': '0000000000001'}, '04'),
            ({'vnp_ResponseCode': 'AB'}, '99'),
            ({'vnp_TransactionStatus': 'AB'}, '99'),
            ({'vnp_TransactionNo': '0'}, '99'),
            ({'vnp_BankCode': ''}, '99'),
            ({'vnp_OrderInfo': ''}, '99'),
        ):
            with self.subTest(field=next(iter(changes))):
                self.assertEqual(code, self.ipn(self.callback(response, **changes))['RspCode'])
                self.assertEqual(before, (self.bookings(), self.payments()))


    def test_14_ipn_checks_transaction_status_and_releases_slots(self):
        response = self.create_legacy_vnpay()
        self.assertEqual('00', self.ipn(self.callback(response, vnp_TransactionStatus='02'))['RspCode'])
        self.assertEqual((4, 3), (self.bookings()[0]['Status'], self.bookings()[0]['PaymentStatus']))

    def test_zzz_vnpay_cancel_ipn_releases_schedule_and_keeps_failed_result(self):
        self.target = vietnam_day(3)
        _, court, slot, _ = self.make_deletable_court('vnpay cancel')
        response = self.create_legacy_vnpay((slot,), court=court)
        fields = self.callback(response, vnp_ResponseCode='24', vnp_TransactionStatus='02', vnp_TransactionNo='0')
        self.assertEqual('00', self.ipn(fields)['RspCode'])
        self.assertEqual((4, 3), (self.bookings()[0]['Status'], self.bookings()[0]['PaymentStatus']))
        self.assertEqual((3, '24'), (self.payments()[0]['Status'], self.payments()[0]['ResponseCode']))
        self.create((slot,), client=self.login('customer2@sportbooking.vn'), court=court)
        self.assertEqual([4, 0], [b['Status'] for b in self.bookings()])
        self.assertEqual('02', self.ipn(fields)['RspCode'])

    def test_15_late_ipn_preserves_rebooked_slot(self):
        response = self.create_legacy_vnpay((1, 2))
        original_ids = [x['Id'] for x in self.bookings()]
        self.sql(f"UPDATE Bookings SET PaymentDeadline=DATEADD(minute,419,SYSUTCDATETIME()) WHERE Note='{self.note}'")
        deadline = time.monotonic() + 12
        while time.monotonic() < deadline and any(x['Status'] != 4 for x in self.bookings()):
            time.sleep(.25)
        self.assertTrue(all(x['Status'] == 4 for x in self.bookings()))
        other = self.login('customer2@sportbooking.vn')
        self.create(client=other)
        self.assertEqual('00', self.ipn(self.callback(response))['RspCode'])
        states = self.rows('SELECT Id,Status,PaymentStatus FROM Bookings WHERE Id IN (' + ','.join(map(str, original_ids)) + ')')
        self.assertTrue(all(x['Status'] == 4 and x['PaymentStatus'] == 4 for x in states))
        fresh = [x for x in self.bookings() if x['Id'] not in original_ids]
        self.assertEqual(1, len(fresh))
        self.assertEqual(0, fresh[0]['Status'])
        payments = self.payments()
        self.assertEqual([(4, '00'), (4, '00')], [(p['Status'], p['ResponseCode']) for p in payments])
        self.assertEqual('02', self.ipn(self.callback(response))['RspCode'])
        self.assertEqual(payments, self.payments())

    def test_16_return_url_never_confirms_payment(self):
        response = self.create_legacy_vnpay()
        fields = self.callback(response)
        returned = self.customer.get(self.base + '/Payment/VnPayReturn', params=fields, timeout=20, allow_redirects=False)
        self.assertEqual(302, returned.status_code)
        self.assertIn('/Booking/Detail/', returned.headers['Location'])
        self.assertIn('no-store', returned.headers['Cache-Control'])
        self.assertEqual('no-referrer', returned.headers['Referrer-Policy'])
        self.assertEqual(0, self.bookings()[0]['PaymentStatus'])
        signed_out = self.session().get(self.base + '/Payment/VnPayReturn', params=fields,
                                        timeout=20, allow_redirects=False)
        self.assertEqual(302, signed_out.status_code)
        location = urlparse(signed_out.headers['Location'])
        self.assertEqual('/Account/Login', location.path)
        self.assertEqual({'returnUrl': ['/Booking/MyBookings']}, parse_qs(location.query))
        self.assertIn('no-store', signed_out.headers['Cache-Control'])
        self.assertEqual(0, self.bookings()[0]['PaymentStatus'])
        other = self.login('customer2@sportbooking.vn')
        returned = other.get(self.base + '/Payment/VnPayReturn', params=fields, timeout=20, allow_redirects=False)
        self.assertIn('/Booking/MyBookings', returned.headers['Location'])
        self.assertEqual('00', self.ipn(fields)['RspCode'])
        self.assertEqual(1, self.bookings()[0]['PaymentStatus'])

    def test_17_inactive_complex_blocks_booking(self):
        self.sql('UPDATE SportComplexes SET IsActive=0 WHERE Id=(SELECT SportComplexId FROM Courts WHERE Id=1)')
        try:
            response = self.post(self.customer, '/Booking/Create', {'CourtId': 1, 'BookingDate': self.target,
                                 'SelectedTimeSlotIds': [1], 'PaymentMethod': 'Counter'}, source='/Booking/MyBookings')
            self.assertEqual(200, response.status_code)
            self.assertEqual([], self.bookings())
            slots = self.get(self.customer, f'/Booking/GetTimeSlots?courtId=1&date={self.target}').json()['slots']
            self.assertTrue(all(not x['selectable'] for x in slots))
        finally:
            self.sql('UPDATE SportComplexes SET IsActive=1 WHERE Id=(SELECT SportComplexId FROM Courts WHERE Id=1)')

    def test_18_locked_owner_blocks_booking(self):
        self.sql("UPDATE Users SET IsActive=0 WHERE Email='owner1@sportbooking.vn'")
        try:
            response = self.post(self.customer, '/Booking/Create', {'CourtId': 1, 'BookingDate': self.target,
                                 'SelectedTimeSlotIds': [1], 'PaymentMethod': 'Counter'}, source='/Booking/MyBookings')
            self.assertEqual(200, response.status_code)
            slots = self.get(self.customer, f'/Booking/GetTimeSlots?courtId=1&date={self.target}').json()['slots']
            self.assertTrue(all(not x['selectable'] for x in slots))
        finally:
            self.sql("UPDATE Users SET IsActive=1 WHERE Email='owner1@sportbooking.vn'")

    def test_19_online_refund_cannot_be_confirmed_as_cash(self):
        response = self.create_legacy_vnpay()
        self.assertEqual('00', self.ipn(self.callback(response))['RspCode'])
        booking = self.bookings()[0]
        self.post(self.customer, '/Booking/Cancel', {'BookingId': booking['Id'],
                  'CancellationReason': 'Khong the tham gia do thay doi lich'}, source=f"/Booking/Cancel/{booking['Id']}")
        self.post(self.owner, '/Owner/Bookings/ConfirmCounterRefund', {'bookingId': booking['Id']}, source='/Owner/Bookings')
        self.assertEqual(4, self.bookings()[0]['PaymentStatus'])

    def test_20_invalid_payment_and_csrf_are_rejected(self):
        response = self.post(self.customer, '/Booking/Create', {'CourtId': 1, 'BookingDate': self.target,
                             'SelectedTimeSlotIds': [1], 'PaymentMethod': 'Other'}, source='/Booking/Create?courtId=1')
        self.assertEqual(200, response.status_code)
        self.assertEqual([], self.bookings())
        response = self.customer.post(self.base + '/BankTransfer/Report', data={'code': 'invalid'}, timeout=20)
        self.assertEqual(400, response.status_code)

    def test_21_concurrent_ipn_is_idempotent(self):
        response = self.create_legacy_vnpay((1, 2))
        fields = self.callback(response)
        barrier = threading.Barrier(2)
        clients = [self.session(), self.session()]
        def submit(client):
            barrier.wait(timeout=10)
            return client.get(self.base + '/Payment/VnPayIpn', params=fields, timeout=40).json()['RspCode']
        with concurrent.futures.ThreadPoolExecutor(max_workers=2) as executor:
            codes = list(executor.map(submit, clients))
        self.assertEqual(['00', '02'], sorted(codes))
        self.assertTrue(all(x['Status'] == 1 and x['PaymentStatus'] == 1 for x in self.bookings()))

    def test_22_retired_vnpay_history_still_readable(self):
        fields = self.create_legacy_vnpay()
        self.assertEqual('00', self.ipn(self.callback(fields))['RspCode'])
        booking = self.bookings()[0]
        customer = self.get(self.customer, f"/Booking/Detail/{booking['Id']}")
        owner = self.get(self.owner, '/Owner/Bookings?keyword=' + booking['BookingGroupCode'])
        for page in (customer, owner):
            self.assertEqual(200, page.status_code)
            self.assertIn('VNPay', page.text)
            self.assertIn('Đã thanh toán', html.unescape(page.text))
        self.assertNotIn('/Payment/Pay', customer.text)



class StartupRegression(unittest.TestCase):
    """Startup scenarios use a fresh disposable database per test."""

    def setUp(self):
        self.database = 'SportBookingRegression_' + uuid.uuid4().hex[:12]
        self.temp = tempfile.TemporaryDirectory(prefix='sportbooking-startup-')
        self.processes = []
        self.addCleanup(self.cleanup_startup)

    def cleanup_startup(self):
        for process, log in self.processes:
            if process.poll() is None:
                process.terminate()
                try:
                    process.wait(timeout=10)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait(timeout=5)
            log.close()
        if not re.fullmatch(r'SportBookingRegression_[0-9a-f]{12}', self.database):
            raise RuntimeError('Refusing to clean up an unexpected database name')
        Regression.sql(f"IF DB_ID(N'{self.database}') IS NOT NULL BEGIN "
                       f'ALTER DATABASE [{self.database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; '
                       f'DROP DATABASE [{self.database}]; END', database='master')
        self.temp.cleanup()

    def rows(self, query):
        return json.loads(Regression.sql(query + ' FOR JSON PATH', database=self.database) or '[]')

    def start(self, *, demo=False, migrate=True, bootstrap=False, email=None, password=None,
              production=False, failure=None, test_utc_now=None, scan_seconds=60):
        with socket.socket() as sock:
            sock.bind(('127.0.0.1', 0))
            port = sock.getsockname()[1]
        base = f'http://127.0.0.1:{port}'
        environment = os.environ.copy()
        for key in ('InitialAdmin__Email', 'InitialAdmin__Password', 'DOTNET_ENVIRONMENT',
                    'SPORTBOOKING_TEST_UTC_NOW'):
            environment.pop(key, None)
        environment.update({
            'ASPNETCORE_ENVIRONMENT': 'Production' if production else 'Development',
            'ASPNETCORE_URLS': base,
            'ConnectionStrings__DefaultConnection':
                f'Server=(localdb)\\MSSQLLocalDB;Database={self.database};Trusted_Connection=True;'
                'MultipleActiveResultSets=true;TrustServerCertificate=True',
            'DatabaseInitialization__ApplyMigrations': str(migrate).lower(),
            'DatabaseInitialization__SeedDemoData': str(demo).lower(),
            'DatabaseInitialization__CreateInitialAdmin': str(bootstrap).lower(),
            'EmailSettings__UseConsoleSender': 'true',
            'BookingSettings__ExpirationScanIntervalSeconds': str(scan_seconds),
            'Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command': 'Warning'
        })
        if test_utc_now is not None:
            environment['ASPNETCORE_ENVIRONMENT'] = 'Testing'
            environment['SPORTBOOKING_TEST_UTC_NOW'] = test_utc_now
        if email is not None:
            environment['InitialAdmin__Email'] = email
        if password is not None:
            environment['InitialAdmin__Password'] = password
        log_path = Path(self.temp.name) / f'app-{len(self.processes)}.log'
        log = log_path.open('w', encoding='utf-8')
        process = subprocess.Popen(['dotnet', str(APP_DLL)],
                                   cwd=ROOT, env=environment, stdout=log, stderr=subprocess.STDOUT,
                                   creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
        self.processes.append((process, log))
        client = requests.Session()
        client.trust_env = False
        self.addCleanup(client.close)
        deadline = time.monotonic() + 50
        while time.monotonic() < deadline:
            if process.poll() is not None:
                break
            try:
                if client.get(base + '/Account/Login', timeout=1).status_code == 200:
                    if failure:
                        self.fail('Startup unexpectedly succeeded')
                    Regression.prepare_fixture_views(self.database)
                    return base, client
            except requests.RequestException:
                pass
            time.sleep(.2)
        log.flush()
        output = log_path.read_text(encoding='utf-8', errors='replace')
        if failure:
            self.assertIsNotNone(process.poll(), output[-1600:])
            self.assertIn(failure, output)
            return base, client
        self.fail('Startup failed: ' + output[-2000:])

    def stop(self):
        process, log = self.processes[-1]
        if process.poll() is None:
            process.terminate()
            process.wait(timeout=10)
        log.flush()

    def test_empty_database_roles_admin_and_existing_user_are_preserved(self):
        base, client = self.start()
        self.assertEqual({'Admin', 'Owner', 'Customer'},
                         {r['Name'] for r in self.rows('SELECT Name FROM Roles')})
        for table in ('Users', 'SportTypes', 'SportComplexes', 'Courts', 'TimeSlots',
                      'Bookings', 'Payments', 'Reviews', 'Notifications', 'DemoSeedStates'):
            self.assertEqual(0, self.rows(f'SELECT COUNT(*) AS n FROM {table}')[0]['n'], table)
        page = client.get(base + '/Account/Register', timeout=20)
        registered = client.post(base + '/Account/Register', data={
            'Email': 'existing@example.test', 'Password': 'Existing@123',
            'ConfirmPassword': 'Existing@123', 'FullName': 'Existing customer',
            'PhoneNumber': '0901234567', 'AccountType': 'Customer', 'AgreeToTerms': 'true',
            '__RequestVerificationToken': token(page)}, timeout=20, allow_redirects=False)
        self.assertEqual(302, registered.status_code)
        before = self.rows("SELECT Id, PasswordHash, IsActive FROM Users WHERE Email='existing@example.test'")[0]
        self.stop()

        admin_email, admin_password = 'bootstrap@example.test', 'Str0ngTest!2026'
        base, client = self.start(bootstrap=True, email=admin_email, password=admin_password)
        admin = self.rows("SELECT u.Id, u.PasswordHash, u.IsActive FROM Users u "
                          "WHERE u.Email='bootstrap@example.test'")[0]
        roles = self.rows("SELECT r.Name FROM UserRoles ur JOIN Roles r ON r.Id=ur.RoleId "
                          f"WHERE ur.UserId='{admin['Id']}'")
        self.assertEqual(['Admin'], [r['Name'] for r in roles])
        page = client.get(base + '/Account/Login', timeout=20)
        login = client.post(base + '/Account/Login', data={'Email': admin_email,
            'Password': admin_password, '__RequestVerificationToken': token(page)},
            timeout=20, allow_redirects=False)
        self.assertEqual(302, login.status_code)
        self.stop()

        Regression.sql(f"UPDATE Users SET IsActive=0 WHERE Id='{admin['Id']}'", database=self.database)
        self.start(bootstrap=True, email=admin_email, password='Different@123')
        after = self.rows(f"SELECT PasswordHash, IsActive FROM Users WHERE Id='{admin['Id']}'")[0]
        self.assertEqual(admin['PasswordHash'], after['PasswordHash'])
        self.assertFalse(after['IsActive'])
        self.assertEqual(before, self.rows("SELECT Id, PasswordHash, IsActive FROM Users "
                                           "WHERE Email='existing@example.test'")[0])
        self.stop()
        self.start()
        self.assertEqual(3, self.rows('SELECT COUNT(*) AS n FROM Roles')[0]['n'])
        self.assertEqual(2, self.rows('SELECT COUNT(*) AS n FROM Users')[0]['n'])

    def test_initial_admin_requires_configuration_and_never_elevates_customer(self):
        self.start(bootstrap=True, failure='InitialAdmin:Email')
        self.start(bootstrap=True, email='bootstrap@example.test', password='short',
                   failure='InitialAdmin:Password')
        base, client = self.start()
        page = client.get(base + '/Account/Register', timeout=20)
        self.assertEqual(302, client.post(base + '/Account/Register', data={
            'Email': 'bootstrap@example.test', 'Password': 'Existing@123',
            'ConfirmPassword': 'Existing@123', 'FullName': 'Ordinary customer',
            'PhoneNumber': '0901234567', 'AccountType': 'Customer', 'AgreeToTerms': 'true',
            '__RequestVerificationToken': token(page)}, timeout=20, allow_redirects=False).status_code)
        before = self.rows("SELECT Id, PasswordHash FROM Users WHERE Email='bootstrap@example.test'")[0]
        self.stop()
        self.start(bootstrap=True, email='bootstrap@example.test', password='Str0ngTest!2026',
                   failure='role Admin')
        self.assertEqual(before, self.rows("SELECT Id, PasswordHash FROM Users "
                                           "WHERE Email='bootstrap@example.test'")[0])
        self.assertEqual(['Customer'], [r['Name'] for r in self.rows(
            "SELECT r.Name FROM UserRoles ur JOIN Roles r ON r.Id=ur.RoleId "
            f"WHERE ur.UserId='{before['Id']}'")])

    def test_demo_is_complete_idempotent_and_disabling_it_keeps_data(self):
        self.start(demo=True)
        tables = ('Users', 'SportTypes', 'SportComplexes', 'Courts', 'TimeSlots',
                  'Bookings', 'Payments', 'Reviews', 'Notifications', 'DemoSeedStates')
        counts = {table: self.rows(f'SELECT COUNT(*) AS n FROM {table}')[0]['n'] for table in tables}
        self.assertTrue(all(counts.values()), counts)
        self.assertEqual({'Users': 8, 'SportTypes': 6, 'SportComplexes': 4,
                          'Courts': 28, 'DemoSeedStates': 1},
                         {key: counts[key] for key in ('Users', 'SportTypes', 'SportComplexes',
                                                       'Courts', 'DemoSeedStates')})
        password_hash = self.rows("SELECT PasswordHash FROM Users WHERE Email='admin@sportbooking.vn'")[0]
        self.stop()
        self.start(demo=True)
        self.assertEqual(counts, {table: self.rows(f'SELECT COUNT(*) AS n FROM {table}')[0]['n']
                                  for table in tables})
        self.assertEqual(password_hash, self.rows(
            "SELECT PasswordHash FROM Users WHERE Email='admin@sportbooking.vn'")[0])
        self.stop()
        self.start(demo=False)
        self.assertEqual(counts, {table: self.rows(f'SELECT COUNT(*) AS n FROM {table}')[0]['n']
                                  for table in tables})

    def test_failed_demo_seed_rolls_back_and_can_be_retried(self):
        self.start()
        Regression.sql("INSERT INTO SportTypes(Name,DisplayOrder,IsActive,CreatedAt) "
                       "VALUES(N'Bóng đá mini',0,1,GETUTCDATE())", database=self.database)
        self.stop()
        self.start(demo=True, failure='UX_SportTypes_Name')
        self.assertEqual(1, self.rows('SELECT COUNT(*) AS n FROM SportTypes')[0]['n'])
        self.assertEqual(0, self.rows('SELECT COUNT(*) AS n FROM Users')[0]['n'])
        self.assertEqual(0, self.rows('SELECT COUNT(*) AS n FROM DemoSeedStates')[0]['n'])
        Regression.sql("DELETE FROM SportTypes WHERE Name=N'Bóng đá mini'", database=self.database)
        self.start(demo=True)
        self.assertEqual(1, self.rows('SELECT COUNT(*) AS n FROM DemoSeedStates')[0]['n'])

    def test_demo_does_not_change_an_existing_customer(self):
        base, client = self.start(bootstrap=True, email='admin@sportbooking.vn',
                                  password='AdminUnique@2026')
        page = client.get(base + '/Account/Register', timeout=20)
        self.assertEqual(302, client.post(base + '/Account/Register', data={
            'Email': 'existing@example.test', 'Password': 'Existing@123',
            'ConfirmPassword': 'Existing@123', 'FullName': 'Existing customer',
            'PhoneNumber': '0901234567', 'AccountType': 'Customer', 'AgreeToTerms': 'true',
            '__RequestVerificationToken': token(page)}, timeout=20, allow_redirects=False).status_code)
        before = self.rows("SELECT Id, PasswordHash, IsActive FROM Users WHERE Email='existing@example.test'")[0]
        before_admin = self.rows("SELECT Id, PasswordHash, IsActive FROM Users "
                                 "WHERE Email='admin@sportbooking.vn'")[0]
        self.stop()
        self.start(demo=True)
        self.assertEqual(before, self.rows("SELECT Id, PasswordHash, IsActive FROM Users "
                                           "WHERE Email='existing@example.test'")[0])
        self.assertEqual(before_admin, self.rows("SELECT Id, PasswordHash, IsActive FROM Users "
                                                 "WHERE Email='admin@sportbooking.vn'")[0])
        self.assertEqual(9, self.rows('SELECT COUNT(*) AS n FROM Users')[0]['n'])
        self.assertEqual(1, self.rows('SELECT COUNT(*) AS n FROM DemoSeedStates')[0]['n'])

    def test_production_rejects_demo_before_creating_database(self):
        self.start(demo=True, production=True, failure='SeedDemoData')
        result = Regression.sql(f"SELECT CASE WHEN DB_ID(N'{self.database}') IS NULL THEN 1 ELSE 0 END",
                                database='master')
        self.assertIn('1', result)

    def test_migration_disabled_on_new_database_fails_clearly(self):
        self.start(migrate=False, failure='ApplyMigrations')

    def test_requested_migration_failure_stops_startup(self):
        Regression.sql(f'CREATE DATABASE [{self.database}]', database='master')
        Regression.sql('CREATE TABLE SportTypes (Id int NOT NULL PRIMARY KEY)', database=self.database)
        self.start(migrate=True, failure='Database migration')
        self.assertEqual(0, self.rows('SELECT COUNT(*) AS n FROM SportTypes')[0]['n'])


if __name__ == '__main__':
    unittest.main(verbosity=2)
