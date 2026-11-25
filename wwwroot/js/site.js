// File: wwwroot/js/site.js

function handleCourtPhotoError(photo) {
    const media = photo.parentElement;
    const label = media.querySelector('.court-photo-label');
    const fallbackUrl = photo.dataset.illustrationUrl;

    if (fallbackUrl) {
        delete photo.dataset.illustrationUrl;
        photo.alt = photo.dataset.illustrationAlt;
        label.hidden = false;
        photo.src = fallbackUrl;
        return;
    }

    photo.hidden = true;
    label.hidden = true;
    media.querySelector('[data-court-photo-placeholder]').hidden = false;
}

// Image errors do not bubble. Check completed images too, in case they failed before this script loaded.
document.addEventListener('error', (event) => {
    if (event.target instanceof HTMLImageElement && event.target.hasAttribute('data-court-photo')) {
        handleCourtPhotoError(event.target);
    }
}, true);

document.querySelectorAll('img[data-court-photo]').forEach((photo) => {
    if (photo.complete && photo.naturalWidth === 0) {
        handleCourtPhotoError(photo);
    }
});

// =========================================================================
// 2. CONFIRM DELETE (Dùng SweetAlert2)
// =========================================================================
function confirmDelete(message) {
    return Swal.fire({
        title: 'Xác nhận xóa',
        text: message || 'Bạn có chắc chắn muốn xóa? Hành động này không thể hoàn tác.',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#DC2626',
        cancelButtonColor: '#6B7280',
        confirmButtonText: 'Xóa',
        cancelButtonText: 'Hủy'
    }).then((result) => {
        return result.isConfirmed;
    });
}

// =========================================================================
// 3. CONFIRM ACTION (Generic confirmation)
// =========================================================================
function confirmAction(title, message, confirmText, confirmColor) {
    return Swal.fire({
        title: title || 'Xác nhận',
        text: message || 'Bạn có chắc chắn muốn thực hiện hành động này?',
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: confirmColor || getComputedStyle(document.documentElement).getPropertyValue('--color-primary').trim(),
        cancelButtonColor: '#6B7280',
        confirmButtonText: confirmText || 'Xác nhận',
        cancelButtonText: 'Hủy'
    }).then((result) => {
        return result.isConfirmed;
    });
}

// =========================================================================
// 4. LOADING SPINNER (Hiển thị khi submit form hoặc gọi API)
// =========================================================================
function showLoading() {
    Swal.fire({
        title: 'Đang xử lý...',
        html: 'Vui lòng chờ',
        allowOutsideClick: false,
        didOpen: () => {
            Swal.showLoading();
        }
    });
}

function hideLoading() {
    Swal.close();
}

// =========================================================================
// 5. SUCCESS/ERROR TOAST
// =========================================================================
function showSuccess(message) {
    Swal.fire({
        icon: 'success',
        title: 'Thành công',
        text: message,
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 3000,
        timerProgressBar: true
    });
}

function showError(message) {
    Swal.fire({
        icon: 'error',
        title: 'Lỗi',
        text: message,
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 4000,
        timerProgressBar: true
    });
}

function showInfo(message) {
    Swal.fire({
        icon: 'info',
        title: 'Thông báo',
        text: message,
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 3000,
        timerProgressBar: true
    });
}

// =========================================================================
// 6. FORMAT CURRENCY (VNĐ)
// =========================================================================
function formatCurrency(value) {
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND'
    }).format(value);
}

// =========================================================================
// 7. FORMAT DATE (dd/MM/yyyy)
// =========================================================================
function formatDate(dateString) {
    if (!dateString) return '';
    var parts = dateString.slice(0, 10).split('-');
    return parts.length === 3 ? parts[2] + '/' + parts[1] + '/' + parts[0] : dateString;
}

