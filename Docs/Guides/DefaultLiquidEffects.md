# Hiệu ứng hồ mặc định

`Assets/Resources/DefaultLiquidSurfaces.asset` lưu material mặc định cho mặt nước và mặt độc. Các material vẫn có các thông số nhịp pixel, tốc độ, độ sáng, bọt và màu riêng.

Khi Play hoặc tải scene, tilemap tên `Water`, `Lakes`, `Map_Boundaries`, `Poison`, hoặc có `WaterSlowZone` / `PoisonZone`, tự nhận `LiquidSurfaceEffect`. Auto chọn độc nếu có PoisonZone hoặc tên Poison; các trường hợp khác chọn nước. Không cần gắn lại material trên từng lớp hoặc nhân vật.

Hồ tạo mới khi game đang chạy tự nhận mặc định khi gắn WaterSlowZone/PoisonZone sau TilemapRenderer. Với tên hồ riêng hoặc hồ chỉ trang trí, gắn LiquidSurfaceEffect và chọn Kind Water/Poison. Muốn dùng material riêng, bỏ Use Default Material.

Hệ này chỉ gắn hiệu ứng hiển thị: không tự tạo collider hoặc thêm gây độc cho một lớp chỉ vì nó mang tên Poison. Hồ gây độc vẫn dùng PoisonZone; gợn sóng dưới chân, chìm nước và hiệu ứng trạng thái nhân vật tiếp tục dùng DefaultWaterEffects / DefaultStatusEffects đã có.
