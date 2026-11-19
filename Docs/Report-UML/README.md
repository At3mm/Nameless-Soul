# Sơ đồ Use Case và Class cho báo cáo NONAME

- Mở `index.html` để xem, chọn sơ đồ và phóng to.
- Chèn ảnh `images/*.png` vào Word. Ảnh đã xuất độ phân giải cao và gắn metadata 300 DPI.
- Dùng `images/*.svg` nếu công cụ báo cáo hỗ trợ ảnh vector để giữ nét khi phóng to.
- Mở `NONAME-UML.drawio` bằng diagrams.net/draw.io để chỉnh sửa sáu trang. Các ô, nhãn và đường nối là phần tử editable, không phải ảnh nhúng.
- `sources/*.puml` là mã PlantUML; `sources/*.dot` là Graphviz. `build-diagrams.mjs` xuất lại SVG/PNG và draw.io từ mô hình đã đối chiếu.
- Đọc `Bao-cao-UML.md` để lấy thuyết minh, đặc tả UC và giới hạn phạm vi. `Doi-chieu-ma-nguon.md` liên kết các lớp với file thật.

Sáu hình: UC tổng quan; UC tượng thần; UC hội thoại/nhiệm vụ; Class chiến đấu; Class tương tác/nhiệm vụ; Class checkpoint/kinh tế/lưu game.

Đây là tài liệu phân tích, không sửa code gameplay, prefab, scene hoặc dữ liệu lưu.
