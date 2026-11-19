# Cấu trúc dự án Nameless Soul

## Mã nguồn khi chạy

`Assets/Scripts/` chia theo trách nhiệm:

- `Core/`: các thành phần nhân vật và dịch vụ nền tảng còn dùng chung.
- `Combat/`, `Combat/Status/`: xử lý chiến đấu và trạng thái nhân vật.
- `Economy/`, `Input/`, `Items/`, `Quests/`, `NPCs/`: tài nguyên, đầu vào, vật phẩm, nhiệm vụ và tương tác NPC.
- `Player/`, `Player/DeathDrops/`: hành vi người chơi và tài nguyên rơi khi chết.
- `Enemies/Shared/`: hành vi quái dùng chung; `Enemies/Bosses/`: phase, moveset và thưởng boss; `Enemies/Archetypes/`: đặc tính riêng từng loại.
- `World/Areas/`, `Checkpoints/`, `Doors/`, `Interaction/`, `Liquids/`, `Map/`, `SceneFlow/`: từng nhóm hành vi của thế giới.
- Các thư mục giao diện và hệ thống khác giữ phạm vi chức năng hiện có.

Tên lớp và namespace không đổi khi di chuyển. Luôn dùng Unity Project window hoặc `AssetDatabase.MoveAsset` để giữ GUID trong `.meta`. Không di chuyển đường dẫn `Resources` tùy tiện vì mã có thể tải tài nguyên bằng chuỗi.

## Công cụ Unity Editor

`Assets/Editor/` chỉ chứa công cụ chạy trong Editor:

- `Authoring/`: sửa vật phẩm và icon, nhiệm vụ, hội thoại dạng đồ thị, vùng và hierarchy.
- `Setup/`: cấu hình nội dung, prefab và hoạt ảnh. Không chạy lại hàng loạt sau khi đã chỉnh tay clip hoặc marker.
- `Diagnostics/`: công cụ chơi thử, reset và xem trước.
- `Tests/`: các lớp kiểm thử hồi quy có menu `Tools/Diagnostics` hoặc phương thức `Run`.
- `Infrastructure/`: tích hợp build và môi trường Editor.

Các phiên kiểm thử có thể dùng Play Mode, nên lưu scene trước khi chạy và chờ hoàn tất. `ProjectPlayRegression` ghi trạng thái vào `Library/ProjectPlayValidation/status.txt`, sao lưu và phục hồi dữ liệu lưu cùng scene bắt đầu. Các bộ kiểm thử khác ghi kết quả vào thư mục `Library/*Validation/` tương ứng; đọc kết quả của phiên mới nhất thay vì coi sự tồn tại của tệp cũ là thành công.

## Nội dung và tài liệu

- `Assets/Scenes`, `Prefabs`, `Animations`, `Sprites`, `Tiles`: nội dung đã import và cấu hình.
- `Assets/Resources`: dữ liệu nạp khi chạy, bao gồm vật phẩm, nhiệm vụ, âm thanh và giao diện. Các thư mục `Resources` lồng trong `Effects` vẫn được giữ vì Unity hỗ trợ và dự án đang sử dụng.
- `Assets/Effects`: hiệu ứng và tài nguyên liên quan. Logic trạng thái dùng chung đã chuyển về `Scripts/Combat/Status`.
- `References/`: nguồn tải và giấy phép; giữ nguyên cấu trúc gói của bên thứ ba để tra cứu và cập nhật.
- `Docs/`: hướng dẫn, sơ đồ và hình minh họa; báo cáo ĐATN được lưu riêng. `Tools/`: công cụ hỗ trợ bên ngoài Unity.
- `Library`, `Temp`, `Logs`: dữ liệu Unity sinh ra, không chỉnh như mã nguồn và không đưa vào bản phát hành.

## Lưu trữ và khôi phục

`Backups/UnityRecovery` giữ các scene khôi phục từng nằm trong `Assets/_Recovery`; `Backups/LegacyReportWork` giữ công cụ và dữ liệu biên tập báo cáo cũ. Toàn bộ `Backups/` chỉ lưu cục bộ, không được Unity import, đưa vào build hoặc đẩy lên GitHub. Các bản báo cáo đã được chủ dự án sao lưu riêng và xóa khỏi cây mã nguồn.

Muốn phục hồi scene, sao chép đúng scene cần dùng vào một thư mục mới trong `Assets` bằng Unity; tránh phục hồi toàn bộ cây cùng GUID khi asset tương ứng đã tồn tại. Không chép các bản báo cáo cũ đè lên báo cáo chính nếu chưa đối chiếu nội dung.

Lần sắp xếp này chuyển 144 tệp C# bằng AssetDatabase và giữ nguyên GUID, tên lớp, namespace, prefab, Animation Event và đường dẫn dữ liệu Resources. Các thư mục rỗng Materials, Shaders và Screenshots ở gốc Assets đã được bỏ; tài nguyên thực tế trong các thư mục chức năng không bị xóa.
