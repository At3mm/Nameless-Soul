# PHÂN TÍCH USE CASE VÀ THIẾT KẾ LỚP — NONAME

**Đối chiếu mã nguồn ngày 14/11/2026.** Dự án sử dụng Unity 6000.3.9f1, C#, hệ thống component của Unity và các asset cấu hình ScriptableObject. Build Settings hiện có MainMenu, WastelandScene và CaveScene.

Tài liệu mô tả thiết kế đang có trong code, không phải thiết kế đề xuất cho tương lai. Sơ đồ lớp được chia thành ba phần nghiệp vụ để giữ khả năng đọc khi đưa vào báo cáo. Các lớp UI, hiệu ứng, âm thanh, công cụ Editor và một số cấu trúc lưu nội bộ được lược bớt; đây không phải sơ đồ toàn bộ mọi lớp trong dự án.

## 1. Phạm vi và tác nhân

Tác nhân chính là **Người chơi**, tương tác qua bàn phím và chuột. Người chơi có thể bắt đầu hoặc tiếp tục hành trình, khám phá các khu vực, chiến đấu, dùng vật phẩm, hội thoại, thực hiện nhiệm vụ, giao dịch và sử dụng tượng thần.

NPC và quái vật được xử lý bên trong hệ thống game nên không vẽ thành tác nhân bên ngoài. Unity Editor và các công cụ Quest Manager/Item Manager phục vụ phát triển, không thuộc phạm vi Use Case gameplay. Mã nguồn được đọc chưa thể hiện chức năng tài khoản, đăng nhập, multiplayer hoặc cơ sở dữ liệu trên máy chủ; các chức năng này không được đưa vào sơ đồ.

**Quy ước ký hiệu:**

- Đường liền giữa tác nhân và hình ellipse: tác nhân tham gia Use Case.
- `«include»`: hành vi bắt buộc trong một lần thực hiện thành công của Use Case gốc; mũi tên hướng đến hành vi được dùng lại.
- `«extend»`: hành vi tùy chọn tại điểm mở rộng; mũi tên từ Use Case mở rộng đến Use Case cơ sở. Các mục ở tượng thần và lựa chọn trong hội thoại là những điểm mở rộng được mô hình hóa ở mức nghiệp vụ.
- Mũi tên tam giác rỗng trong Class Diagram: kế thừa, hướng về lớp cha.
- Hình thoi đặc: thành phần dữ liệu được lớp chứa sở hữu, ví dụ danh sách `Node`, `Choice` hoặc `SkillData`.
- Đường liền có mũi tên: tham chiếu/liên kết. Đường đứt: phụ thuộc hoặc gọi dịch vụ.
- `+` là public, `-` là private, `#` là protected; `[static]` chỉ thành viên tĩnh. Tham số và kiểu dữ liệu ở một số ô được rút gọn để sơ đồ dễ đọc.
- `«MonoBehaviour»`, `«ScriptableObject»`, `«static service»` mô tả cách triển khai. Những component cùng nằm trên một GameObject không được coi là kế thừa lẫn nhau.

## 2. Use Case tổng quan

![Hình 1. Use Case tổng quan](images/01-UC-tong-quan.png)

### UC01 — Bắt đầu hành trình mới

**Tiền điều kiện:** đang ở menu đầu game và scene khởi đầu có trong Build Settings.

**Luồng chính:** người chơi chọn Bắt đầu mới; nếu có tiến trình cũ thì xác nhận thay thế; hệ thống giữ một bản sao tiến trình trước, đặt lại dữ liệu hành trình và tải scene đầu qua màn hình chuyển cảnh.

**Luồng thay thế:** người chơi hủy xác nhận thì tiếp tục ở menu. Cấu hình phím và âm lượng là thiết lập riêng, không đồng nhất với dữ liệu hành trình.

**Mã nguồn chính:** `MainMenuController.LoadFirstScene`, `CheckpointService.ClearSaved`, `ScreenFader`.

### UC02 — Tiếp tục tiến trình đã lưu

**Tiền điều kiện:** tồn tại dữ liệu tiến trình hợp lệ.

**Luồng chính:** chọn Tiếp tục; hệ thống đọc checkpoint đã lưu, tải scene tương ứng và đặt người chơi vào vị trí hồi sinh. Các dịch vụ nạp lại dữ liệu kinh tế, nhiệm vụ và trạng thái hạ quái.

