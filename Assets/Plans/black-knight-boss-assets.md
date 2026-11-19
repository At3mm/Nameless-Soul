# Hồ sơ giai đoạn đã hoàn tất: Black Knight Boss
Phần này lưu lịch sử triển khai boss. Kế hoạch đang xin duyệt cho luồng Wasteland → Cave và bản đồ cuối game nằm ở phần **Giai đoạn tiếp theo: CaveScene cuối game** bên dưới. Không áp dụng lại mô tả beam cũ: hiện đạn bay thẳng theo hướng khóa, hình ảnh luôn đứng, cần giữ nguyên.

## Project Overview
- Game Title: Dự án Noname (tên thư mục; chưa xác nhận tên phát hành).
- High-Level Concept: Hoàn thiện Black Knight Boss biến thể C từ chín spritesheet đã đặt đúng thư mục: cắt ô 100×100, 10 PPU, animation, AI chiến đấu và beam độc lập hướng về người chơi. Người dùng đã chọn tạo prefab riêng; không thay BlackKnight trong WastelandScene và không sửa nhiệm vụ của boss cũ.
- Players: Giữ nguyên hệ thống người chơi hiện có; boss dùng mục tiêu mang tag Player.
- Inspiration / Reference Games: Không áp dụng cho việc tổ chức tài nguyên.
- Tone / Art Direction: Nhân vật 2D pixel art hiện có; giữ nguyên pixel và alpha, không thêm lớp phủ.
- Target Platform: StandaloneWindows64.
- Screen Orientation / Resolution: Giữ nguyên cấu hình dự án.
- Render Pipeline: UniversalRP; Unity 6000.3.9f1.

# Game Mechanics
## Core Gameplay Loop
- Boss nhận biết người chơi, truy đuổi trong lãnh thổ, chọn chiêu theo khoảng cách và cooldown, chuẩn bị/tấn công/hồi phục rồi tiếp tục áp lực. Dùng EnemyAwareness, EnemyBehavior, EnemyCombat, SkillDamageManager, EnemyBreak và BossMovesetDirector hiện có; không thêm controller platformer hoặc NavMesh.
- NormalAttack dùng Attack01; Skill2 dùng Attack02; Skill3 dùng Attack03(No beam effects) kết hợp hiệu ứng Beam độc lập. Không dùng logic thrust/leap của BlackKnightCombat biến thể A cho những ảnh C có động tác khác.
- Ngắm vị trí hiện tại của người chơi trong lúc vận Skill3, chốt hướng tại khung phát beam; không tự quay bám theo sau khi bắn. Tín hiệu vận chiêu và hướng khóa cho người chơi cơ hội né: cơ chế tạo thử thách, động lực đọc chiêu, trải nghiệm công bằng theo MDA.
- Beam là luồng tia tại điểm phát, không phải đạn bay, có chiều dài/độ rộng/lifetime chỉnh trong Inspector. Quay hướng 360° về người chơi; ray/shape cast chặn ở vật cản đầu tiên. Chỉ vùng thật sự nhìn thấy của tia mới có sát thương, không dùng hình tròn khổng lồ thay tia.
- Mỗi lần phát beam chỉ xử lý một lần trên mỗi nhân vật trong cửa sổ gây sát thương; không nhân sát thương theo FPS hoặc số collider. Dùng công thức ATK/STR/crit, DEF, stamina, parry và i-frame của dự án. Dừng beam khi boss chết, bị ngắt chiêu, bị break, chuyển phase hoặc bị vô hiệu hóa.
- Giữ cấu trúc 2 phase như boss gốc: phase 1 NormalAttack/Skill2; phase 2 thêm Skill3 ở 50% HP. Dùng thời gian chuyển phase hiện có 5 giây, với clip chuyển phase dựa trên Idle không có sự kiện sát thương, không thêm lớp màu phủ.
- Đòn cận chiến và beam cần khoảng nghỉ hồi phục; tham số khởi đầu lấy từ boss gốc rồi kiểm tra bằng animation C, không tuyên bố đã cân bằng cuối cùng.
## Controls and Input Methods
Dự án dùng cả Input System mới và Legacy (Input System 1.18.0); không thêm hoặc sửa điều khiển. Boss lấy mục tiêu từ hệ thống hiện có, không đọc bàn phím trực tiếp.

# UI
Không tạo HUD/menu mới. Prefab dùng EnemyClassification/Boss để tham gia HUD boss hiện có; không thêm lớp phủ màu lên nhân vật. Wireframe khi giao chiến: `[HUD boss hiện có: Black Knight Boss | HP / Stamina]` phía trên vùng chơi, nhân vật và beam nằm trong thế giới; giữ nguyên bố cục UI của dự án. Bố cục thư mục trong cửa sổ Project:
```text
Assets/Sprites/Black Knight Boss/
├── Animation/                 (controller và clip nhân vật)
├── BlackKnightBossAttack/     (4 spritesheet đánh)
├── BlackKnightBossDeath/      (1 spritesheet)
├── BlackKnightBossIdle/       (1 spritesheet)
├── BlackKnightBossTakeDmg/    (1 spritesheet)
├── BlackKnightBossWalk/       (1 spritesheet)
└── Effects/Beam/              (1 spritesheet hiệu ứng độc lập)
```

# Key Asset & Context
Ảnh mới gửi là ảnh chụp ứng dụng xem ảnh, chỉ dùng để đọc tên và đối chiếu. Không đưa ảnh chụp cửa sổ vào thư mục sprite, không cắt nhân vật từ ảnh chụp.

Nguồn thật đã xác minh tồn tại tại `Assets/AI Toolkit/Temp/AssistantImageReferences/`. Bảng đối chiếu với thứ tự CHÍN ẢNH SPRITESHEET GỬI BAN ĐẦU (không phải thứ tự ảnh chụp tên mới):

