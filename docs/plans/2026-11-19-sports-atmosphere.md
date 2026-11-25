# SportBooking — giao diện thể thao theo ảnh tham chiếu

**Goal:** Chuyển giao diện sang xanh đen/kính mờ như ảnh người dùng, nền ảnh sáu môn chuyển tiếp và biểu tượng riêng nhiều màu trong khung chọn môn.

**Architecture:** Giữ ASP.NET MVC/Razor, Bootstrap, jQuery và các endpoint hiện tại. Thêm một stylesheet theme, partial nền và bộ chọn môn dùng chung, SVG nhận diện từng môn, ảnh nền cục bộ. Không đổi dịch vụ booking hoặc database sản xuất.

## Lựa chọn

- Chọn theme tối toàn bộ shell và trang chủ, panel kính đủ đậm để chữ rõ trên mọi ảnh; vàng cam cho tiêu đề/điểm nhấn, gradient xanh–hồng cho CTA theo ảnh tham chiếu.
- Giữ điều hướng hiện tại để các vai trò tiếp tục sử dụng; không sao chép logo thương hiệu trong ảnh.
- Nền ảnh chụp thể thao riêng từng môn, chủ thể bên phải; crossfade 7 giây, có trước/sau/tạm dừng. Dừng khi tab ẩn, người dùng hover/focus toolbar và khi prefers-reduced-motion bật. Điều khiển vẫn hoạt động bằng tay.
- Bộ chọn môn hiển thị logo nhiều màu bằng SVG, giữ select/name SportTypeId cho GET binding. Native details/summary và button cho keyboard, Escape đóng và trả focus. Card chọn môn dùng cùng nhận diện.
- Tra cứu design-system thể thao gợi ý phong cách quảng bá đỏ không khớp ảnh nên chỉ tham khảo bố cục; kết quả glass/dark và UX carousel dùng sau đối chiếu, bảng màu lấy từ ảnh người dùng.

## Công việc

1. Đọc Home, layout, picker tìm sân, MASTER và ảnh tham chiếu.
2. Tạo 6 ảnh nền bằng imagegen built-in, copy vào wwwroot/images/sports; tối ưu WebP. Tạo SVG bóng đá, cầu lông, tennis, bóng rổ, pickleball, bóng chuyền và fallback nhiều màu.
3. Thêm SportVisual helper/partial và picker dùng chung; cập nhật trang chủ và bộ lọc.
4. Thêm nền ảnh/toolbar/controller và dark theme, điều chỉnh Bootstrap states, form, bảng/alert/summary, focus, mobile và reduced motion.
5. Cập nhật MASTER theo yêu cầu mới, lưu provenance ảnh/prompt.
6. Build, browser kiểm tra 375/768/1440, carousel (pause/manual/reduced motion), picker bàn phím/form binding và luồng booking hiện có. Chụp ảnh và báo cáo.

Không có Git trong checkout nên làm trên thư mục hiện tại. Dùng database kiểm thử riêng. Yêu cầu theme tối mới thay thế quyết định chủ đề sáng trước đó.