**Luồng thay thế:** nếu không có tiến trình thì lựa chọn bị vô hiệu hóa; checkpoint không dùng được thì luồng menu có cơ chế về scene đầu.

**Mã nguồn chính:** `MainMenuController.ContinueGame`, `CheckpointService.ResumeSavedGame`.

### UC03 — Điều chỉnh cài đặt và gán phím

Người chơi mở cài đặt, điều chỉnh âm lượng hoặc vào phần điều khiển để gán lại thao tác. `GameControls` kiểm tra phím hợp lệ và trùng phím trước khi lưu, sau đó cập nhật các InputAction đang được theo dõi. Escape được giữ cho hủy/quay lại và chức năng pause dự phòng. Các lựa chọn cài đặt được lưu cục bộ.

**Mã nguồn chính:** `GameControls`, `ControlBindingsMenu`, `SettingsMenuToggle`, `MainMenuController`.

### UC04 — Di chuyển và khám phá khu vực

Người chơi di chuyển, lướt hoặc chạy khi đủ điều kiện tài nguyên. Khi đi qua vùng đã khai báo, `AreaManager` xác định khu vực và phát sự kiện đổi vùng; UI có thể hiển thị tên khu vực. Khi chạm trigger chuyển scene, hệ thống chuẩn bị spawn point và tải scene qua fade/loading.

**Mã nguồn chính:** `PlayerController`, `PlayerDash`, `AreaTrigger`, `AreaManager`, `SceneTransitionTrigger`, `SceneSpawnPoint`.

### UC05 — Chiến đấu với quái và boss

Người chơi thực hiện đánh thường, kỹ năng, đỡ đòn và né/lướt theo các điều kiện hành động, stamina, POS và cooldown. Animation Event gọi `SkillDamageManager.ExecuteHit` đúng thời điểm trúng đòn. Hệ thống tìm mục tiêu trong hitbox, tính sát thương từ chỉ số và hiệu ứng rồi chuyển cho `CharacterHealth`.

Quái sử dụng nhận biết có kiểm tra vật cản, đi tuần, đuổi, ra đòn và quay về. Khi thanh choáng cạn, `EnemyBreak` khóa hành động và phục hồi thanh trong `recoveryDelay`; bị đánh trong break không làm tụt tiến độ hay kéo dài thời gian. Boss có thể có phase và bộ chọn combo riêng. Chuyển phase là trạng thái khác break: có bảo vệ riêng, có thể xóa break và hiện tại ẩn toàn bộ UI boss trong thời gian chuyển.

**Hậu điều kiện:** cập nhật HP/tài nguyên; khi mục tiêu chết, ghi nhận hạ quái và trao phần thưởng theo loại. Chiến thắng boss được QuestService ghi nhận để tính tiến độ nhiệm vụ.

### UC06 — Quản lý túi đồ và sử dụng vật phẩm

Người chơi xem ba nhóm Vật phẩm hỗ trợ, Vật phẩm nhiệm vụ và Cổ vật cường địch; chọn vật phẩm phù hợp để gắn vào danh sách dùng nhanh và dùng ngoài menu. Vật phẩm tiêu hao chỉ bị trừ khi hiệu ứng áp dụng thành công. Bùa hồi quy là vật phẩm nhiệm vụ tái sử dụng, gọi dịch vụ checkpoint để về tượng đã cầu nguyện/du hành đến gần nhất và không được mua bán như thuốc.

**Mã nguồn chính:** `InventoryConsumablesView`, `PlayerConsumables`, `ConsumableCatalog`, `GameEconomy`, `QuestService`, `BossItemInventory`.

### UC07 — Hội thoại và chọn câu trả lời

**Tiền điều kiện:** NPC khả dụng, người chơi đủ gần và có thể tương tác.

Hệ thống mở node đầu của `DialogueDefinition`, thay các biến tiến độ/phần thưởng trong nội dung và lọc câu trả lời theo trạng thái nhiệm vụ. Người chơi chọn đáp án; hệ thống chuyển node hoặc thực hiện hành động nhận nhiệm vụ, báo cáo, mở cửa hàng hay kết thúc hội thoại. Lựa chọn không hợp lệ không được thực hiện.