| Ảnh ban đầu | Tệp nguồn trong thư mục tạm | Tệp đích tương đối trong Assets/Sprites/Black Knight Boss | Kích thước / số ô 100×100 |
|---|---|---|---|
| 1 | 016123aff8f8596ba64e3010e65284ea8c174d38.png | BlackKnightBossTakeDmg/Black Knight_C_Hurt.png | 400×100 / 4 |
| 2 | 1fdb37680960ab60c8329f61d5d0e1b548f7f2e1.png | BlackKnightBossIdle/Black Knight_C_Idle.png | 600×100 / 6 |
| 3 | f8649fb1c090f6aa8658209dc1e56cac47a7e399.png | BlackKnightBossWalk/Black Knight_C_Walk.png | 800×100 / 8 |
| 4 | 7af6210fc5d4938aa5115b0a1ad1f9fd3e62ab7e.png | BlackKnightBossAttack/Black Knight_C_Attack01.png | 800×100 / 8 |
| 5 | 4dff0b15fafdd217bbcf8b2bcf4b9d1f95dec97d.png | BlackKnightBossAttack/Black Knight_C_Attack02.png | 900×100 / 9 |
| 6 | dc07adf7e7996c2848e8289178a72e16fd917d32.png | BlackKnightBossAttack/Black Knight_C_Attack03.png | 1300×100 / 13 |
| 7 | 75079a10366ef18eac7423db0910f5fc9a28cd22.png | Effects/Beam/Black Knight_C_Beam.png | 700×100 / 7 |
| 8 | 513f5d501ce3d6f79a35abf88714d263fde0dc68.png | BlackKnightBossDeath/Black Knight_C_Death.png | 1000×100 / 10 |
| 9 | 3d927b6dadbc008d097cf4303bcab7c047a0aedf.png | BlackKnightBossAttack/Black Knight_C_Attack03(No beam effects).png | 1300×100 / 13 |

Phân biệt quan trọng: `Attack03(No beam effects)` là nguồn animation nhân vật để sau này bắn beam riêng; `Attack03` có beam gắn sẵn chỉ lưu để tham khảo, không chạy đồng thời cùng beam độc lập.

Cách thực hiện: sao chép ảnh nguồn sang đích với tên gốc (không chuyển/xóa nguồn tạm), tạo GUID mới để không làm hỏng liên kết tạm. Ưu điểm: an toàn, độc lập thư mục tạm; nhược điểm: thêm bản sao chín tệp PNG dung lượng nhỏ. Không sửa Black Knight biến thể A, prefab hay scene hiện có.

## Trạng thái đã xác minh và giới hạn thay đổi
- Chín PNG đã được sao chép/đổi tên đúng; chưa cấu hình 10 PPU. Importer hiện có Sprite Multiple, 100 PPU, Bilinear, Compressed; cần thay đúng phạm vi boss C.
- Đã đọc dữ liệu alpha gốc của Idle: 56.244 pixel alpha 0, 3.756 pixel alpha 255, không có alpha trung gian. Các mẫu trong mảng xám của preview có RGBA (83,69,69,0): đây là RGB ở vùng hoàn toàn trong suốt, không phải lớp phủ nhìn thấy trong game. Không xóa pixel hoặc đục alpha; giữ alpha FromInput và kiểm tra render thực tế.
- Unity 6000.3.9f1, URP 17.3.0, Sprite 1.0.0, Test Framework 1.6.0. Không cần thêm package, define hoặc sửa package source. Camera hiện có orthographic + CinemachineBrain; không thay camera/AA/resolution để tránh thay hình ảnh cả màn chơi.
- BlackKnight.prefab gốc có cổng nhiệm vụ QuestBossEncounter và FriendlyConversation: nếu sao chép nguyên sẽ khiến prefab mới không chiến đấu. Prefab mới phải loại bỏ cổng nhiệm vụ và nhánh hội thoại riêng của boss A, không sửa tài nguyên nhiệm vụ gốc.
- SkillDamageManager.cs hiện được dùng trực tiếp bởi 13 prefab gồm Player và các quái. Bổ sung đường xử lý mục tiêu hình học beam phải nhỏ, giữ API/serialization cũ và kiểm thử hồi quy. Không sửa BlackKnightCombat.cs hoặc EnemyBehavior.cs chỉ để phục vụ boss mới.
- Đã xem phụ thuộc gốc và các tài nguyên; trước mỗi sửa tiếp tục kiểm tra phụ thuộc/đối tượng sử dụng ở trạng thái mới nhất.

## Tài nguyên tạo/sửa trong đợt hoàn thiện
- Sửa importer/sprite metadata của chín PNG tại các đường dẫn trong bảng; giữ nguyên PNG và GUID texture.
- Tạo `Assets/Sprites/Black Knight Boss/Animation/BlackKnightBoss.controller`.
- Tạo clip cùng thư mục: `BlackKnightBossIdle.anim`, `BlackKnightBossWalk.anim`, `BlackKnightBossHurt.anim`, `BlackKnightBossDeath.anim`, `BlackKnightBossAttack01.anim`, `BlackKnightBossAttack02.anim`, `BlackKnightBossAttack03.anim`, `BlackKnightBossBroken.anim`, `BlackKnightBossPhaseTransition.anim`.
- Tạo `Assets/Sprites/Black Knight Boss/Effects/Beam/BlackKnightBossBeam.anim` và controller hiệu ứng nếu dùng Animator để chạy sprite; hiệu ứng chỉ có renderer/Animator của chính nó, không pha màu phủ nhân vật.
- Tạo `Assets/Scripts/Enemies/BlackKnightBossCombat.cs` kế thừa EnemyCombat, tự chọn tầm và quản lý mục tiêu/vận chiêu/khóa hướng/ngắt Skill3.
- Tạo `Assets/Scripts/Enemies/BlackKnightBossBeam.cs`: lifetime, hình học tia, hướng khóa, vật cản, danh sách mục tiêu đã xử lý và dọn hiệu ứng.
- Bổ sung API sát thương ở `Assets/Scripts/Combat/SkillDamageManager.cs`: đường nhận tập mục tiêu đã được chọn theo hình học để beam không phải gọi hit hình tròn. Dùng lại pipeline sát thương hiện tại, không sao chép công thức vào script beam.
- Tạo `Assets/Prefabs/Characters/BlackKnightBoss.prefab` và `Assets/Prefabs/Effects/Black Knight Boss/BlackKnightBossBeam.prefab`.
- Tạo `Assets/Combat/Balance/BlackKnightBoss.asset`, dùng EnemyDamageProfile với archetype `black-knight-boss`. Gán profileOverride cho prefab mới để quy tắc black-knight cũ không ghi đè. Tham số ATK 35, STR 3, crit 0 và hệ số 1/1.3/1.5 là khởi điểm lấy từ boss gốc, có thể chỉnh riêng; không sửa bốn profile vùng cũ.
- Kiểm tra EditMode/PlayMode bằng công cụ của dự án và môi trường tạm. Chỉ tạo test asset/asmdef khi dự án có thể tham chiếu Assembly-CSharp đúng cách; không tự tái cấu trúc assembly của toàn bộ game.

