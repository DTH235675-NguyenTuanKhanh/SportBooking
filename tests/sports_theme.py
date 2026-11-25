"""Dark sports theme / background / colorful picker browser acceptance checks."""
import asyncio
import json
import os
from datetime import datetime, timedelta, timezone
from pathlib import Path

from playwright.async_api import async_playwright, expect
from regression import ROOT, Regression

OUT = Path(os.environ.get('SPORTBOOKING_UI_EVIDENCE_DIR',
                          Path(__file__).resolve().parents[1] / 'docs/ui-review')) / 'sports-theme'
RESULTS = []


def passed(message):
    RESULTS.append(message)
    print('PASS:', message, flush=True)


def contrast(foreground, background):
    def lum(color):
        parts = [int(c) / 255 for c in color.removeprefix('rgb(').removesuffix(')').split(',')]
        linear = [c / 12.92 if c <= .04045 else ((c + .055) / 1.055) ** 2.4 for c in parts]
        return sum(c * w for c, w in zip(linear, (.2126, .7152, .0722)))
    lo, hi = sorted((lum(foreground), lum(background)))
    return (hi + .05) / (lo + .05)


async def run():
    OUT.mkdir(parents=True, exist_ok=True)
    Regression.setUpClass()
    errors = []
    try:
        async with async_playwright() as p:
            browser = await p.chromium.launch(channel='msedge', headless=True)
            context = await browser.new_context(viewport={'width': 1440, 'height': 1000}, locale='vi-VN', timezone_id='Asia/Ho_Chi_Minh')
            page = await context.new_page()
            page.on('pageerror', lambda error: errors.append(str(error)))

            async def go(path):
                response = await page.goto(Regression.base + path)
                assert response.status == 200
                await page.wait_for_load_state('networkidle')

            async def fits():
                assert await page.evaluate('document.documentElement.scrollWidth <= innerWidth')

            async def shot(name):
                await page.evaluate('window.scrollTo(0,0)')
                await page.mouse.move(0, 0)
                await page.screenshot(path=str(OUT / name), full_page=True)

            await go('/')
            await page.keyboard.press('Tab')
            await expect(page.locator('.skip-link')).to_be_focused()
            skip_colors = await page.locator('.skip-link').evaluate('(el)=>({fg:getComputedStyle(el).color,bg:getComputedStyle(el).backgroundColor})')
            assert contrast(skip_colors['fg'], skip_colors['bg']) >= 4.5
            # All six backdrops and sport logos are served as local WebP images.
            expected_logos = set()
            for sport in ('football', 'badminton', 'tennis', 'basketball', 'pickleball', 'volleyball'):
                logo_path = f'/images/sports/icons/{sport}.webp'
                expected_logos.add(logo_path)
                for path in (f'/images/sports/{sport}.webp', logo_path):
                    response = await context.request.get(Regression.base + path)
                    assert response.status == 200 and response.headers['content-type'] == 'image/webp'
                    assert (ROOT / 'wwwroot' / path.lstrip('/')).stat().st_size < 150000
            logos = await page.locator('.sports-selection .sport-logo').evaluate_all('(els)=>els.map(x=>x.getAttribute("src"))')
            assert len(logos) == 6 and set(logos) == expected_logos
            passed('6 ảnh nền và 6 logo WebP riêng, mỗi ảnh dưới 150 KB')

            # Native details / buttons preserve SportTypeId and sport-specific backdrop.
            await page.locator('#sportPickerButton').click()
            await expect(page.locator('.sport-picker-options')).to_be_visible()
            await shot('picker-1440.png')
            tennis = page.locator('.sport-picker-option[data-sport-key=tennis]')
            selected_id = await tennis.get_attribute('data-sport-value')
            await tennis.click()
            assert await page.locator('#SportTypeId').input_value() == selected_id
            await expect(page.locator('#selectedSportName')).to_have_text('Tennis')
            await expect(page.locator('body')).to_have_attribute('data-backdrop-sport', 'tennis')
            await page.locator('.hero-search-button').click()
            await page.wait_for_load_state('networkidle')
            assert 'SportTypeId=' + selected_id in page.url
            await expect(page.locator('#selectedSportName')).to_have_text('Tennis')
            for sport_name in await page.locator('.court-card-topline .chip-sport').all_text_contents():
                assert 'Tennis' in sport_name
            passed('Chọn môn gửi đúng SportTypeId, lọc thật và đổi nền đúng môn')

            await go('/')
            summary = page.locator('#sportPickerButton')
            await summary.focus()
            await page.keyboard.press('ArrowDown')
            await expect(page.locator('.sport-picker-option').first).to_be_focused()
            await page.keyboard.press('ArrowDown')
            await expect(page.locator('.sport-picker-option').nth(1)).to_be_focused()
            await page.keyboard.press('End')
            await expect(page.locator('.sport-picker-option').last).to_be_focused()
            await page.keyboard.press('Escape')
            await expect(summary).to_be_focused()
            assert not await page.locator('.sport-picker').evaluate('(el)=>el.open')
            passed('Picker hỗ trợ Arrow/Home/End/Escape và trả focus')

            # Avoid hovering/focusing the toolbar for automatic advancement.
            await page.locator('.hero-title').click()
            await page.mouse.move(0, 600)
            previous = await page.locator('body').get_attribute('data-backdrop-sport')
            await page.wait_for_function('(sport)=>document.body.dataset.backdropSport!==sport', arg=previous, timeout=11000)
            assert await page.locator('.backdrop-image.is-active').count() == 1
            passed('Nền tự chuyển ảnh theo vòng, luôn có một lớp ảnh đang chọn')

            await page.locator('[data-backdrop-pause]').click()
            await expect(page.locator('[data-backdrop-pause]')).to_have_attribute('aria-pressed', 'true')
            frozen = await page.locator('body').get_attribute('data-backdrop-sport')
            await page.locator('.hero-title').click()
            await page.mouse.move(0, 600)
            await page.wait_for_timeout(7600)
            assert await page.locator('body').get_attribute('data-backdrop-sport') == frozen
            await page.locator('[data-backdrop-next]').click()
            await page.wait_for_function('(sport)=>document.body.dataset.backdropSport!==sport', arg=frozen)
            assert await page.locator('[data-sports-backdrop]').get_attribute('data-paused') == 'true'
            seen = {await page.locator('body').get_attribute('data-backdrop-sport')}
            for _ in range(6):
                current_sport = await page.locator('body').get_attribute('data-backdrop-sport')
                await page.locator('[data-backdrop-next]').click()
                await page.wait_for_function('(sport)=>document.body.dataset.backdropSport!==sport', arg=current_sport)
                seen.add(await page.locator('body').get_attribute('data-backdrop-sport'))
            assert len(seen) == 6
            passed('Tạm dừng giữ ảnh; nút trước/sau vẫn đổi được bằng tay')

            # Reduced motion never automatically starts, but manual navigation works.
            await page.emulate_media(reduced_motion='reduce')
            await page.reload()
            await page.wait_for_load_state('networkidle')
            await expect(page.locator('[data-backdrop-pause]')).to_be_disabled()
            before = await page.locator('body').get_attribute('data-backdrop-sport')
            await page.wait_for_timeout(7600)
            assert before == await page.locator('body').get_attribute('data-backdrop-sport')
            await page.locator('[data-backdrop-next]').click()
            await page.wait_for_function('(sport)=>document.body.dataset.backdropSport!==sport', arg=before)
            assert await page.locator('.backdrop-image').first.evaluate('(el)=>getComputedStyle(el).transitionDuration') == '0s'
            passed('Reduced motion: không tự chạy, không fade, nút thủ công hoạt động')
            await page.emulate_media(reduced_motion='no-preference')

            race_context = await browser.new_context()
            race = await race_context.new_page()
            requested, release = asyncio.Event(), asyncio.Event()
            async def slow_photo(route):
                requested.set()
                await release.wait()
                await route.continue_()
            await race.route('**/images/sports/badminton.webp', slow_photo)
            await race.goto(Regression.base + '/')
            await race.wait_for_load_state('networkidle')
            await asyncio.wait_for(requested.wait(), timeout=11)
            await race.locator('[data-backdrop-pause]').click()
            release.set()
            await race.wait_for_load_state('networkidle')
            assert await race.locator('body').get_attribute('data-backdrop-sport') == 'football'
            await race.locator('[data-backdrop-next]').click()
            await expect(race.locator('body')).to_have_attribute('data-backdrop-sport', 'badminton')
            await race_context.close()
            passed('Ảnh tải chậm: bấm tạm dừng chặn chuyển cảnh đang chờ; chuyển tay vẫn hoạt động')

            # Responsive screens include the existing actual booking form.
            await go('/Account/Login')
            await page.locator('#Email').fill('customer1@sportbooking.vn')
            await page.locator('input[name=Password]').fill('Sport@123')
            await page.locator('.auth-card button[type=submit]').click()
            await page.wait_for_load_state('networkidle')
            date = str(datetime.now(timezone(timedelta(hours=7))).date() + timedelta(days=8))
            for width in (375, 768, 1440):
                await page.set_viewport_size({'width': width, 'height': 1000 if width == 1440 else 900})
                for name, path in (('home', '/'), ('search', '/Court/Search'), ('detail', '/Court/Detail/1'), ('booking', '/Booking/Create?courtId=1&bookingDate=' + date)):
                    await go(path)
                    if name == 'booking':
                        await page.locator('#timeSlotGrid input[type=checkbox]:not(:disabled)').first.check()
                    await fits()
                    assert await page.locator('main h1').count() == 1
                    await shot(name + '-' + str(width) + '.png')
                await go('/')
                await page.locator('#sportPickerButton').click()
                await fits()
                boxes = await page.locator('.sport-picker-option').evaluate_all('(els)=>els.map(x=>x.getBoundingClientRect().height)')
                assert min(boxes) >= 44
                await shot('picker-' + str(width) + '.png')
            passed('Home/tìm/chi tiết/đặt + picker vừa 375/768/1440, option ≥44px')

            await go('/Booking/Create?courtId=1&bookingDate=' + date)
            checkbox = page.locator('#timeSlotGrid input[type=checkbox]:not(:disabled)').first
            await checkbox.check()
            await checkbox.locator('..').hover()
            colors = await checkbox.locator('..').evaluate('(el)=>({bg:getComputedStyle(el).backgroundColor,fg:getComputedStyle(el.querySelector(".timeslot-meta")).color})')
            assert contrast(colors['fg'], colors['bg']) >= 4.5, colors
            await page.locator('#bookingSubmit').click()
            await page.wait_for_url('**/Booking/Confirmation?*')
            await page.wait_for_load_state('networkidle')
            assert 'Đặt sân thành công' in await page.locator('h1').inner_text()
            passed('Khung đang chọn đạt 4.5:1 trên theme tối; gửi booking tại quầy thành công')

            # Enhancement failure/no JavaScript preserves a usable native select.
            plain = await browser.new_context(java_script_enabled=False, viewport={'width': 375, 'height': 900})
            nojs = await plain.new_page()
            await nojs.goto(Regression.base + '/')
            await expect(nojs.locator('#SportTypeId')).to_be_visible()
            await nojs.locator('#SportTypeId').select_option(value=selected_id)
            await nojs.locator('.hero-search-button').click()
            assert 'SportTypeId=' + selected_id in nojs.url
            passed('Không JavaScript: select gốc vẫn hiển thị và gửi form được')
            await plain.close()
            assert not errors, errors
            passed('Không có lỗi JavaScript')
            await browser.close()
    finally:
        (OUT / 'results.json').write_text(json.dumps({'passed': RESULTS, 'page_errors': errors}, ensure_ascii=False, indent=2), encoding='utf-8')
        Regression.doClassCleanups()


if __name__ == '__main__':
    asyncio.run(run())