**Mã nguồn chính:** `WorldInteractable`, `PlayerInteractor`, `NpcInteractable` và partial class trong `NpcDialogueChoices.cs`.

### UC08 — Nhận, thực hiện và trả nhiệm vụ

![Hình 3. Use Case hội thoại và nhiệm vụ](images/03-UC-nhiem-vu-hoi-thoai.png)

**Luồng chính:**

1. Người chơi chọn nhận nhiệm vụ từ hội thoại; hệ thống kiểm tra trạng thái Available và điều kiện nhiệm vụ trước.
2. QuestService lưu nhiệm vụ đang hoạt động. Khi boss chết, hệ thống ghi nhận `bossId` và `encounterId`.
3. Khi đạt đủ mục tiêu, trạng thái chuyển thành ReadyToReport.
4. Người chơi quay lại NPC và chọn báo cáo.
5. QuestService kiểm tra điều kiện trả nhiệm vụ, chuyển phần thưởng kinh tế qua GameEconomy, thêm vật phẩm nhiệm vụ rồi lưu Completed.

**Luồng thay thế:** thiếu điều kiện nhận thì nhiệm vụ chưa mở; thiếu mục tiêu thì chưa thể báo cáo; nhiệm vụ đã Completed không trả thưởng lại. `prerequisites` nối chuỗi nhiệm vụ. `countPreviousKills` quyết định có tính boss bị hạ trước khi nhận nhiệm vụ hay không.

**Phân biệt:** hạ đủ boss không tự trả nhiệm vụ. Với `QuestBossEncounter`, NPC chỉ chuyển sang trạng thái chờ chiến đấu sau Completed, rồi đợi hội thoại/menu kết thúc trước khi trở thành boss.

### UC09 — Mua và bán tại NPC

Người chơi mở cửa hàng qua hội thoại, chọn chế độ mua/bán, vật phẩm và số lượng rồi xác nhận. Hệ thống kiểm tra vàng, số lượng sở hữu và giới hạn kho trước khi thay đổi ví và vật phẩm. Người chơi có thể hủy trước khi giao dịch hoàn tất.

**Mã nguồn chính:** `NpcShop.Trade`, `GameEconomy.BuyConsumable`, `SellConsumable`, `ConsumableCatalog`.

### UC10 — Thao tác tại tượng thần

![Hình 2. Use Case tượng thần](images/02-UC-tuong-than.png)

**Tiền điều kiện:** người chơi còn sống, đủ gần, không giao tranh và không bị khóa tương tác.

Tương tác hợp lệ lưu/mở tượng. Với tượng đã mở, người chơi có thể chọn các hành vi mở rộng:

- **Cầu nguyện:** fade màn hình, hồi phục người chơi/bình máu, đặt lại quái thường và ghi nhớ tượng cho hồi quy.
- **Nâng chỉ số:** dùng EXP để nâng một chỉ số được chọn theo giá trong EconomySettings.
- **Hiến tế:** tiêu hao một Hạt sinh mệnh để tăng sức chứa bình máu, trong giới hạn từ 3 đến 10 bình.
- **Thanh tẩy:** khi đang nhiễm Ăn hồn và có Hương thanh hồn, tiêu hao vật phẩm để giải trạng thái và đặt lại bộ đếm xác suất nhiễm.
- **Du hành:** chọn một tượng đã mở và có thể đến; hệ thống chuyển vị trí hoặc scene, hồi phục theo luồng du hành và ghi nhớ tượng đích.

Không phải mỗi lần mở menu đều thực hiện toàn bộ các hành vi này; vì vậy sơ đồ dùng extend, không nối tất cả thành include từ thao tác tượng.

### UC11 — Thu hồi Soul/EXP và vàng đã rơi

Khi chết, hệ thống ghi một khoản rơi bằng 50% vàng và EXP hiện có, lấy phần nguyên. Người chơi hồi sinh và có thể quay lại đúng scene để tương tác với điểm rơi. Thu hồi thành công cộng lại tài nguyên và xóa khoản rơi. Nếu chết lần nữa trước khi nhặt, khoản cũ bị thay thế, kể cả khi lần chết mới không có tài nguyên để rơi.