// =========================================================================
// 8. DEBOUNCE (Dùng cho tìm kiếm real-time)
// =========================================================================
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// =========================================================================
// 9. TIỆN ÍCH CHUNG KHI TẢI TRANG
// =========================================================================
$(document).ready(function () {
    // Errors remain visible until dismissed or corrected.
    var $errorSummary = $('#bookingErrors:not([hidden])');
    if ($errorSummary.length) $errorSummary.trigger('focus');

    // Tự submit form lọc khi người dùng đổi select (lọc nhanh, không cần bấm nút).
    $(document).on('change', 'form[data-auto-submit] select', function () {
        $(this).closest('form').trigger('submit');
    });

    // Xác nhận trước khi submit: <form data-confirm="Nội dung cảnh báo">.
    $(document).on('submit', 'form[data-confirm]', function (e) {
        var $form = $(this);
        if ($form.data('confirmed') === true) {
            return true;
        }

        e.preventDefault();

        Swal.fire({
            title: $form.data('confirm-title') || 'Xác nhận',
            text: $form.data('confirm') || 'Bạn có chắc chắn muốn thực hiện thao tác này?',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#DC2626',
            cancelButtonColor: '#6B7280',
            confirmButtonText: 'Đồng ý',
            cancelButtonText: 'Quay lại'
        }).then(function (result) {
            if (result.isConfirmed) {
                $form.data('confirmed', true);
                $form.trigger('submit');
            }
        });

        return false;
    });

    // Direct form validation runs before this delegated submit handler.
    $(document).on('submit', 'form[data-disable-double-submit]', function (e) {
        var $form = $(this);
        if (e.isDefaultPrevented()) return;
        if ($form.data('submitting')) { e.preventDefault(); return; }
        if ($form.data('validator') && !$form.valid()) { e.preventDefault(); return; }
        $form.data('submitting', true).attr('aria-busy', 'true');
        window.setTimeout(function () {
            $form.find('button[type="submit"]').each(function () {
                var $button = $(this);
                $button.data('idle-html', $button.html()).prop('disabled', true)
                    .html('<span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>Đang xử lý…');
            });
        }, 0);
    });
    $(window).on('pageshow', function () {
        $('form[data-disable-double-submit]').removeData('submitting').removeAttr('aria-busy')
            .find('button[type="submit"]').each(function () {
                var $button = $(this);
                if ($button.data('idle-html')) $button.html($button.data('idle-html')).prop('disabled', false);
            });
    });

    initTimeSlotGrid();
    initRegionCascade();
    initStarPicker();
    initCharCounter();
    initFilterKeyword();
    var summary = document.querySelector('.booking-page .booking-summary');
    if (summary) {
        var adjustSummary = function () {
            summary.classList.toggle('can-stick', window.innerWidth >= 992 && summary.offsetHeight + 136 < window.innerHeight);
        };
        new ResizeObserver(adjustSummary).observe(summary);
        window.addEventListener('resize', adjustSummary);
        adjustSummary();
    }
});

// =========================================================================
// 10. LƯỚI CHỌN KHUNG GIỜ (trang đặt sân)
// =========================================================================
var TIME_SLOT_CLASS = {
    0: 'timeslot-available',
    1: 'timeslot-booked',
    2: 'timeslot-maintenance',
    3: 'timeslot-past'
};

var TIME_SLOT_LABEL = {
    0: 'Còn trống',
    1: 'Đã đặt',
    2: 'Bảo trì',
    3: 'Đã qua'
};

/**
 * Render 1 khung giờ trong lưới chọn giờ.
 * Cấu trúc markup dùng chung giữa render phía server và AJAX.
 */
function buildTimeSlotElement(slot) {
    var selectable = !!slot.selectable;
    var $item = $('<label>', {
        class: 'timeslot-item ' + (TIME_SLOT_CLASS[slot.status] || 'timeslot-available') + (selectable ? '' : ' timeslot-disabled'),
        'data-slot-id': slot.id, 'data-price': slot.price, 'data-selectable': selectable
    });
    $item.append($('<input>', {
        type: 'checkbox', name: 'SelectedTimeSlotIds', value: slot.id, disabled: !selectable,
        'aria-describedby': 'slotHelp slotError'
    }));
    if (selectable) $item.append($('<input>', { type: 'hidden', name: 'SlotQuotes[' + slot.id + ']', value: slot.quote }));
    $item.append($('<span>', { class: 'timeslot-time', text: slot.timeRange || (slot.startTime + ' - ' + slot.endTime) }));
    $item.append($('<span>', { class: 'timeslot-price', text: slot.priceFormatted || formatCurrency(slot.price) }));
    $item.append($('<span>', { class: 'timeslot-meta', text: TIME_SLOT_LABEL[slot.status] || 'Không khả dụng' }));
    return $item;
}

