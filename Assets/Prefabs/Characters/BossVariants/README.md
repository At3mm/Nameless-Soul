# Hai lần gặp Necromancer

Hai prefab bên cạnh là **Prefab Variant** của `Assets/Prefabs/Characters/Necromancer.prefab`, dùng chung sprite, Animator, animation event, hiệu ứng, summon và code.

- `Necromancer_WildForest.prefab`: Rừng hoang, **một phase**, dùng NormalAttack / HeavyAttack / Summon trong cả trận. Đã đặt `Necromancer_WildForest` tại (-69.5, -60.5) trong vùng `area2` của WastelandScene. Dùng preset sát thương `03_WildForest`.
- `Necromancer_DeadForest.prefab`: Rừng chết (asset khu vực hiện tên **Vùng đất chết**, `area3`), giữ vị trí cũ (17.77, 11.37). Hai phase: nửa đầu chỉ NormalAttack, còn 50% máu chuyển phase 5 giây rồi có đủ ba chiêu. Dùng preset `04_Deadlands`.

## Tinh chỉnh

Chọn variant hoặc instance trong scene → **Boss Phase Controller**:

- **Use Second Phase** tắt: chỉ có **Single Phase Skills**, chọn các chiêu dùng suốt trận.
- Bật: chọn **Phase 1 Skills**, **Phase 2 Skills**, ngưỡng máu và thời gian chuyển phase như trước.

Thay đổi animation/marker trên các clip chung ảnh hưởng cả hai phiên bản. Lần thiết lập này không sửa clip, frame hay marker. Muốn chỉnh riêng chỉ số / chiêu / hiệu ứng, chỉnh trên variant tương ứng để giữ kế thừa các phần còn lại từ prefab gốc.

## ID và save

Hai lần gặp có encounter ID khác nhau, mỗi lần ghi nhận chiến thắng và phần thưởng độc lập. Instance ở Rừng chết giữ **đúng ID cũ** để save và mục tiêu nhiệm vụ `SilenceTheNecromancer` vẫn trỏ tới lần gặp đó; không cần nhận lại nhiệm vụ. Instance Rừng hoang dùng `boss:necromancer:wild-forest:01`.

**Preserve Persistent Id** trên instance Rừng chết giữ ID cũ khi thay prefab. Khi sao chép một encounter có ID được giữ thủ công, đặt ID mới duy nhất cho bản sao. Các variant không đóng đinh ID dùng chung cho mọi instance.

Các khu vực này hiện cùng nằm trong `Assets/Scenes/WastelandScene.unity`; không tạo thêm scene mới. Có thể di chuyển instance Rừng hoang để chỉnh vị trí chiến đấu mà giữ nguyên ID của nó.

Đã kiểm tra: Rừng hoang còn 10% HP vẫn phase 1 và nhận sát thương bình thường; Rừng chết chuyển ở 50%, có bảo vệ trong lúc chuyển và dùng đủ chiêu sau đó; preset sát thương đúng; ID / thưởng / trạng thái chết không lẫn nhau. 14 file animation được đối chiếu hash và giữ nguyên. Save người chơi được phục hồi sau thử nghiệm.