# Implementation Steps
1. **Ảnh đã tổ chức (hoàn tất từ đợt trước).** Chín PNG đúng thư mục, byte giống nguồn, GUID riêng. Không sao chép screenshot hoặc tái tạo ảnh. Dependency: không.
2. **Sao lưu/ngăn sửa nhầm và kiểm tra API sprite.** Kiểm tra tồn tại tài nguyên mới, phụ thuộc từng ảnh và SkillDamageManager; ghi nhận hash tệp boss A, nhiệm vụ và cảnh để đối chiếu cuối. Đọc API ISpriteEditorDataProvider của package đang cài, kiểm tra năng lực sửa/tạo SpriteRect, pivot, tên trước thao tác; nếu không hỗ trợ thì dừng thay vì dùng API cũ hoặc bỏ qua guard. Không ghi đè clip/controller/prefab người dùng đã sửa mà chưa so sánh. Dependency: bản kế hoạch mở rộng được duyệt.
3. **Cắt và nhập chín sheet.** Ô 100×100 từ trái sang phải, không offset/spacing; số ô đúng 4/6/8/8/9/13/7/10/13, tổng 78 sprite. Sprite Multiple, 10 PPU, Point, mipmaps tắt, Uncompressed, alpha FromInput + alphaIsTransparency, giữ max texture size đủ 1300px và không có platform override làm đổi kích thước. Dùng SpriteRect/DataProvider + cập nhật name/file ID mapping theo API Unity 6, ổn định ID khi chạy lại. Chọn Full Rect để giữ lưới và tránh tight mesh vô tình cắt chiêu. Pivot nhân vật đặt tại điểm chân cố định tính từ pixel alpha Idle và dùng nhất quán mọi animation; beam có pivot riêng tại chân tia trong cell, không ở góc dưới của toàn bộ vùng trống. Không rescale ảnh hoặc đổi root scale để làm sai 10 PPU. Dependency: bước 2.
4. **Tạo clip/controller nhân vật.** Dùng nhịp PixelAnimationSettings.FPS hiện có (mặc định 12); kiểm tra giá trị asset trước khi tạo. Idle/Walk lặp, Hurt/Death/Attack không lặp. Binding sprite lên renderer root; không có đường cong tint/material/shader overlay. State mặc định BlackKnightBossIdle; IsWalking điều khiển Walk; trigger/state NormalAttack, Skill2, Skill3, TakeDamage và Die khớp EnemyCombat/CharacterHealth. Broken và PhaseTransition có motion và tên đúng theo EnemyBreak/BossPhaseController. Không dùng Animator.speed toàn cục để treo vận chiêu. Khóa hành động đầu clip, mở cuối clip; Death gọi DestroyCharacter cuối động tác. Chọn frame ExecuteHit của Attack01 và Attack02 sau khi xem pose từng ô, không sao chép frame event A vào C. Attack03 dùng bản No beam effects; sự kiện phát beam tại chỉ số 6 (frame thứ 7), tương ứng frame đầu tia xuất hiện ở bản Attack03 có beam, sau khi xác minh visually. Không đặt ExecuteHit hình tròn cho Skill3. Các event nằm trong độ dài clip, không lặp phát hai beam. Dependency: bước 3.
5. **Bổ sung sát thương theo tập mục tiêu.** Giữ ExecuteHit(string), ExecuteHitAtPosition(string,Vector2) và trường serialize hiện có. Thêm API rõ ràng, ví dụ ExecuteHitOnTargets(string skillName, IReadOnlyList<CharacterHealth> targets), dùng chung pipeline kiểm tra caster chết/break/phase, ATK/STR/crit, TakeDamage, status buildup, hiệu ứng và POS. Chỉ đường mới khử trùng theo CharacterHealth; bảo toàn hành vi hit hình tròn cũ. Mục tiêu beam lấy CharacterHealth từ collider/parent, lọc layer và caster. API không tự gọi tìm mục tiêu diện rộng. Thêm kiểm tra null, mục tiêu chết/untargetable; không tạo sát thương qua tường. Dependency: bước 2.
6. **Implement logic boss và beam.** BlackKnightBossCombat thay BlackKnightCombat chỉ trên prefab mới. Override CanSelectSkill/IsInAttackRange/ShouldLunge/OnAttackStarted phù hợp cận chiến và tia; tiếp tục dùng MarkSkillUsed và director để cooldown/chi phí không tính hai lần. Chọn tầm cận chiến từ collider/range đã chỉnh theo 10 PPU; beam có tầm cấu hình. Trong vận Skill3 lưu target, trước lúc phát cập nhật hướng nếu hợp lệ; lúc phát normalize/chốt hướng, tạo đúng một beam rồi giữ hướng. Nhân vật chỉ lật trái/phải, không xoay toàn bộ thân theo tia. Sprite beam gốc hướng lên nên bù trục gốc khi quay sang atan2 của hướng bắn; độc lập với localScale âm của caster. Cast tới vật cản đầu tiên, giới hạn chiều dài tia/hitbox theo khoảng trống; không stretch tự động toàn bộ cell trống, dùng vùng alpha/visual child để crop hình học hợp lý. Dùng vùng oriented capsule/box theo trục tia để lấy mục tiêu và API bước 5; mỗi actor một lần/cast, không gây hit theo từng collider hoặc Update. Tầm/lifetime/width/spawnOffset/cooldown có Inspector và clamp. Khi thiếu target/prefab/frames hoặc hướng bằng 0: hủy an toàn, không phát sát thương mặc định. CancelAttack/OnDisable/death/break/phase/thoát state phải dọn beam, reset cờ chuẩn bị và không để khóa hành động treo. Dependency: bước 4 và 5.
7. **Lắp prefab riêng hoàn chỉnh.** Sao chép cấu hình phù hợp từ boss A sang prefab độc lập, không chỉnh prefab A. Giữ Rigidbody2D Dynamic, gravityScale 0, FreezeRotation, material frictionless và layer/tag đúng. Root scale magnitude 1 để phù hợp logic AI hiện có; capsule collider theo thân/feet, không theo cell 10×10 hoặc vũ khí/beam; kiểm tra offset bằng sprite alpha và frame đứng. Giữ EnemyAwareness/Behavior, SkillDamageManager, CharacterStats/Health, AnimationEventHandler, EnemyClassification(Boss), EnemyBreak, audio, phase/director, WaterPresence và EnemyAreaDamage. Gỡ BlackKnightCombat và QuestBossEncounter/FriendlyConversation riêng khỏi bản mới; tránh RequireComponent sinh EnemyCombat thứ hai. DisplayName Black Knight Boss, questBossId black-knight-boss, persistentId rỗng cho trận mới, không sao chép ID/save/quest black-knight. Gán controller, sprite Idle, material hỗ trợ alpha như hiện có, renderer Color.white. Gán AttackPoint, effect prefab và ba SkillData; tầm hit cận chiến/cooldown dựa trên động tác C và thông số gốc 1.7/4/6 giây để thử trước. Phase 1 hai chiêu; phase 2 ba chiêu; combo cận chiến có recovery, không dùng leap/thrust movement của A. Gán profile balance riêng, chỉnh idleState/brokenState/phaseState đúng tên; bind mọi reference, kiểm tra chỉ một EnemyCombat và không có Missing Script. Dependency: bước 6.
8. **Kiểm thử và bàn giao không đổi cảnh chơi.** Dùng prefab mới trong môi trường PlayMode tạm với player/vật cản để xác minh toàn chuỗi animation/logic; khôi phục scene đang mở và không lưu actor thử vào WastelandScene. Chạy kiểm thử import/state/events, beam 360°, khóa hướng/né/tường, sát thương và interrupt; hồi quy đường sát thương cũ. So sánh hash PNG, prefab A, nhiệm vụ và scene; báo thông số/đường dẫn và các điểm chưa kiểm tra được. Không tuyên bố boss đã đặt trong màn chơi vì người dùng chọn prefab riêng. Dependency: bước 7.

