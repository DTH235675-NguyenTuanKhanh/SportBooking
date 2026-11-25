for (const profile of document.querySelectorAll('[data-transfer-profile]')) {
    const preview = profile.querySelector('[data-qr-preview]');
    const message = profile.querySelector('[data-qr-preview-message]');
    const confirmation = profile.querySelector('[data-qr-confirmation]');
    const fileInput = profile.querySelector('[data-qr-file]');
    let previewUrl;

    for (const input of profile.querySelectorAll('[data-transfer-value]')) {
        const value = profile.querySelector('[data-preview="' + input.dataset.transferValue + '"]');
        value.textContent = input.value;
        input.addEventListener('input', () => {
            value.textContent = input.value;
            confirmation.checked = false;
        });
    }

    fileInput.addEventListener('change', () => {
        confirmation.checked = false;
        if (previewUrl) URL.revokeObjectURL(previewUrl);
        const file = fileInput.files[0];
        previewUrl = file ? URL.createObjectURL(file) : null;
        const src = previewUrl || preview.dataset.existingSrc;
        preview.hidden = !src;
        if (src) preview.src = src;
        else preview.removeAttribute('src');
        message.textContent = 'Quét thử QR và đối chiếu thông tin trước khi lưu.';
    });
    preview.addEventListener('error', () => {
        preview.hidden = true;
        message.textContent = 'Không xem được ảnh. Hãy chọn lại ảnh PNG hoặc JPEG.';
    });
}

const image = document.querySelector('[data-transfer-qr]');
if (image) {
    const showImageError = () => {
        image.hidden = true;
        document.querySelector('[data-transfer-image-error]').hidden = false;
    };
    image.addEventListener('error', showImageError);
    if (image.complete && image.naturalWidth === 0) showImageError();
}

for (const button of document.querySelectorAll('[data-copy]')) {
    button.hidden = false;
    button.addEventListener('click', async () => {
        const result = document.querySelector('[data-copy-result]');
        try {
            await navigator.clipboard.writeText(button.dataset.copy);
            result.textContent = 'Đã sao chép.';
        } catch {
            result.textContent = 'Không sao chép tự động được. Hãy chọn và sao chép nội dung hiển thị.';
        }
    });
}

// Handle submit after form-level validation has had a chance to cancel it.
document.addEventListener('submit', event => {
    const form = event.target;
    if (event.defaultPrevented || !form.matches('form[data-qr-submit]')) return;

    const button = event.submitter;
    if (form.dataset.busy || (button?.dataset.qrConfirm && !window.confirm(button.dataset.qrConfirm))) {
        event.preventDefault();
        return;
    }

    form.dataset.busy = 'true';
    if (button) {
        button.dataset.previousLabel = button.innerHTML;
        button.setAttribute('aria-disabled', 'true');
        button.textContent = 'Đang xử lý…';
    }
});

window.addEventListener('pageshow', () => {
    for (const form of document.querySelectorAll('form[data-qr-submit]')) {
        delete form.dataset.busy;
        for (const button of form.querySelectorAll('[data-previous-label]')) {
            button.innerHTML = button.dataset.previousLabel;
            delete button.dataset.previousLabel;
            button.removeAttribute('aria-disabled');
        }
    }
});
