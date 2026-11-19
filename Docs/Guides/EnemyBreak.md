# Break quái

Đã gắn `EnemyBreak` vào prefab Orc và Elite Orc. Quái dùng `CharacterStats.maxStamina/currentStamina` làm thanh sức bền. Player giữ cơ chế Stamina hiện tại.

## Luồng chiến đấu

1. Đòn đánh trực tiếp hợp lệ làm giảm Stamina của quái. Lượng giảm mặc định dựa trên sát thương vật lý và sát thương chuẩn trước khi trừ DEF; chí mạng và buff tăng sát thương được tính vào lực tác động. DEF chặn hết HP vẫn có thể mất Stamina.
2. Khi Stamina bằng 0, quái vào `Broken`: hủy đòn lao/nhảy, khóa AI di chuyển và tấn công, chặn các sự kiện gây sát thương đến muộn, giữ tư thế nhận đòn. Thanh sức bền hiện `BREAK`.
3. Trong lúc break, DEF hiệu dụng mặc định giảm 50%. DEF gốc không bị ghi đè; đòn gây break dùng DEF trước khi break, các đòn tiếp theo dùng DEF giảm.
4. Khi đã break, nhận đòn tiếp vẫn mất HP nhưng không kéo dài thời hạn break và không tiếp tục làm tụt thanh choáng. Thanh break hiển thị tiến độ phục hồi trong thời gian cấu hình, sau đó DEF trở lại bình thường và AI tiếp tục. Hồi sức bền khi chưa break được xử lý theo cấu hình hồi phục riêng.
5. Pause/cutscene/menu làm dừng thời gian chờ. Quái chết sẽ không được hồi sinh hoặc bật lại AI bởi cơ chế này.

Đòn bị parry hoặc bị chặn bởi bất tử không làm mất Stamina. Thiêu đốt/độc gây sát thương duy trì không làm mất Stamina và không kéo dài break. Elite không tự hồi HP do trạng thái quay về khi đang bị break.

## Chỉnh trong Inspector

- **Character Stats → Max Stamina**: sức bền tối đa, hiện giữ giá trị 100 đang có ở hai prefab.
- **Enemy Break → Recovery Delay**: thời gian không trúng đòn trước khi hồi đầy, mặc định 4 giây.
- **Stamina Damage Multiplier**: hệ số lượng Stamina quái mất khi trúng đòn, mặc định 1.
- **Defense Reduction Percent**: phần trăm DEF bị giảm trong break, mặc định 50.
- **Broken State / Idle State**: tên trạng thái Animator dùng lúc choáng và hồi phục.
- **Bar Height**: vị trí thanh nhỏ trên đầu quái thường.
- **Skill Damage Manager → Skills → Stamina Damage Multiplier**: hệ số phá sức bền riêng từng kỹ năng. 0 không trừ Stamina nhưng đòn trúng vẫn đặt lại thời gian hồi.

Quái thường có thanh vàng dưới thanh HP trên đầu. Elite/Boss có thanh vàng và giá trị Stamina dưới HUD thanh máu rồng. Hai clip `Assets/Animations/Enemies/OrcBroken.anim` và `EliteOrcBroken.anim` giữ tư thế từ sprite nhận đòn sẵn có, không chứa sự kiện tấn công/chết.

Khi tạo loại quái mới, gắn `EnemyBreak`, thêm state `Broken` không có transition tự thoát vào Animator, và cấu hình tên idle đúng. Script điều khiển/tấn công mới cần tôn trọng `EnemyBreak.IsBroken` tương tự Orc hiện tại.

## Kiểm tra

Trong Play Mode không pause: **Tools → Diagnostics → Test Enemy Break**. Bộ kiểm tra chạy trên bản sao quái, bao gồm mất sức bền, hệ số từng skill, giảm/khôi phục DEF, ngắt lao/nhảy, sự kiện animation đến muộn, đánh liên tục, pause và chết trong break.
