# Phần thưởng vật phẩm khi hạ boss

- Orc Rider nhận vật phẩm từ `OrcRiderMemory.asset` qua component `BossItemReward` trên prefab.
- Tạo vật phẩm khác bằng menu `Create > Game > Boss Reward Item`. Lưu asset trong `Resources/BossRewards` để tên, mô tả và icon được tìm lại sau khi tải save.
- Điền `itemId` duy nhất, tên, mô tả và icon; gán asset vào trường `Item` của `BossItemReward` trên prefab boss. Giữ ổn định `itemId` sau khi đã có save.
- Mỗi encounter boss chỉ nhận phần thưởng một lần, dựa trên `EnemyClassification.DefeatId`. Boss sinh bằng code cần `persistentId` ổn định từ spawner.
- Boss không gán vật phẩm riêng vẫn nhận một kỷ vật ký ức mang tên boss. Normal và Elite không tự nhận thưởng này.
- Vật phẩm tự vào túi đồ và có thông báo; kỷ vật không phải vật phẩm tiêu hao hay ô dùng nhanh. Phần thưởng hiện không tự tăng chỉ số.
- Lưu trong `Noname.BossItems.v1`, tách biệt Gold/EXP. Công cụ `Tools > Game Testing > Reset Game` đã bao gồm dữ liệu kỷ vật trong sao lưu/reset/khôi phục.


## Cổ vật đã gắn
- Orc Rider: OrcRiderMemory (giữ nguyên).
- Necromancer ở Rừng hoang: WildForestNecromancerMemory.
- Necromancer ở Rừng chết và prefab Necromancer gốc: NecromancerMemory.
- BlackKnight và BlackKnightBoss: BlackKnightMemory.

Các cổ vật có icon, nằm ở tab Cổ vật cường địch, tự nhận khi hạ boss. Có thể sửa tên, mô tả và icon trong Item Manager. Cổ vật có vai trò BossEvidence dùng làm mục tiêu xác nhận trong nhiệm vụ. Mỗi encounter chỉ trao một lần; Hạt sinh mệnh cũng tự nhận vào túi khi hạ Elite/Boss.
