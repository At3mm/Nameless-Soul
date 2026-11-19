# Cutscene checkpoint trong game

Play `WastelandScene`, đến gần tượng và bấm **E**. Checkpoint được lưu trước, rồi chạy cutscene khoảng 5,15 giây: zoom thẳng vào tượng trong 1,25 giây, giữ góc trong 2,65 giây để các ký tự sáng lên, rồi zoom về trong 1,25 giây. Camera không lia hoặc nghiêng.

Checkpoint tắt **Allow Skip**: Esc không bỏ qua cutscene và không mở menu trong khi đang phát. Sau khi hoàn tất, Esc mở/đóng menu bình thường. `SettingsMenuToggle` kiểm tra `InGameCutscene.BlocksPauseMenu` để tránh xung đột pause.

Vòng gồm 12 ký tự dùng trực tiếp bốn sprite `TX Props Altar Rune 1–4` trong `Assets/Cainos/Pixel Art Top Down - Basic/Texture/TX Props.png`. Component **Checkpoint Rune Visuals** cho chúng sáng dần theo thứ tự, với 8 mức sáng trong 2,2 giây. Tượng chưa mở không hiện ký tự; tượng đã mở giữ ký tự sáng, kể cả khi lưu checkpoint khác. Trạng thái được đọc lại khi tải scene hoặc mở game. Cutscene chỉ phát lần đầu mở tượng; các lần tương tác sau chọn lại điểm hồi sinh và mở menu checkpoint.

## Chỉnh trong Inspector

Chọn `Checkpoint Statue` → component **In Game Cutscene**:

- **Shots**: mỗi phần tử là một góc máy. `Offset` tính từ `Focus` (để trống sẽ dùng tâm tượng), `Camera Size` càng nhỏ càng zoom gần, `Roll` là góc nghiêng 2D, `Travel Time` là thời gian chuyển, `Hold Time` là thời gian giữ góc và `Caption` là phụ đề.
- **Return Time**: thời gian trở về camera trước cutscene.
- **Effect Animator**: Animator chạy trong lúc cutscene tạm dừng gameplay.
- **Checkpoint Runes**: bộ ký tự sáng lên sau khi camera đến góc gần. `Glyphs`, `Dormant Opacity`, `Lit Opacity`, `Activation Duration` nằm trong **Checkpoint Rune Visuals**.
- **Allow Skip**: hiện tắt ở prefab checkpoint.
- **Play Only Once**: bật nếu chỉ muốn xem một lần trong mỗi lượt tải scene. Mặc định tắt để thử nhiều lần.

Cấu hình nằm trong `Assets/Prefabs/World/CheckpointStatue.prefab`, áp dụng cho các tượng dùng prefab này. Có thể override từng tượng trong scene.

## Hành vi khi phát

Tạm dừng gameplay bằng `Time.timeScale = 0`, tắt input combat/di chuyển của Player, ẩn HUD. Camera và bộ ký tự dùng thời gian thực. Khi kết thúc hoặc component bị tắt/scene bị gỡ, hệ thống trả lại camera, trạng thái Cinemachine Brain, HUD, input và tốc độ thời gian trước đó.

Game dùng camera orthographic 2D: đổi góc bằng pan, zoom và nghiêng trong mặt phẳng. Cutscene không tạo phối cảnh 3D hoặc xoay ra sau sprite.

Để gọi từ vật thể khác, nối code tới `InGameCutscene.Play(PlayerInteractor)`. Chỉ một cutscene được chạy cùng lúc. Chức năng này chưa dùng Timeline; các shot chỉnh trực tiếp trong Inspector.