# Verification & Testing
## Import và hình ảnh
- Đủ 9 PNG, 78 SpriteRect, từng rect đúng 100×100, PPU=10; thứ tự trái→phải theo số chứ không sort tên lexicographic.
- PNG byte/hash không đổi, alpha không đổi; RGB ở alpha 0 không được làm thành vùng opaque. Kiểm tra game render trên nền sáng và tối để xác nhận không có mảng xám/đỏ phủ ngoài pixel thật.
- Point/no mipmap/no compression, pivot nhất quán, mesh không làm hụt vũ khí; đường cong animation không đổi màu/material.
- Collider trùng thân và chân, không phình 10×10 chỉ vì cell; không giật chân qua Idle/Walk/Attack.

## Animation và kết nối
- Mọi state, parameter và event có receiver thật; CharacterHealth.Play("TakeDamage"), Die trigger, EnemyBreak/BossPhaseController idleState đều hợp lệ.
- Idle/Walk loop; Attack/Hurt/Death không loop. Death hoàn tất hình ảnh trước cleanup; không UnlockAction sau chết.
- Attack01/02 chỉ hit ở impact frame đã kiểm tra; Skill3 không hit cận chiến vô tình, phát đúng một beam ở pose phát và effect chỉ sống đúng lifetime.
- Ba chiêu thực sự được chọn, cooldown/chi phí chỉ áp dụng một lần; phase 1 không beam, phase 2 có beam; thoát phase không kẹt animator/action lock.

## Beam và sát thương
- Player đứng trái/phải/trên/dưới/chéo: tia đi đúng hướng; caster flipX/localScale âm không đảo nhầm tia.
- Di chuyển trước release: hướng ngắm được cập nhật; di chuyển sau release: tia không đuổi theo. Có cơ hội né sau khi đọc vận chiêu.
- Target ngoài chiều rộng nhưng trong bán kính lớn không trúng; target phía sau caster không trúng; target bên kia tường không trúng. Tường giữa chừng làm hình ảnh và hitbox dừng cùng chỗ.
- Target có nhiều collider vẫn nhận một hit/cast; không tăng damage theo FPS. Không lặp spawn khi event chạy lại, không xử lý stale target.
- I-frame, parry, DEF, stamina damage, status buildup và crit tiếp tục theo CharacterHealth/SkillData; đường sát thương cũ ở Player, Orc và BlackKnight A không thay kết quả.
- Boss chết/break/chuyển phase/mất target/disable/thoát Skill3: effect dọn, không phát hit muộn, hành động được reset an toàn.
- Pause timeScale=0 không tiến frame/lifetime hoặc phát hit; resume đúng clock gameplay.

