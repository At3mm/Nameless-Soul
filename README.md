# Nameless Soul

Game nhập vai hành động 2D phong cách pixel, phát triển bằng Unity và C#.

## Tải dự án và mở bằng Unity

### 1. Chuẩn bị

- Cài [Unity Hub](https://unity.com/download), [Git](https://git-scm.com/downloads) và [Git LFS](https://git-lfs.com/).
- Trong Unity Hub, cài **Unity Editor 6000.3.9f1**. Đây là phiên bản ghi trong `ProjectSettings/ProjectVersion.txt`; nếu Hub chưa liệt kê, tìm trong Unity Download Archive.
- Nếu cần build Windows, cài thêm module **Windows Build Support (IL2CPP)** cho Editor này.
- Repo đang riêng tư: tài khoản GitHub phải được chủ repo cấp quyền truy cập. Khi Git yêu cầu đăng nhập, dùng tài khoản đã được cấp quyền.

### 2. Tải mã nguồn và tài nguyên

Mở PowerShell hoặc Git Bash tại thư mục muốn lưu dự án, chạy lần lượt:

```sh
git lfs install
git clone https://github.com/At3mm/Nameless-Soul.git
cd Nameless-Soul
git lfs pull
git lfs fsck
```

Lệnh cuối kiểm tra dữ liệu LFS; kết quả bình thường là `Git LFS fsck OK`. Ảnh, âm thanh, font và một số tài nguyên khác dùng Git LFS, vì vậy cần hoàn tất bước này trước khi mở Unity. Ưu tiên clone bằng Git thay cho Download ZIP để lấy đúng tài nguyên.

### 3. Thêm dự án vào Unity Hub

1. Mở Unity Hub, vào **Projects**, chọn **Add / Add project from disk** (tên nút có thể khác tùy phiên bản Hub).
2. Chọn thư mục **Nameless-Soul** vừa clone, là thư mục chứa cả `Assets`, `Packages` và `ProjectSettings`. Không chọn riêng thư mục `Assets`.
3. Chọn Editor **6000.3.9f1**, sau đó mở dự án.
4. Chờ Unity tải package, import tài nguyên và biên dịch script xong. Lần đầu có thể mất nhiều thời gian; cần kết nối mạng để khôi phục các phụ thuộc, bao gồm package Unity MCP lấy từ GitHub.
5. Trong cửa sổ Project, mở **Assets/Scenes/MainMenu.unity**, sau đó bấm **Play** để chơi từ menu chính. Bấm Play lần nữa để dừng.

Repo chứa các tệp dự án Unity cần thiết, gồm scene, prefab, script, tài nguyên và `.meta`. Không cần chép bộ cài Unity vào repo. Các thư mục `Library`, `Temp`, `Logs`, `UserSettings` được Unity tạo lại trên máy mới.

### 4. Lấy các cập nhật sau này

Lưu công việc và đóng Unity trước khi cập nhật. Trong thư mục dự án, chạy:

```sh
git pull --ff-only
git lfs pull
```

Nếu đã sửa dự án trên máy, hãy commit hoặc lưu riêng thay đổi trước khi pull. Nếu Git báo phân nhánh hoặc xung đột, xử lý thay đổi trước; không xóa hay ghi đè tệp để cập nhật bằng mọi giá.

### 5. Khi không mở được

- **Repository not found / không có quyền:** kiểm tra URL và tài khoản GitHub đã được cấp quyền vào repo riêng tư.
- **Thiếu ảnh, âm thanh hoặc lỗi import tài nguyên:** đóng Unity, chạy lại `git lfs install`, `git lfs pull`, `git lfs fsck`, sau đó mở lại.
- **Sai phiên bản Editor:** cài và chọn Unity **6000.3.9f1** trong Hub.
- **Package chưa tải hoặc script chưa biên dịch:** kiểm tra mạng, Git có trong PATH và lỗi đầu tiên trong Console / Package Manager. Chờ import hoàn tất trước khi bấm Play.

## Nội dung

- `Assets/`: mã C#, scene, prefab, sprite, animation, âm thanh và dữ liệu gameplay.
- `Packages/`, `ProjectSettings/`: phụ thuộc và thiết lập Unity.
- `Docs/`: hướng dẫn sử dụng, sơ đồ và cấu trúc dự án.
- `References/`: tài nguyên tham chiếu và hồ sơ nguồn, giấy phép.
- `Tools/`: công cụ hỗ trợ ngoài Unity.

Các thư mục cache, thiết lập máy cá nhân và bản sao lưu cục bộ không được đưa lên Git. Báo cáo ĐATN được chủ dự án lưu riêng.

## Repository

- Repo chính: https://github.com/At3mm/Nameless-Soul
- Repo phụ trước đây: https://github.com/ManhQuyet2836/Dungeon-Souls

Repo chính giữ riêng tư. Một số tài nguyên thuộc bên thứ ba; xem giấy phép đi kèm trước khi phân phối lại.

## Xuất game Windows

Xem [hướng dẫn build và tạo bộ cài](Docs/Guides/WINDOWS_RELEASE.md). Trong Unity, dùng Tools > Build > Validate Windows Release rồi Build Windows Release. Bộ cài được tạo bằng Tools/Build/Package-Windows.ps1; output nằm trong Builds và không đưa vào Git.
