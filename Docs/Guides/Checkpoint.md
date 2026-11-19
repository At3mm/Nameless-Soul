# Tương tác và checkpoint

- Trong game: đến gần tượng và bấm **E** để mở khóa tượng và mở menu. Kích hoạt lần đầu hoặc mở lại menu không hồi HP/Stamina, không bổ sung bình và không xóa debuff. Xác nhận **Cầu nguyện** hoặc **Du hành** mới hồi phục.
- Đang giao tranh không thể dùng tượng: không lưu checkpoint, hồi phục, mở cutscene/menu, cầu nguyện hay du hành. Áp dụng cho quái thường, Elite và Boss đang nhắm vào người chơi trong cùng scene, cả lúc phát hiện, truy đuổi, tấn công, vờn/rút lui và bị break. Quái ở sát trong vùng phát hiện tức thì cũng chặn ngay trước lần cập nhật AI kế tiếp. Khi quái chết hoặc đã mất mục tiêu và quay về nhà (ngoài vùng tái phát hiện), tượng tự dùng lại được. Gần tượng sẽ hiện thông báo bị khóa do giao tranh; cửa cũng bị khóa tương tác trong combat; rương/công tắc dùng điều kiện riêng.
- Chết: đợi 1,5 giây, chuyển cảnh rồi hồi sinh trước tượng đã lưu; nếu chưa lưu, trở lại vị trí bắt đầu phiên chơi. Quái cũng được tạo lại khi scene tải lại.
- Checkpoint được lưu cục bộ bằng PlayerPrefs, khóa `Noname.Checkpoint.v1`. Khóa này giữ điểm hồi sinh; tiến trình kinh tế, quái đã hạ, vật phẩm và nhiệm vụ có các khóa lưu riêng. Cầu nguyện lưu hành trình; hạ Elite/Boss tự lưu thưởng mà không đổi tượng hồi sinh.
- Mở game vào MainMenu; nút Tiếp tục tải checkpoint đã lưu. Trong Unity Editor, Play bắt đầu tại scene đang mở để thuận tiện chỉnh sửa; chết vẫn về checkpoint đã lưu.

## Trạng thái và menu

- Chưa mở: hoàn toàn không hiển thị ký tự dưới chân tượng. Tương tác lần đầu mở khóa tượng và chạy cutscene kích hoạt không thể bỏ qua, không tự hồi phục.
- Đã mở: ký tự giữ sáng, kể cả khi đã chọn checkpoint khác hoặc tải lại game. Mỗi lần bấm E mở menu, chưa thay đổi điểm hồi sinh, không phát lại cutscene.
- Hồi sinh tại checkpoint đã **cầu nguyện hoặc du hành đến gần nhất**, không tự chọn tượng gần vị trí chết nhất.
- Menu có năm mục: **Nâng chỉ số**, **Hiến tế vật phẩm**, **Thanh tẩy**, **Cầu nguyện**, **Du hành**. Nâng chỉ số nay dùng EXP, không có level: chọn từng thông số, xem trước rồi xác nhận; chi phí tăng theo tổng số lần đã nâng. Hiến tế tiêu hao một Hạt sinh mệnh để tăng giới hạn bình máu (3 đến 10). Thanh tẩy tiêu hao Hương thanh hồn để chữa Ăn hồn và đặt lại xác suất nhiễm về 6%. Quy tắc Gold/EXP và cấu hình ở [Economy.md](Economy.md).
- Chọn **Cầu nguyện**, rồi bấm nút **Cầu nguyện [Enter]** hoặc Enter để xác nhận. Chọn/rê chuột qua mục không tự kích hoạt. Hồi đầy HP, Stamina, bổ sung bình máu và xóa debuff của người chơi; quái thường trong scene của tượng trở về vị trí và trạng thái xuất hiện ban đầu, hồi đầy máu/sức bền, hết break/debuff và hồi sinh kể cả khi xác đã bị xóa. Boss và Elite giữ nguyên, kể cả trạng thái đã chết. Các scene khác đang tải cộng thêm không bị ảnh hưởng. Menu vẫn mở sau nghi thức; Esc trở lại gameplay.
- `NormalEnemyRespawns` tự tạo bộ lưu trạng thái ban đầu theo scene; không cần gắn thêm component vào prefab quái. Chỉ quái có `EnemyClassification.enemyType = Normal` được ghi nhận, kể cả quái thường sinh ra trong lúc chơi. Trạng thái này tồn tại trong lượt tải scene, không ghi vào PlayerPrefs. Cơ chế không tải lại scene để bảo toàn Boss/Elite.
- **Du hành** → **Chọn điểm đến** → chọn tượng bằng chuột, ↑/↓, W/S hoặc cuộn chuột → **Du hành [Enter]**. Danh sách chỉ có tượng đã kích hoạt, bao gồm tượng đang đứng nhưng làm mờ và khóa xác nhận lựa chọn đó. Nếu có nhiều tượng, dùng nút ‹/› để đổi trang. Esc trong danh sách quay lại menu tượng; Esc lần nữa rời menu. Khi chưa mở tượng khác, nút xác nhận bị khóa và có thông báo.
- Du hành cùng scene đưa nhân vật đến `RespawnPoint` của tượng đích và đặt lại quái thường bằng cùng cơ chế Cầu nguyện: hồi sinh quái đã chết, trả vị trí/máu/sức bền/AI về ban đầu, giữ ngoại lệ Elite/Boss trong scene đang chạy. Du hành khác scene tải lại khu vực đích từ dữ liệu scene. Cả hai dùng hiệu ứng tối/sáng, khóa điều khiển và pause menu trong lúc chuyển; tìm tượng bằng `Checkpoint Id` và lấy vị trí hiện tại của tượng.
- Đến tượng đích sẽ hồi đầy HP/Stamina, bình thuốc, xóa debuff và buff tạm thời, đưa chiến ý về ban đầu, làm mới hồi chiêu kỹ năng/dash/bình. Sức chứa bình hiện có được giữ lại. Tượng đích trở thành checkpoint hồi sinh mới; Gold, EXP, điểm đã nâng và danh sách tượng đã mở được giữ nguyên. Không phát lại cutscene kích hoạt. Hồi sinh sau khi chết vẫn hồi đầy như trước.
- Save checkpoint cũ vẫn dùng được. Danh sách mở nay lưu thêm tên tượng; tương tác lại sẽ cập nhật tên từ `Checkpoint Menu.checkpointName`. Với save cũ chưa có tên và scene chưa tải, danh sách tạm dùng ID của tượng. Scene của tượng phải nằm trong Build Settings; các mục có scene đã bị xóa khỏi Build Settings sẽ không hiện.
- Esc hoặc nút Đóng đóng menu, trả lại gameplay. Menu checkpoint và menu pause không mở cùng lúc.
- Danh sách đã mở lưu riêng trong `Noname.Checkpoint.Unlocked.v1`; vị trí hồi sinh gần nhất vẫn ở `Noname.Checkpoint.v1`. Save cũ tự được nhận diện là checkpoint đã mở.