function slotSelectionError() {
    var $grid = $('#timeSlotGrid');
    var $checked = $grid.find('input[type="checkbox"]:checked');
    if ($grid.data('ready') === false) return 'Vui lòng tải lại lịch sân thành công trước khi đặt.';
    if (!$checked.length) return 'Vui lòng chọn ít nhất một khung giờ còn trống.';
    if ($checked.length > Number($grid.data('max-slots'))) return 'Số khung giờ vượt quá giới hạn cho phép.';
    var ranges = [];
    $checked.each(function () {
        var match = $(this).closest('.timeslot-item').find('.timeslot-time').text().match(/(\d{2}):(\d{2})\s*[-–]\s*(\d{2}):(\d{2})/);
        if (match) ranges.push([Number(match[1]) * 60 + Number(match[2]), Number(match[3]) * 60 + Number(match[4])]);
    });
    ranges.sort(function (a, b) { return a[0] - b[0]; });
    return ranges.some(function (range, index) { return index > 0 && ranges[index - 1][1] !== range[0]; })
        ? 'Vui lòng chọn các khung giờ liền nhau. Khung rời phải đặt riêng.' : '';
}

function showBookingErrors(errors) {
    var $summary = $('#bookingErrors');
    var $list = $summary.find('ul').empty();
    errors.forEach(function (error) {
        $list.append($('<li>').append($('<a>', { href: '#' + error.field, text: error.text })));
    });
    $summary.prop('hidden', false).trigger('focus');
}

function initTimeSlotGrid() {
    var $grid = $('#timeSlotGrid');
    if (!$grid.length) return;
    $grid.data('ready', true);
    $grid.on('change', 'input[type="checkbox"]', function () {
        var max = Number($grid.data('max-slots')) || 1;
        var message = '';
        if ($grid.find('input[type="checkbox"]:checked').length > max) {
            this.checked = false;
            message = 'Chỉ được chọn tối đa ' + max + ' khung trong một lần đặt.';
        }
        updateBookingSummary();
        if (!message && $grid.find('input:checked').length) message = slotSelectionError();
        $('#slotError').text(message);
        $grid.attr('aria-invalid', message ? 'true' : 'false');
    });
    $grid.closest('form').on('submit', function (e) {
        var form = this;
        var errors = [];
        var date = document.getElementById('BookingDate');
        if (!date.checkValidity()) {
            var dateError = 'Chọn ngày sử dụng sân trong khoảng cho phép.';
            $('#dateError').text(dateError); $(date).attr('aria-invalid', 'true');
            errors.push({field: 'BookingDate', text: dateError});
        } else $(date).removeAttr('aria-invalid');
        var slotError = slotSelectionError();
        $('#slotError').text(slotError); $grid.attr('aria-invalid', slotError ? 'true' : 'false');
        if (slotError) errors.push({field: 'timeSlotGrid', text: slotError});
        if (!$(form).find('input[name="PaymentMethod"]:checked:not(:disabled)').length) {
            var paymentError = 'Vui lòng chọn phương thức thanh toán khả dụng.';
            $('#paymentError').text(paymentError); errors.push({field: 'paymentChoices', text: paymentError});
        } else $('#paymentError').text('');
        var notes = document.getElementById('Notes');
        if (notes.value.length > 500) {
            $('#notesError').text('Ghi chú tối đa 500 ký tự.'); errors.push({field: 'Notes', text: 'Ghi chú tối đa 500 ký tự.'});
        }
        if (errors.length) { e.preventDefault(); showBookingErrors(errors); }
        else $('#bookingErrors').prop('hidden', true);
    });
    // Links to a non-control group still receive keyboard focus.
    $('#bookingErrors').on('click', 'a', function (e) {
        var target = document.getElementById(this.hash.slice(1));
        if (target) { e.preventDefault(); target.focus(); target.scrollIntoView({block: 'center'}); }
    });
    updateBookingSummary();
}

