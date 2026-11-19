# Bùa hồi quy

Vật phẩm nhiệm vụ có thể tái sử dụng, không tiêu hao và không mua/bán tại cửa hàng.

- Asset: `Assets/Resources/QuestItems/ShrineReturnCharm.asset`, ID `shrine-return-charm`.
- Trong Quest Manager, phần thưởng nhiệm vụ → Items: thêm asset này, số lượng 1. Chưa có nhiệm vụ nào được tự động tạo hoặc sửa để cấp bùa.
- Sau khi nhận, bùa xuất hiện trong Túi đồ → Vật phẩm nhiệm vụ. Chọn bùa rồi Trang bị dùng nhanh; đóng menu và bấm nút dùng vật phẩm để hồi quy.
- Trở về tượng thần đã cầu nguyện hoặc du hành đến gần nhất. Chỉ mở/lưu tại tượng khác không thay đổi đích.
- Không dùng khi giao tranh, chết hoặc đang chuyển cảnh. Save cũ chưa ghi nhớ đích cần cầu nguyện hoặc du hành đến tượng trước.
- Bùa cũ đã sở hữu vẫn dùng được và chuyển sang tab nhiệm vụ; không còn tiêu hao. ID 9 trong catalog được giữ để tương thích ô dùng nhanh và save cũ.

Đích hồi quy vẫn nằm trong record checkpoint hiện có. Khi dùng, vẫn fade và hồi phục như du hành thông thường.
