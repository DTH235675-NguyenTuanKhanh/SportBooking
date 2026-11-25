# SportBooking — Design system

Cập nhật: 19/11/2026. Nguồn nghiệp vụ: `docs/BUSINESS_RULES.md`.

Yêu cầu mới của người dùng: màu sắc/phong cách theo ảnh tham chiếu tối xanh đen, nền ảnh thể thao chuyển tiếp, mỗi môn có logo nhiều màu. Quy chuẩn này **thay thế chủ đề sáng ngày 18/11**, giữ các quyết định về responsive, accessibility và backend binding đã nghiệm thu.

## Định hướng và bố cục

- Phong cách **Night Stadium**: charcoal/navy, teal, panel kính mờ, chữ trắng, nhấn vàng cam; CTA gradient xanh–hồng. Ảnh nền chủ thể bên phải để phần nội dung bên trái rõ.
- Giữ Be Vietnam Pro 400/500/600/700, đầy đủ dấu tiếng Việt. Bootstrap Icons cho điều khiển hệ thống; bộ SVG riêng nhiều màu cho các môn thể thao. Không sao chép logo thương hiệu trong ảnh tham chiếu.
- Container theo Bootstrap, tối đa 1296px nội dung. Gutter 16px mobile, 24px tablet; breakpoint 768/992/1200/1400. Header nhất quán, menu thu gọn trên mobile.

| Vai trò | Bố cục |
|---|---|
| Khách | Trang chủ có hero thể thao, bộ tìm sân kính mờ, thẻ chọn môn, sân nổi bật. Tìm sân có sidebar desktop/collapse mobile. Chi tiết có ảnh, giá, đánh giá và CTA. Đặt sân có 3 phần và summary cạnh form desktop/trong luồng mobile. |
| Owner | Shell/tokens tối dùng chung; menu Tổng quan, Lịch đặt, Cụm sân; bảng và form giữ cấu trúc hiện có. Không tái cấu trúc nghiệp vụ trong đợt đổi theme. |
| Admin | Shell/tokens tối dùng chung, bảng quản trị và hành động theo quyền. Định hướng sidebar khi mở rộng, không thêm route hoặc chức năng giả. |

## Palette và tokens

Theme nằm trong `wwwroot/css/sports-theme.css`, nạp sau `site.css`. Không dùng bảng màu sáng cũ để suy ra tương phản tối.

| CSS variable | Giá trị | Sử dụng |
|---|---|---|
| `--color-bg-page` | `#161c26` | Nền fallback |
| `--color-bg-card` | `#202e39` | Surface nền đậm |
| `--surface-glass` | `rgb(33 45 56 / 91%)` | Card kính đủ đậm để đọc trên ảnh |
| `--surface-soft` | `rgb(29 42 53 / 84%)` | Section phụ |
| `--surface-input` | `#192832` | Input/select/picker |
| `--color-secondary` | `#f4f7fb` | Chữ chính |
| `--color-text-secondary` | `#bdc9d4` | Chữ phụ |
| `--color-primary` | `#b6c7ff` | Link/nhấn màu lam nhạt |
| `--color-primary-hover` | `#d7e1ff` | Link hover |
| `--color-primary-light` | `#29364e` | Nền nhãn lam |
| `--color-accent` | `#ffbc42` | Vàng cam cho heading/active/sao |
| `--action-start` | `#405aca` | Đầu gradient nút |
| `--action-end` | `#a72c80` | Cuối gradient nút |
| `--color-on-primary` | `#ffffff` | Chữ nút/chọn giờ |
| `--color-border` | `#53616e` | Divider/viền |
| `--color-control-border` | `#8295a5` | Ranh giới control |
| `--color-focus` | `#ffd27c` | Focus 3px + offset 3px |
| `--color-success` | `#9be4bc` | Thành công |
| `--color-warning` | `#ffcb76` | Cảnh báo |
| `--color-danger` | `#ffb8ae` | Lỗi |
| `--color-info` | `#aed6ff` | Thông tin |
| `--color-slot-muted-bg` | `#2a333e` | Đã đặt/đã qua |
| `--color-danger-light` | `#452b35` | Nền lỗi |

Selected slot dùng `#4b5eb2` và chữ trắng; available nền `#1c303c`, viền `#86d2ae`; maintenance nền `#4a303a`, chữ `#ffd2ca`; booked/past nền `#303a45`, chữ `#c2ced8`. Nhãn trạng thái luôn có chữ, không chỉ màu. Pending vàng đậm/chữ kem, confirmed lam đậm/chữ lam nhạt, completed xanh đậm/chữ xanh nhạt, cancelled đỏ đậm/chữ đỏ nhạt, expired/noshow xám đậm/chữ sáng.

## Font và hình khối

- Body/input 16px, line-height 1.6; phụ 14px, metadata 12–13px. H1 trang tác vụ 28–40px, hero trang chủ 36–64px/1.15; H2 24–32px, H3 18–20px. Tiêu đề ngắn có text-wrap balance khi phù hợp.
- Khoảng cách 4/8/12/16/24/32/48px. Radius control 8px, card 12–16px, pill chỉ cho nhãn/toolbar. Viền card 1px, selected có thêm viền rõ.
- Bóng vừa `0 16px 48px rgb(0 0 0 / 23%)`; blur 12px ưu tiên navbar/search, không làm mờ toàn bộ nội dung. Reduced transparency dùng surface đặc.
- Giá tabular-nums, wrap đầy đủ đơn vị; dữ liệu dài có min-width:0/overflow-wrap:anywhere. Không ellipsis thông tin sân/địa chỉ thiết yếu.

