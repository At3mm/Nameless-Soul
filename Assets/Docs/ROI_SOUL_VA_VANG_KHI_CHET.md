# Rơi Soul/EXP và vàng khi chết

Mỗi lần người chơi chết, 50% số vàng và Soul/EXP hiện có được trừ khỏi ví, rồi để lại trong một túi tại vị trí chết và scene đã chết. Phần rơi làm tròn xuống từng loại; phần lẻ giữ lại trong ví. Ví dụ 101 vàng + 55 EXP sẽ rơi 50 vàng + 27 EXP và giữ lại 51 vàng + 28 EXP.

Chỉ có một túi tồn tại trên toàn bộ bản đồ. Nếu chết tiếp khi chưa thu hồi, túi cũ bị xóa vĩnh viễn, và túi mới chỉ chứa 50% ví tại lần chết mới. Nếu ví không đủ để rơi gì (0/1 mỗi loại), túi cũ vẫn mất và không tạo túi trống.

Sau hồi sinh, quay lại dấu đồng tiền tại nơi chết rồi bấm E để thu hồi. Không thu hồi khi đang chết, trong menu hoặc ở scene khác. Có thể thu hồi khi đang giao chiến. Mỗi túi chỉ nhặt được một lần. Nhặt lại là hoàn trả đúng số đã rơi, không áp dụng hệ số thưởng Ăn hồn và không giải trạng thái Ăn hồn.

Ví và túi dùng chung save GameEconomy (Noname.Economy.v1), được ghi trong cùng một lần cập nhật. Đổi scene, cầu nguyện, tải lại hoặc thoát game không xóa túi. Trở lại scene cũ sẽ khôi phục dấu nhặt; chết tiếp dù ở scene khác vẫn xóa túi trước. New Game và các công cụ reset/test từ đầu xóa/khôi phục cùng save kinh tế hiện có.

Nếu nhặt khiến ví vượt giới hạn số nguyên, túi được giữ lại; cần tiêu bớt tài nguyên trước khi thu hồi. Vật phẩm, Hạt sinh mệnh, nâng cấp đã mua, tiến độ nhiệm vụ và cổ vật không bị rơi.

Chỉnh icon, cỡ dấu, khoảng cách tương tác và nhịp nổi tại Assets/Resources/DefaultPlayerDeathDrop.asset. Hệ thống tự tạo dấu nhặt khi cần, không phải đặt sẵn trên map. Hoạt ảnh dấu nhặt lấy mẫu 12 FPS; không sửa clip/frame/marker của nhân vật.

Đã kiểm tra 21 trường hợp trong Play Mode: hook chết thực tế, làm tròn phần rơi, marker duy nhất, điều kiện nhặt, chống nhặt trùng, xóa túi cũ, tải lại, ví đầy và giữ nguyên vật phẩm/Ăn hồn. Đã xem dấu nhặt và lời nhắc E trực tiếp trong Unity. Toàn bộ save thật được khôi phục sau test.

