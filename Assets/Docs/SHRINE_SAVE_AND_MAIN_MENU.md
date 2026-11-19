# Lưu hành trình và về menu chính

- Mở tượng thần để mở khóa điểm du hành. Chỉ mở menu không đổi vị trí lưu.
- Chọn **Cầu nguyện**, đợi hết fade để lưu scene, ID tượng, điểm hồi sinh và mốc của Bùa hồi quy. Cầu nguyện đồng thời hồi phục và đặt lại quái thường.
- Du hành vẫn đặt tượng đích làm điểm hồi sinh như trước.
- Trong menu tạm dừng, chọn **Thiết lập → Về menu chính**. Chọn Quay lại hoặc Esc để hủy; chọn Về menu chính để xác nhận. Có thể dùng chuột hoặc W/S, A/D và E.
- Ở menu chính, **Tiếp tục** tải về tượng đã lưu. Nếu chưa lưu tượng, bắt đầu ở vị trí mặc định của bản đồ đầu tiên.
- Vàng, Soul/EXP, vật phẩm, nâng cấp, nhiệm vụ và tiến trình boss tiếp tục được lưu tự động theo giao dịch. Về menu không hoàn tác những dữ liệu này và không lưu tọa độ hiện tại ngoài tượng.
- Hạ Elite hoặc Boss tự động lưu một lần nữa sau khi xử lý phần thưởng, vật phẩm rơi và tiến trình nhiệm vụ. Không đổi điểm hồi sinh; hạt của Elite vẫn cần được nhặt nếu đang nằm trên đất.
- Đây là một slot lưu cục bộ qua PlayerPrefs. Bắt đầu mới có xác nhận riêng và thay thế tiến trình hiện tại.

Điểm xuất hiện được chỉnh bằng `CheckpointInteractable.respawnPoint`; ID tượng cần duy nhất và ổn định trong scene. Scene cần có trong Build Settings.