## An toàn dự án
- Không Missing Script, lỗi compile hoặc warning thiếu event/parameter mới. Lỗi UIElements nhập văn bản đã thấy trước đó được phân biệt với lỗi tính năng mới, không tự sửa Unity/package.
- Prefab mới không bị quest gate Friendly cũ khiến đứng yên; ID mới không trùng encounter cũ.
- WastelandScene/MainMenu, prefab BlackKnight A, tài nguyên nhiệm vụ và profile vùng cũ không đổi. Không thay camera, render pipeline, quality hoặc input settings.
- Bàn giao BlackKnightBoss.prefab và BlackKnightBossBeam.prefab cùng controller/clip/Inspector; ghi rõ phạm vi và kết quả test thực tế.

# Kết quả thực hiện
- Đã tạo prefab Regular độc lập tại `Assets/Prefabs/Characters/BlackKnightBoss.prefab`, effect prefab, profile riêng, 10 clip và 2 controller. Không có instance BlackKnightBoss mới trong cảnh đang mở.
- Đã nhập 78 sprite từ 9 PNG, ô 100×100 / 10 PPU / Point / Uncompressed / alpha FromInput. PNG nguồn không thay đổi; mảng RGB tại alpha 0 không xuất hiện như lớp phủ khi render.
- Kiểm thử runtime đã đạt 16 tình huống đòn/beam và 5 tình huống vòng đời bổ sung: 8 hướng, event phát tia, không sinh tia trùng, một lần/actor dù nhiều collider, bị tường chặn, né và khóa hướng, Hurt ngắt beam, đòn cận chiến, phase gating, pipeline cũ/mới 28/28 damage, i-frame và parry; thêm pause/resume, disable mở khóa, break, lifetime và death cleanup. Kết quả lần cuối vòng đời success=true / failures=0.
- Beam thử với ATK35/STR3/DEF mục tiêu10 gây 45.5 damage/cast. Collider theo thân 1.8×2.6, offset (0,1.3). Skill3 cooldown6 giây, tia dài8/rộng1, sống7/12 giây, cửa sổ damage3/12 giây; các giá trị còn có thể cân chỉnh.
- Đã nhìn ảnh runtime trên nền tối/sáng để kiểm tra alpha và hướng tia. Môi trường render kiểm thử dùng material Unlit tạm trên instance ngoài bản đồ và camera riêng; prefab thật vẫn dùng Sprite Lit mặc định của dự án. Không coi đây là xác minh ánh sáng ở mọi điểm trong bản đồ.
- Phase 2 đã được kiểm tra ở instance có thời lượng chuyển phase 0.3 giây để kiểm tra chu trình; prefab vẫn giữ cấu hình 5 giây. Chưa kiểm thử nguyên trận với AI tuần tra/truy đuổi và cân bằng cuối, hoặc toàn bộ tương tác quest/save cho boss mới.
- Các hash bảo vệ của PNG, boss A, MainMenu, nhiệm vụ và profile vùng không đổi. **Ngoại lệ:** `Assets/Scenes/WastelandScene.unity` đã khác hash ban đầu sau thao tác lưu scene. Không thêm/thay boss hoặc sửa scene objects trong quy trình dựng prefab; không có bản nội dung trước-save để khẳng định mọi khác biệt chỉ là serialization. Không tự ghi đè/khôi phục scene để tránh mất công việc của người dùng; cần báo rõ trong bàn giao.
- Đã sửa vấn đề action lock khi vô hiệu hóa component chiến đấu và xác minh lại bằng runtime test. Editor kết thúc ngoài Play Mode, không có lỗi compile mới.

---
# Giai đoạn tiếp theo: CaveScene cuối game

# Project Overview
- Game Title: NONAME, theo MainMenuController hiện có.
- High-Level Concept: Kiểm chứng và hoàn thiện luồng WastelandScene → CaveScene → hồi sinh/quay lại, rồi mở rộng bản đồ hang hiện có thành dungeon cuối gồm ba khu vực, nhánh thưởng và đấu trường BlackKnightBoss. Thắng boss mở điểm tương tác kết thúc hành trình, có màn hoàn thành và khả năng tiếp tục khám phá.
- Players: Một người chơi; giữ kiến trúc Player hiện có, không thêm multiplayer.
- Inspiration / Reference Games: Dungeon dạng Souls-like với nhịp khám phá, chiến đấu, checkpoint và lối tắt; không sao chép bản đồ của game cụ thể.
- Tone / Art Direction: Hang/di tích tối, nền đá DarkCastle, hồ dung nham và đuốc đã có trong dự án; giữ pixel art 16×16 / 16 PPU cho tilemap. Không tạo ảnh AI hoặc nhập gói mới.
- Target Platform: Windows 64-bit, Unity 6000.3.9f1 / URP 17.3.0.
- Screen Orientation / Resolution: PC ngang; giữ camera và cấu hình độ phân giải hiện có. UI dùng Canvas Scaler như các menu hiện tại.
- Render Pipeline: URP, Tilemap và Sprite Lit hiện có; không đổi pipeline, package, global defines hoặc sửa package source.

## Quyết định đã được người dùng chọn
1. **Mở rộng bản đồ hiện có**, không xóa/vẽ lại toàn bộ CaveScene.
2. **Boss cuối và kết thúc**, dùng BlackKnightBoss đã tạo làm boss cuối riêng của hang.
3. Giữ nguyên dạng chiêu hiện tại của boss: đạn bay theo hướng khóa, hình beam luôn đứng thẳng. Không áp dụng lại mô tả tia đứng yên ở giai đoạn lịch sử phía trên.

