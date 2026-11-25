# Quy tắc bán khung giờ và giá

Ngày chốt: 14/11/2026. Phạm vi: phiên bản hiện tại của SportBooking.

Tài liệu này là quyết định nghiệp vụ cho phần quản lý khung giờ và giá. Phần quản lý đã được triển khai ngày 14/11/2026; đối chiếu mã tại mục 5.

## 1. Đơn vị bán và chọn nhiều khung

- Một khung giờ là một đơn vị bán trọn gói; giá là VNĐ/khung, không phải VNĐ/giờ. Không chia nhỏ hoặc tính tiền theo số phút khách sử dụng.
- Một lần đặt chọn từ 1 đến 6 khung, trên cùng một sân và cùng một ngày sử dụng. Nếu cấu hình chỉ cho một khung thì giới hạn còn 1.
- Khi chọn nhiều khung, bắt buộc liền nhau: sau khi sắp xếp theo giờ bắt đầu, giờ kết thúc khung trước phải bằng giờ bắt đầu khung sau. Muốn đặt các khung rời nhau phải tạo các lần đặt riêng.
- Ví dụ: 18:00–19:00 và 19:00–20:00 được đặt chung; 18:00–19:00 và 20:00–21:00 bị từ chối trong cùng lần đặt.
- Mỗi khung tạo một Booking; các Booking trong lần đặt có chung BookingGroupCode. Tổng tiền nhóm là tổng giá các khung. Tạo nhóm phải thành công toàn bộ hoặc không tạo khung nào.
- Khoảng giờ được hiểu là [bắt đầu, kết thúc): hai khung chung mốc 19:00 không chồng lấn. Trong cùng sân không cho các khung đang bán chồng lấn nhau. Các sân khác nhau có lịch độc lập.
- Khung phải thuộc sân đang chọn, đang hoạt động, còn trống và chưa bắt đầu. Giờ bắt đầu/kết thúc phải nằm trong giờ mở cửa của cụm nếu cụm có khai báo giờ mở cửa.

## 2. Thay đổi giờ, trạng thái và giá khi đã có booking

### Đổi giờ bắt đầu/kết thúc

- Owner chỉ sửa khung thuộc sân và cụm của mình.
- Chặn sửa giờ nếu khung có Booking ở trạng thái Pending hoặc Confirmed với EndDateTime > thời điểm hiện tại. Điều này bảo vệ cả lịch tương lai và lượt đang diễn ra; áp dụng cho mọi phương thức thanh toán.
- Booking Pending đã quá hạn nhưng chưa được xử lý vẫn được tính là đang giữ chỗ đến khi hệ thống chuyển trạng thái và giải phóng chỗ. Không suy đoán hết hạn chỉ từ giao diện.
- Không tự dời lịch, tự hủy, tự hoàn tiền hoặc đổi giá booking để cho phép sửa giờ. Owner phải xử lý lượt đặt theo luồng nghiệp vụ riêng trước.
- Nếu không còn booking đang giữ chỗ nói trên, cho sửa giờ khi giờ mới hợp lệ, trong giờ mở cửa và không chồng lấn lịch bán khác của sân.
- Trước khi ghi thay đổi, server phải kiểm tra lại điều kiện trong cùng giao dịch với thao tác cập nhật. Luồng tạo booking và cập nhật khung phải phối hợp khóa/kiểm tra để không tạo booking mới trên giờ cũ trong lúc sửa.

### Ngừng bán và bảo toàn lịch sử

- Cho ngừng bán khung dù còn booking: khung biến mất khỏi lựa chọn đặt mới; các Booking đã tạo vẫn giữ hiệu lực và vẫn cho thanh toán/xử lý theo trạng thái hiện có.
- Không xóa cứng khung đã được Booking tham chiếu. Không làm mất lịch sử bằng bộ lọc ẩn khung ngừng bán.
- Mọi nơi hiển thị booking phải lấy giờ từ Booking.StartDateTime và Booking.EndDateTime, số tiền từ Booking.TotalAmount. Không lấy lại giờ hoặc tính lại tiền từ TimeSlot hiện tại.
- Thay đổi giờ mở cửa cụm cũng phải chặn nếu làm booking đang giữ chỗ nằm ngoài giờ mở cửa mới. Không tự thay đổi giờ các sân con hoặc booking.

