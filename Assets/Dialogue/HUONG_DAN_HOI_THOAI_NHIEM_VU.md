# Hướng dẫn hội thoại và nhiệm vụ

## Ví dụ đã thiết lập

- NPC: Assets/Prefabs/NPCs/NPC_Priest.prefab (Tu sĩ).
- Hội thoại: Assets/Dialogue/PriestConversation.asset.
- Nhiệm vụ: Assets/Resources/Quests/DefeatTheWerebear.asset — Khuất phục Werebear.
- Vật phẩm thưởng: Assets/Resources/QuestItems/PriestBlessing.asset — Ấn nguyện bình an.

Nói chuyện với Tu sĩ để tự nhận nhiệm vụ. Hạ đúng Werebear rồi quay lại nói chuyện với Tu sĩ: nhiệm vụ tự trả, trao thưởng và mở shop. Hội thoại sau nhiệm vụ chỉ tiếp tục câu chuyện. Quy tắc tự hoàn thành áp dụng cho mọi nhiệm vụ đã nhận, kể cả nhiệm vụ mới tạo.

Phím ↑ ↓ hoặc W/S chọn đáp án; E/Enter xác nhận; Esc rời hội thoại. Có thể bấm trực tiếp đáp án bằng chuột. Tab Vật phẩm nhiệm vụ trong túi đồ hiển thị nhiệm vụ đã nhận, tiến độ, trạng thái và vật phẩm kỷ niệm; phần mô tả dài cuộn bằng chuột.

Cách triển khai lấy cảm hứng từ nhịp hội thoại/nhiệm vụ của Sekiro: lựa chọn nói chuyện rõ ràng, lời thoại đổi theo tiến độ thế giới, hội thoại hậu nhiệm vụ. Lời thoại và hình ảnh là nội dung riêng của dự án, không trích từ Sekiro.

## Chỉnh lời thoại và đáp án

1. Chọn PriestConversation.asset trong Project. Mỗi phần tử Nodes là một lượt nói của NPC.
2. Id là mã nút, duy nhất trong hội thoại; Start Node Id là mã nút bắt đầu.
3. Text là lời NPC; Speaker để trống sẽ dùng Display Name của NPC.
4. Nếu nút không có Choices, E sẽ chuyển tới Next Node Id; để trống Next Node Id sẽ kết thúc.
5. Mỗi Choice là một câu trả lời của người chơi. Text là nội dung đáp án; Next Node Id trỏ tới nút sau khi chọn.
6. Action:
   - Continue: chuyển nút bình thường.
   - AcceptQuest: nhận Quest được gắn vào lựa chọn.
   - ReportQuest: tương thích hội thoại cũ; nhiệm vụ mới dùng Continue cho hội thoại hậu nhiệm vụ vì phần thưởng đã tự trao.
   - OpenShop: mở NpcShop trên cùng NPC.
   - End: kết thúc hội thoại.
7. Condition Quest + Required Status quyết định khi nào đáp án xuất hiện. Để trống Condition Quest thì luôn hiện.
   - Available: chưa nhận, đã đáp ứng nhiệm vụ tiền đề.
   - Active: đã nhận, còn thiếu mục tiêu.
   - ReadyToReport: đủ mục tiêu, chờ quay lại nói chuyện với NPC.
   - Completed: đã quay lại NPC và tự trả nhiệm vụ. Phần thưởng tự trao; nếu đầy kho thì giữ chờ và tự thử lại khi có chỗ.
   - Locked: chưa mở hoặc cấu hình mục tiêu chưa hợp lệ.
8. Text Quest là nhiệm vụ dùng để thay các từ khóa {title}, {progress}, {rewards}, {status} trong Text.

Lưu ý: Quest của Action và Condition Quest là hai trường khác nhau. Action nhận nhiệm vụ hoặc tiếp tục hội thoại; Condition chỉ quyết định hiện đáp án. Không đổi Id của nút đang được nút khác trỏ tới mà chưa cập nhật Next Node Id. Inspector báo lỗi liên kết thiếu và ID trùng.

## Tạo hội thoại cho NPC khác

Create → Game → Dialogue → Conversation, lưu ở Assets/Dialogue. Tạo node start và các đáp án, sau đó kéo asset vào trường Dialogue trên NpcInteractable của NPC. Nếu Dialogue để trống, NPC tiếp tục dùng mảng Dialogue Lines cũ. NPC có OpenShop cần thêm NpcShop trên cùng GameObject.

