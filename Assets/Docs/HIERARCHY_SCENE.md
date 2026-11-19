# Hierarchy gameplay scene

Đã gom WastelandScene từ 67 xuống 6 đối tượng cấp cao nhất:

- **Environment**: Grid và Global Light 2D. Nội bộ Grid / tilemap được giữ nguyên.
- **Characters**: Player, NPCs và Enemies. Quái nằm trong nhóm theo khu vực xuất hiện.
- **World**: World Areas, Checkpoints, Transitions, SpawnPoints.
- **UI**: SettingsMenu, PlayerHUD, DragonEnemyHUD. Giữ nguyên nội bộ canvas / menu.
- **Cameras**: Main Camera và CinemachineCamera.
- **ScreenFader**: giữ ở cấp gốc vì DontDestroyOnLoad chỉ áp dụng an toàn cho root GameObject.

Nhóm mới chỉ có Transform trung tính: vị trí 0, rotation 0, scale 1. Không đổi tên nhân vật / tượng / vùng cũ, ID, tọa độ thế giới, trạng thái active, layer, tag, prefab hoặc nội dung animation.

## Dùng về sau

**Tools → World → Organize Active Scene Hierarchy** gom các đối tượng cấp gốc có thể nhận diện trong scene đang mở. Không chạy trong Play Mode hoặc Prefab Mode. Chỉ di chuyển cả root của prefab, không tách / unpack nội bộ prefab.

Tool đối chiếu dữ liệu trước và sau; nếu khác ngoài phạm vi gom nhóm thì hoàn tác thao tác. Nếu scene sạch trước khi chạy, tool lưu scene sau khi kiểm tra đạt. Nếu scene đã có thay đổi chưa lưu, tool giữ trạng thái chưa lưu để bạn tự Ctrl+S. Ngay sau thao tác có thể Undo; mở lại scene sẽ cần dùng bản sao lưu nếu muốn phục hồi.

Mỗi lần chạy tạo bản sao file scene cùng snapshot / báo cáo trong **Library/DeveloperTools/HierarchyCleanup**. Tên nhóm mới không thay thế ID gameplay. Đừng đổi scale / di chuyển cả nhóm nếu chỉ muốn sắp xếp Hierarchy.

## Kiểm tra lần thiết lập

- Đối chiếu 746 GameObject ban đầu, component và tham chiếu, tên, tag/layer, trạng thái active, kết nối prefab, ID và transform thế giới.
- Lưu và mở lại scene; dữ liệu Tilemap lưu trên đĩa giữ nguyên. Sprite cache của tilemap animation có thể được Unity tái tạo khi mở scene.
- Play Mode: player bootstrap / camera / ScreenFader, 44 quái, 9 checkpoint, 4 khu vực.
- Hồi sinh 38 quái thường: đúng parent nhóm và vị trí; 6 Elite/Boss giữ ID cũ.
- Save / cài đặt của người chơi được phục hồi nguyên trạng sau test.

Các dịch vụ và hiệu ứng được tạo lúc chạy vẫn có thể xuất hiện thêm ở scene runtime / DontDestroyOnLoad; lần gom này không thay logic quản lý vòng đời của chúng.