### Thay đổi giá

- Cho đổi giá dù có booking tương lai; chỉ ảnh hưởng những lần đặt được tạo sau khi cập nhật.
- Khách đã mở trang đặt nhưng chưa gửi thành công chưa được giữ giá. Server tính lại giá tại thời điểm tạo booking, thông báo nếu tổng tiền khác với giá khách vừa xác nhận và yêu cầu khách xác nhận lại trước khi tạo.
- Booking đã tạo, kể cả đang chờ thanh toán, giữ nguyên TotalAmount đến khi thanh toán, hủy hoặc hết hạn. Thanh toán lại một booking chưa hết hạn vẫn dùng số tiền đã chốt.
- Giá ngày thường và cuối tuần từ 1 đến 999.999.999.999 VNĐ/khung, là số nguyên, không dùng dấu phân cách khi nhập; không bán miễn phí trong phiên bản này. Giới hạn này nằm trong cột tiền database và bảo đảm tổng tối đa 6 khung được tính chính xác trên trình duyệt. Tổng nhóm bằng tổng snapshot các booking, không cộng phụ phí ngầm.

## 3. Giờ qua nửa đêm

- Phiên bản này chưa hỗ trợ khung qua nửa đêm. Mọi khung thuộc trọn một ngày, với 00:00 <= StartTime < EndTime < 24:00; không nhận giá trị 24:00.
- Ví dụ 22:00–23:00 hợp lệ; 23:00–01:00, 23:00–00:00 và 18:00–18:00 đều bị từ chối. Không tự hiểu giờ kết thúc nhỏ hơn giờ bắt đầu là ngày hôm sau.
- Không ghép nhiều khung thành một lần đặt bắc qua hai ngày. Khung 00:00–01:00 thuộc ngày mới và phải đặt riêng.
- Cụm có khai báo giờ mở/đóng cũng phải trong cùng ngày. Tất cả phép xác định ngày, cuối tuần, giờ đã qua và kiểm tra booking tương lai dùng múi giờ Việt Nam (UTC+7), độc lập múi giờ máy chủ.

## 4. Giá ngày thường, cuối tuần và ngày lễ

- Chưa áp dụng giá lễ trong phiên bản này. Chỉ có hai mức giá theo ngày sử dụng sân: thứ Hai–thứ Sáu dùng PriceWeekday; thứ Bảy–Chủ nhật dùng PriceWeekend.
- Ngày lễ rơi vào thứ trong tuần vẫn dùng giá ngày thường; ngày lễ rơi vào cuối tuần dùng giá cuối tuần. Không dựa vào ngày tạo booking hoặc ngày thanh toán để chọn giá.
- Không hiển thị trường nhập giá lễ hoặc hứa hẹn tự nhận diện ngày lễ trên giao diện Owner/khách hàng.
- Giữ nguyên PriceHoliday và DayType.Holiday trong model để tương thích dữ liệu hiện có; không sử dụng cho tính tiền hay hiển thị giá bán của phiên bản này. Lịch lễ và giá lễ là phạm vi mở rộng riêng.
- Ví dụ khung có giá thường 200.000đ, cuối tuần 250.000đ: đặt cho thứ Sáu thu 200.000đ, cho thứ Bảy thu 250.000đ, kể cả ngày đó là ngày lễ. Hai khung liền nhau giá 200.000đ và 300.000đ có tổng tiền 500.000đ.

## 5. Đối chiếu triển khai ngày 14/11/2026

