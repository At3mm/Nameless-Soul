# Skeleton enemies

Các prefab trong `Assets/Prefabs/Characters`:

- `Skeleton.prefab`: Normal, hai đòn `NormalAttack`, `HeavyAttack`.
- `ArmorSkeleton.prefab`: Normal, đâm `NormalAttack` và xoay `HeavyAttack` (hai mốc hit).
- `GreatswordSkeleton.prefab`: Elite, chém `NormalAttack`, bổ mạnh `HeavyAttack`, đâm `Skill3`. Không có chuyển phase.

Kéo prefab vào map để đặt quái. Chúng dùng chung EnemyBehavior (đi tuần/đuổi/nhớ vị trí), EnemyAwareness (vật cản che tầm nhìn), EnemyCombat, SkillDamageManager, EnemyBreak và hệ thống phần thưởng hiện có. Không có script chiến đấu riêng rỗng hoặc bản sao AI. Greatsword dùng quy tắc lưu hạ Elite và rơi Hạt sinh mệnh hiện có; mỗi encounter đặt trong scene cần ID riêng như các Elite khác.

Animation clip và controller nằm trong thư mục `Animation` tương ứng của mỗi bộ sprite. Tất cả chạy 12 FPS. Clip gọi `LockAction`, `ExecuteHit` với tên skill, `UnlockAction`; clip chết gọi `DestroyCharacter` để dùng fade xác hiện có. State nghỉ/break dùng đúng controller từng loại, không trỏ về OrcIdle.

Muốn đổi thời điểm gây sát thương: mở clip Attack trong Animation Window, kéo marker ExecuteHit. Muốn đổi damage/range/cooldown: chỉnh SkillDamageManager trên prefab. EnemyAreaDamage gán archetype `skeleton`, `armor-skeleton`, `greatsword-skeleton`; sát thương theo khu vực còn chịu profile trong `Assets/Combat/Balance`.

PPU hiện tại: Skeleton/Armor 25, Greatsword 18. Riêng Armor Attack02 được sửa từ cắt tự động thành 9 ô 100×100 và PPU 25 để đồng bộ bộ sprite. Tất cả dùng Point, không mipmap và không nén. Audio profile riêng hiện dùng bộ âm thanh sẵn có của Orc, chỉnh pitch; có thể thay clip trong `Assets/Effects/Audio/Enemies/Profiles`.

Menu `Tools → Enemies → Create Missing Skeleton Assets` chỉ tạo clip/controller/prefab còn thiếu, không ghi đè marker hay prefab đã chỉnh. Các profile damage chỉ được bổ sung mục archetype còn thiếu.