Việc chết/hồi sinh là phản ứng tự động của hệ thống; mục tiêu do người chơi chủ động thực hiện trong Use Case là quay lại và thu hồi tài nguyên.

**Mã nguồn chính:** `CharacterHealth`, `PlayerDeathDrops`, `PlayerDeathDropPickup`, `GameEconomy.RecordPlayerDeathDrop`, `RecoverPlayerDeathDrop`, `CheckpointService`.

### UC12 — Tạm dừng, trở về menu và thoát

Người chơi mở pause khi không có luồng tương tác ưu tiên chặn pause, xem các trang menu, tiếp tục chơi hoặc chọn hành động trở về/thoát được giao diện cung cấp. Menu đầu game cũng có chức năng thoát. Đây là nhóm chức năng quản lý phiên chơi, không phải một bước bắt buộc của chiến đấu.

## 3. Thiết kế lớp

### 3.1. Điều khiển và chiến đấu

![Hình 4. Class chiến đấu](images/04-class-chien-dau.png)

`PlayerController`, `PlayerDash` và `PlayerCombat` tách điều khiển, di chuyển đặc biệt và hành động chiến đấu. `SkillDamageManager` là nơi dùng chung để kiểm tra cooldown/tài nguyên, đọc danh sách `SkillData` và thực thi hit. `CharacterHealth` cùng `CharacterStats` xử lý chỉ số và kết quả sát thương cho cả người chơi lẫn quái.

`EnemyBehavior` quản lý các trạng thái Patrol, Alert, Chase, Strafe, Attack, Retreat và Return. Nó phối hợp với `EnemyAwareness` để kiểm tra phát hiện/LOS, và gọi `EnemyCombat` để chọn, phát động đòn đánh. `EnemyCombat` có các hook protected virtual cho lớp con thay đổi lựa chọn/di chuyển khi đánh.

Các lớp con hiện có gồm SlimeCombat, WerebearCombat, WerewolfCombat, OrcRiderCombat, NecromancerCombat, BlackKnightCombat và BlackKnightBossCombat. Hai lớp Black Knight là những biến thể logic khác nhau đang tồn tại trong code. Các prefab Skeleton không có lớp runtime riêng mà dùng EnemyCombat chung.

`BossPhaseController` lọc chiêu theo phase và quản lý chuyển phase; `BossMovesetDirector` quan sát trạng thái hành động người chơi để chọn đòn/combo theo cấu hình. Đây là logic heuristic và cấu hình, không phải mô hình học máy.

### 3.2. Tương tác, hội thoại và nhiệm vụ

![Hình 5. Class nhiệm vụ](images/05-class-hoi-thoai-nhiem-vu.png)

`WorldInteractable` là lớp trừu tượng dùng chung cho các đối tượng có thể tương tác. `PlayerInteractor` chọn đối tượng gần nhất khả dụng rồi gọi TryInteract. `NpcInteractable` kế thừa lớp này và sử dụng `DialogueDefinition` để trình bày hội thoại.

`DialogueDefinition` chứa các Node, mỗi Node chứa các Choice. Choice tham chiếu nhiệm vụ để thực hiện hành động hoặc kiểm tra điều kiện hiển thị. Các Node và Choice là dữ liệu được sở hữu trong định nghĩa hội thoại; QuestDefinition được tham chiếu và có thể dùng chung giữa nhiều NPC.

`QuestDefinition` chứa điều kiện, mục tiêu và phần thưởng; `QuestService` quản lý trạng thái tiến trình, lịch sử boss bị hạ và vật phẩm nhiệm vụ đã nhận. `QuestItemDefinition` chỉ là định nghĩa nội dung/icon, không phải số lượng vật phẩm mà người chơi đang sở hữu.

`QuestBossEncounter` kết nối một nhiệm vụ với NPC/boss đang có, theo dõi thay đổi trạng thái để chuyển Friendly → WaitingForCombat → Hostile. Nó không phải lớp cha của NPC hoặc EnemyCombat.

### 3.3. Checkpoint, kinh tế và lưu game

![Hình 6. Class lưu game](images/06-class-checkpoint-luu-game.png)

`CheckpointInteractable` kiểm tra/lưu tương tác tượng, `CheckpointMenu` cung cấp các hành động, và `CheckpointService` xử lý bản ghi hồi sinh, danh sách tượng đã mở, du hành và hồi quy. `ScreenFader` chịu trách nhiệm chuyển cảnh; `NormalEnemyRespawns` đặt lại quái thường sau khi nghỉ.

