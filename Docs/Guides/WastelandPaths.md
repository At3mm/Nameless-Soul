# Đường dẫn trong Wasteland

Đã tạo 5 tuyến đường liên thông trên lớp Ground bằng tile đất có sẵn `RA_Wasteland_2`, nối vùng tượng (13,12), cửa hang, vị trí xuất phát thực tế (-20.63,-13.98) và ba hướng khám phá của vùng đất.

Đường rộng khoảng hai ô khi địa hình cho phép, tổng cộng 302 ô. Các tuyến được tính trên nền đất hiện có, loại trừ ô hồ/nước, vật cản, chân cây và vách đá. Không thêm ô đất ra ngoài bản đồ hay thay đổi tọa độ/collider tilemap. Đã dời 19 chi tiết trang trí khỏi các ô đường.

Các tuyến ô liên tiếp được kiểm tra liên thông theo bốn hướng. Unity đã tải được scene với 72 prefab trang trí; đã xác nhận tile/sprite ở điểm xuất phát khớp loại đất đường và làm mới tilemap.

Danh sách tuyến: `Library/CheckpointTests/wasteland-routes.json`. Scene trước khi tạo đường: `Library/CheckpointTests/wasteland-before-paths.unity`.