function updateBookingSummary() {
    var $grid = $('#timeSlotGrid');
    if (!$grid.length) return;
    var total = 0;
    var $chips = $('#selectedSlotChips').empty();
    var $checked = $grid.find('input[type="checkbox"]:checked');
    $grid.find('.timeslot-item[data-selectable="true"]').each(function () {
        var selected = $(this).find('input[type="checkbox"]').prop('checked');
        $(this).toggleClass('timeslot-selected', selected).find('.timeslot-meta').text(selected ? 'Đang chọn' : 'Còn trống');
    });
    $checked.each(function () {
        var $item = $(this).closest('.timeslot-item');
        total += Number($item.data('price')) || 0;
        $chips.append($('<span>', {class: 'selected-slot-chip', text: $item.find('.timeslot-time').text()}));
    });
    $('#selectedSlotCount').text($checked.length);
    $('#totalAmountPreview').text(formatCurrency(total));
    $('#totalAmountSelected').val(total);
    $('#summaryPayment').text($('input[name="PaymentMethod"]:checked').val() === 'BankTransfer' ? 'Chuyển khoản qua QR' : 'Tại quầy');
    $('#bookingLiveSummary').text('Đã chọn ' + $checked.length + ' khung, ngày ' + $('#summaryDate').text() + ', tổng tạm tính ' + formatCurrency(total) + '.');
    if (!$checked.length) $chips.append($('<span>', {class: 'text-muted small', text: 'Chưa chọn khung giờ nào.'}));
}

var timeSlotRequest = null;
var timeSlotVersion = 0;
function invalidateTimeSlots(message) {
    timeSlotVersion++;
    if (timeSlotRequest) timeSlotRequest.abort();
    $('#timeSlotGrid').empty().data('ready', false).attr('aria-busy', 'false');
    $('#slotLoadStatus').text(message).addClass('text-danger');
    $('#retryTimeSlots').prop('hidden', false);
    $('#bookingSubmit').prop('disabled', true);
    updateBookingSummary();
}

function reloadTimeSlots(courtId, date, onDone) {
    var $grid = $('#timeSlotGrid');
    if (!$grid.length) return;
    var version = ++timeSlotVersion;
    if (timeSlotRequest) timeSlotRequest.abort();
    $grid.empty().data('ready', false).attr('aria-busy', 'true');
    $('#slotError').text('');
    $('#slotLoadStatus').text('Đang tải lịch và giá cho ngày ' + formatDate(date) + '…').removeClass('text-danger');
    $('#retryTimeSlots').prop('hidden', true);
    $('#bookingSubmit').prop('disabled', true);
    updateBookingSummary();
    function failed(message) {
        $grid.empty().data('ready', false);
        $('#slotLoadStatus').text(message).addClass('text-danger');
        $('#retryTimeSlots').prop('hidden', false);
    }
    timeSlotRequest = $.ajax({url: '/Booking/GetTimeSlots', dataType: 'json', data: {courtId: courtId, date: date}, timeout: 15000})
        .done(function (response) {
            if (version !== timeSlotVersion) return;
            if (!response || !response.success || !Array.isArray(response.slots)) {
                failed((response && response.message) || 'Không tải được lịch sân. Vui lòng thử lại.'); return;
            }
            response.slots.forEach(function (slot) { $grid.append(buildTimeSlotElement(slot)); });
            $grid.data('ready', true);
            var free = response.slots.filter(function (slot) {return slot.selectable;}).length;
            $('#slotLoadStatus').text(free ? free + ' khung giờ còn trống ngày ' + formatDate(date) + '.' : 'Ngày này không có khung giờ trống. Hãy chọn ngày khác.');
            if (!response.slots.length) $grid.append($('<p>', {class: 'slot-empty text-muted', text: 'Sân chưa có khung giờ đặt.'}));
            $('#bookingSubmit').prop('disabled', false);
            updateBookingSummary();
            if (typeof onDone === 'function') onDone(response.slots);
        })
        .fail(function (xhr, status) {
            if (version === timeSlotVersion && status !== 'abort') failed('Không kết nối được tới máy chủ. Vui lòng thử tải lại lịch.');
        })
        .always(function () { if (version === timeSlotVersion) $grid.attr('aria-busy', 'false'); });
}