# Game Mechanics
## Core Gameplay Loop
- Từ Wasteland vào cửa hang, khám phá khu ngoài → khu hồ/di tích → đấu trường, mở checkpoint và nhánh thưởng, đánh elite/quái thường rồi đối đầu boss cuối.
- Đường chính luôn liên thông; nhánh vòng có phần thưởng nhỏ và lối trở lại, tránh ngõ cụt buộc chạy lại cả bản đồ. Theo MDA, địa hình tạo nhịp căng–nghỉ; theo SDT, nhánh thưởng cho người chơi lựa chọn nhưng không khóa tiến trình chính.
- Ba vùng đề xuất: `cave-entrance` / Cửa hang cổ; `cave-depths` / Hầm dung nham; `cave-final` / Điện hắc kỵ sĩ. Dùng AreaDefinition/AreaTrigger hiện có cho thông báo tên, phân vùng và profile sát thương.
- Checkpoint tại khu ngoài và trước đấu trường. ID cố định: `cave-entrance-shrine`, `cave-final-shrine`; respawn ở nền an toàn, không nằm trong cổng/nhóm quái/hồ.
- Quái thường dùng Skeleton/ArmorSkeleton sẵn có; elite dùng GreatswordSkeleton. Không tự tạo Minotaur/Demon/Flame Golem khi mới chỉ có folder trống.
- Boss là instance riêng từ BlackKnightBoss.prefab, có encounter ID ổn định `cave-final-black-knight`, không trùng boss đã đặt ở Wasteland và không thay prefab gốc. Arena giữ khoảng trống để né đạn đứng, không rải props che toàn bộ tầm nhìn.
- Khi đúng boss cuối chết, mở điểm kết thúc phía sau arena. Tương tác E mới hiện màn hoàn thành, để người chơi nhận thưởng/nhìn animation chết trước. Tải lại cảnh có boss đã bị đánh bại vẫn mở điểm kết thúc; không cần boss còn tồn tại trong hierarchy.
- Lưu hoàn thành cục bộ theo PlayerPrefs riêng; xem lại kết thúc không trao thưởng lặp. `Bắt đầu mới` trong MainMenu xóa cờ hoàn thành cùng save hiện tại, vẫn giữ xác nhận/backup của menu.
- Cổng trở lại Wasteland vẫn hoạt động. Không tự bổ sung khóa nhiệm vụ/lvl cho cửa hang vì người dùng chưa chỉ định điều kiện mở cửa; endgame được thể hiện bằng độ khó, vị trí đấu trường và mục tiêu kết thúc.
## Controls and Input Methods
- Project Settings dùng Both; gameplay hiện có dùng Input System 1.18.0 và GameControls. Không đổi backend hoặc tự viết điều khiển legacy.
- Tương tác điểm kết thúc qua WorldInteractable.TryInteract(PlayerInteractor), dùng action Interact đã có; label sử dụng binding hiện tại, không hardcode E nếu người dùng đã đổi phím.
- Các nút màn hoàn thành dùng EventSystem/InputSystemUIInputModule; khóa điều khiển gameplay khi màn đang mở, phục hồi đúng trạng thái khi đóng. Không thay controller Player toàn bộ chỉ để chặn menu.

# UI
## Sơ đồ bản đồ đề xuất (không phải minimap mới)
```text
                     [Điểm kết thúc]
                            |
             [Đấu trường BlackKnightBoss]
                  x≈-13..13, y≈50..70
                            |
                [Checkpoint trước boss]
                  [Hành lang rộng 4–5 ô]
                            |
 [Nhánh thưởng/elite] — [Hầm dung nham] — [Lối vòng quay lại]
                   x≈-24..23, y≈25..43
                            |
                  [Cầu/hành lang nối]
                            |
         [Cửa hang cổ + checkpoint + quái nhỏ]
                   x≈-14..14, y≈4..21
                            |
                    [Spawn_CaveEntrance]
                            |
             [CaveExit_Trigger → Wasteland]
```
- Các tọa độ là vùng đích thiết kế dựa trên tilemap đang có, không phải lệnh xóa hình dạng cũ. Điều chỉnh mép phòng/cầu để giữ art và vật cản hiện có; chỉ xóa tile chặn trong phạm vi hành lang/arena đã xác định.
- HUD HP/Stamina và HUD boss giữ nguyên. AreaNotice hiện có hiển thị ba tên khu vực.
## Màn hoàn thành (uGUI)
```text
┌──────────────────────────────────────────┐
│          HÀNH TRÌNH HOÀN THÀNH            │
│       Điện hắc kỵ sĩ đã được giải thoát    │
│                                          │
│       [Tiếp tục khám phá]                 │
│       [Về menu chính]                    │
└──────────────────────────────────────────┘
```
- Dùng font GameFont, Text/uGUI và màu vàng/đen như MainMenu hiện có; chữ dễ đọc, nút được chọn mặc định, hỗ trợ chuột/keyboard.
- Về menu qua ScreenFader; đóng màn hoàn thành không tự hồi sinh boss hoặc xóa checkpoint. Không thêm màn cảnh riêng chỉ để hiện thông báo kết thúc.

# Key Asset & Context
## Những gì đã kiểm tra trước khi sửa
- MainMenu, WastelandScene và CaveScene đều enabled trong EditorBuildSettings.
- Wasteland có `CaveEntrance_Trigger`: target CaveScene, destinationSpawnPoint đang trống, vị trí (9.99,17.58), trigger size (3,1.5). Đang dùng vị trí Player mặc định ở Cave, chưa có arrival marker rõ ràng.
- Cave có Player ở (0,0), một camera bám đúng Player, Fader và hai HUD. HUD target null là chế độ autoFindPlayer, không phải tự kết luận hỏng binding.
- `CaveExit_Trigger` ở (0,-3.5), trigger size (3,1), target WastelandScene và spawn ID Wasteland_CaveEntrance. Wasteland có marker đúng ID ở (9.99,15.93), ngoài cổng khoảng 1.65 đơn vị.
- Cave có 9 tilemap: Ground, GroundDetails, Water, Map_Boundaries, Obstacles, Walls_Cliffs, Overhead, Trees_Roots, Trees_Canopy. Đang có art thật, không phải scene trống. Water có collider non-trigger, không có water slow/damage component; không tự đổi mọi hồ thành vùng sát thương.
- BFS bảo thủ trên tile có collider từ spawn đi được khoảng 505 ô, tới (0,10), nhưng không tới phòng trên tại (0,55). Đây là dấu hiệu phòng chưa nối; cần kiểm chứng bằng collider thật, không xem BFS tile thô là kết luận runtime cuối cùng.
- Chưa có AreaTrigger, SceneSpawnPoint, checkpoint, quái hoặc logic kết thúc trong CaveScene ở lần kiểm tra mới nhất. Chưa chạy luồng chuyển cảnh PlayMode nên chưa được tuyên bố luồng đã hoạt động hoàn toàn đúng.
- SceneTransitionTrigger hiện chưa kiểm tra player chết hoặc transition đang diễn ra ở Fader, nên có thể tạo pending spawn sai khi hai cửa chồng nhau. SceneSpawnPoint chưa thông báo camera warp; CheckpointService có warp camera riêng.
- CharacterStats.Awake hồi đầy HP/Stamina trên mỗi instance mới; PlayerProgression lấy nâng cấp từ GameEconomy. Không tự đổi quy tắc hồi đầy khi chuyển cảnh; kiểm tra và báo rõ, chỉ sửa thất thoát dữ liệu bền vững nếu tìm ra lỗi.

