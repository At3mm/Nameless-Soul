# Thông báo vàng và EXP

Bảng nhỏ bên phải hiển thị số vàng và EXP vừa thực nhận, dùng lại icon và font của HUD. Các khoản nhận liên tiếp được cộng dồn, chỉ hiện dòng có số nhận lớn hơn 0. Thông báo trượt vào / mờ ra theo nhịp pixel chung, mặc định 12 FPS.

Gồm thưởng quái (sau hệ số Ăn hồn), thưởng nhiệm vụ, bán vật phẩm và nhặt lại tiền / EXP đã rơi. Không hiện khi tiêu tiền, nâng chỉ số, tải save, giao dịch thất bại hoặc lặp lại phần thưởng đã nhận.

Mở `Assets/Resources/DefaultCurrencyGainNotice.asset` để chỉnh icon, màu, vị trí, thời gian giữ và thời gian vào / ra. Mặc định giữ 3 giây, vào 0,25 giây và ra 0,4 giây. Position dùng độ phân giải tham chiếu 1280 × 720: X là khoảng cách tới mép phải (âm để lùi vào), Y là độ lệch so với giữa màn hình.

Khi đang mở menu, hội thoại hoặc cutscene, bảng tạm ẩn và giữ thời gian còn lại. Khoản nhận lúc mở menu chờ đến khi trở lại chơi. Chuyển scene xóa thông báo cũ. Bảng không chặn chuột hay thao tác gameplay.

`GameEconomy.CurrencyReceived` phát sau giao dịch cộng tiền / EXP đã lưu thành công. `CurrencyGainNotice` nhận sự kiện trực tiếp, không suy đoán từ chênh lệch tổng ví, nên tải save không tạo thông báo giả.

Đã kiểm tra các nguồn nhận, cộng dồn, một dòng / hai dòng, loại trừ giao dịch lỗi / trùng, hệ số Ăn hồn, ngừng thời gian trong menu, tự hết hạn, không chặn UI và không nhân đôi khi đổi scene. Save người chơi được phục hồi nguyên trạng sau test.
