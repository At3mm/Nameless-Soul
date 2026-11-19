# Black Knight

Prefab: `Assets/Prefabs/Characters/BlackKnight.prefab`. Kéo prefab này vào vị trí muốn đặt trên map; lần thiết lập này không tự đặt boss lên map đang chỉnh.

Prefab hiện có chế độ NPC trước khi đánh: nhận và báo cáo nhiệm vụ **Lời hẹn hiệp sĩ đen**, rồi đóng đối thoại để chuyển thành boss. Hướng dẫn cấu hình: `Assets/Docs/BLACK_KNIGHT_NPC_VA_BOSS.md`. Phần dưới mô tả bộ chiêu khi đã trở thành boss.

## Bộ chiêu và phase

- `NormalAttack`: bộ Attack01, đòn đánh thường.
- `Skill2`: bộ Attack02, chuẩn bị rồi đâm lao tới.
- `Skill3`: bộ Attack03, chuẩn bị rồi nhảy tới vị trí đã chốt, gây sát thương lúc đáp xuống / bung gai.

Boss có 800 HP, phase 1 dùng NormalAttack + Skill2; khi còn 50% HP chuyển phase 5 giây, sau đó có thêm Skill3. Chỉnh danh sách trong **Boss Phase Controller**. State đứng yên riêng của boss là **BlackKnightIdle**, không dùng tên OrcIdle.

## Độ trễ và lao tới

Trong **Black Knight Combat**:

- `Thrust Delay` / `Leap Delay`: thời gian giữ thêm tư thế chuẩn bị, mặc định 0,15 giây.
- `Thrust Move Duration` / `Leap Move Duration`: thời gian di chuyển, mặc định 0,28 / 0,45 giây.
- `Max Thrust Distance` / `Max Leap Distance`: giới hạn quãng đường, mặc định 4,5 / 5,5 đơn vị.
- `Stop Distance`: khoảng cách muốn chừa tới người chơi, mặc định 1,25.
- `Slash Distance` / `Thrust Distance` / `Leap Distance`: khoảng cách cho phép chọn đòn.

Animation đã có thời gian chuẩn bị trước event. Mặc định boss bắt đầu đâm sau khoảng 0,57 giây từ lúc chọn chiêu; nhảy sau khoảng 0,4 giây. Điểm đến được chốt khi bắt đầu lao, nên người chơi có thể né. Chuyển động dừng trước vật cản và không vượt bán kính lãnh địa. Tư thế nhảy trên không nằm trong sprite Attack03; không nâng collider theo trục Y của map.

## Animation Event

Các clip trong `Animation` là 12 FPS; chỉ sprite đã cung cấp được dùng. Mốc frame bên dưới tính từ 0:

- Attack01: `ExecuteHit("NormalAttack")` tại frame 5 và 11.
- Attack02: `BeginThrust()` ở frame 5; `EndSpecialMovement()` rồi `ExecuteHit("Skill2")` ở frame 11.
- Attack03: `BeginLeap()` ở frame 3; `EndSpecialMovement()` ở frame 9; `ExecuteHit("Skill3")` ở frame 10.
- Các clip đánh / Hurt có `LockAction()` đầu clip và `UnlockAction()` cuối clip.
- Death có `DestroyCharacter()` ở frame 3 để dùng cơ chế xác biến mất hiện có.

Có thể tự di chuyển các marker này. Giữ event dừng di chuyển trước event gây sát thương và đủ khoảng thời gian cho chuyển động. Không có frame số nào được hard-code trong logic di chuyển; logic chỉ bắt đầu khi event được gọi. Delay giữ riêng tốc độ state tấn công bằng `SpecialAttackSpeed`, không khóa Animator.speed, để Hurt / Break / chuyển phase không bị đứng hình.

## Kế thừa hệ thống chung

Giữ đi tuần, đuổi theo, mất dấu, về nhà, break, phần thưởng boss và combo từ hệ thống quái hiện tại. Có mục `black-knight` trong bốn preset sát thương khu vực. Prefab có profile âm thanh riêng sao chép từ Orc làm cấu hình ban đầu, có thể thay clip sau.

Bảy sprite sheet giữ slicing / pivot; lọc Point, không mipmap / nén. PPU là 10 cho cả bộ, gồm Hurt trước đó đang khác các sprite còn lại.

`Tools → Enemies → Create Missing Black Knight Assets` chỉ tạo phần còn thiếu; không ghi đè các clip, controller hoặc prefab bạn đã chỉnh. `Tools → Enemies → Test Black Knight Combat (Play Mode)` chạy kiểm tra tích hợp trên hai actor thử ở xa map, không giết boss thật hoặc reset save.

Đã chạy đạt 27 kiểm tra tích hợp: sát thương qua animation event, chờ trước khi lao, pause, chốt điểm đến, khoảng cách, va chạm, hủy khi nhận đòn / break và chuyển phase ngay trong lúc chuẩn bị. Kiểm tra riêng với AI và combo director cùng bật cũng xác nhận skill 2 lao tới và gây sát thương. Save được phục hồi nguyên trạng; các chỉnh sửa scene chưa lưu trước đó không bị lưu đè.