## Tạo hoặc chỉnh nhiệm vụ

Create → Game → Quests → Quest. Lưu asset trong Assets/Resources/Quests để hệ thống tìm lại tên, icon và nhật ký sau khi tải save.

- Quest Id: mã duy nhất, ví dụ silence-necromancer. Không đổi sau khi đã có save; đổi ID sẽ được xem là nhiệm vụ khác. Khi Duplicate asset, phải đổi Quest Id.
- Title, Description, Icon: tên, mô tả và biểu tượng trong túi đồ.
- Objectives: tất cả mục tiêu đều phải đủ. Count là số boss cần hạ.
- Boss Id: khớp Quest Boss Id trên EnemyClassification của boss. Hai prefab mẫu dùng necromancer và orc-rider.
- Encounter Id: để trống nếu chấp nhận mọi boss cùng loại. Điền DefeatId nếu chỉ chấp nhận một boss cụ thể trên map. Dùng menu chuột phải component EnemyClassification → Quests/Copy Encounter ID, hoặc dùng nút lấy mục tiêu từ boss trong Inspector nhiệm vụ.
- Count Previous Kills: bật để tính cả chiến thắng trước lúc nhận nhiệm vụ. Với save cũ có từ trước hệ thống nhiệm vụ, dùng Encounter Id chính xác để khôi phục chiến thắng từ EnemyDefeatState. Nếu tắt, chỉ tính những lần hạ boss sau khi nhận nhiệm vụ.
- Prerequisites: các nhiệm vụ phải hoàn thành trước khi nhiệm vụ này mở.
- Gold, Experience, Life Seeds: số tài nguyên thưởng.
- Consumables: chọn loại vật phẩm hỗ trợ và số lượng, nhận vào kho hỗ trợ có sẵn.
- Items: vật phẩm nhiệm vụ/kỷ niệm riêng; chọn QuestItemDefinition và số lượng.

Boss không bị tính trùng nếu phát tín hiệu chết nhiều lần trong cùng một encounter. Boss cùng loại nhưng khác encounter có thể tính riêng cho Count > 1. Vì boss đã hạ không hồi sinh mặc định, đừng yêu cầu Count lớn hơn số boss thực tế trên map. Muốn Count > 1 với cùng loại boss, để Encounter Id trống.

## Tạo vật phẩm thưởng riêng

Create → Game → Quests → Quest Item. Lưu trong Assets/Resources/QuestItems. Đặt Item Id duy nhất, Display Name, Description, Icon. Kéo vào Items của nhiệm vụ. Vật phẩm này nằm trong tab Vật phẩm nhiệm vụ và không tự có hiệu ứng chiến đấu. Muốn thưởng vật phẩm sử dụng được, chọn Consumables; muốn thay đổi chỉ số/buff đặc biệt cho mề đay cần bổ sung logic sử dụng riêng.

Ví dụ hiện tại: 150 vàng, 300 EXP, 1 Hạt sinh mệnh và 1 Ấn nguyện bình an. Có thể đổi hoặc xóa từng phần thưởng trong asset nhiệm vụ.

## Lưu tiến độ và thử lại

Trạng thái nhiệm vụ, các boss đã hạ cho nhiệm vụ, vật phẩm nhiệm vụ và dấu đã xem hội thoại hậu nhiệm vụ được lưu ở Noname.Quests.v1. Phần thưởng kinh tế có biên nhận trong GameEconomy, nên kiểm tra lại/tải lại không trả thưởng hai lần, kể cả khi bước trả thưởng trước đó bị gián đoạn.

MainMenu New Game, Game Reset và Tools/Game Testing/Test From Beginning đã bao gồm save nhiệm vụ. Dùng Test From Beginning để thử luồng sạch và khôi phục save thật khi dừng Play. Chỉnh lời thoại/mục tiêu không xóa tiến độ đang có; nhiệm vụ đã hoàn thành vẫn giữ trạng thái theo Quest Id cũ.

Tools → Dialogue and Quests → Create Missing Example chỉ tạo ví dụ còn thiếu, không ghi đè asset hội thoại/nhiệm vụ bạn đã sửa.

