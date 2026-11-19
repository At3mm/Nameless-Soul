# Gold, EXP và nâng chỉ số

Gold là tiền mua đồ; EXP là tài nguyên để mua một lần tăng chỉ số. Không có level nhân vật, tự tăng chỉ số khi đủ EXP, hay quy đổi Gold/EXP qua lại. Bắt đầu với 0 Gold, 0 EXP. Khi chết, 50% Gold và EXP được để lại tại điểm chết để thu hồi; chết tiếp trước khi nhặt sẽ thay điểm rơi cũ. Điểm chỉ số đã mua được giữ nguyên. Cầu nguyện và đổi scene không trừ tiền.

## Nâng tại tượng

Ngoài combat, bấm E ở tượng đã mở → Nâng chỉ số → Chọn chỉ số → chọn một dòng → Nâng chỉ số [Enter]. Giao diện hiện số dư, giá trị trước/sau và giá EXP; chọn dòng không tự trừ điểm. Thiếu EXP thì không thể xác nhận. Esc quay lại menu tượng, Esc lần nữa rời tượng.

Bản đầu nâng các thông số hiện có: máu tối đa +10, stamina tối đa +5, tấn công +1 hoặc phòng thủ +0,5 mỗi lần. Không thay định nghĩa STR và chưa thêm VIT hoặc một bộ chỉ số cơ bản mới. Việc nâng máu/stamina cũng cộng phần tăng thêm vào tài nguyên hiện tại.

Giá dùng **tổng số lần đã mua**, không tính riêng cho từng chỉ số:

`EXP cần = 100 + 50 × N + 10 × N²`

N là số lần đã mua trước đó. Ví dụ: tăng máu tốn 100, tiếp theo tăng tấn công tốn 160, tăng stamina tiếp theo tốn 240, lần sau tốn 340. N chỉ phục vụ tính chi phí, không phải level hay điều kiện mở khóa.

## Cấu hình

Mở `Assets/Resources/DefaultEconomy.asset` trong Inspector:

- First Upgrade Cost, Linear Cost Increase, Quadratic Cost Increase: hệ số của giá nâng.
- Health/Stamina/Attack/Defense Per Purchase: mức tăng của từng lần mua.
- Normal/Elite/Boss Gold và Experience: phần thưởng mặc định theo loại quái.

Các giá trị trên là cân bằng thử nghiệm, có thể chỉnh sau. Điểm đã mua được lưu dưới dạng số lần mua từng loại, nên đổi mức tăng trong cấu hình cũng cập nhật hiệu lực của những điểm đã mua khi nhân vật được làm mới.

## Nhận tiền và EXP

Quái thường mặc định cho 10 Gold + 25 EXP, Elite cho 50 Gold + 150 EXP, Boss cho 250 Gold + 1000 EXP. Phần thưởng cấp khi quái chết, mỗi quái chỉ cấp một lần trong một mạng sống. Xác quái bị xóa, disable hay callback lặp không cấp thêm. Quái thường được tạo lại bằng Cầu nguyện có mạng sống mới, có thể kiếm thưởng lại. Hiện thưởng theo cái chết của quái, chưa phân biệt người gây đòn kết liễu.

`EnemyClassification` tự gắn `EnemyCurrencyReward` khi chạy. Muốn cấu hình riêng một quái, gắn `EnemyCurrencyReward` sẵn trên prefab, bỏ Use Default Rewards rồi chỉnh Gold/Experience. Bỏ Rewards Enabled nếu quái đó không được phát thưởng.

Nhiệm vụ hoặc vật phẩm nhặt dùng `GameEconomy.AddGold(amount)`, `AddExperience(amount)` hoặc `AddRewards(gold, exp)`. Các UnityEvent có thể gọi `PlayerProgression.GrantGold(int)` / `GrantExperience(int)` trên Player khi chạy. Số âm và giao dịch vượt giới hạn số nguyên bị từ chối, không làm thay đổi một phần số dư.

## Nối cửa hàng

Hiện chưa đặt NPC bán hàng hoặc tạo inventory/catalog mới. Logic giao dịch dùng Gold đã sẵn sàng:

```csharp
bool bought = GameEconomy.TryPurchase(priceInGold, () => inventory.TryAdd(item, amount));
```

`inventory.TryAdd` ở ví dụ là hàm cấp đồ của hệ inventory sẽ nối sau, không phải API đã có trong dự án. Callback phải trả về true khi đã cấp đồ thành công. Thiếu Gold thì không gọi callback. Callback trả false hoặc ném exception thì không trừ Gold; callback không được làm thay đổi ví hoặc mở giao dịch ví lồng nhau. `TrySpendGold(int)` dùng cho khoản phí không cấp vật phẩm.

## Lưu và khôi phục

Ví và số lần mua lưu chung trong PlayerPrefs ở khóa `Noname.Economy.v1`, độc lập với vị trí checkpoint. Mỗi giao dịch thành công tự lưu. `PlayerProgression` tự gắn vào CharacterStats có tag Player và áp dụng tiền thưởng chỉ số khi nhân vật xuất hiện; không cộng trùng khi bật lại component, đổi scene hay hồi sinh. Không cần sửa từng scene để gắn lại.

Gold/EXP hiển thị trên HUD ở dòng dưới thanh stamina, thay dòng số HP/stamina cũ: icon chồng tiền PaperBook `Items_14` cho Gold và icon xanh `Items_03` cho EXP. Số dư cập nhật ngay khi giao dịch thay đổi ví, tự khôi phục sau khi bật lại HUD hoặc chuyển scene. Menu tượng và bảng nâng chỉ số cũng hiển thị số dư. Các trạng thái buff/debuff tạm thời vẫn dùng hệ hiện tại, không được ghi thành nâng cấp vĩnh viễn.

Kiểm thử trong unpaused Play Mode: Tools → Diagnostics → Test Gold EXP Economy. Kiểm thử tự sao lưu/khôi phục khóa economy hiện tại; kết quả ở `Library/EconomyTests/results.txt`.