`GameEconomy` là dịch vụ tĩnh quản lý ví, nâng cấp, vật phẩm dùng nhanh, hạt, dữ liệu Ăn hồn và khoản rơi khi chết. `PlayerProgression` nghe Changed để áp dụng phần nâng chỉ số lên nhân vật. `EconomySettings` và `ConsumableCatalog` là dữ liệu cấu hình.

`EnemyDefeatState` giữ các encounter Elite/Boss đã bị hạ; `BossItemInventory` giữ các lần nhận cổ vật. `QuestService` và `GameControls` cũng có dữ liệu lưu riêng nhưng được lược bớt ở hình 6 để tránh lặp lại quá nhiều lớp.

## 4. Cơ chế dữ liệu và liên kết sự kiện

Tiến trình hiện được lưu **cục bộ bằng PlayerPrefs**, chủ yếu là chuỗi JSON qua JsonUtility. ScriptableObject lưu định nghĩa/cấu hình trong project. Hai nhóm này không được mô tả thành bảng cơ sở dữ liệu hoặc quan hệ ORM.

Các khóa lưu chính được đối chiếu trong code:

- CheckpointService: `Noname.Checkpoint.v1`, `Noname.Checkpoint.Unlocked.v1`.
- GameEconomy: `Noname.Economy.v1`.
- QuestService: `Noname.Quests.v1`.
- GameControls: `Noname.Controls.v1`.
- EnemyDefeatState: `Noname.DefeatedEnemies.v1`.
- BossItemInventory: `Noname.BossItems.v1`.

`CharacterHealth.AnyDied` cho phép các dịch vụ phản ứng khi một nhân vật chết. QuestService lọc boss để ghi nhận tiến độ, còn CheckpointService lọc người chơi để chạy luồng hồi sinh. Ngoài sự kiện này, CharacterHealth còn gọi trực tiếp các hook phần thưởng/điểm rơi trong luồng chết; không nên mô tả toàn bộ luồng này là chỉ dùng Observer.

`GameEconomy.Changed` cập nhật các thành phần quan sát trạng thái kinh tế; `CurrencyReceived` mang giá trị vàng/EXP thực nhận để UI hiển thị thông báo. `QuestService.Changed` phục vụ cập nhật hội thoại, inventory và QuestBossEncounter. `AreaManager.AreaChanged` phục vụ thông báo khu vực.

## 5. Ghi chú khi trình bày báo cáo

1. Dùng Hình 1 cho phần yêu cầu tổng quan; Hình 2–3 là phân rã nghiệp vụ tượng thần và nhiệm vụ.
2. Dùng Hình 4–6 cho phần thiết kế lớp, ghi rõ đã lược bớt thuộc tính/phương thức phụ trợ. Với hình rộng, dùng trang ngang hoặc SVG để tránh chữ bị nhỏ khi thu cả hình vào một trang dọc.
3. Không vẽ Player kế thừa Enemy hoặc một lớp Character tự đặt tên: code hiện tổ chức bằng component dùng chung. Không đồng nhất tên prefab với tên lớp C#.
4. Không ghi SQL/database server, hệ thống đăng nhập hay multiplayer là chức năng đã triển khai.
5. Không gọi kiến trúc hiện tại là MVC/MVVM thuần. Các cách tổ chức quan sát được là component của Unity, FSM cho AI, dữ liệu ScriptableObject, các dịch vụ tĩnh/singleton, hook virtual và sự kiện.
6. Các sơ đồ là ảnh chụp thiết kế tại ngày đối chiếu. Nếu thêm hệ thống hoặc đổi quan hệ lớp, sửa draw.io/PlantUML và cập nhật thuyết minh tương ứng.

Danh sách đường dẫn lớp nằm trong [Đối chiếu mã nguồn](Doi-chieu-ma-nguon.md). Bản xem/phóng to nằm trong [index.html](index.html). File [NONAME-UML.drawio](NONAME-UML.drawio) chứa sáu trang chỉnh sửa được; thư mục `sources` có PlantUML, Graphviz và mô hình dữ liệu dùng để xuất hình.
