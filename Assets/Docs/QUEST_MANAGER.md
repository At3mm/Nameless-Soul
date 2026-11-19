# Quest Manager

Mở **Tools → Dialogue and Quests → Quest Manager**. Cửa sổ dùng tiếng Việt và sửa trực tiếp các Quest asset hiện có. Không thay đổi tiến trình / save người chơi.

## Tạo và sửa

- **Tạo mới**: lưu vào `Assets/Resources/Quests`. Tool sinh Quest Id riêng; thêm tên, mô tả, mục tiêu và phần thưởng trong tab **Nhiệm vụ**.
- **Nhân bản**: sao chép nội dung nhiệm vụ đang chọn, đồng thời sinh ID mới.
- **Tạo nhiệm vụ tiếp**: tạo nhiệm vụ mới với điều kiện trước là nhiệm vụ đang chọn. Ví dụ chọn A → tạo B; chọn B → tạo C để thành A → B → C.
- **Lưu asset**: lưu nhiệm vụ và hội thoại đang chọn; không tự lưu scene. Các sửa đổi hỗ trợ **Ctrl+Z**.
- Dùng ô tìm kiếm bên trái theo tên / ID. Có thể nhấp phải Quest asset trong Project → **Open in Quest Manager**.

Quest Id mặc định khóa để tránh đổi nhầm ID mà save đang dùng. Có thể bật ô cho phép sửa khi cần chủ động đổi ID.

## Mục tiêu và chuỗi

Kéo boss từ Hierarchy vào **Boss trên map / prefab**, chọn **Chỉ đúng lần gặp này**, rồi bấm **Thêm mục tiêu từ boss**. Boss phải có `Quest Boss Id` trong Enemy Classification.

Chọn một prefab boss sẽ lấy loại boss, không lấy một encounter cụ thể. Chọn instance trong scene để nhắm tới đúng lần gặp. Mục tiêu cũng có thể nhập tay; Encounter Id để trống thì tính mọi boss cùng loại.

Danh sách **Điều kiện trước** dùng để nối các nhiệm vụ có sẵn. Nhiệm vụ sau chỉ mở khi tất cả nhiệm vụ điều kiện đã được **báo cáo / trả xong**. Tool ngăn nối bản thân, nối trùng và tạo vòng lặp. Nếu thêm một dòng điều kiện chưa gán, hãy gán nhiệm vụ hoặc xóa dòng đó.

## Hội thoại và NPC

Trong tab **Hội thoại & NPC**:

1. Kéo NPC vào ô NPC; với Black Knight, dùng component ở con `FriendlyConversation` (chọn root trên Hierarchy trước cũng có thể tự nhận NPC con).
2. Chọn hội thoại hiện có hoặc **Tạo hội thoại mới**.
3. Bấm **Thêm lựa chọn nhận / trả nhiệm vụ này**. Tool thêm nhánh nhận khi Available và nhánh báo cáo khi ReadyToReport; giữ nguyên các đoạn thoại cũ và không thêm trùng nhánh đã có.
4. Bấm **Gán hội thoại cho NPC** nếu muốn áp dụng hội thoại đang chọn. Lưu scene nếu NPC đặt trên map.

Câu thoại mẫu có thể sửa bằng nút **Chọn hội thoại trong Inspector**. Với Black Knight đã có nội dung riêng, tool giữ các nhánh đã cấu hình.

Nếu NPC sẽ trở thành boss, kéo component **Quest Boss Encounter** vào ô tương ứng và bấm **Dùng nhiệm vụ này làm điều kiện mở boss**. Với một chuỗi nhiệm vụ, chọn nhiệm vụ cuối chuỗi làm điều kiện.

## Kiểm tra

Tab **Kiểm tra** phát hiện ID trùng / trống, thiếu mục tiêu, số lượng không hợp lệ, điều kiện bị vòng lặp hoặc asset nằm ngoài thư mục runtime cần dùng. Dừng Play Mode để chỉnh; khi Play tool chỉ cho xem.

Đã kiểm tra 46 trường hợp trên asset tạm: tạo / nhân bản / nối chuỗi, mục tiêu theo loại và encounter, chống trùng, bảo toàn hội thoại, thêm nhánh không lặp, gán NPC, Undo và lưu tham chiếu. Không thay đổi bảy khóa save / cài đặt người chơi hoặc trạng thái scene trong lúc test.
