# Cân bằng sát thương theo khu vực

Thứ tự: Lối mòn cổ → Rừng tiên → Rừng hoang → Vùng đất chết.

## Chỉnh trong Unity

1. Mở `Assets/Combat/Balance`. Bốn preset được đánh số theo thứ tự trên.
2. Chọn preset, mở danh sách **Enemies**, chọn đúng **Archetype Id**.
3. **Attack** là ATK; **True Damage** là STR xuyên phòng thủ; **Skills / Damage Multiplier** là hệ số từng đòn. **Critical Chance** hiện bằng 0 để sát thương dễ dự đoán.
4. Mỗi asset khu vực ở `Assets/WorldAreas` có ô **Enemy Damage Profile** để gán preset. Khu vực mới có thể dùng lại hoặc tạo preset qua Create → Game → Balance → Area Enemy Damage.
5. Khi Play, quái tự lấy preset ở vị trí xuất hiện. Đuổi người chơi sang vùng khác không đổi sát thương giữa trận. Dơi triệu hồi kế thừa preset của Necromancer.

Muốn cập nhật chỉ số hiển thị của các quái đang đặt trên scene ngay trong Edit Mode: Tools → Enemies → Apply Area Damage Balance, sau đó lưu scene. Công cụ giữ các preset đã tồn tại và các gán khu vực đã có; không ghi đè chỉnh sửa preset của bạn.

Các prefab quái có **Enemy Area Damage**: `Archetype Id` xác định loại quái, `Fallback Profile` dùng nếu không tìm thấy preset khu vực, `Profile Override` ưu tiên cao nhất cho trường hợp cần cấu hình riêng. Khi component này hoạt động, chỉnh ATK/STR/hệ số đòn trong preset vì các giá trị tương ứng trên CharacterStats và SkillDamageManager được áp lại lúc xuất hiện.

## Mốc ban đầu

Với người chơi DEF 10, không buff và không Ăn hồn:

- Lối mòn cổ: Slime khoảng 5,3 / 14,3 máu với đòn thường / nặng.
- Rừng tiên: Orc 13 / 21,8; Werewolf 15 / 21; đòn mạnh Werebear khoảng 34; Orc Rider khoảng 31,2.
- Rừng hoang: Orc 17 / 27; Werewolf 19 / 25,75; EliteOrc khoảng 16,75–38,2 tùy đòn.
- Vùng đất chết: Necromancer 30 / 39,25; dơi 7 / 13,4.

Sát thương mỗi hit = max(0, ATK × hệ số đòn − DEF người chơi) + STR. Buff và Ăn hồn vẫn được áp bởi hệ thống hiện có. Ăn hồn mặc định nhân sát thương nhận vào 1,25.

Người chơi có 1 giây miễn sát thương sau hit, vì vậy nhiều marker ExecuteHit liên tiếp không đồng nghĩa tất cả cùng gây mất HP. Summon chỉ tạo dơi; hệ số của Summon không phải một đòn gây sát thương trực tiếp.

## Giới hạn và kiểm tra

Đây là vòng cân bằng sát thương đầu tiên dựa trên nhân vật cơ bản 100 HP / 10 DEF, chưa phải kết quả chơi thử toàn tuyến. Không thay HP, phòng thủ, tốc độ, cooldown, phần thưởng, phase, clip hoặc marker animation.

EliteOrc hiện đặt trong map vẫn có 60.000 HP và DEF 1.000. Chỉ ATK 600 / STR 90 cũ được đưa về preset Rừng hoang ATK 33 / STR 2. Nếu các chỉ số sống sót này là dữ liệu thử nghiệm, cần cân bằng riêng để thời gian hạ quái hợp lý.

Đã kiểm tra: đủ 42 quái được gán trong Edit Mode; các chỉ số ngoài sát thương được giữ; chạy lại không cộng dồn; runtime chọn đúng cả 4 vùng; 80 trường hợp qua CharacterHealth; hit kế tiếp trong thời gian miễn sát thương bị chặn; một lần triệu hồi tạo 2 dơi nhận đúng preset boss. Save người chơi được sao lưu và phục hồi nguyên trạng sau kiểm tra.
