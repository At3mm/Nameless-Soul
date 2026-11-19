# Lội dung nham

Player, LavaSlime và FlameGolem có WaterPresence + PlayerWaterRipples trên prefab. PlayerWaterRipples dùng chung cho nước và dung nham; khi vào dung nham, shader che phần chân theo collider, phát gợn cam, vẫn giữ làm chậm của WaterSlowZone. Không dịch chuyển collider hoặc vị trí thật của nhân vật.

Chỉnh Lava Submerged Fraction trên PlayerWaterRipples: Player 0.34, LavaSlime 0.48, FlameGolem 0.4. Số lớn hơn làm chìm sâu hơn. Lava Ripple Color chỉnh màu gợn. Nước vẫn dùng Submerged Fraction từ DefaultWaterEffects. Khi lên bờ, material bình thường được khôi phục.

LavaAffinity trên hai quái bản địa bật Immune To Lava Burn: chỉ miễn bỏng từ vùng dung nham, không miễn mọi đòn lửa của người chơi. Player không có miễn nhiễm này, vẫn bị làm chậm và bỏng.

CaveScene/Lava đã có TilemapCollider2D (Merge), CompositeCollider2D trigger, WaterSlowZone và LavaZone. TilemapCollider riêng không bật trigger là đúng vì va chạm đã gộp vào Composite. Không đổi lava thành collider rắn.

Kiểm tra: Tools → Diagnostics → Test Project Gameplay and Lava. Dùng save thử và khôi phục save thật sau khi dừng Play. Ảnh thử được lưu ở Library/ProjectPlayValidation/LavaWading.png.

Bộ kiểm tra tổng cũng chạy stamina, action lock, stun/break, tầm nhìn bị vật cản, teleport Werewolf, menu/stat/checkpoint, cầu nguyện có fade, du hành trong/cross-scene và túi đồ hai tab. Dữ liệu thử được khôi phục sau khi kết thúc.
