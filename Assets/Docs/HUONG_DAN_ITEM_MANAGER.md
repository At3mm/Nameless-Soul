# Item Manager

Mở **Tools > Items > Item Manager**. Chọn Mẫu tạo mới trước khi bấm Thêm vật phẩm. Các mẫu gồm thuốc hồi máu, hồi stamina, buff, giải độc, bùa hồi quy tái sử dụng, chìa khóa và bằng chứng boss. Món mới nằm ở `Assets/Resources/Items`; không cần thêm enum hoặc code cho mỗi món.

Tool có hai tab **Vật phẩm** và **Cổ vật cường địch**, giống túi đồ. Thuốc, dây chuyền, chìa khóa và vật phẩm nhiệm vụ nằm chung trong Vật phẩm. Vai trò Quest / Key / BossEvidence vẫn chỉnh độc lập; không mất điều kiện nhiệm vụ khi gộp tab. Cổ vật có thể bật Hiển thị trong tab Vật phẩm.

Elite/Boss tự cộng 1 Hạt sinh mệnh khi chết; không cần tạo pickup hay gán hạt thêm vào Boss Item Reward. Trường Item trên Boss Item Reward dành cho cổ vật/vật phẩm riêng của boss.

## Dữ liệu

- ID duy nhất cho mọi loại vật phẩm. Đổi ID không đổi tham chiếu asset trong nhiệm vụ, nhưng save cũ dùng ID cũ sẽ không tự chuyển. Bùa hồi quy cũ giữ ID hệ thống.
- Tên, mô tả, Icon (kéo Sprite đã cắt vào ô), độ hiếm, tab túi đồ.
- Vai trò có thể chọn nhiều: Quest, Key, BossEvidence. Đây là nhãn; gán điều kiện thật ở cửa / Quest Manager.
- Max Stack là tổng số lượng sở hữu tối đa. Hạ giới hạn không xóa đồ đã có; chỉ chặn nhận thêm.
- Consumable: bật để mất một món sau lần dùng thành công; tắt để tái sử dụng. Không đủ điều kiện hồi phục sẽ không tiêu hao.
- Effects có thể chứa nhiều hiệu ứng. Hồi máu/stamina tính % tối đa; buff dùng Percent và Duration. Hồi quy phải là hiệu ứng duy nhất, bị chặn trong combat và cần đã cầu nguyện/du hành đến tượng.
- Không có Effects: món không sử dụng trực tiếp, phù hợp chìa khóa/cổ vật/bằng chứng.
- Buyable/Sellable bật giao dịch trong cửa hàng hiện tại. Giá bán lại không cao hơn giá mua. Cửa hàng hiện dùng nguồn hàng không giới hạn.

## Nguồn nhận và nhiệm vụ

1. Quest Manager > Items: kéo bất kỳ GameItemDefinition vào phần thưởng.
2. Mục tiêu > Cổ vật xác nhận: kéo món cần sở hữu, đặt số lượng. Đủ mục tiêu rồi quay lại nói chuyện với NPC thì nhiệm vụ tự trả; vật phẩm xác nhận không bị tiêu hao. Nhiệm vụ tiếp theo nối qua Prerequisites.
3. Door Interactable > Required Key: kéo vật phẩm chìa khóa. Kiểm tra sở hữu, không tiêu hao. Cửa bị chặn trong combat.
4. Item Manager > Tạo pickup trong scene: tạo tại tâm Scene View. Di chuyển đến nơi cần đặt, mỗi pickup cần Pickup Id riêng; nếu nhân bản phải đổi ID. Lưu scene. Nhặt một lần sẽ được ghi trong save.
5. Gắn Enemy Item Drop lên quái: Item, Quantity, Chance và Once Per Encounter. Quái chết sẽ chuyển đồ vào túi và báo nhận. Boss Item Reward hiện cũng nhận vật phẩm chung.
6. **Tools > Game Testing > Add Gold EXP and Items** có ô Vật phẩm tùy biến để cộng test vào save đang dùng.

## Trong game

Vật phẩm hiện ở tab đã chọn. Món có Effects có thể gắn/tháo dùng nhanh ở phần chi tiết; tổng vật phẩm mới và cũ cùng giới hạn 5 ô. Phím đổi/dùng vẫn theo thiết lập game. Vật phẩm cũ, GUID và các save key được giữ; đồ cũ không tự chuyển ID.

Hiệu ứng hoàn toàn mới cần lập trình một lần vào ItemEffectKind và PlayerConsumables, sau đó dùng lại trong tool.

## Kiểm tra

Tools > Items > Test Item Manager (Ctrl+Shift+F7) kiểm tra dữ liệu và các giao dịch/giới hạn/lưu/điều kiện nhiệm vụ; tự khôi phục dữ liệu save sau test. Đây là kiểm tra cấu trúc và logic, không thay thế kiểm tra cảm giác chơi.

Cấu hình NPC trả nhiệm vụ: trong Dialogue Graph, chọn Bắt đầu → Tự trả khi nói chuyện, thêm nhiệm vụ của NPC. Nhiệm vụ ở Tự nhận, lựa chọn AcceptQuest/ReportQuest hoặc dấu hậu nhiệm vụ cũng được tự nhận diện. NPC khác không tự trả thay. Không cần nút báo cáo.

## Hạt sinh mệnh

Asset: Assets/Resources/Items/LifeSeed.asset, ID hệ thống life-seed. Chọn trong tab Vật phẩm để đổi tên, mô tả, icon. Số lượng trong túi chính là GameEconomy.LifeSeeds đã lưu, không tạo một kho hạt thứ hai. Elite/Boss tự cộng 1; hạt từ pickup, phần thưởng nhiệm vụ, Enemy Item Drop hoặc tool cộng đồ cũng dùng cùng kho và biên nhận chống nhận trùng. Có thể dùng asset này làm mục tiêu hoặc phần thưởng nhiệm vụ.

Không có hiệu ứng dùng nhanh. Dùng tại Tượng thần → Hiến tế vật phẩm, mỗi hạt tăng sức chứa bình máu 1, mặc định 3 và tối đa 10. Đạt giới hạn không mất hạt. Giữ ID life-seed và số lượng tối đa 2147483647; không thêm Effects. Mặc định không mua/bán; chỉ bật khi chủ động thiết kế cửa hàng riêng.