| Nội dung | Hiện trạng / yêu cầu triển khai |
|---|---|
| Số khung | Đã có cấu hình MaxSlotsPerBooking=6 và kiểm tra giới hạn; AllowMultipleConsecutiveSlots hiện chỉ bật/tắt chọn nhiều khung. |
| Khung liền nhau | BookingService kiểm tra trên server và kiểm tra lại dưới khóa giao dịch; trình duyệt hướng dẫn và chặn gửi các khung rời. |
| Đổi giờ, ngừng bán, giá | Owner → Cụm sân → Quản lý sân → Khung giờ và giá. Có thêm/sửa/ngừng bán, kiểm tra chồng lấn và booking chưa kết thúc. ScheduleGuard dùng khóa giao dịch theo cụm cho tạo booking và sửa cấu hình. |
| Lịch sử giờ/giá | Các projection booking của khách/Owner dùng StartDateTime, EndDateTime và TotalAmount đã lưu, độc lập khung hiện tại. |
| Giờ/giá khi gửi đặt | Form và AJAX gửi báo giá cho từng khung gồm ngày/giờ/giá; server so sánh với dữ liệu đọc lại dưới khóa. Thay đổi giờ hoặc giá yêu cầu khách xem và xác nhận lại, kể cả khi tổng tiền không đổi. |
| Qua nửa đêm | Validation từ chối giờ qua ngày, 24:00, giờ bằng nhau và giờ không tròn phút. |
| Giá lễ | ResolveDayType chỉ phân biệt ngày thường/cuối tuần; trang giá công khai và biểu mẫu Owner không hiển thị giá lễ. |
| Múi giờ | ScheduleGuard.Now cung cấp giờ Việt Nam cho booking, Owner, hết hạn, VNPay, hủy và lịch chọn ngày; mốc đếm ngược gửi kèm +07:00. |
| Giờ mở cửa cụm | Thay đổi giờ kiểm tra khung đang bán và booking chưa kết thúc trong giao dịch cùng khóa lịch; khung mới và lần đặt mới đều phải trong giờ mở cửa. |
| Trùng lịch sau đổi giờ | Lưới giờ và bộ lọc tìm sân so khoảng StartDateTime/EndDateTime đã lưu trên Booking; POST tạo booking cũng so khoảng giờ dưới khóa giao dịch. Không dùng riêng TimeSlotId để kết luận còn trống. |

Khung cùng giờ bắt đầu không được tạo hai bản ghi do unique index hiện có. Muốn bán lại hãy sửa/mở bán khung cũ. Không mở bán khung chồng lấn booking chưa kết thúc trên khung đã ngừng bán.

## 6. Ca nghiệm thu bắt buộc

1. Một khung và hai khung liền nhau đặt thành công; hai khung cách nhau bị từ chối cả trên UI lẫn POST trực tiếp. Quá 6 khung, khung trùng ID hoặc khác sân đều bị từ chối.
2. Khung chồng lấn bị chặn; hai khung tiếp giáp được chấp nhận. Giờ ngoài giờ mở cửa cụm bị chặn khi cụm có cấu hình giờ.
3. Có booking tương lai Pending/Confirmed hoặc booking đang diễn ra: sửa giờ bị chặn. Booking đã hủy/hết hạn không còn giữ chỗ. Kiểm tra cả hai luồng chạy đồng thời: tạo booking và sửa giờ.
4. Ngừng bán khung không làm mất booking, không ngăn xử lý booking cũ và không cho tạo booking mới trên khung đó.
5. Đổi giá không thay đổi tiền booking cũ; booking mới dùng giá mới. Trang khách mở trước khi đổi giá phải yêu cầu xác nhận lại nếu gửi đặt sau khi giá đổi.
6. Sau khi sửa giờ một khung không còn lịch đang giữ chỗ, mọi trang khách/Owner và xác nhận thanh toán vẫn hiển thị giờ, tiền cũ cho booking lịch sử.
7. Từ chối 23:00–01:00, 23:00–00:00, giờ bằng nhau, 24:00 và lần đặt bắc qua hai ngày.
8. Ngày lễ vẫn áp dụng giá theo thứ trong tuần; không dùng PriceHoliday dù database có giá trị khác 0.
9. Máy chủ chạy UTC vẫn xác định đúng ngày bán, cuối tuần và giờ đã qua theo Việt Nam. Thử gần 00:00 và ranh giới thứ Sáu/thứ Bảy.

