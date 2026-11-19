# Black Knight: NPC rồi trở thành boss

Áp dụng ngay trên `Assets/Prefabs/Characters/BlackKnight.prefab` và instance đang đặt trong scene. Giữ nguyên model, vị trí, encounter ID, animation và cấu hình chiến đấu. Không tạo hai Black Knight chồng lên nhau.

## Luồng mặc định

1. Black Knight đứng yên, có thể tương tác và chọn câu trả lời. Không tấn công, không hiện thanh boss, không nhận sát thương trực tiếp / DOT / trạng thái bất lợi.
2. Nhận **Lời hẹn hiệp sĩ đen** từ hắn.
3. Hạ đúng Necromancer ở **Rừng hoang** (`boss:necromancer:wild-forest:01`). Chiến thắng trước khi nhận nhiệm vụ cũng được tính.
4. Quay về chọn báo cáo và nhận thưởng mặc định **100 vàng + 200 EXP**.
5. Đọc lời thách đấu rồi kết thúc đối thoại. Sau khi đóng xong và chờ **0,8 giây**, Black Knight trở thành boss hai phase như cấu hình chiến đấu hiện có.

Đủ mục tiêu nhưng chưa báo cáo vẫn là NPC. Boss không đánh trong khi đối thoại / menu đang mở. Báo cáo lại không nhận thưởng hai lần. Nếu đã báo cáo rồi tải lại game, Black Knight chuyển sang trạng thái boss; nếu đã hạ boss thì không xuất hiện lại thành NPC.

## Chỉnh trong Unity

- Trên root **BlackKnight**, component **Quest Boss Encounter**: `Required Quest` là điều kiện chuyển, `Aggression Delay` là khoảng chờ sau khi đóng đối thoại, `Conversation` trỏ tới NPC con.
- `Assets/Resources/Quests/BlackKnightChallenge.asset`: đổi tên, mô tả, mục tiêu và phần thưởng. `Encounter Id` để trống nếu muốn chấp nhận bất kỳ boss cùng loại; giữ ID hiện tại để chỉ đích danh Necromancer Rừng hoang.
- `Assets/Dialogue/BlackKnightConversation.asset`: chỉnh câu thoại và lựa chọn. `AcceptQuest` nhận, `ReportQuest` báo cáo và phát thưởng, `End` đóng đối thoại. Các lựa chọn chỉ xuất hiện khi khớp `Condition Quest / Required Status`.
- NPC nằm ở con **FriendlyConversation**, dùng **Npc Interactable** và giao diện đối thoại chung. Renderer và Animator vẫn nằm trên root, dùng cùng sprite với boss.
- Nếu đổi sang một Quest asset khác, đổi cả `Required Quest` trên root và các tham chiếu `Quest / Condition Quest / Text Quest` trong dialogue cho cùng asset.

Không cần bật/tắt thủ công các component combat khi sửa prefab: hệ thống ghi nhớ cấu hình đang bật, tạm ngắt lúc làm NPC rồi khôi phục khi chiến đấu. Không gắn NpcStationary vào root có Rigidbody của boss; NPC con đã có phần đứng yên riêng và controller quản lý thân vật lý của root.

## Kiểm tra

Đã kiểm tra trực tiếp qua lựa chọn hội thoại: nhận nhiệm vụ, đủ mục tiêu vẫn thân thiện, xác nhận báo cáo trả đúng thưởng một lần, chờ đối thoại đóng rồi khôi phục AI / tag / layer / Rigidbody / khả năng nhận đòn. Kiểm tra thêm tải lại nhiệm vụ đã hoàn thành và encounter đã bị hạ. Save dùng cho test được phục hồi nguyên trạng; các thay đổi scene chưa lưu được giữ lại.
