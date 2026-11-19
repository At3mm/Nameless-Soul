# Gán lại phím trong game

Mở **Thiết lập → Gán phím** ở menu đầu game, hoặc **Esc → Thiết lập → Gán phím** trong khi chơi.

- Bấm vào thao tác rồi nhấn phím bàn phím hoặc nút chuột mới. Hỗ trợ chuột trái, phải, giữa và hai nút phụ.
- Phím đã dùng cho thao tác khác sẽ bị từ chối và hiện tên thao tác đang dùng. Đổi phím của thao tác đó trước nếu muốn chuyển sang phím này.
- Esc hủy lượt gán hiện tại; Esc lần nữa quay lại. Sau 10 giây không chọn, phím cũ được giữ lại.
- Có nút **Khôi phục mặc định**. Mỗi thay đổi hợp lệ được áp dụng và lưu tự động, không cần khởi động lại game.
- Có thể dùng W/S hoặc mũi tên để chọn, E/Enter để xác nhận. Trong trang Thiết lập của menu tạm dừng, W/S đổi giữa âm lượng và Gán phím; A/D chỉnh âm lượng.

14 thao tác: bốn hướng di chuyển, lướt/giữ chạy, đánh thường, đỡ đòn, kỹ năng, uống máu, dùng vật phẩm, vật phẩm trước/tiếp, tương tác và tạm dừng.

Esc luôn là phím dự phòng để mở menu tạm dừng, kể cả khi gán thêm phím tạm dừng khác. Các phím điều hướng menu giữ nguyên. Cấu hình này dành cho bàn phím/chuột; các binding gamepad vốn có được giữ lại.

## Cấu trúc cho lập trình

`Assets/Scripts/Input/GameControls.cs` quản lý cấu hình và binding. `Assets/Scripts/UI/ControlBindingsMenu.cs` dựng hộp thoại dùng chung cho hai menu. Dữ liệu lưu ở PlayerPrefs `Noname.Controls.v1`, độc lập với tiến trình chơi nên Bắt đầu mới không xóa phím đã chọn.

InputAction được tạo qua `GameControls.CreateButton` / `CreateMove`, giải phóng bằng `GameControls.Release`. Các input đọc mỗi frame dùng `GameControls.Pressed` / `Button`. Nhãn HUD và nhắc tương tác dùng `GameControls.Display`, để hiện đúng phím sau khi đổi.

Không dùng các trường `useKey` / `interactKey` cũ để chỉnh input nữa; chúng được giữ ẩn để tương thích dữ liệu prefab đã lưu.

Đã kiểm tra routing của đủ 14 thao tác sau gán, từ chối trùng và Esc dành riêng, capture/cancel bằng Input System, không dùng bình khi đang gán, bình máu hoạt động với phím mới và bỏ qua phím cũ, di chuyển theo phím mới, cập nhật binding đang bật/tắt và khôi phục mặc định. Bố cục được kiểm tra trực tiếp trong Game View cho cả menu đầu game và menu tạm dừng.
