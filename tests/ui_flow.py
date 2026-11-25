"""Real-browser QA using the existing disposable LocalDB regression harness.

Requires Playwright and Microsoft Edge; no real payment or SMTP is used.
Run from the project root: python -X utf8 tests/ui_flow.py
"""
import asyncio
from datetime import datetime, timedelta, timezone
import json
import os
from pathlib import Path
from urllib.parse import urlencode, urlparse, parse_qs

from playwright.async_api import async_playwright, expect
from regression import Regression

OUT = Path(os.environ.get('SPORTBOOKING_UI_EVIDENCE_DIR',
                          Path(__file__).resolve().parents[1] / 'docs/ui-review'))
SHOTS = OUT / 'screenshots'
RESULTS = []


def passed(name):
    RESULTS.append(name)
    print('PASS:', name, flush=True)


def sql(query):
    # Regression generates this name and refuses cleanup outside its namespace.
    assert Regression.database.startswith('SportBookingRegression_')
    return Regression.sql(query)


async def run():
    SHOTS.mkdir(parents=True, exist_ok=True)
    Regression.setUpClass()
    base = Regression.base
    today = datetime.now(timezone(timedelta(hours=7))).date()
    date = str(today + timedelta(days=9))
    other_date = str(today + timedelta(days=10))
    console_errors = []
    try:
        async with async_playwright() as p:
            browser = await p.chromium.launch(channel='msedge', headless=True)
            context = await browser.new_context(viewport={'width': 1440, 'height': 1000}, locale='vi-VN', timezone_id='Asia/Ho_Chi_Minh')
            page = await context.new_page()
            page.on('pageerror', lambda error: console_errors.append(str(error)))

            async def go(path):
                response = await page.goto(base + path)
                assert response.status == 200, (path, response.status)
                await page.wait_for_load_state('networkidle')

            async def fits(label):
                size = await page.evaluate('({width:innerWidth,scroll:document.documentElement.scrollWidth})')
                assert size['scroll'] <= size['width'], (label, size)

            async def shot(**kwargs):
                # Full-page captures should show the sticky header at the top,
                # regardless of controls scrolled into view by prior actions.
                await page.evaluate('window.scrollTo(0, 0)')
                await page.mouse.move(0, 0)
                await page.screenshot(**kwargs)

            async def booking(day=date):
                await go('/Booking/Create?' + urlencode({'courtId': 1, 'bookingDate': day}))
                await expect(page.locator('#bookingForm')).to_be_visible()

            async def select_slot(index):
                item = page.locator('#timeSlotGrid input[type=checkbox]:not(:disabled)').nth(index)
                await item.check()
                return item

            # Filters and result counts use the real controller, service and SQL data.
            await go('/Court/Search')
            await page.locator('#Province').select_option(label='TP. Hồ Chí Minh')
            await expect(page.locator('#District option')).not_to_have_count(1)
            districts = await page.locator('#District option').all_text_contents()
            assert 'Quận 5' in districts, districts
            await page.locator('#District').select_option(label='Quận 5')
            await page.locator('#BookingDate').fill(date)
            await page.locator('.search-filter-form button[type=submit]').click()
            await page.wait_for_load_state('networkidle')
            assert parse_qs(urlparse(page.url).query)['District'] == ['Quận 5']
            assert await page.locator('.court-card').count() > 0
            passed('Bộ lọc tỉnh/quận/ngày và cascade dữ liệu thật')

            await go('/Court/Search?' + urlencode({'BookingDate': date, 'PageSize': 2, 'SortBy': 'price_asc'}))
            await page.locator('.pagination a').filter(has_text='2').click()
            await page.wait_for_load_state('networkidle')
            query = parse_qs(urlparse(page.url).query)
            assert query['Page'] == ['2'] and query['BookingDate'] == [date] and query['SortBy'] == ['price_asc']
            search_url = page.url
            await page.locator('.court-name a').first.click()
            await page.wait_for_load_state('networkidle')
            assert await page.locator('#quickDate').input_value() == date
            await page.locator('.back-to-results').click()
            await page.wait_for_load_state('networkidle')
            assert page.url == search_url
            passed('Phân trang và quay lại giữ ngày, sắp xếp, số trang')

            # Selected district must survive slow/failed initial cascade loading.
            async def delayed_district(route):
                await asyncio.sleep(.7)
                await route.abort()
            await page.route('**/Court/GetDistricts?*', delayed_district)
            await page.goto(base + '/Court/Search?' + urlencode({'Province': 'TP. Hồ Chí Minh', 'District': 'Quận 5'}))
            assert await page.locator('#District').input_value() == 'Quận 5'
            assert not await page.locator('#District').is_disabled()
            await expect(page.locator('#districtFeedback')).to_be_visible()
            assert await page.locator('#District').input_value() == 'Quận 5'
            await page.unroute('**/Court/GetDistricts?*', delayed_district)
            await page.locator('#districtFeedback button').click()
            await expect(page.locator('#District')).not_to_have_attribute('aria-busy', 'true')
            await expect(page.locator('#District option')).not_to_have_count(2)
            assert await page.locator('#District').input_value() == 'Quận 5'
            passed('Quận đã lọc được giữ khi AJAX chậm/lỗi và thử lại thành công')

            await go('/Court/Search?' + urlencode({'Keyword': 'KhôngCóKếtQuả' * 20}))
            await page.set_viewport_size({'width': 375, 'height': 900})
            await fits('long filter chip')
            await expect(page.locator('.search-empty')).to_be_visible()
            await shot(path=str(SHOTS / 'empty-long-filter-375.png'), full_page=True)
            passed('Danh sách rỗng và nhãn lọc dài ở 375px')

            # Guest authentication follows the actual booking URL including date.
            await go('/Court/Detail/1?' + urlencode({'bookingDate': date, 'returnUrl': '/Court/Search?Page=2'}))
            await page.locator('.detail-booking-card button[type=submit]').click()
            await page.wait_for_load_state('networkidle')
            assert '/Account/Login' in page.url
            await page.locator('#Email').fill('customer1@sportbooking.vn')
            await page.locator('input[name=Password]').fill('Sport@123')
            await page.locator('.auth-card button[type=submit]').click()
            await page.wait_for_load_state('networkidle')
            assert '/Booking/Create' in page.url
            assert await page.locator('#BookingDate').input_value() == date
            passed('Khách chưa đăng nhập trở lại đúng sân/ngày sau đăng nhập')

            # Responsive, font, primary target and keyboard checks.
            for width in (375, 768, 1440):
                await page.set_viewport_size({'width': width, 'height': 1000 if width == 1440 else 900})
                for name, path in [('search', '/Court/Search'), ('detail', '/Court/Detail/1'), ('booking', '/Booking/Create?courtId=1&bookingDate=' + date)]:
                    await go(path)
                    if name == 'booking':
                        await select_slot(0)
                        await expect(page.locator('.booking-summary')).to_be_visible()
                        assert await page.locator('#totalAmountSelected').input_value() != '0'
                    await fits(f'{name} {width}')
                    assert await page.locator('h1').count() == 1
                    assert await page.evaluate("document.fonts.check('16px \"Be Vietnam Pro\"', 'Đặt sân thể thao ă â ê ô ơ ư đ Ắ Ễ Ộ Ữ')")
                    bad = await page.locator('main button:visible, main .form-control:visible, main .form-select:visible, main a.btn:visible').evaluate_all('(els) => els.filter(el=>el.getBoundingClientRect().height<43.9).map(el=>({text:el.textContent,h:el.getBoundingClientRect().height}))')
                    assert not bad, (name, width, bad)
                    await shot(path=str(SHOTS / f'{name}-{width}.png'), full_page=True)
            passed('375/768/1440px: không tràn ngang, font tiếng Việt, H1 và control chính ≥44px')

            await page.set_viewport_size({'width': 375, 'height': 900})
            await go('/Court/Search')
            await expect(page.locator('#courtFilters')).not_to_be_visible()
            await page.locator('.filter-toggle').click()
            await expect(page.locator('#courtFilters')).to_be_visible()
            await page.locator('.navbar-toggler').click()
            await expect(page.locator('#navbarMain')).to_be_visible()
            await page.locator('.navbar-toggler').click()
            passed('Bộ lọc thu gọn và menu responsive thao tác được bằng nút')

            await booking()
            await page.locator('#bookingSubmit').click()
            await expect(page.locator('#bookingErrors')).to_be_focused()
            await expect(page.locator('#slotError')).to_contain_text('ít nhất')
            assert not await page.locator('#bookingSubmit').is_disabled()
            await page.locator('#bookingErrors a[href="#timeSlotGrid"]').click()
            await expect(page.locator('#timeSlotGrid')).to_be_focused()
            first = page.locator('#timeSlotGrid input[type=checkbox]:not(:disabled)').first
            await first.focus()
            await page.keyboard.press('Space')
            await expect(first).to_be_checked()
            outline = await first.locator('..').evaluate('(el)=>getComputedStyle(el).outlineWidth')
            assert outline == '3px', outline
            await page.keyboard.press('Space')
            await expect(first).not_to_be_checked()
            passed('Lỗi thiếu giờ có focus/link, sửa và gửi lại; checkbox dùng Space với focus rõ')

            await select_slot(0)
            await select_slot(2)
            await page.locator('#bookingSubmit').click()
            await expect(page.locator('#slotError')).to_contain_text('liền nhau')
            assert not await page.locator('#bookingSubmit').is_disabled()
            await page.locator('#timeSlotGrid input[type=checkbox]:not(:disabled)').nth(2).uncheck()
            await select_slot(1)
            await expect(page.locator('#slotError')).to_be_empty()
            for i in range(2, 6):
                await select_slot(i)
            await page.locator('#timeSlotGrid input[type=checkbox]:not(:disabled)').nth(6).locator('..').click()
            assert await page.locator('#timeSlotGrid input:checked').count() == 6
            await expect(page.locator('#slotError')).to_contain_text('tối đa 6')
            passed('Khung rời bị chặn, sửa được; không chọn quá 6 khung')

            await booking()
            chosen = await select_slot(0)
            await chosen.locator('..').hover()
            colors = await chosen.locator('..').evaluate('(el)=>({bg:getComputedStyle(el).backgroundColor, fg:getComputedStyle(el.querySelector(".timeslot-meta")).color})')
            from sports_theme import contrast
            assert contrast(colors['fg'], colors['bg']) >= 4.5, colors
            passed('Khung đang chọn giữ tương phản khi hover')

            # Race uses delayed *real server responses*, not invented availability.
            async def delayed_slots(route):
                response = await route.fetch()
                if parse_qs(urlparse(route.request.url).query).get('date') == [date]:
                    await asyncio.sleep(.6)
                try:
                    await route.fulfill(response=response)
                except Exception:
                    pass  # The old XHR is expected to be aborted.
            await page.route('**/Booking/GetTimeSlots?*', delayed_slots)
            await page.locator('#BookingDate').fill(date)
            await page.locator('#BookingDate').dispatch_event('change')
            await page.locator('#BookingDate').fill(other_date)
            await page.locator('#BookingDate').dispatch_event('change')
            await expect(page.locator('#timeSlotGrid')).to_have_attribute('aria-busy', 'false')
            assert await page.locator('#totalAmountSelected').input_value() == '0'
            quotes = await page.locator('#timeSlotGrid input[name^=SlotQuotes]').evaluate_all('(els)=>els.map(x=>x.value)')
            assert quotes and all(other_date.replace('-', '') in quote for quote in quotes), quotes
            await page.unroute('**/Booking/GetTimeSlots?*', delayed_slots)
            passed('Đổi ngày nhanh: bỏ lựa chọn cũ và chỉ nhận báo giá của ngày mới nhất')

            async def offline(route):
                await route.abort()
            await page.route('**/Booking/GetTimeSlots?*', offline)
            await page.locator('#BookingDate').fill(date)
            await page.locator('#BookingDate').dispatch_event('change')
            await expect(page.locator('#retryTimeSlots')).to_be_visible()
            assert await page.locator('#bookingSubmit').is_disabled()
            assert await page.locator('#timeSlotGrid input').count() == 0
            await page.unroute('**/Booking/GetTimeSlots?*', offline)
            await page.locator('#retryTimeSlots').click()
            await expect(page.locator('#timeSlotGrid input[type=checkbox]')).not_to_have_count(0)
            await expect(page.locator('#bookingSubmit')).to_be_enabled()
            passed('Lỗi AJAX: không gửi lựa chọn cũ, có thử lại và phục hồi')

            # Actual availability states: seed has booked, current day has past,
            # maintenance is taken from a maintained court in the test database.
            booked_day = Regression.rows(f"SELECT TOP 1 CONVERT(varchar(10),BookingDate,23) AS d FROM Bookings WHERE CourtId=1 AND Status=1 AND BookingDate>='{today}' ORDER BY BookingDate")[0]['d']
            await booking(booked_day)
            await expect(page.locator('.timeslot-booked .timeslot-meta').first).to_have_text('Đã đặt')
            await booking(str(today))
            if await page.locator('.timeslot-past').count():
                await expect(page.locator('.timeslot-past .timeslot-meta').first).to_have_text('Đã qua')
            maintained = Regression.rows('SELECT TOP 1 Id FROM Courts WHERE Status=1')[0]['Id']
            await go(f'/Court/Detail/{maintained}')
            await expect(page.locator('.detail-booking-card')).to_contain_text('bảo trì')
            assert await page.locator('.detail-booking-card button').count() == 0
            await go(f'/Booking/GetTimeSlots?courtId={maintained}&date={date}')
            maintenance_data = json.loads(await page.locator('body').inner_text())
            if maintenance_data.get('success') and maintenance_data.get('slots'):
                assert all(s['status'] == 2 for s in maintenance_data['slots'])
            passed('Trạng thái đã đặt/đã qua và sân bảo trì phản ánh backend')

            # Real SQL price edit between viewing and submitting must require a
            # separate confirmation and must not create any reservation yet.
            await booking()
            slot = await select_slot(0)
            slot_id = int(await slot.input_value())
            old_price = int(await page.locator('#totalAmountSelected').input_value())
            before = Regression.rows(f"SELECT COUNT(*) AS n FROM Bookings WHERE CourtId=1 AND BookingDate='{date}'")[0]['n']
            sql(f'UPDATE TimeSlots SET PriceWeekday=PriceWeekday+12345, PriceWeekend=PriceWeekend+12345 WHERE Id={slot_id}')
            await page.locator('#bookingSubmit').click()
            await page.wait_for_load_state('networkidle')
            await expect(page.locator('#bookingErrors')).to_contain_text('thay đổi')
            await expect(page.locator('#bookingErrors')).to_be_focused()
            assert int(await page.locator('#totalAmountSelected').input_value()) == old_price + 12345
            after = Regression.rows(f"SELECT COUNT(*) AS n FROM Bookings WHERE CourtId=1 AND BookingDate='{date}'")[0]['n']
            assert before == after
            await shot(path=str(SHOTS / 'price-reconfirm-375.png'), full_page=True)
            # Check error does not vanish while the customer reads it.
            await page.wait_for_timeout(6100)
            await expect(page.locator('#bookingErrors')).to_be_visible()
            passed('Đổi giá: server không tạo booking, cập nhật tổng mới và yêu cầu xác nhận lại; lỗi không tự ẩn')

            # A valid second submission creates two consecutive bookings. Delay
            # the real POST response so the loading state can be asserted.
            await select_slot(1)
            expected_total = int(await page.locator('#totalAmountSelected').input_value())
            note = 'UI browser QA: hai khung liền nhau'
            await page.locator('#Notes').fill(note)
            post_count = []
            submitted = asyncio.Event()
            release_response = asyncio.Event()
            loading_state = asyncio.get_running_loop().create_future()
            def capture_loading(message):
                if message.text.startswith('UI_SUBMIT_STATE ') and not loading_state.done():
                    loading_state.set_result(json.loads(message.text[len('UI_SUBMIT_STATE '):]))
            page.on('console', capture_loading)
            # Observe DOM during submission without Playwright locator navigation
            # auto-waits, which intentionally wait for the held POST response.
            await page.locator('#bookingForm').evaluate("""form=>form.addEventListener('submit',()=>setTimeout(()=>{
                const button=document.getElementById('bookingSubmit');
                console.log('UI_SUBMIT_STATE '+JSON.stringify({disabled:button.disabled,text:button.textContent}));
                form.dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}));
            },20),{once:true})""")
            async def slow_submit(route):
                if route.request.method != 'POST':
                    await route.continue_(); return
                post_count.append(1)
                response = await route.fetch(max_redirects=0)
                submitted.set()
                await release_response.wait()
                await route.fulfill(response=response)
            await page.route('**/Booking/Create*', slow_submit)
            clicking = asyncio.create_task(page.locator('#bookingSubmit').click())
            await asyncio.wait_for(submitted.wait(), timeout=20)
            try:
                state = await asyncio.wait_for(loading_state, timeout=5)
                assert state['disabled'] and 'Đang xử lý' in state['text'], state
            finally:
                release_response.set()
            await clicking
            page.remove_listener('console', capture_loading)
            await page.wait_for_url('**/Booking/Confirmation?*')
            await page.wait_for_load_state('networkidle')
            assert len(post_count) == 1
            saved = Regression.rows("SELECT TotalAmount FROM Bookings WHERE Note=N'UI browser QA: hai khung liền nhau'")
            assert len(saved) == 2 and sum(int(row['TotalAmount']) for row in saved) == expected_total, saved
            await shot(path=str(SHOTS / 'confirmation-375.png'), full_page=True)
            await page.unroute('**/Booking/Create*', slow_submit)
            passed('Gửi tại quầy thành công: 2 booking liền nhau, tổng snapshot đúng, loading và chỉ 1 POST')

            # Long real SQL data, maximum supported price, and image dimensions.
            original = Regression.rows('SELECT Name, ImageUrl FROM Courts WHERE Id=1')[0]
            long_name = ('Sân thể thao ' + ('ĐườngDẫnDàiKhôngCóKhoảngTrắng' * 5))[:150]
            sql("UPDATE Courts SET Name=N'" + long_name + "', ImageUrl='/images/hero-pattern.svg' WHERE Id=1")
            prices = Regression.rows('SELECT Id, PriceWeekday, PriceWeekend FROM TimeSlots WHERE CourtId=1')
            sql('UPDATE TimeSlots SET PriceWeekday=999999999999, PriceWeekend=999999999999 WHERE CourtId=1')
            try:
                for width in (375, 768, 1440):
                    await page.set_viewport_size({'width': width, 'height': 900})
                    await go('/Court/Search?' + urlencode({'Keyword': long_name}))
                    await fits('long search')
                    img = page.locator('.court-card img').first
                    assert await img.evaluate('(el)=>el.complete && el.naturalWidth>0')
                    assert await img.get_attribute('alt') == long_name
                    await go('/Court/Detail/1')
                    await fits('long detail and maximum prices')
                    await booking(other_date)
                    await select_slot(0)
                    await fits('long booking')
                    assert await page.locator('#totalAmountSelected').input_value() == '999999999999'
                await shot(path=str(SHOTS / 'long-content-1440.png'), full_page=True)
            finally:
                sql("UPDATE Courts SET Name=N'" + original['Name'].replace("'", "''") + "', ImageUrl=" + ("N'" + original['ImageUrl'].replace("'", "''") + "'" if original.get('ImageUrl') else 'NULL') + ' WHERE Id=1')
                for row in prices:
                    sql(f"UPDATE TimeSlots SET PriceWeekday={row['PriceWeekday']}, PriceWeekend={row['PriceWeekend']} WHERE Id={row['Id']}")
            passed('Dữ liệu SQL dài, giá tối đa và ảnh thật từ URL ứng dụng không gây tràn ở 3 kích thước')

            # Reduced motion, landscape, zoom-equivalent increased text size.
            await page.emulate_media(reduced_motion='reduce')
            await page.set_viewport_size({'width': 812, 'height': 375})
            await booking(other_date)
            await fits('landscape')
            await page.locator('html').evaluate("el=>el.style.fontSize='200%'")
            await fits('text zoom 200%')
            await page.locator('html').evaluate("el=>el.style.fontSize=''")
            duration = await page.locator('#bookingSubmit').evaluate('(el)=>getComputedStyle(el).transitionDuration')
            assert all(float(part.strip().rstrip('s')) == 0 for part in duration.split(',')), duration
            await page.locator('#Notes').focus()
            rect = await page.locator('#Notes').bounding_box()
            header = await page.locator('nav.navbar').bounding_box()
            assert rect['y'] >= header['height'] and rect['y'] + rect['height'] <= 375, rect
            passed('Landscape, chữ 200%, reduced motion và focus không bị header che')

            assert not console_errors, console_errors
            passed('Không có lỗi JavaScript trong toàn bộ luồng')
            await browser.close()
    finally:
        (OUT / 'browser-results.json').write_text(json.dumps({'passed': RESULTS, 'page_errors': console_errors}, ensure_ascii=False, indent=2), encoding='utf-8')
        Regression.doClassCleanups()


if __name__ == '__main__':
    asyncio.run(run())
