# Flame Golem

Prefab: Assets/Prefabs/Characters/FlameGolem.prefab
Sprite: Assets/Sprites/Flame Golem/
Animation/Animator Controller: Assets/Sprites/Flame Golem/Animation/

- Boss (EnemyBoss), 10 PPU; ô 100×100; Point, không mipmap/compression.
- Animation 12 FPS. Idle/Walk loop; Hurt/Death/Attack không loop.
- Attack01 → NormalAttack; Attack02 → HeavyAttack; Attack03 → Skill3. Animation Event ExecuteHit dùng đúng tên này trong String.
- LockAction ở đầu và UnlockAction ở cuối đòn; Death gọi DestroyCharacter. Có state Broken và idleState riêng để hồi phục đúng animation.
- Dùng EnemyBehavior/EnemyAwareness/EnemyCombat/SkillDamageManager chung: tuần tra, đuổi theo, kiểm tra vật cản/line of sight, cooldown, hitbox, break và chết. Thông số đòn sửa trên prefab trong Skill Damage Manager; marker sửa trong clip.
- Restrict Lunge Skills / Lunge Skill Names trên EnemyCombat chọn chiêu nào nhích tới khi bắt đầu; các chiêu còn lại đứng tại chỗ.
- Có cấu hình sát thương theo 5 khu vực, audio profile dùng lại nguồn âm thanh sẵn trong dự án.
- Boss một phase, 1.680 HP ở khu cuối; thấp hơn Black Knight (2.400 HP). Khi hạ nhận cổ vật Lõi hỏa thạch và 1 Hạt sinh mệnh trực tiếp vào túi.
- Setup chỉ tạo phần còn thiếu, không ghi đè clip/controller/prefab đã chỉnh: Tools → Enemies → Create Missing Cave Creature Assets.
