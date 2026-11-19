# Ăn hồn và thanh tẩy

Trạng thái dành cho người chơi. Mỗi lần người chơi chết, nếu chưa nhiễm, hệ thống thử một lần theo tỉ lệ hiện tại. Lần chết đầu: 6%; lần tiếp theo: 7%, 8%... tăng 1 điểm phần trăm mỗi lần chết, tối đa 100%. Khi đã nhiễm, hiệu ứng không cộng dồn. Số lần chết vẫn được ghi nhận; thanh tẩy thành công xóa trạng thái và đặt bộ đếm về 0, tương ứng 6% cho lần chết tiếp theo.

Trạng thái và bộ đếm nằm trong GameEconomy (Noname.Economy.v1), tồn tại qua hồi sinh, tải save, đổi scene và cầu nguyện. New Game xóa cùng tiến trình. Không dùng bộ đếm thời gian của CharacterDebuffs nên thuốc giải độc, thuốc bỏng, thuốc an thần và hồi sinh không xóa trạng thái này.

## Hệ số ban đầu

- Sát thương người chơi gây ra: x0.8 (giảm 20%).
- Sát thương người chơi nhận vào: x1.25 (tăng 25%), gồm cả DOT.
- Vàng từ hạ quái: x1.5.
- Soul/EXP từ hạ quái: x1.5.

EXP hiện là tài nguyên Soul dùng nâng cấp trong dự án; không tạo thêm ví Soul riêng. Phần thưởng nhiệm vụ theo số cố định và tiền bán vật phẩm không được nhân, để tránh thay đổi phần thưởng đã hứa hoặc mua/bán kiếm lời do hệ số. Phần thưởng tiêu diệt quái được làm tròn tới số nguyên gần nhất (0.5 làm tròn lên).

Chỉnh tỉ lệ và hệ số tại Assets/Resources/DefaultSoulConsumption.asset. Thay Status Icon tại cùng asset. Sát thương được nhân với các buff hiện có, không sửa vĩnh viễn ATK/DEF gốc. Chỉ người chơi chịu hiệu ứng; boss và quái giữ nguyên chỉ số.

## Giải trạng thái

Mua Hương thanh hồn từ cửa hàng NPC, hoặc chọn vật phẩm này làm phần thưởng nhiệm vụ (Consumables → SoulPurification). Vào tượng thần → Thanh tẩy → Dùng 1 Hương thanh hồn [E].

Chỉ tiêu hao đúng 1 vật phẩm khi đang nhiễm và thanh tẩy thành công. Không nhiễm hoặc thiếu vật phẩm thì không thể thanh tẩy và không mất đồ. Vật phẩm không thể dùng ngoài tượng hoặc gắn vào ô dùng nhanh. Lần đầu mua có giá 150 vàng; giá bán lại 30 vàng. Chỉnh tên, mô tả, icon và giá trong DefaultConsumables.

Biểu tượng trạng thái thường trực xuất hiện trên HUD khi còn nhiễm. Mục Thanh tẩy hiển thị trạng thái, hệ số, số vật phẩm và xác suất hiện tại. Cầu nguyện chỉ hồi phục các trạng thái thông thường; Ăn hồn vẫn còn.

Đã kiểm tra trong Play Mode: 22 trường hợp xác suất/sát thương/phần thưởng/lưu tải, 9 trường hợp thanh tẩy tại tượng, mua bán vật phẩm, biểu tượng HUD và đăng ký sự kiện chết đúng một lần. Đã xem trực tiếp menu Thanh tẩy. Save thật được khôi phục sau test.