Kiểm tra tự động bao gồm tự trả khi quay lại đúng NPC, mở shop, chống thưởng lặp, chuyển đổi save cũ, phần thưởng chờ khi đầy kho và hội thoại hậu nhiệm vụ. Save thật được khôi phục sau test.



## Black Knight — nhánh theo Necromancer đầu tiên

- Chưa hạ Necromancer Rừng hoang: vào before_necromancer, chưa nhận nhiệm vụ.
- Đã hạ và chưa nhận nhiệm vụ: giữ các đoạn thoại start → black_knight_1…4 → offer.
- Đang làm: vào progress, mục tiêu là đúng Necromancer Rừng chết.
- Đã hạ Necromancer Rừng chết: vào report. Nhiệm vụ tự hoàn thành khi quay lại nói chuyện với Black Knight sau khi hạ mục tiêu. Quest này cấu hình phần thưởng bằng 0 nên không trả vàng/EXP/vật phẩm; report chỉ là hội thoại.
- report → after_report → duel giữ lời thoại đã viết. Cuối duel có Chiến đấu (ChallengeBoss) và Chưa chiến đấu (End).
- Chọn Chưa chiến đấu hoặc Esc: Black Knight vẫn là NPC. Nói chuyện lại được đi qua nhánh sau nhiệm vụ và chọn lại.
- Chọn Chiến đấu: đóng đối thoại, chờ Aggression Delay rồi bật AI boss. Require Dialogue Challenge trên Quest Boss Encounter phải bật. Save đã hoàn thành nhiệm vụ cũng không tự kích hoạt combat.

Trong Dialogue Graph: chọn report để sửa thoại hậu nhiệm vụ; chọn duel để sửa hai lựa chọn. Action ChallengeBoss dùng Quest Boss Encounter ở cha của NPC để kiểm tra nhiệm vụ đã hoàn thành. Không gán ReportQuest vào report nữa.

Test: Tools > Dialogue and Quests > Test Black Knight Flow. Bài test dùng actor tạm, kiểm tra nhận nhiệm vụ, boss mục tiêu, không thưởng, từ chối và đồng ý chiến đấu; khôi phục save/start scene sau khi chạy.


## Priest — Werebear và mở khóa shop

Nhiệm vụ Khuất phục Werebear dùng asset Resources/Quests/DefeatTheWerebear, ID priest-defeat-werebear. Hạ đúng Werebear Elite đã đặt trên WastelandScene, quay lại nói chuyện với Tu sĩ để tự trả, nhận thưởng và mở shop. Chiến thắng trước khi nhận nhiệm vụ vẫn được ghi nhận. Phần thưởng cũ được giữ.

Trong PriestConversation, lựa chọn OpenShop chỉ hiện khi nhiệm vụ Completed. NpcShop trên NPC_Priest cũng gán Required Quest để chặn gọi mở/giao dịch trực tiếp trước khi hoàn tất. Với NPC khác có thể dùng cùng trường Required Quest; để trống là không khóa theo nhiệm vụ.

Test: Tools > Dialogue and Quests > Test Priest Quest and Shop. Dữ liệu test được khôi phục sau khi chạy.

## Quy tắc chung cho nhiệm vụ mới

Nhận nhiệm vụ → đủ tất cả Objectives → ReadyToReport → quay lại nói chuyện với NPC → tự Completed và tự trao Rewards. Prerequisites mở ngay khi nhiệm vụ trước Completed. Không cần nút trả nhiệm vụ. Dùng điều kiện Completed để nối thoại tiếp theo. Nếu muốn thoại hậu nhiệm vụ chỉ xem một lần, bật Chưa xem hậu nhiệm vụ trên điều kiện vào và gán Đã xem hết thoại hậu nhiệm vụ ở bảng cuối. Save cũ đã hoàn thành không bị nhận thưởng lại.

Cấu hình NPC trả nhiệm vụ: trong Dialogue Graph, chọn Bắt đầu → Tự trả khi nói chuyện, thêm nhiệm vụ của NPC. Nhiệm vụ ở Tự nhận, lựa chọn AcceptQuest/ReportQuest hoặc dấu hậu nhiệm vụ cũng được tự nhận diện. NPC khác không tự trả thay. Không cần nút báo cáo.
