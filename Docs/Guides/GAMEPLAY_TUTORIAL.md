# Hướng dẫn ngay trong gameplay

Khi Bắt đầu mới, các gợi ý nhỏ xuất hiện ở góc phải phía dưới trong WastelandScene. Không dừng thời gian và không khóa điều khiển. Các thao tác thành công được ghi nhớ, kể cả khi thực hiện trước thứ tự gợi ý. Uống máu chỉ được nhắc khi còn dưới 65% HP và còn bình; không bắt buộc bị thương để hoàn thành phần nhập môn. Cầu nguyện thành công tại tượng hoàn tất bước lưu hành trình.

- Chỉnh nội dung, thời gian bắt đầu, quãng di chuyển và khoảng nhắc tượng tại `Assets/Resources/DefaultGameplayTutorial.asset`.
- Các token `{Up}`, `{Dash}`, `{Attack}`, `{Parry}`, `{Skill}`, `{Heal}`, `{Interact}` tự hiển thị phím đang gán trong cài đặt.
- Gợi ý tạm ẩn khi mở menu, hội thoại, cutscene, fade hoặc khi chết. Tiếp tục lại khi trở về gameplay.
- Nút Bỏ qua hướng dẫn kết thúc hướng dẫn cho lượt đó. Chọn Bắt đầu mới sẽ bật lại.
- Save cũ chưa có dữ liệu hướng dẫn được giữ nguyên, không tự chèn hướng dẫn vào giữa lượt chơi.
- Dùng `Tools > Game Testing > Test From Beginning` để thử với save tạm; dừng Play để khôi phục cả tiến trình hướng dẫn cũ.
- Không thay đổi scene, bố trí quái, clip hay animation marker. GameplayTutorial tự khởi tạo trong scene được cấu hình.
