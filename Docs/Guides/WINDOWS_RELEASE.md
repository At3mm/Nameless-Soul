# Xuất bản Windows

## Cấu trúc và phạm vi

- Mã chạy: Assets/Scripts; công cụ, kiểm thử và build: Assets/Editor.
- Nội dung game: Assets/Scenes, Prefabs, Sprites, Effects, Resources và WorldAreas.
- Tài liệu: Docs; nguồn và giấy phép: References; đóng gói ngoài Unity: Tools/Build.
- Các thư mục cache, bản khôi phục và bản xuất sơ đồ thử không được commit hoặc đưa vào bộ cài.
- Giữ nguyên GUID, file .meta và đường dẫn Resources. Không xóa asset chỉ vì không xuất hiện trong scene hiện tại.

## Build lại

1. Dùng Unity 6000.3.9f1 và cài Windows Build Support.
2. Lưu công việc, dừng Play Mode; chạy Tools > Build > Validate Windows Release.
3. Chạy Tools > Build > Build Windows Release.
4. Bản phát hành dùng Windows x64, Mono và không bật Development Build. Ba scene theo thứ tự MainMenu, WastelandScene, CaveScene.
5. Kết quả: Builds/Windows/NamelessSoul.exe, NamelessSoul_Data và các thư viện đi kèm. Phải giữ cả thư mục khi chép sang máy khác.

## Đóng gói bộ cài

Cài Inno Setup 6, sau đó chạy từ thư mục gốc dự án:

    powershell -ExecutionPolicy Bypass -File Tools/Build/Package-Windows.ps1 -Version 0.1.0

Bộ cài và mã kiểm tra SHA-256 nằm trong Builds/Installer. Có thể chỉ định -Compiler nếu ISCC.exe ở đường dẫn khác.

Bộ cài dùng thư mục tài khoản hiện tại, không cần quyền quản trị. Phím tắt Desktop là tùy chọn. Gỡ cài đặt không xóa tiến trình trong PlayerPrefs. Không đóng gói save cá nhân, dữ liệu Unity Editor hoặc source asset thô vào bản game.

## Kiểm tra trước khi gửi

- Bắt đầu mới và tiếp tục game, chuyển scene, menu cài đặt và thoát.
- Build không còn lỗi; kiểm tra log khi mở bản Windows độc lập.
- Kiểm tra thư mục dữ liệu, âm thanh, font và giấy phép đi kèm.
- Thử cài vào thư mục sạch, chạy file đã cài, rồi thử gỡ.
- Không xem thời lượng chơi dự kiến là kết quả đo; cần một lượt chơi đầy đủ để đánh giá cân bằng cuối cùng.

Các output trong Builds không được đẩy vào lịch sử mã nguồn Git.