## Tệp sẽ sửa/tạo
- Sửa `Assets/Scenes/CaveScene.unity`: tile trong phạm vi mở rộng, Areas/Spawn/Checkpoints/Enemies/Finale, kết nối giữ Player/Camera/Fader/HUD hiện có.
- Sửa `Assets/Scenes/WastelandScene.unity` chỉ để đặt destinationSpawnPoint `Cave_Entrance` và kiểm tra cửa/marker; không vẽ lại Wasteland hoặc thay quái/nhiệm vụ đang đặt.
- Sửa nhỏ `Assets/Scripts/World/SceneTransitionTrigger.cs` cho guard chuyển cảnh; `SceneSpawnPoint.cs` cho thông báo camera warp sau đặt player, nếu kiểm thử xác nhận cần.
- Tạo marker `Spawn_CaveEntrance` với ID `Cave_Entrance`, đặt ngoài trigger thoát và trên sàn đi được.
- Tạo `Assets/WorldAreas/CaveEntrance.asset`, `CaveDepths.asset`, `CaveFinal.asset` và polygon AreaTrigger tương ứng.
- Tạo `Assets/Combat/Balance/05_CaveEndgame.asset` từ profile Deadlands hiện có để chỉnh sát thương chỉ trong Cave, không sửa các profile vùng cũ. Nếu cần boss mạnh hơn, tạo profile override riêng cho encounter Cave; không đổi profile BlackKnightBoss dùng ở Wasteland.
- Dùng prefab `Assets/Prefabs/World/CheckpointStatue.prefab`, Skeleton/ArmorSkeleton/GreatswordSkeleton, BlackKnightBoss và DarkCastle tile/animated torch/crystal/pool hiện có. Chỉ tạo tile phụ nếu thật sự thiếu, không reslice PNG gốc/palette.
- Tạo `Assets/Scripts/World/CaveFinale.cs` cho boss gate và điểm tương tác; `Assets/Scripts/World/GameCompletionState.cs` cho save hoàn thành; `Assets/Scripts/UI/EndingScreen.cs` cho uGUI.
- Sửa `Assets/Scripts/UI/MainMenuController.cs` để thêm save key hoàn thành vào backup/reset; không thay luồng menu hoặc xóa các save khác ngoài yêu cầu.
- Có thể tạo `Assets/Editor/CaveEndgameSetup.cs` làm builder có phạm vi rõ để sửa lặp tối đa 3 lần sau capture; không chạy asset postprocessor tự vẽ lại scene khi mở Unity.

