# Trang trí Wasteland

Các prefab trang trí đã được gom trực tiếp dưới `GroundDetails` trong Hierarchy: 72 prefab Cainos gồm 50 cỏ nhỏ, 11 bụi cây và 11 đá, bố trí thành 12 cụm. Nhóm `Wasteland Added Scenery` đã bỏ.

Vị trí dựa trên các ô đất đang có trong Ground. Loại trừ ô nước/hồ, lớp Obstacles, chân/tán cây và vách; mỗi điểm cần vùng đất trống xung quanh. Chừa vùng bán kính 3,5 đơn vị quanh tượng, điểm hồi sinh trước tượng, cửa hang và gốc bản đồ. Các vật mới là trang trí, không thêm collider cản nhân vật hoặc AI; đặt ở lớp GroundDetails dưới nhân vật.

Có thể chọn từng prefab con của GroundDetails để di chuyển, tắt hoặc xóa. Tilemap GroundDetails gốc và màu đất được giữ nguyên. Bản scene trước khi trang trí nằm tại `Library/CheckpointTests/wasteland-before-scenery.unity`; danh sách asset/tọa độ ở `Library/CheckpointTests/wasteland-scenery-placement.json`.

Đã xác nhận qua Unity MCP có đủ 72 prefab con dưới GroundDetails và không còn nhóm cũ.
