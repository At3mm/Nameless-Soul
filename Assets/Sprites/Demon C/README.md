# Demon C

Prefab: Assets/Prefabs/Characters/DemonC.prefab
Sprite: Assets/Sprites/Demon C/
Animation/Animator Controller: Assets/Sprites/Demon C/Animation/

- Normal (EnemyNormal), 25 PPU; ô 100×100; Point, không mipmap/compression.
- Animation 12 FPS. Idle/Walk loop; Hurt/Death/Attack không loop.
- Attack01 → NormalAttack; Attack02 → HeavyAttack. Animation Event ExecuteHit dùng đúng tên này trong String.
- LockAction ở đầu và UnlockAction ở cuối đòn; Death gọi DestroyCharacter. Có state Broken và idleState riêng để hồi phục đúng animation.
- Dùng EnemyBehavior/EnemyAwareness/EnemyCombat/SkillDamageManager chung: tuần tra, đuổi theo, kiểm tra vật cản/line of sight, cooldown, hitbox, break và chết. Thông số đòn sửa trên prefab trong Skill Damage Manager; marker sửa trong clip.
- Restrict Lunge Skills / Lunge Skill Names trên EnemyCombat chọn chiêu nào nhích tới khi bắt đầu; các chiêu còn lại đứng tại chỗ.
- Có cấu hình sát thương theo 4 khu vực, audio profile dùng lại nguồn âm thanh sẵn trong dự án.
- Minotaur tự cộng 1 Hạt sinh mệnh khi hạ, cùng logic Elite hiện tại. Quái thường không tự nhận hạt.
- Setup chỉ tạo phần còn thiếu, không ghi đè clip/controller/prefab đã chỉnh: Tools → Enemies → Create Missing Cave Creature Assets.
