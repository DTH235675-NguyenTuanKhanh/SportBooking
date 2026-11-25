/* Presentation only: native SportTypeId remains the submitted form field. */
(function () {
    'use strict';

    function initSportsBackdrop() {
        const toolbar = document.querySelector('[data-sports-backdrop]');
        const layers = Array.from(document.querySelectorAll('.backdrop-image'));
        if (!toolbar || layers.length !== 2) return null;

        const header = document.querySelector('.sports-theme > .navbar');
        if (header) {
            const syncBackdropTop = function () {
                document.body.style.setProperty('--backdrop-header-height', header.getBoundingClientRect().height + 'px');
            };
            syncBackdropTop();
            new ResizeObserver(syncBackdropTop).observe(header);
        }

        const sports = [
            { key: 'football', name: 'Bóng đá', athlete: 'Kylian Mbappé' },
            { key: 'badminton', name: 'Cầu lông', athlete: 'An Se Young' },
            { key: 'tennis', name: 'Tennis', athlete: 'Alexander Zverev' },
            { key: 'basketball', name: 'Bóng rổ', athlete: 'Shai Gilgeous-Alexander' },
            { key: 'pickleball', name: 'Pickleball', athlete: 'Hunter Johnson' },
            { key: 'volleyball', name: 'Bóng chuyền', athlete: 'Yūki Ishikawa' }
        ];
        const reduced = window.matchMedia('(prefers-reduced-motion: reduce)');
        const pause = toolbar.querySelector('[data-backdrop-pause]');
        const name = document.getElementById('backdropSportName');
        const athlete = document.getElementById('backdropAthleteName');
        const announcement = document.getElementById('backdropAnnouncement');
        let paused = false, hovered = false, current = 0, displayed = 0, activeLayer = 0, requestVersion = 0, timer;
        const loaded = new Map();
        function read(key) { try { return sessionStorage.getItem(key); } catch (_) { return null; } }
        function save(key, value) { try { sessionStorage.setItem(key, value); } catch (_) { /* Storage may be unavailable. */ } }
        paused = read('sportBackdropPaused') === 'true';

        function paintPause() {
            const stopped = paused || reduced.matches;
            pause.setAttribute('aria-pressed', String(stopped));
            pause.disabled = reduced.matches;
            const label = reduced.matches ? 'Tự động chuyển ảnh đã tắt theo tùy chọn giảm chuyển động'
                : paused ? 'Tiếp tục chuyển ảnh nền' : 'Tạm dừng chuyển ảnh nền';
            pause.setAttribute('aria-label', label);
            pause.title = label;
            pause.querySelector('i').className = stopped ? 'bi bi-play-fill' : 'bi bi-pause-fill';
            toolbar.dataset.paused = String(stopped);
        }
        function schedule() {
            window.clearTimeout(timer);
            paintPause();
            if (!paused && !reduced.matches && !document.hidden && !hovered && !toolbar.contains(document.activeElement)) {
                timer = window.setTimeout(function () { move(current + 1, false); }, 7000);
            }
        }
        function preload(sport) {
            if (!loaded.has(sport.key)) {
                loaded.set(sport.key, new Promise(function (resolve, reject) {
                    const photo = new Image();
                    photo.onload = function () { resolve(photo.src); };
                    photo.onerror = function () { loaded.delete(sport.key); reject(new Error('Background unavailable')); };
                    photo.src = '/images/sports/' + sport.key + '.webp';
                }));
            }
            return loaded.get(sport.key);
        }
        async function move(index, manual) {
            current = (index + sports.length) % sports.length;
            const selected = sports[current];
            const version = ++requestVersion;
            window.clearTimeout(timer);
            try {
                const src = await preload(selected);
                if (version !== requestVersion) return;
                if (!manual && (paused || reduced.matches || document.hidden || hovered || toolbar.contains(document.activeElement))) { current = displayed; return; }
                const next = 1 - activeLayer;
                layers[next].src = src;
                if (layers[next].decode) await layers[next].decode();
                if (version !== requestVersion) return;
                if (!manual && (paused || reduced.matches || document.hidden || hovered || toolbar.contains(document.activeElement))) { current = displayed; return; }
                layers[next].classList.add('is-active');
                layers[activeLayer].classList.remove('is-active');
                activeLayer = next;
                displayed = current;
                name.textContent = selected.name;
                athlete.textContent = selected.athlete;
                toolbar.dataset.sport = selected.key;
                document.body.dataset.backdropSport = selected.key;
                save('sportBackdropKey', selected.key);
                if (manual) announcement.textContent = `${selected.name} – ${selected.athlete}.`;
            } catch (_) {
                if (manual && version === requestVersion) announcement.textContent = 'Chưa tải được ảnh nền. Bạn có thể chọn ảnh tiếp theo.';
            } finally {
                if (version === requestVersion) schedule();
            }
        }
        toolbar.querySelector('[data-backdrop-prev]').addEventListener('click', function () { move(current - 1, true); });
        toolbar.querySelector('[data-backdrop-next]').addEventListener('click', function () { move(current + 1, true); });
        pause.addEventListener('click', function () { paused = !paused; save('sportBackdropPaused', String(paused)); schedule(); });
        toolbar.addEventListener('mouseenter', function () { hovered = true; schedule(); });
        toolbar.addEventListener('mouseleave', function () { hovered = false; schedule(); });
        toolbar.addEventListener('focusin', schedule);
        toolbar.addEventListener('focusout', function () { window.setTimeout(schedule, 0); });
        document.addEventListener('visibilitychange', schedule);
        reduced.addEventListener('change', schedule);
        const stored = sports.findIndex(function (sport) { return sport.key === read('sportBackdropKey'); });
        toolbar.dataset.sport = 'football';
        document.body.dataset.backdropSport = 'football';
        if (stored > 0) move(stored, false); else schedule();
        return {
            selectSport: function (key) {
                const index = sports.findIndex(function (sport) { return sport.key === key; });
                if (index >= 0) move(index, true);
            }
        };
    }

    function initSportPickers(backdrop) {
        document.querySelectorAll('.sport-picker-field').forEach(function (field) {
            const select = field.querySelector('.sport-native');
            const details = field.querySelector('.sport-picker');
            const summary = details.querySelector('summary');
            const options = Array.from(details.querySelectorAll('[data-sport-value]'));
            options.forEach(function (option) {
                option.addEventListener('click', function () {
                    select.value = option.dataset.sportValue;
                    select.dispatchEvent(new Event('change', { bubbles: true }));
                    summary.querySelector('img').src = option.querySelector('img').src;
                    summary.querySelector('span').textContent = option.querySelector('span').textContent;
                    options.forEach(function (item) { item.setAttribute('aria-pressed', String(item === option)); });
                    details.open = false;
                    summary.focus();
                    if (backdrop) backdrop.selectSport(option.dataset.sportKey);
                });
                option.addEventListener('keydown', function (event) {
                    const position = options.indexOf(option);
                    let target;
                    if (event.key === 'ArrowDown') target = options[(position + 1) % options.length];
                    if (event.key === 'ArrowUp') target = options[(position + options.length - 1) % options.length];
                    if (event.key === 'Home') target = options[0];
                    if (event.key === 'End') target = options[options.length - 1];
                    if (target) { event.preventDefault(); target.focus(); }
                });
            });
            details.addEventListener('keydown', function (event) {
                if (event.key === 'Escape') { event.preventDefault(); details.open = false; summary.focus(); }
                if (event.target === summary && event.key === 'ArrowDown') {
                    event.preventDefault(); details.open = true;
                    (options.find(function (option) { return option.getAttribute('aria-pressed') === 'true'; }) || options[0]).focus();
                }
            });
            field.querySelector('label').addEventListener('click', function (event) { event.preventDefault(); summary.focus(); });
            document.addEventListener('click', function (event) { if (!field.contains(event.target)) details.open = false; });
            field.classList.add('is-enhanced');
            const initial = options.find(function (option) { return option.dataset.sportValue === select.value; });
            if (initial && select.value && backdrop) backdrop.selectSport(initial.dataset.sportKey);
        });
    }
    function init() { initSportPickers(initSportsBackdrop()); }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init); else init();
})();
