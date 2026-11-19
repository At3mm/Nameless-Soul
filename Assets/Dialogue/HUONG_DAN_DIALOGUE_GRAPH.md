# Dialogue Graph

Mở **Tools > Dialogue and Quests > Dialogue Graph**, hoặc nhấp đúp asset hội thoại. Có thể bấm **Mở đồ thị hội thoại** từ Quest Manager. Graph sử dụng trực tiếp DialogueDefinition, không tạo bản sao hội thoại và không chuyển định dạng dữ liệu cũ.

## Danh sách hội thoại

Tab **Tất cả hội thoại** liệt kê mọi DialogueDefinition trong thư mục Assets của dự án. Có ô tìm theo tên/đường dẫn, số bảng và điều kiện vào; bấm tên để mở đồ thị, hoặc Tìm asset để định vị trong Project. Danh sách tự cập nhật khi tạo/đổi tên/xóa asset; cũng có nút Làm mới.

## Làm một đoạn thoại có lựa chọn

1. Chọn asset ở thanh trên, hoặc bấm Tạo hội thoại.
2. Bấm **+ Bảng thoại** (hoặc chuột phải trên nền). Mỗi bảng có ID riêng.
3. Viết Người nói và lời thoại trực tiếp trong bảng. Bấm bảng để chỉnh đầy đủ ở cột phải.
4. Kéo chấm đầu ra bên phải của Bắt đầu vào chấm Vào bên trái bảng đầu tiên.
5. Kéo dây Tiếp tục từ bảng này đến bảng tiếp theo. Chưa nối đích tương đương kết thúc.
6. Trong cột phải, bấm **+ Lựa chọn**, nhập câu trả lời, chọn Hành động và kéo dây của lựa chọn đến đoạn tiếp theo.
7. Bấm **Lưu · Ctrl+S**. Lưu này chỉ lưu asset hội thoại, không lưu scene.

## Nhận và tự hoàn thành nhiệm vụ

- Action Continue: đi đến bảng được nối.
- AcceptQuest: gán nhiệm vụ ở Nhận / trả nhiệm vụ, nối đến câu xác nhận đã nhận.
- Nhiệm vụ đã nhận đủ mọi mục tiêu sẽ chờ trả; quay lại nói chuyện với NPC giao nhiệm vụ thì tự hoàn thành và trao thưởng. Nhánh hậu nhiệm vụ dùng Continue và điều kiện Completed; không cần ReportQuest. ReportQuest chỉ còn để tương thích hội thoại cũ.
- End: đóng hội thoại, không cần dây đi tiếp.
- ChallengeBoss: đồng ý thách đấu; kiểm tra Quest Boss Encounter ở cha của NPC, đóng thoại rồi mới kích hoạt boss.
- Đã xem hết thoại hậu nhiệm vụ: gán quest trên bảng cuối của chuỗi thoại. Điều kiện vào Completed có thể bật Chưa xem hậu nhiệm vụ để chuỗi này chỉ phát đến khi được xem xong. Đây là dấu hội thoại, không quyết định trao thưởng.
- OpenShop: mở cửa hàng đã gắn trên NPC, không dùng dây tiếp theo.
- Điều kiện hiển thị: gán Condition Quest và Required Status cho lựa chọn. Lựa chọn chỉ hiện khi nhiệm vụ ở trạng thái đó.
- Text Quest: chọn nhiệm vụ để dùng {title}, {progress}, {rewards}, {status} trong lời thoại.

## Hội thoại khác nhau theo tiến độ

Bấm **+ Điều kiện vào**. Bảng cam chọn nhánh khởi đầu theo tình trạng boss / nhiệm vụ. Gán Persistent Id của boss, chọn đã hạ hoặc chưa hạ; có thể thêm điều kiện trạng thái nhiệm vụ. Khi có cả hai điều kiện, cần thỏa cả hai.

Các điều kiện kiểm tra từ trên xuống. Điều kiện khớp đầu tiên thắng. Nếu không khớp, game dùng nhánh Mặc định màu xanh. Chọn bảng cam rồi dùng nút tăng/giảm ưu tiên ở cột phải. Không để điều kiện trống nếu không muốn nó luôn khớp.

BlackKnightConversation đã có các nhánh trước khi hạ Necromancer đầu, lời thoại nhận nhiệm vụ, đang làm, báo cáo và thách đấu. Chỉnh trực tiếp những bảng này; mở/sắp xếp graph không tự sửa câu chữ hoặc điều kiện cũ.

## Thao tác và kiểm tra

- Kéo tiêu đề để di chuyển bảng; giữ chuột giữa kéo nền; cuộn để zoom.
- Xem toàn bộ để thu đồ thị vừa màn hình; tìm theo ID hoặc lời thoại rồi bấm Tìm để tập trung vào bảng.
- Sắp xếp bố trí lại các bảng theo luồng. Vị trí lưu riêng ở ProjectSettings/DialogueGraphLayouts.asset, không ảnh hưởng gameplay.
- Chọn bảng/dây rồi Delete để xóa. Xóa bảng sẽ gỡ các liên kết trỏ đến nó. Ctrl+Z hoàn tác.
- Đổi ID ở cột phải sẽ tự cập nhật các liên kết đến bảng đó.
- Cột phải và chân cửa sổ báo đích không tồn tại, ID trùng/trống, điều kiện chưa nối đích, lựa chọn nhận/trả chưa gán nhiệm vụ.
- Dừng Play để chỉnh sửa.

Kiểm tra tự động: **Tools > Dialogue and Quests > Test Dialogue Graph**. Dùng dữ liệu tạm trong bộ nhớ, không sửa nội dung Black Knight hay dữ liệu chơi.


## Tự nhận nhiệm vụ khi nói chuyện

Chọn bảng Bắt đầu, gán **Tự nhận nhiệm vụ**. Nhánh **Vừa nhận, chưa đủ mục tiêu** dùng cho lần nhận đầu tiên khi chưa hoàn thành mục tiêu. Nếu đã đủ mục tiêu từ trước, game tự nhận rồi chọn Entry Rule theo trạng thái Completed để đi thẳng vào đoạn hoàn thành. Lần nói chuyện sau không nhận lại.

Priest tự nhận nhiệm vụ Werebear khi nói chuyện lần đầu. Khi đủ mục tiêu, nhiệm vụ chờ trả. Quay lại nói chuyện với Priest thì tự trả, nhận thưởng và mở shop. Lần nói chuyện sau đi qua report → rewarded → reward_2 → reward_3 → reward_4 → start; bảng reward_4 ghi nhận đã xem hậu nhiệm vụ. Các lần sau vào menu start. Black Knight vẫn dùng lựa chọn nhận nhiệm vụ riêng và chỉ chiến đấu khi người chơi đồng ý.

Cấu hình NPC trả nhiệm vụ: trong Dialogue Graph, chọn Bắt đầu → Tự trả khi nói chuyện, thêm nhiệm vụ của NPC. Nhiệm vụ ở Tự nhận, lựa chọn AcceptQuest/ReportQuest hoặc dấu hậu nhiệm vụ cũng được tự nhận diện. NPC khác không tự trả thay. Không cần nút báo cáo.
