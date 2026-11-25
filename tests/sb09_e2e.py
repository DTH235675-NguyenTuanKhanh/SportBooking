"""SB-09 acceptance through real Razor forms on a fresh LocalDB database."""
import json
import os
from datetime import datetime, time, timedelta, timezone
from pathlib import Path
import re
import secrets
import unittest
from urllib.parse import urlencode, urlsplit

from playwright.sync_api import TimeoutError as PlaywrightTimeoutError, expect, sync_playwright

import regression
from regression import ROOT, Regression, StartupRegression, sample_png


VN = timezone(timedelta(hours=7))
SPORT = 'Cầu lông SB09'
COMPLEX = 'Cụm sân SB09'
COURT = 'Sân cầu lông SB09 số 1'
COMMENT = 'Sân sạch, khung giờ đúng và thanh toán tại quầy thuận tiện.'
REPLY = 'Cảm ơn bạn đã đặt sân, hẹn gặp lại!'
SLOTS = (('18:00', '19:00', 180000, 210000), ('19:00', '20:00', 220000, 260000))


class CleanDatabaseJourney(StartupRegression):
    evidence_folder = ROOT / 'docs/sb-09/evidence'
    # Load only the journey in __main__. StartupRegression supplies the disposable DB harness.
    def setUp(self):
        super().setUp()
        default = self.evidence_folder / self.database.removeprefix('SportBookingRegression_')
        self.evidence = Path(os.environ.get('SB09_EVIDENCE_DIR', str(default)))
        self.evidence.mkdir(parents=True, exist_ok=True)
        print(f'Evidence: {self.evidence}', flush=True)
        self.steps = []
        self.flow_passed = False
        self.page_errors = []
        self.password = 'Aa9!' + secrets.token_urlsafe(20)
        self.before = datetime.now(VN).replace(microsecond=0)
        self.day = self.before.date() + timedelta(days=3)
        self.after = datetime.combine(self.day, time(20, 1), VN)

    def cleanup_startup(self):
        removed = False
        try:
            super().cleanup_startup()
            self.assertEqual('1', Regression.sql(
                f"PRINT CASE WHEN DB_ID(N'{self.database}') IS NULL THEN 1 ELSE 0 END", database='master'))
            self.assertTrue(all(process.poll() is not None for process, _ in self.processes))
            removed = True
        finally:
            self.write_evidence(removed)

    def write_evidence(self, cleaned=False):
        result = {'database': self.database, 'demo_enabled': False, 'passed': self.flow_passed,
                  'database_and_app_processes_cleaned': cleaned,
                  'booking_day_vietnam': self.day.isoformat(),
                  'clock_before_vietnam': self.before.isoformat(), 'clock_after_vietnam': self.after.isoformat(),
                  'page_error_count': len(self.page_errors), 'steps': self.steps}
        (self.evidence / 'result.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n',
                                                   encoding='utf-8')

    def record(self, name, data, page=None):
        entry = {'step': name, 'checks': data}
        if page is not None:
            entry['screenshot'] = f'{len(self.steps) + 1:02d}-{name}.png'
            entry['viewport'] = page.viewport_size
            page.screenshot(path=self.evidence / entry['screenshot'], animations='disabled')
        self.steps.append(entry)
        self.write_evidence()
        print(f'PASS {name}', flush=True)

    def run_app(self, instant, bootstrap=False):
        utc = instant.astimezone(timezone.utc).strftime('%Y-%m-%dT%H:%M:%SZ')
        self.base, _ = self.start(test_utc_now=utc, demo=False, bootstrap=bootstrap,
                                  email='admin@sb09.example.test' if bootstrap else None,
                                  password=self.password if bootstrap else None)

    def new_page(self, browser, mobile=False):
        size = {'width': 390, 'height': 844} if mobile else {'width': 1440, 'height': 1000}
        page = browser.new_context(viewport=size, is_mobile=mobile, has_touch=mobile,
                                   locale='vi-VN', timezone_id='Asia/Ho_Chi_Minh').new_page()
        page.on('pageerror', lambda error: self.page_errors.append(str(error)))
        return page

    def go(self, page, path):
        response = page.goto(self.base + path, wait_until='domcontentloaded')
        self.assertEqual(200, response.status, path)

    def fill(self, page, **fields):
        for name, value in fields.items():
            page.locator(f'#{name}').fill(str(value))

    def submit(self, page, action, label, status=302):
        try:
            with page.expect_response(lambda res: res.request.method == 'POST'
                                      and urlsplit(res.url).path.rstrip('/').lower() == action.lower(),
                                      timeout=12000) as sent:
                page.locator('main button[type=submit]').filter(has_text=label).first.click(timeout=10000)
        except PlaywrightTimeoutError as error:
            invalid_fields = page.locator('main form input:invalid, main form [aria-invalid=true]').evaluate_all(
                '(fields) => fields.map(field => field.name || field.id)')
            self.fail(f'{action} did not submit. Invalid fields: {invalid_fields}. {error}')
        self.assertEqual(status, sent.value.status, action)
        page.wait_for_load_state('domcontentloaded')

    def register(self, page, alias, name, owner=False, qr_image=None, *, check_validation_retry=False):
        self.go(page, '/Account/Register')
        account_type = 'typeOwner' if owner else 'typeCustomer'
        page.locator(f'label[for={account_type}]').click()
        expect(page.locator(f'#{account_type}')).to_be_checked()
        if owner:
            self.fill(page, BusinessName=f'Cơ sở {name}', BusinessPhone='0901234567')
        if qr_image is not None:
            page.locator('#Transfer_Enabled').check()
            self.fill(page, Transfer_BankName='Ngân hàng thử', Transfer_AccountNumber='0123456789',
                      Transfer_AccountName='CHU SAN THU')
            page.locator('#Transfer_Qr').set_input_files(qr_image)
            expect(page.locator('[data-qr-preview]')).to_be_visible()
            page.locator('#Transfer_Confirmed').check()
        self.fill(page, FullName=name, Email=f'{alias}@sb09.example.test', PhoneNumber='0901234567',
                  passwordInput=self.password, ConfirmPassword=self.password)
        page.locator('#agreeTerms').check()
        if check_validation_retry:
            self.fill(page, ConfirmPassword=self.password + 'x')
            submit_button = page.locator('main button[type=submit]')
            password_error = page.locator('[data-valmsg-for="ConfirmPassword"]')
            submit_button.click()
            expect(password_error).to_contain_text('Mật khẩu xác nhận không khớp.')
            expect(submit_button).to_be_enabled()
            confirmation = page.locator('#ConfirmPassword')
            confirmation.press('End')
            confirmation.press('Backspace')
            expect(password_error).to_be_empty()

        self.submit(page, '/Account/Register', 'Đăng ký')
        expect(page).to_have_url(re.compile(r'/Account/Login(?:\?|$)', re.I))
        return self.rows(f"SELECT Id, ApprovalStatus FROM Users WHERE Email='{alias}@sb09.example.test'")[0]

    def login(self, page, alias, path, pending=False):
        self.go(page, '/Account/Login')
        self.fill(page, Email=f'{alias}@sb09.example.test', passwordInput=self.password)
        self.submit(page, '/Account/Login', 'Đăng nhập', status=200 if pending else 302)
        suffix = r'(?:\?|$)' if path == '/' else r'(?:[/\?]|$)'
        expect(page).to_have_url(re.compile(re.escape(self.base + path) + suffix, re.I))

    def approve(self, page, user_id):
        self.go(page, f'/Admin/OwnerApproval/Detail/{user_id}')
        expect(page.locator('#decisionForm')).to_be_visible()
        self.submit(page, '/Admin/OwnerApproval/Decide', 'Duyệt')
        self.assertEqual(2, self.rows(f"SELECT ApprovalStatus FROM Users WHERE Id='{user_id}'")[0]['ApprovalStatus'])

    def protected_post(self, page, action, values):
        csrf = page.locator('form[action="/Account/Logout"] input[name=__RequestVerificationToken]')
        return page.request.post(self.base + action, form={**values, '__RequestVerificationToken': csrf.input_value()},
                                 max_redirects=0)

    def bookings(self):
        return self.rows('SELECT Id, BookingCode, BookingGroupCode, CustomerId, CourtId, TimeSlotId, '
                         'BookingDate, StartDateTime, EndDateTime, TotalAmount, Status, PaymentStatus, '
                         'PaymentMethod FROM Bookings ORDER BY StartDateTime')

    def fits(self, page):
        self.assertTrue(page.evaluate('document.documentElement.scrollWidth <= innerWidth'), page.url)

    def check_role_viewports(self, admin, owner, customer, court_id, confirmation_path, owner_bookings):
        for width in (375, 768, 1440):
            for page in (admin, owner, customer):
                page.set_viewport_size({'width': width, 'height': 1000})

            self.go(admin, '/Admin')
            admin.locator('main a[href^="/Admin/OwnerApproval"]').first.click()
            expect(admin.locator('main h1')).to_contain_text('Duyệt tài khoản chủ sân')
            admin.locator('main #Status').select_option('Approved')
            admin.locator('main form').get_by_role('button', name='Lọc').click()
            expect(admin.locator('main table tbody')).to_contain_text('Chủ sân SB09')
            self.fits(admin)
            self.record(f'admin-viewport-{width}', {'approval_table': True}, admin)

            self.go(owner, '/Owner')
            owner.locator('main a[href="/Owner/Complexes"]').first.click()
            expect(owner.locator('main table tbody')).to_contain_text(COMPLEX)
            self.fits(owner)
            owner.locator('main a[href^="/Owner/Complexes/Create"]').click()
            expect(owner.locator('#OpeningTime')).to_be_visible()
            self.fits(owner)
            self.go(owner, owner_bookings)
            expect(owner.locator('main').get_by_text('Hoàn thành', exact=True)).not_to_have_count(0)
            self.fits(owner)
            self.go(owner, '/Owner/Reviews')
            expect(owner.locator('main')).to_contain_text(REPLY)
            self.fits(owner)
            self.go(owner, '/Owner/Revenue')
            expect(owner.locator('main h1')).to_contain_text('Doanh thu sân')
            self.fits(owner)
            self.record(f'owner-viewport-{width}', {'complex_table': True, 'form': True, 'booking_status': True}, owner)

            self.go(customer, f'/Court/Detail/{court_id}')
            if width < 992:
                customer.locator('.navbar-toggler').click()
                expect(customer.locator('#navbarMain')).to_be_visible()
            customer.locator('#navbarMain a[href="/Booking/MyBookings"]').click()
            expect(customer.locator('main h1')).to_contain_text('Lịch đặt sân')
            self.fits(customer)
            self.go(customer, confirmation_path)
            self.fits(customer)
            table = customer.locator('.booking-confirmation-table')
            expect(table.locator('tbody tr')).to_have_count(2)
            self.assertTrue(table.evaluate('table => table.parentElement.scrollWidth <= table.parentElement.clientWidth'),
                            f'Confirmation table overflowed at {width}px')
            table.locator('tbody tr').first.scroll_into_view_if_needed()
            self.record(f'customer-viewport-{width}', {'navigation': True, 'history': True, 'confirmation_fits': True},
                        customer)

    def owner_reply(self, admin, owner, other_owner, customer, court):
        review_id = self.rows('SELECT Id FROM Reviews')[0]['Id']
        reply_path = f'/Owner/Reviews/Reply/{review_id}'
        self.go(other_owner, '/Owner/Reviews')
        expect(other_owner.locator('main')).to_contain_text('Chưa có đánh giá')
        self.assertEqual(404, other_owner.request.get(self.base + reply_path, max_redirects=0).status)
        self.assertEqual(404, self.protected_post(other_owner, reply_path, {'reply': REPLY}).status)
        self.go(owner, '/Owner/Reviews')
        owner.get_by_role('link', name='Phản hồi', exact=True).click()
        self.assertEqual(400, owner.request.post(self.base + reply_path, form={'reply': REPLY}, max_redirects=0).status)
        self.assertEqual(200, self.protected_post(owner, reply_path, {'reply': 'x' * 1001}).status)
        self.assertNotIn('OwnerReply', self.rows('SELECT OwnerReply FROM Reviews')[0])
        owner.locator('#reply').fill(REPLY)
        self.submit(owner, reply_path, 'Lưu phản hồi')
        saved = self.rows('SELECT OwnerReply, OwnerReplyAt FROM Reviews')[0]
        self.assertEqual(REPLY, saved['OwnerReply'])
        self.assertTrue(saved['OwnerReplyAt'])
        self.assertEqual(200, customer.reload(wait_until='domcontentloaded').status)
        expect(customer.locator('#courtReviews')).to_contain_text(REPLY)
        self.go(admin, '/Admin/ReviewManagement')
        visibility = '/Admin/ReviewManagement/SetVisibility'
        self.assertEqual(302, self.protected_post(admin, visibility, {'reviewId': review_id, 'isVisible': 'false'}).status)
        self.assertEqual(404, self.protected_post(owner, reply_path, {'reply': 'Đánh giá đã ẩn'}).status)
        self.assertEqual(200, customer.reload(wait_until='domcontentloaded').status)
        expect(customer.locator('main')).not_to_contain_text(REPLY)
        self.assertEqual(302, self.protected_post(admin, visibility, {'reviewId': review_id, 'isVisible': 'true'}).status)
        self.go(owner, '/Owner/Reviews')
        self.record('owner-reply', {'reply': saved, 'csrf_cross_owner_hidden_blocked': True}, owner)
        return saved

    def book_and_receive_qr(self, owner, customer, court):
        qr_day = self.day + timedelta(days=1)
        self.go(customer, f'/Booking/Create?courtId={court}&bookingDate={qr_day.isoformat()}')
        slots = customer.locator('#timeSlotGrid input[type=checkbox]:not(:disabled)')
        expect(slots).to_have_count(2)
        slots.first.check()
        customer.locator('label[for=payOnline]').click()
        self.submit(customer, '/Booking/Create', 'Xác nhận đặt sân')
        expect(customer.locator('main h1')).to_have_text('Chuyển khoản qua QR')
        expect(customer.locator('main')).to_contain_text('0123456789')
        self.submit(customer, '/BankTransfer/Report', 'Tôi đã chuyển khoản')
        expect(customer.locator('main')).to_contain_text('Chờ chủ sân xác nhận')
        transfer = self.rows('SELECT GroupCode, Amount, State FROM BankTransfers')[0]
        qr_code = transfer['GroupCode']
        qr_rows = [row for row in self.bookings() if row['BookingGroupCode'] == qr_code]
        self.assertEqual([(0, 0, 3)], [(row['Status'], row['PaymentStatus'], row['PaymentMethod']) for row in qr_rows])
        self.go(owner, '/Owner/Bookings?TransferReview=true')
        owner.get_by_role('link', name='Chờ chủ sân xác nhận').first.click()
        owner.once('dialog', lambda dialog: dialog.accept())
        self.submit(owner, '/BankTransfer/Review', 'Xác nhận đã nhận đủ tiền')
        expected = [dict(row, Status=1, PaymentStatus=1) for row in qr_rows]
        current = [row for row in self.bookings() if row['BookingGroupCode'] == qr_code]
        self.assertEqual(expected, current)
        self.assertEqual(transfer['Amount'], current[0]['TotalAmount'])
        self.record('qr-received', {'rows': current, 'amount': transfer['Amount']}, owner)
        return qr_code, qr_day, current

    def revenue(self, owner, day, court, gross):
        url = f'/Owner/Revenue?From={day}&To={day}&CourtId={court}'
        self.go(owner, url)
        expect(owner.locator('#received')).to_contain_text(f'{int(gross):,}'.replace(',', '.'))
        expect(owner.locator('#net')).to_contain_text(f'{int(gross):,}'.replace(',', '.'))
        expect(owner.locator('#refunded dd')).to_have_text('0 ₫')
        self.fits(owner)
        return url

    def test_clean_database_journey(self):
        self.start(production=True, failure='SB09')
        self.assertEqual('1', Regression.sql(
            f"PRINT CASE WHEN DB_ID(N'{self.database}') IS NULL THEN 1 ELSE 0 END", database='master'))
        self.run_app(self.before, bootstrap=True)
        roles = [row['Name'] for row in self.rows('SELECT Name FROM Roles ORDER BY Name')]
        self.assertEqual(['Admin', 'Customer', 'Owner'], roles)
        applied = self.rows('SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId')
        self.assertTrue(applied)
        empty = {table: self.rows(f'SELECT COUNT(*) AS N FROM {table}')[0]['N'] for table in
                 ('SportTypes', 'SportComplexes', 'Courts', 'TimeSlots', 'Bookings', 'Payments', 'Reviews',
                  'Notifications', 'DemoSeedStates')}
        self.assertEqual({table: 0 for table in empty}, empty)
        accounts = self.rows('SELECT u.Email, r.Name AS Role FROM Users u JOIN UserRoles ur ON u.Id=ur.UserId '
                             'JOIN Roles r ON r.Id=ur.RoleId')
        self.assertEqual([{'Email': 'admin@sb09.example.test', 'Role': 'Admin'}], accounts)
        self.record('empty-database', {'migrations': applied, 'roles': roles, 'table_counts': empty,
                                       'bootstrap_accounts': accounts, 'test_build_rejected_production': True})

        with sync_playwright() as playwright:
            browser = playwright.chromium.launch(channel='msedge', headless=True)
            admin = self.new_page(browser)
            owner = self.new_page(browser)
            outsider_owner = self.new_page(browser)
            customer = self.new_page(browser, mobile=True)
            outsider_customer = self.new_page(browser)

            self.login(admin, 'admin', '/Admin')
            self.go(admin, '/Admin/SportTypeManagement/Create')
            self.fill(admin, Name=SPORT, DisplayOrder=1)
            admin.locator('#IsActive').check()
            self.submit(admin, '/Admin/SportTypeManagement/Create', 'Lưu loại sân')
            expect(admin.locator('main')).to_contain_text(SPORT)
            sport = self.rows('SELECT Id, Name, IsActive FROM SportTypes')
            self.assertEqual([(SPORT, True)], [(r['Name'], r['IsActive']) for r in sport])
            self.record('admin-sport-type', {'sport_type': sport[0]}, admin)

            owner_user = self.register(owner, 'owner', 'Chủ sân SB09', owner=True,
                                       qr_image={'name': 'qr-test.png', 'mimeType': 'image/png', 'buffer': sample_png()},
                                       check_validation_retry=True)
            self.assertEqual(1, owner_user['ApprovalStatus'])
            self.login(owner, 'owner', '/Account/Login', pending=True)
            expect(owner.locator('main')).to_contain_text('đang chờ Admin phê duyệt')
            self.record('owner-pending', {'approval_status': 'Pending', 'login_rejected': True}, owner)
            blocked = owner.request.get(self.base + '/Owner/Complexes/Create', max_redirects=0)
            self.assertEqual(302, blocked.status)
            self.assertIn('/Account/Login', blocked.headers['location'])

            self.approve(admin, owner_user['Id'])
            self.record('admin-owner-approved', {'approval_status': 'Approved', 'pending_owner_create_http': blocked.status}, admin)
            other_user = self.register(outsider_owner, 'other-owner', 'Chủ sân khác SB09', owner=True)
            self.approve(admin, other_user['Id'])
            self.login(outsider_owner, 'other-owner', '/Owner')

            self.login(owner, 'owner', '/Owner')
            self.go(owner, '/Owner/Complexes/Create')
            self.fill(owner, Name=COMPLEX, Address='10 Đường Kiểm Thử', Province='TP. Hồ Chí Minh',
                      District='Quận 5', OpeningTime='07:00', ClosingTime='22:00')
            self.submit(owner, '/Owner/Complexes/Create', 'Lưu cụm sân')
            expect(owner.locator('main')).to_contain_text(COMPLEX)
            complex_row = self.rows('SELECT Id, OwnerId, Name, OpeningTime, ClosingTime, IsActive FROM SportComplexes')[0]
            self.assertEqual((owner_user['Id'], COMPLEX, '07:00', '22:00', True),
                             (complex_row['OwnerId'], complex_row['Name'], complex_row['OpeningTime'][:5],
                              complex_row['ClosingTime'][:5], complex_row['IsActive']))
            self.record('owner-complex', complex_row, owner)

            self.go(owner, f"/Owner/Courts/Create?complexId={complex_row['Id']}")
            self.fill(owner, Name=COURT)
            owner.locator('#SportTypeId').select_option(str(sport[0]['Id']))
            self.submit(owner, '/Owner/Courts/Create', 'Lưu sân')
            expect(owner.locator('main')).to_contain_text(COURT)
            court = self.rows('SELECT Id, SportComplexId, SportTypeId, Name, Status, IsDeleted FROM Courts')[0]
            court_id = court['Id']
            self.assertEqual((complex_row['Id'], sport[0]['Id'], COURT, 0, False),
                             (court['SportComplexId'], court['SportTypeId'], court['Name'], court['Status'], court['IsDeleted']))
            self.record('owner-court', court, owner)

            for start, end, weekday, weekend in SLOTS:
                self.go(owner, f'/Owner/TimeSlots/Create?courtId={court_id}')
                self.fill(owner, StartTime=start, EndTime=end, PriceWeekday=weekday, PriceWeekend=weekend)
                self.submit(owner, '/Owner/TimeSlots/Create', 'Lưu khung giờ')
            slots = self.rows('SELECT Id, CourtId, StartTime, EndTime, PriceWeekday, PriceWeekend, IsActive '
                              'FROM TimeSlots ORDER BY StartTime')
            self.assertEqual([(court_id, *slot, True) for slot in SLOTS],
                             [(r['CourtId'], r['StartTime'][:5], r['EndTime'][:5], r['PriceWeekday'],
                               r['PriceWeekend'], r['IsActive']) for r in slots])
            for start, end, *_ in SLOTS:
                expect(owner.locator('main')).to_contain_text(start)
                expect(owner.locator('main')).to_contain_text(end)
            self.record('owner-time-slots', {'rows': slots}, owner)

            customer_user = self.register(customer, 'customer', 'Khách SB09')
            self.login(customer, 'customer', '/')
            self.register(outsider_customer, 'other-customer', 'Khách khác SB09')
            self.login(outsider_customer, 'other-customer', '/')
            search = '/Court/Search?' + urlencode({'Keyword': COURT, 'BookingDate': self.day.isoformat()})
            self.go(customer, search)
            self.fits(customer)
            expect(customer.locator('main').get_by_role('link', name=COURT, exact=True)).to_be_visible()
            self.record('mobile-search', {'court_id': court_id, 'found': True}, customer)
            customer.locator('main').get_by_role('link', name=COURT, exact=True).click()
            expect(customer).to_have_url(re.compile(rf'/Court/Detail/{court_id}(?:\?|$)', re.I))
            customer.locator(f'main a[href^="/Booking/Create"][href*="courtId={court_id}"]').first.click()
            expect(customer.locator('#BookingDate')).to_have_value(self.day.isoformat())
            self.fits(customer)
            selectable = customer.locator('#timeSlotGrid input[type=checkbox]:not(:disabled)')
            expect(selectable).to_have_count(2)
            for index, slot in enumerate(slots):
                expect(selectable.nth(index)).to_have_value(str(slot['Id']))
                selectable.nth(index).check()
            customer.locator('label[for=payCounter]').click()
            prices = [slot[3 if self.day.weekday() >= 5 else 2] for slot in SLOTS]
            expect(customer.locator('#totalAmountSelected')).to_have_value(str(sum(prices)))
            self.record('mobile-slot-selection', {'prices': prices, 'total_vnd': sum(prices)}, customer)
            self.go(outsider_customer, search)
            expect(outsider_customer.locator('main').get_by_role('link', name=COURT, exact=True)).to_be_visible()
            self.go(outsider_customer, f'/Booking/Create?courtId={court_id}&bookingDate={self.day.isoformat()}')
            self.fits(outsider_customer)
            expect(outsider_customer.locator('#timeSlotGrid input[type=checkbox]:not(:disabled)')).to_have_count(2)
            self.record('desktop-slot-availability', {'court_id': court_id, 'available_slots': 2}, outsider_customer)

            self.submit(customer, '/Booking/Create', 'Xác nhận đặt sân')
            expect(customer.get_by_role('heading', name='Đặt sân thành công!')).to_be_visible()
            confirmation_url = urlsplit(customer.url)
            confirmation_path = confirmation_url.path + '?' + confirmation_url.query
            initial = self.bookings()
            self.assertEqual(2, len(initial))
            group = initial[0]['BookingGroupCode']
            for index, row in enumerate(initial):
                self.assertEqual((group, customer_user['Id'], court_id, slots[index]['Id'],
                                  self.day.isoformat(), *SLOTS[index][:2], prices[index], 0, 0, 0),
                                 (row['BookingGroupCode'], row['CustomerId'], row['CourtId'], row['TimeSlotId'],
                                  row['BookingDate'], row['StartDateTime'][11:16], row['EndDateTime'][11:16],
                                  row['TotalAmount'], row['Status'], row['PaymentStatus'], row['PaymentMethod']))
                line = customer.locator('main tbody tr').filter(has_text=row['BookingCode'])
                expect(line).to_contain_text(' - '.join(SLOTS[index][:2]))
                expect(line).to_contain_text(f'{prices[index]:,}'.replace(',', '.'))
                expect(line).to_contain_text('Chờ xác nhận')
            expect(customer.locator('main tfoot')).to_contain_text(f'{sum(prices):,}'.replace(',', '.'))
            self.assertEqual([], self.rows('SELECT Id FROM Payments'))
            customer.locator('main tfoot .text-accent').scroll_into_view_if_needed()
            self.record('mobile-counter-booking', {'rows': initial, 'total_vnd': sum(prices)}, customer)
            self.go(customer, '/Booking/MyBookings')
            for row in initial:
                expect(customer.locator('main')).to_contain_text(row['BookingCode'])

            owner_bookings = '/Owner/Bookings?' + urlencode({'Keyword': group})
            self.go(outsider_owner, owner_bookings)
            expect(outsider_owner.locator('main')).to_contain_text('Không có lịch đặt phù hợp')
            wrong_confirm = self.protected_post(outsider_owner, '/Owner/Bookings/ConfirmCounterPayment',
                                                {'bookingGroupCode': group, 'Keyword': group})
            self.assertEqual(302, wrong_confirm.status)
            self.go(outsider_owner, urlsplit(wrong_confirm.headers['location']).path + '?' +
                    urlsplit(wrong_confirm.headers['location']).query)
            expect(outsider_owner.locator('body')).to_contain_text('không tìm thấy nhóm đặt sân', ignore_case=True)
            self.go(outsider_customer, '/Booking/MyBookings')
            for row in initial:
                expect(outsider_customer.locator('main')).not_to_contain_text(row['BookingCode'])
            foreign_detail = outsider_customer.request.get(self.base + f"/Booking/Detail/{initial[0]['Id']}", max_redirects=0)
            self.assertEqual(302, foreign_detail.status)
            self.assertIn('/Booking/MyBookings', foreign_detail.headers['location'])
            self.assertEqual(initial, self.bookings())
            self.assertEqual([], self.rows('SELECT Id FROM Payments'))

            self.go(owner, owner_bookings)
            for row in initial:
                expect(owner.locator('main')).to_contain_text(row['BookingCode'])
            missing_token = owner.request.post(self.base + '/Owner/Bookings/ConfirmCounterPayment',
                                               form={'bookingGroupCode': group}, max_redirects=0)
            self.assertEqual(400, missing_token.status)
            self.assertEqual(initial, self.bookings())
            self.submit(owner, '/Owner/Bookings/ConfirmCounterPayment', 'Xác nhận đã thu tiền nhóm')
            confirmed = [dict(row, Status=1, PaymentStatus=1) for row in initial]
            self.assertEqual(confirmed, self.bookings())
            payments = self.rows('SELECT BookingId, Method, Amount, Status FROM Payments ORDER BY BookingId')
            self.assertEqual([(r['Id'], 0, r['TotalAmount'], 1) for r in initial],
                             [(p['BookingId'], p['Method'], p['Amount'], p['Status']) for p in payments])
            expect(owner.get_by_role('button', name='Đánh dấu hoàn thành')).to_have_count(0)
            early = self.protected_post(owner, '/Owner/Bookings/UpdateStatus',
                                        {'bookingId': initial[0]['Id'], 'targetStatus': 'Completed', 'Keyword': group})
            self.assertEqual(302, early.status)
            self.go(owner, owner_bookings)
            expect(owner.locator('body')).to_contain_text('Chỉ đánh dấu hoàn thành sau khi khung giờ kết thúc')
            self.assertEqual(confirmed, self.bookings())
            self.record('desktop-counter-payment', {'rows': confirmed, 'payments': payments,
                        'cross_owner_payment_blocked': True, 'cross_customer_detail_blocked': True,
                        'csrf_missing_http': missing_token.status, 'early_completion_blocked': True}, owner)

            self.go(customer, '/Booking/MyBookings')
            logged_out = self.protected_post(customer, '/Account/Logout', {})
            self.assertEqual(302, logged_out.status)
            after_logout = customer.request.get(self.base + '/Booking/MyBookings', max_redirects=0)
            self.assertIn('/Account/Login', after_logout.headers['location'])
            self.login(customer, 'customer', '/')
            self.go(customer, '/Booking/MyBookings')
            expect(customer.locator('main')).to_contain_text(initial[0]['BookingCode'])

            self.stop()
            self.run_app(self.after)
            self.assertEqual(confirmed, self.bookings())
            self.assertEqual(payments, self.rows('SELECT BookingId, Method, Amount, Status FROM Payments ORDER BY BookingId'))
            for page, alias, path in ((owner, 'owner', '/Owner'), (outsider_owner, 'other-owner', '/Owner'),
                                      (customer, 'customer', '/'), (outsider_customer, 'other-customer', '/')):
                page.context.clear_cookies()
                self.login(page, alias, path)
            self.go(outsider_owner, owner_bookings)
            other_complete = self.protected_post(outsider_owner, '/Owner/Bookings/UpdateStatus',
                                                  {'bookingId': initial[0]['Id'], 'targetStatus': 'Completed'})
            self.assertEqual(302, other_complete.status)
            self.assertEqual(confirmed, self.bookings())
            self.go(owner, owner_bookings)
            for row in initial:
                article = owner.locator('article').filter(has_text=row['BookingCode'])
                expect(article.get_by_role('button', name='Đánh dấu hoàn thành')).to_be_visible()
                with owner.expect_response(lambda res: res.request.method == 'POST'
                                           and urlsplit(res.url).path.lower() == '/owner/bookings/updatestatus') as sent:
                    article.get_by_role('button', name='Đánh dấu hoàn thành').click()
                self.assertEqual(302, sent.value.status)
                owner.wait_for_load_state('domcontentloaded')
                expect(article).to_contain_text('Hoàn thành', ignore_case=True)
            completed = [dict(row, Status=2, PaymentStatus=1) for row in initial]
            self.assertEqual(completed, self.bookings())
            owner.locator('main article').last.scroll_into_view_if_needed()
            self.record('desktop-completed', {'rows': completed, 'logout_login_verified': True,
                        'restart_verified': True, 'cross_owner_completion_blocked': True}, owner)

            self.go(outsider_customer, '/Booking/MyBookings')
            wrong_review = self.protected_post(outsider_customer, '/Booking/Review',
                                                {'BookingId': initial[0]['Id'], 'Rating': 5, 'Comment': COMMENT})
            self.assertIn(wrong_review.status, (200, 302))
            self.assertEqual([], self.rows('SELECT Id FROM Reviews'))
            self.go(customer, '/Booking/MyBookings')
            self.fits(customer)
            expect(customer.locator('main')).to_contain_text('Hoàn thành', ignore_case=True)
            customer.locator(f'main a[href="/Booking/Review/{initial[0]["Id"]}"]').click()
            customer.get_by_role('radio', name=re.compile('^5 sao')).click()
            expect(customer.locator('#Rating')).to_have_value('5')
            self.fill(customer, Comment=COMMENT)
            review_action = urlsplit(customer.locator('main form:has(#Rating)').get_attribute('action')).path
            self.assertIn(review_action.lower(), ('/booking/review', f'/booking/review/{initial[0]["Id"]}'))
            self.submit(customer, review_action, 'Gửi đánh giá')
            saved = self.rows('SELECT BookingId, CourtId, CustomerId, Rating, Comment, IsVisible FROM Reviews')
            self.assertEqual([{'BookingId': initial[0]['Id'], 'CourtId': court_id, 'CustomerId': customer_user['Id'],
                               'Rating': 5, 'Comment': COMMENT, 'IsVisible': True}], saved)
            self.go(customer, f'/Court/Detail/{court_id}#courtReviews')
            expect(customer.locator('#courtReviews')).to_contain_text(COMMENT)
            expect(customer.locator('#courtReviews')).to_contain_text('1 đánh giá')
            self.fits(customer)
            customer.locator('#courtReviews').get_by_text(COMMENT, exact=False).last.scroll_into_view_if_needed()
            self.record('mobile-public-review', {'review': saved[0], 'cross_customer_review_blocked': True}, customer)
            admin.context.clear_cookies()
            self.login(admin, 'admin', '/Admin')
            reply = self.owner_reply(admin, owner, outsider_owner, customer, court_id)
            qr_code, qr_day, qr_bookings = self.book_and_receive_qr(owner, customer, court_id)
            self.revenue(owner, self.before.date(), court_id, sum(prices))
            qr_amount = qr_bookings[0]['TotalAmount']
            qr_revenue_path = self.revenue(owner, self.day, court_id, qr_amount)
            self.go(outsider_owner, qr_revenue_path)
            expect(outsider_owner.locator('main')).to_contain_text('không thuộc phạm vi báo cáo')
            self.assertEqual(0, outsider_owner.locator('#received').count())
            self.go(owner, qr_revenue_path)
            self.record('owner-revenue', {'counter_received': sum(prices), 'qr_received': qr_amount,
                                         'cross_owner_court_blocked': True}, owner)
            self.go(admin, '/Admin')
            same_month = self.before.date().replace(day=1) == self.day.replace(day=1)
            admin_net = int(qr_amount + (sum(prices) if same_month else 0))
            expect(admin.locator('#monthNetRevenue')).to_contain_text(f'{admin_net:,}'.replace(',', '.'))

            self.stop()
            self.run_app(datetime.combine(qr_day, time(20, 1), VN))
            for page, alias, path in ((owner, 'owner', '/Owner'), (customer, 'customer', '/')):
                page.context.clear_cookies()
                self.login(page, alias, path)
            self.go(owner, '/Owner/Bookings?' + urlencode({'Keyword': qr_code}))
            self.submit(owner, '/Owner/Bookings/UpdateStatus', 'Đánh dấu hoàn thành')
            completed += [dict(row, Status=2) for row in qr_bookings]
            self.assertEqual(completed, self.bookings())
            payments = self.rows('SELECT BookingId, Method, Amount, Status FROM Payments ORDER BY BookingId')
            self.assertEqual((3, qr_amount, 1), (payments[-1]['Method'], payments[-1]['Amount'], payments[-1]['Status']))
            release = os.environ.get('SB09_VIEWPORT_DLL')
            if release:
                self.stop()
                regression.APP_DLL = Path(release)
                self.base, _ = self.start()
                self.assertEqual(completed, self.bookings())
                self.assertEqual(payments, self.rows('SELECT BookingId, Method, Amount, Status FROM Payments ORDER BY BookingId'))
                self.assertEqual(saved, self.rows('SELECT BookingId, CourtId, CustomerId, Rating, Comment, IsVisible FROM Reviews'))
                self.assertEqual(reply, self.rows('SELECT OwnerReply, OwnerReplyAt FROM Reviews')[0])
                self.record('release-restart', {'bookings': len(completed), 'payments': len(payments), 'reviews': len(saved)})
            for page, alias, path in ((admin, 'admin', '/Admin'), (owner, 'owner', '/Owner'),
                                      (customer, 'customer', '/')):
                page.context.clear_cookies()
                self.login(page, alias, path)
            self.check_role_viewports(admin, owner, customer, court_id, confirmation_path, owner_bookings)
            self.revenue(owner, self.day, court_id, qr_amount)
            self.assertEqual(0, self.rows('SELECT COUNT(*) AS N FROM DemoSeedStates')[0]['N'])
            self.assertEqual([], self.page_errors)
            browser.close()
        self.flow_passed = True


if __name__ == '__main__':
    regression.APP_DLL = ROOT / 'bin/SB09/net10.0/SportBooking.dll'
    unittest.main(argv=[__file__, 'CleanDatabaseJourney.test_clean_database_journey'], verbosity=2)