## 7. Thanh toán chuyển khoản QR

- Owner đã được duyệt và đang hoạt động có thể bật chuyển khoản với một bộ thông tin dùng chung cho tất cả sân của mình. Chủ sân tự quét thử ảnh, đối chiếu và xác nhận tài khoản.
- Các khung trong cùng lượt đặt dùng chung một nội dung chuyển khoản và một lần đối soát toàn bộ số tiền. Mỗi khung vẫn có Booking và Payment riêng để theo dõi hủy và hoàn tiền.
- Tổng tiền, tài khoản nhận và ảnh QR được lưu lúc đặt. Thay đổi thông tin hoặc tắt cấu hình chỉ áp dụng cho lượt đặt mới. Khách của nhóm và Owner sở hữu nhóm xem được ảnh qua tuyến đã đăng nhập.
- Giữ chỗ ban đầu 15 phút theo mặc định, tối đa đến giờ bắt đầu khung đầu tiên. Lần báo đầu tiên đúng hạn đặt lại hạn chờ kiểm tra thành 15 phút kể từ lúc báo, tối đa đến giờ bắt đầu. Báo lại sau khi được yêu cầu bổ sung giữ nguyên hạn. Dùng giờ Việt Nam.
- Báo chuyển khoản và biên lai tạo thông báo chờ đối soát. Owner xác nhận đủ tiền khi còn hạn mới chuyển cả nhóm sang đã xác nhận/đã thanh toán. Nếu chưa thấy hoặc sai tiền, Owner chọn cần bổ sung và ghi lý do. Báo tiếp tạo bản ghi mới, lịch sử trước đó vẫn xem được.
- Sau hạn, booking chuyển hết hạn và nhả chỗ. Khách đã chuyển có thể báo để đối soát. Owner xác nhận đủ tiền đến sau hạn chỉ ghi nhận chờ hoàn, giữ nguyên trạng thái hết hạn.
- Khi khách đủ điều kiện hủy một khung đã trả, số tiền của khung đó chuyển sang chờ hoàn. Owner hoàn bên ngoài SportBooking rồi nhập ghi chú và xác nhận đã hoàn. Hệ thống lưu người xử lý và thời điểm.
- Callback VNPay chỉ phục vụ giao dịch cũ trong đợt chuyển đổi. [Hướng dẫn QR](THANH_TOAN_QR.md) · [Chuyển tiếp VNPay](THANH_TOAN_VNPAY.md)

## 8. Phản hồi đánh giá và báo cáo doanh thu

- Owner đã duyệt và đang hoạt động được xem đánh giá sân mình, kể cả sân đã xóa mềm. Đánh giá đang hiện có một phản hồi công khai, tối đa 1.000 ký tự. Owner được sửa phản hồi và app lưu thời điểm cập nhật phản hồi. Khi Admin ẩn đánh giá, phản hồi ẩn theo và không thể sửa trong thời gian bị ẩn.
- Báo cáo chủ sân dùng `Payment.Amount`, lọc theo ngày hạch toán giờ Việt Nam, cụm và sân. Tổng thu tính theo thời điểm ghi nhận thu tiền của bản ghi Paid, RefundPending hoặc Refunded. Đã hoàn tính từ bản ghi Refunded có mốc hoàn được ghi nhận. Ròng bằng tổng thu trừ đã hoàn trong kỳ. Khoản chờ hoàn chưa được trừ.
- Thu và hoàn ở hai ngày được ghi tại hai ngày tương ứng. Báo cáo giữ sân xóa mềm trong phạm vi Owner, không cộng booking chưa thu tiền. Khoản cũ thiếu Payment hoặc mốc thu/hoàn được cảnh báo, không tự suy ra ngày hạch toán từ ngày đặt. Dashboard Admin dùng cùng nguồn và cách tính ròng.
