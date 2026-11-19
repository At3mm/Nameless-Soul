# Nút menu pixel dùng chung

- Sprite 9-slice: `Assets/Art/UI/Buttons/PixelMenuButton.png`
- Asset giao diện: `Assets/Resources/UI/PixelMenuButtonStyle.asset`
- Prefab tái sử dụng: `Assets/Prefabs/UI/PixelMenuButton.prefab`

Kéo prefab vào Canvas, sửa chữ trong con `Label` và gán hành động trong Button → On Click.
Kéo giãn bằng RectTransform; viền giữ nguyên nhờ 9-slice. Chỉnh màu bình thường,
được chọn, nhấn, khóa, màu chữ và bóng tại asset PixelMenuButtonStyle.

Các menu main, tạm dừng, túi đồ, gán phím, tượng thần, du hành, nâng cấp,
cửa hàng, xác nhận và lựa chọn hội thoại dùng cùng asset.

Nút tạo bằng code gọi `PixelMenuButtonStyle.Apply(button)` sau khi tạo nhãn.
Menu tự điều hướng gọi `PixelMenuButtonStyle.Select(button, isSelected)`.
Component chỉ xử lý hình thức, không thay listener, điều hướng hay trạng thái interactable.
Nút có sẵn trong scene được gắn giao diện khi scene tải. Nút mới tạo runtime cần gọi Apply.

Tools → UI → Create Shared Pixel Menu Button Assets tạo các asset còn thiếu,
không ghi đè sprite, style hay prefab đã chỉnh.
