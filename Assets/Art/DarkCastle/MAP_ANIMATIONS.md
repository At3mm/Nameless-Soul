# Animation phần map

## Vẽ bằng Tile Palette

Chọn **Cave_DarkCastle_Animated** trong Tile Palette. Palette có 32 nhóm, gồm hồ trống/dung nham đỏ và tím, bốn kiểu đuốc, cửa đóng phát sáng, tinh thể trang trí, cỏ và cây theo mùa. Đây là đồ trang trí map, không có logic nhặt vật phẩm.

Các nhóm lớn được tách ô 16×16. Dùng công cụ Select trong Tile Palette để chọn cả cụm (hồ 3×3, đuốc 1×2, cửa 3×2, cây tối đa 4×5), sau đó vẽ lên tilemap phù hợp. Một số ô trong suốt được bỏ qua. Đặt đuốc/hiệu ứng phía trên nhân vật vào Overhead, nền vào GroundDetails; cấu hình collider của các tilemap vẫn theo scene hiện có.

Tile động nằm trong `Assets/Art/DarkCastle/AnimatedTiles`. Nhịp phát lấy từ PixelAnimationSettings (hiện 12 FPS), tự bù Animation Frame Rate của Tilemap nên không cần đổi tốc độ toàn bộ CaveScene. Các ô cùng cụm dùng chung thứ tự frame để đồng bộ. Cây lớn/nhỏ là hai biến thể riêng; mỗi tư thế cây giữ 6 nhịp để chuyển động nhẹ.

Palette tĩnh cũ không bị thay thế. Tile tĩnh đã vẽ không tự biến thành tile động: vẽ lại phần cần chuyển bằng palette động.

## Prefab đồ trang trí

Trong `Assets/Art/DarkCastle/Prefabs/MapAnimations` có 33 prefab: kéo vào scene để đặt nguyên đồ vật, không cần ghép ô. Component **Map Prop Animation** cho chỉnh danh sách sprite và loop. Các prefab khác là animation lặp; `Grass_Destroy` là hiệu ứng một lần rồi tự tắt, có thể gọi Replay để dùng lại.

## Cửa đóng/mở

- Prefab `Door01`, `Door02`, `Door_Purple01`, `Door_Purple02` có animation đóng, mở và trạng thái mở.
- Gọi `MapPropAnimation.Open()`, `Close()` hoặc `Toggle()` từ cơ chế tương tác/nhiệm vụ/UnityEvent của bạn. Có menu ngữ cảnh Animation/Open và Animation/Close trên component để thử trong Play Mode.
- Cửa không tự mở/đóng lặp lại. Prefab có **Door Interactable**: đứng gần và bấm phím tương tác (mặc định E) để mở. Tương tác tôn trọng phím đã gán trong cài đặt; không mở lại/spam khi cửa đang chạy hoặc đã mở. Chưa gán điều kiện nhiệm vụ.
- Collider chặn khi đóng/đang mở, tắt khi mở hoàn tất và bật lại khi bắt đầu đóng.
- Cửa vẽ bằng tile chỉ có animation nền đóng. Dùng prefab nếu cần mở cửa thật.

CaveScene đã chuyển hai cụm cửa tile thành prefab trong **Interactive Doors**. Chỉ phần tường giữa lối đi được bỏ; hàng header phía trên dùng bản sao tile không có collider để giữ nguyên hình nhưng không chặn người chơi. Không sửa collider của tile tường dùng chung trên toàn map. Trạng thái cửa hiện chỉ giữ trong lượt scene đang chạy, chưa thêm lưu trạng thái cửa qua lần tải lại.

Nếu vẽ thêm đủ một cụm cửa tile 3×2 trong CaveScene, chạy **Tools → World → Enable Painted Cave Doors** để chuyển sang cửa tương tác. Tool có kiểm tra phần tile không liên quan và sao lưu scene trong `Library/DeveloperTools/CaveDoors`. Nếu chỉ đặt prefab cửa, cần tự để trống collider tường ở lối đi; prefab không tự xóa map xung quanh trong lúc chơi.

Sprite animation được cắt riêng trong `AnimationSources`; file gốc Source, nhân vật, Item Icons và các rương Treasure được giữ nguyên. Không có thay đổi sprite nhân vật, inventory hoặc animation combat.

Menu **Tools → World → Create Missing Dark Castle Map Animations** tạo tài nguyên còn thiếu, giữ nguyên tile và prefab đã chỉnh. Không tự vẽ hay đặt đồ vật vào CaveScene.