# Implementation Steps
1. **Backup và baseline transition.** Dependency: kế hoạch được duyệt. Lưu bản nội dung/hash của hai scene và scripts sẽ sửa trước mọi thao tác; bảo toàn dirty state và không ghi đè scene đang chỉnh chưa lưu. Kiểm tra GetDependency cho từng asset, ghi ID các encounter/checkpoint Wasteland trước sửa. Chạy PlayMode baseline Wasteland cửa vào → Cave spawn → cửa ra → Wasteland marker, ghi scene/position/collider overlap/player-camera-Fader-HUD counts. Test dùng backup/restore các PlayerPrefs save liên quan, không để thử boss/death làm mất save người dùng.
2. **Hoàn thiện cổng/arrival marker.** Dependency: bước 1. Thêm SceneSpawnPoint `Cave_Entrance` gần vị trí spawn mặc định (0,0), đảm bảo cách trigger thoát và tường đủ cho collider Player. Gán Wasteland destinationSpawnPoint đúng ID. Guard transition với player sống/active và Fader không busy trước Prepare; kiểm tra target trong build, không tiêu pending spawn khi request bị từ chối. Giữ public/serialized API cũ. Nếu warp camera cần sửa, dùng cùng OnTargetObjectWarped như CheckpointService và không tạo camera thứ hai. Kiểm thử lại cổng để xác nhận không bật ngược tức thì hoặc kẹt màn đen.
3. **Mở đường và hoàn thiện art của Cave.** Dependency: bước 2. Giữ phần sàn/hồ/trang trí đã vẽ. Mở hành lang 4–5 ô qua các dải y≈21..26 và y≈43..50, bỏ tile chặn của Map_Boundaries/Walls_Cliffs/Obstacles/Water chỉ trong hành lang và bù Ground/bridge art. Mở rộng phòng trên thành arena x≈-13..13/y≈50..70, duy trì viền tường và cửa vào rõ. Thêm nhánh thưởng ngắn và một lối vòng trong phòng giữa, không làm all-clear tilemap. Vẽ mép tường/cầu/đuốc/crystal bằng assets DarkCastle đang dùng; hồ non-walkable được bảo toàn, không phát sinh hazard damage mới tùy tiện. Refresh/compress tilemap bounds và collider sau chỉnh. Kiểm tra đường chính có khoảng hở vật lý, không chỉ có sàn nhìn thấy.
4. **Areas, checkpoints và quái.** Dependency: bước 3. Tạo ba AreaDefinition/AreaTrigger có ID duy nhất, polygon bám phòng/lối nối, priority không gây nhảy tên khu liên tục. Gán profile Cave riêng. Đặt 2 checkpoint manual ID ổn định với respawnPoint trên nền an toàn. Đặt nhóm Skeleton/ArmorSkeleton theo nhịp, một GreatswordSkeleton tại nhánh thưởng; tránh spawn chồng tường, hồ, shrine hoặc cửa. Dùng tên/encounter ID Cave riêng cho elite, đảm bảo không bị save defeated từ Wasteland ẩn nhầm. Mức khó tăng bằng bố trí/nhịp chiến đấu trước khi tăng damage; không nhân hệ số vô hạn hoặc để đường chính thành kill corridor.
5. **Đấu trường và boss cuối.** Dependency: bước 4. Đặt instance BlackKnightBoss trong arena, persistentId `cave-final-black-knight`, preservePersistentId=true; giữ hai phase và hình beam đứng/đạn đạo hiện tại. Gán profile override riêng chỉ khi cần cân bằng. Boss chase/home bounds nằm trong arena, không lao qua cổng hoặc đuổi tới checkpoint. Props chỉ ở viền, khoảng giữa đủ né đạn và không che Player bằng Overhead. Boss không phụ thuộc QuestBossEncounter của BlackKnight A. Kiểm tra saved defeat làm encounter đúng boss này không respawn; không sửa ID boss Wasteland.
6. **Completion state và màn kết thúc.** Dependency: bước 5. GameCompletionState có key versioned mới, lưu completion chỉ một lần. CaveFinale bám đúng boss assigned/ID, subscribe/unsubscribe CharacterHealth.AnyDied; khi load kiểm EnemyDefeatState để mở điểm kết thúc dù boss bị disable. Điểm kết thúc kế thừa WorldInteractable, Interact chỉ thành công khi boss đã chết/defeated, Player còn sống và không có menu/cutscene/transition đang chặn. Tương tác ghi completion rồi mở EndingScreen; thưởng không lặp. UI dùng Canvas/GraphicRaycaster/CanvasScaler/Text/GameFont + EventSystem InputSystemUIInputModule, nút tiếp tục và về MainMenu qua Fader. Lưu/khôi phục đúng controls, cursor, timescale và focus khi đóng; không để màn kết thúc trở thành nguồn soft-lock. Thêm completion key vào reset/backup MainMenu, không auto hiện lại màn kết thúc mỗi lần Continue.
7. **Xác minh ảnh và gameplay tích hợp.** Dependency: bước 6. Capture2DScene toàn bản đồ và các phòng; xem ảnh mới sau mỗi sửa, tối đa 3 vòng capture–fix và chỉnh builder cũ thay vì dựng lại từ đầu. Kiểm tra tile mép, props nổi/chìm, arena thoáng, lối đi không bị Overhead che, quy mô so với Player/boss. Chụp Game View khi đang Play để kiểm tra HUD/area/banner/boss/ending thực sự nhìn thấy. Không đánh giá thành công chỉ từ Renderer.isVisible.
8. **Kiểm thử full flow và bàn giao.** Dependency: bước 7. Chạy MainMenu/new game/continue → Wasteland → Cave → shrine → death/respawn → trở lại Wasteland → vào Cave lại → boss defeated → tương tác kết thúc → tiếp tục khám phá/menu → tải lại. Kiểm thử cả nhánh main quest/currency/upgrades giữ nguyên; khôi phục mọi test save, xóa bootstrap và screenshot tạm. So sánh backup để Wasteland chỉ có thay đổi cửa mong muốn, Cave chỉ có thay đổi đã thiết kế, không mất ID cũ. Lưu scene sau khi hoàn tất có chủ đích và báo các giới hạn chưa kiểm thử; không tuyên bố đã hoàn thành nếu bootstrap thất bại hoặc save restore chưa xong.

# Verification & Testing
- **Transition:** cả hai scene có trong build; đúng scene/arrival ID; cổng vào/ra không overlap spawn; collider Player không chạm tường/hồ khi xuất hiện; mỗi lượt đúng 1 Player/Camera/Fader/HUD; timescale/fade/camera follow không kẹt. Chạm cửa khi player chết hoặc Fader busy phải không tạo request/pending spawn mới.
- **Map:** BFS mới liên thông từ spawn tới hai shrine, nhánh thưởng, arena và altar; xác minh thêm collider footprint Player bằng runtime đi/physics probe. Không xóa tile ngoài phạm vi edit, không có hố sàn ở cầu, không có water solid vô hình trên đường chính. Đi về từ arena tới exit được sau boss.
- **Area:** tên ba vùng đúng, không nhấp nháy qua overlap; checkpoint menu có tên vùng phù hợp; damage profile lấy vùng Cave chứ không lẫn Wasteland. Chạy lại builder không sinh duplicate IDs/objects.
- **Checkpoint/save:** lưu shrine, chết hồi đúng vị trí và camera; Continue vào đúng cảnh; di chuyển xuyên cảnh không mất gold/exp/upgrades/quest/items; báo rõ HP/Stamina hiện hồi đầy theo kiến trúc sẵn có. Fast travel/return shrine không bị pending doorway spawn ghi đè.
- **Boss:** chỉ ID boss Cave kích hoạt ending; quái khác/boss Wasteland chết không mở altar; boss bị defeated và reload vẫn cho hoàn thành; boss còn sống thì không hoàn thành; beam vẫn là đạn bay với hình đứng, không revert.
- **Ending:** chỉ ghi/trao reward một lần; menu keyboard/mouse làm việc; close phục hồi gameplay/focus/timescale; về MainMenu không kẹt Fader; Continue không auto spam ending; confirmed new run xóa completion cùng progress, canceled new run không đổi save.
- **An toàn:** không thay package/settings/pipeline/điều khiển; không ghi save test vào save thật; sau test ngoài PlayMode và bootstrap bị xóa; không lỗi compile/missing script/event mới. Tối đa 3 vòng sửa hình và giới hạn số lượt test thất bại theo quy trình, báo chưa xác minh nếu vượt giới hạn.