## Giao diện từ asset có sẵn

Menu tham khảo bố cục tượng Phật trong Sekiro: danh sách bên trái, vùng chọn vàng, cảnh tượng vẫn thấy ở giữa, bảng thông tin tối bán trong suốt bên phải. HUD tạm ẩn và hiện lại khi đóng. Tham chiếu: https://www.fromsoftware.jp/manual/sekiroshadowsdietwice/stadia/sculptorsidolmenu.html

Đã xem bảng đối chiếu 65 icon trong các nhóm Icons, Items, Equipment của PaperBook và kiểm tra thêm rương/mặt trời. Các mục dùng `PBIcon_Items_Items_01` (sách), `PBIcon_TreasureChest_TreasureChest_05` (rương mở), `PBIcon_DayAndNightCycle_DayAndNightCycle_07` (mặt trời), `PBIcon_DayAndNightCycle_DayAndNightCycle_09` (trăng cho Cầu nguyện). Bảng trạng thái dùng tim đỏ, tim vàng, phi tiêu, giáp và bình máu sẵn có. Nền giấy tối và vùng chọn vàng lấy từ FantasyUI; không dùng ảnh hoặc icon của Sekiro trong game.

Có thể đổi `Checkpoint Name`, `Content Sprite`, `Selection Sprite`, `Section Icons`, `Stat Icons` trên **Checkpoint Menu** trong prefab tượng. Font hiện dùng hỗ trợ dấu tiếng Việt. Rê chuột, click, mũi tên hoặc W/S đổi mục; Esc hoặc Rời tượng đóng menu.

Mục Du hành dùng icon ngôi nhà `PBIcon_Icons_Icons_07` của PaperBook. Bảng điểm đến dùng cùng giấy và màu vàng của menu tượng, có nền tối riêng để các chữ phía sau không lẫn vào danh sách.

## Đặt thêm tượng

1. Kéo `Assets/Prefabs/World/CheckpointStatue.prefab` vào scene đã có trong Build Settings.
2. Chỉnh `Checkpoint Id` thành tên dễ nhận biết và riêng cho tượng trong scene. Đặt `Checkpoint Menu → Checkpoint Name` thành tên hiển thị riêng để dễ nhận ra trong danh sách Du hành.
3. Di chuyển con `RespawnPoint` đến vùng đất trống, tránh collider và hồ nước. Đây là vị trí hồi sinh, không phải tâm tượng.
4. Chỉnh `Interaction Radius` nếu muốn thay khoảng cách bấm E, rồi lưu scene.

`CheckpointService` và `PlayerInteractor` được khởi tạo tự động, không cần gắn lại vào từng Player. `deathDelay` của service điều chỉnh thời gian chờ sau khi chết.

## Đồ vật tương tác khác

Gắn `World.EventInteractable` vào cửa, rương hoặc công tắc; đặt `Interaction Label`, `Interaction Radius`, và nối hàm cần gọi vào `On Interact` trong Inspector. Bật `Single Use` nếu chỉ dùng một lần trong lượt tải scene. Collider là tùy chọn cho tương tác theo khoảng cách; vẫn cần collider riêng nếu muốn chặn nhân vật.

Với hành vi phức tạp, kế thừa `WorldInteractable`, triển khai `Interact(PlayerInteractor)` và có thể ghi đè `IsAvailable`. Hệ thống chỉ chọn đồ vật khả dụng gần nhất, không tương tác khi chết, pause, đang dash hoặc khóa hành động.

Xóa checkpoint khi phát triển: gọi `World.CheckpointService.ClearSaved()`; xóa cả vị trí hồi sinh và danh sách checkpoint đã mở, không ảnh hưởng các PlayerPrefs khác.