// =========================================================================
// 11. CASCADE TỈNH -> QUẬN/HUYỆN (trang tìm kiếm)
// =========================================================================
function initRegionCascade() {
    var $province = $('#Province'), $district = $('#District');
    if (!$province.length || !$district.length) return;
    var request = null;
    function loadDistricts(province, selected) {
        if (request) request.abort();
        $('#districtFeedback').remove();
        $district.empty().append($('<option>', {value: '', text: province ? 'Đang tải quận/huyện…' : 'Tất cả quận/huyện'}))
            .prop('disabled', false).attr('aria-busy', province ? 'true' : 'false');
        if (selected) $district.append($('<option>', {value: selected, text: selected, selected: true}));
        if (!province) return;
        request = $.getJSON('/Court/GetDistricts', {province: province}).done(function (districts) {
            $district.empty().append($('<option>', {value: '', text: 'Tất cả quận/huyện'}));
            (districts || []).forEach(function (district) {
                $district.append($('<option>', {value: district, text: district, selected: selected === district}));
            });
            $district.prop('disabled', false).removeAttr('aria-busy');
        }).fail(function (xhr, status) {
            if (status !== 'abort') {
                $district.empty().append($('<option>', {value: '', text: 'Tất cả quận/huyện'}));
                if (selected) $district.append($('<option>', {value: selected, text: selected, selected: true}));
                $district.prop('disabled', false).removeAttr('aria-busy');
                $('#districtFeedback').remove();
                var $feedback = $('<div>', {id: 'districtFeedback', class: 'form-text', role: 'status'}).text('Chưa tải được danh sách quận/huyện. ');
                $feedback.append($('<button>', {type: 'button', class: 'btn btn-link p-0', text: 'Thử lại'}).on('click', function () {
                    $feedback.remove(); loadDistricts(province, selected);
                }));
                $district.after($feedback);
            }
        });
    }
    $province.on('change', function () {loadDistricts($(this).val(), null);});
    if ($province.val()) loadDistricts($province.val(), $district.val());
}

// =========================================================================
// 12. BỘ CHỌN SAO (form đánh giá)
// =========================================================================
function initStarPicker() {
    $('.star-picker').each(function () {
        var $picker = $(this);
        var $target = $($picker.data('target'));

        if ($target.length === 0) {
            return;
        }

        function paint(value) {
            $picker.find('.star').each(function () {
                $(this).toggleClass('is-active', parseInt($(this).data('value'), 10) <= value);
            });
        }

        paint(parseInt($target.val(), 10) || 0);

        $picker.on('mouseenter', '.star', function () {
            paint(parseInt($(this).data('value'), 10));
        });

        $picker.on('mouseleave', function () {
            paint(parseInt($target.val(), 10) || 0);
        });

        $picker.on('click', '.star', function (e) {
            e.preventDefault();
            var value = parseInt($(this).data('value'), 10);
            $target.val(value).trigger('change');
            paint(value);

            $('[data-rating-label]').each(function () {
                $(this).text($(this).data('rating-label').replace('{0}', value));
            });
        });
    });
}

// =========================================================================
// 13. ĐẾM KÝ TỰ TEXTAREA
// =========================================================================
function initCharCounter() {
    $(document).on('input', 'textarea[maxlength]', function () {
        var max = parseInt($(this).attr('maxlength'), 10);
        var $counter = $('[data-counter-for="' + $(this).attr('id') + '"]');
        if ($counter.length) {
            $counter.text($(this).val().length + ' / ' + max);
        }
    });

    $('textarea[maxlength]').trigger('input');
}

// =========================================================================
// 14. TỰ TÌM KIẾM KHI NGƯỜI DÙNG GÕ TỪ KHÓA
// =========================================================================
function initFilterKeyword() {
    var $keyword = $('#Keyword[data-debounce-submit]');
    if ($keyword.length === 0) {
        return;
    }

    var submit = debounce(function () {
        $keyword.closest('form').trigger('submit');
    }, 700);

    $keyword.on('input', submit);
}

// =========================================================================
// 15. COUNTDOWN HẠN THANH TOÁN (booking Pending)
// =========================================================================
function startPaymentCountdown(elementId, deadlineIso) {
    var el = document.getElementById(elementId);
    if (!el || !deadlineIso) {
        return;
    }

    var deadline = new Date(deadlineIso).getTime();

    function tick() {
        var diff = deadline - Date.now();

        if (diff <= 0) {
            el.textContent = 'Đã hết hạn thanh toán';
            el.classList.add('text-danger');
            clearInterval(timer);
            return;
        }

        var minutes = Math.floor(diff / 60000);
        var seconds = Math.floor((diff % 60000) / 1000);
        el.textContent = 'Còn ' + minutes + ':' + String(seconds).padStart(2, '0') + ' để thanh toán';
    }

    tick();
    var timer = setInterval(tick, 1000);
}