## Nền chuyển cảnh

- 6 ảnh: bóng đá, cầu lông, tennis, bóng rổ, pickleball, bóng chuyền. Ảnh trang trí AI-generated được lưu cục bộ tại `wwwroot/images/sports/*.webp`, 1600×900; không trình bày như ảnh sân thật/khách hàng thật. Prompt/provenance: `docs/ui-review/sports-asset-manifest.json`.
- 2 lớp ảnh fixed, crossfade opacity 1200ms; chu kỳ 7 giây, tải ảnh kế tiếp khi cần, giữ ảnh cũ khi chưa tải xong/lỗi. Có version guard cho tải chậm/đổi liên tục.
- Toolbar nằm trong luồng trang, không che focus/CTA. Có trước/sau/tạm dừng, accessible name và trạng thái pressed. Tạm ngưng khi hover/focus toolbar/tab ẩn. Không tự thông báo mỗi lần ảnh chạy; chỉ announce đổi ảnh bằng tay.
- prefers-reduced-motion: không tự chuyển, không fade; nút thủ công vẫn dùng được. Pause chặn cả lượt tự chuyển đang đợi tải/decode. Lưu lựa chọn/pause trong sessionStorage nếu được phép.
- Scrim tối xanh đen phủ ảnh, nặng hơn ở trái/mobile; chữ form nằm trên surface đủ đậm. Không dùng ảnh AI thay ảnh thật của sân trong card/detail.

## Logo và picker môn thể thao

- Logo SVG riêng: bóng đá + huy hiệu teal/vàng; vợt/cầu lông xanh–cam; tennis xanh–lime; bóng rổ cam; paddle pickleball xanh–hồng/bóng lỗ lime; bóng chuyền vàng–lam. Bộ file: `wwwroot/images/sports/icons/`.
- `SportVisual` chuẩn hóa tên môn, không hardcode ID database. Môn tùy chỉnh chưa nhận diện dùng biểu tượng nhiều màu tổng quát. Tên môn luôn hiện cùng logo. Logo bên cạnh chữ là trang trí, alt rỗng/aria-hidden.
- `_SportPicker`: select `SportTypeId` gốc tiếp tục gửi GET; khi JS hoạt động mới tăng cường bằng details/summary và button có logo màu. Không JS vẫn dùng select.
- Tab/Enter/Space mở; Arrow/Home/End chuyển focus; Escape đóng và trả focus; chọn cập nhật label/logo/aria-pressed và ảnh nền đúng môn. Click ngoài đóng. Option cao ít nhất 44px, panel vừa viewport và có scroll khi nhiều môn.
- Dùng cùng logo trong thẻ chọn môn, nhãn và placeholder sân. Bootstrap Icons chỉ giữ cho chức năng điều khiển hệ thống.

## Component và trạng thái

- Button ≥44px. CTA gradient đủ đậm/chữ trắng; outline nền tối và nhãn sáng. Hover/pressed không làm nhảy layout. Loading khóa gửi lặp và ghi Đang xử lý, form lỗi vẫn sửa/gửi lại.
- Input/select/date có label, helper, aria-describedby và inline error. Date picker theo color-scheme dark. Không tự submit khi đang nhập bộ lọc.
- Card sân giữ ảnh 16:10, lazy/alt; thiếu ảnh dùng logo + nhãn Chưa có ảnh sân. Giá VNĐ/khung, review từ dữ liệu thật.
- Filter chip wrap đầy đủ nhãn, toàn chip là link bỏ lọc. Pagination giữ query và có aria-current/disabled.
- Alert có surface trạng thái tối, lỗi/cảnh báo không tự ẩn. Error summary có focus/link tới trường, vẫn giữ inline error. Dialog dùng controls hiện có, có Hủy/Đóng/Escape và trả focus.
- Booking summary hiện trên cả mobile/desktop; chỉ sticky khi đủ chiều cao, mobile trong luồng. Giá/giờ đổi phải qua nhánh xác nhận lại của server.
- Các phần dùng light background cũ như badge tab/skip link/hover được ánh xạ riêng sang màu tối có tương phản.

## Responsive, accessibility và nghiệp vụ

- Kiểm tra 375, 768, 1440px; thêm landscape và chữ 200%. Không tràn ngang; toolbar/picker không che trường. Bảng giá reflow trên mobile.
- Một H1; heading tuần tự; skip link đọc rõ; keyboard focus 3px vàng cam, không bị sticky header che. Control chính ≥44px.
- Chữ thường ≥4.5:1; ranh giới control/focus có nghĩa ≥3:1. Kiểm tra độc lập theme tối, cả selected/hover và inactive count badge.
- Giữ Razor binding, route, antiforgery, `SlotQuotes`, AJAX lịch và quy tắc tính tiền. Ngày UTC+7/cấu hình; giá VNĐ/khung, chỉ ngày thường/cuối tuần; nhiều khung phải liền nhau. Lịch sử dùng snapshot.

## Quyết định

Ảnh tham chiếu của người dùng là nguồn định hướng chính. Tra cứu thể thao trả về màu đỏ và phong cách marketing tổng quát nên không dùng nguyên bản. Tra cứu dark/glass được đối chiếu, chỉ chọn surface tối và focus/contrast; không đưa hiệu ứng lensing/liquid-glass của native Apple vào web. Theme mới thay lựa chọn nền sáng trước đó; bố cục backend và tương tác đặt sân đã có được giữ.
