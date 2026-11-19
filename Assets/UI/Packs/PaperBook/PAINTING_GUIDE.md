# Ve UI bang PaperBook Tile Palette

Palette da duoc tach thanh `Palettes/Static` va `Palettes/Animated`. Chon PaperBook_AnimatedIcons (18 hieu ung) hoac PaperBook_AnimatedBook (16 hieu ung sach/tab) neu muon ve tile co chuyen dong. Co them 34 clip UI Image trong `Animations`. UIFrameAnimation tu duoc them khi SettingsMenuToggle chuyen AnimatedTile thanh UI, hoat dong ca khi pause va trong Live Preview. Xem `Animations/README.md` de biet chuoi lap lai va chuoi chi phat mot lan.

## Tai nguyen

- Palette: `Assets/UI/Packs/PaperBook/Palettes/Static/PaperBook_Settings.prefab` (200 tile).
- Menu mau: `Assets/Prefabs/UI/SettingsMenu.prefab`.
- Script bat/tat: `Assets/Scripts/UI/SettingsMenuToggle.cs`.
- Menu da duoc dat trong `Assets/Scenes/WastelandScene.unity`.

## Ve bo cuc

Mo `Tools > PaperBook > Live Menu Preview` (hoac nut `Open Live Preview` tren SettingsMenuToggle/PauseBookNavigation). Dat cua so Preview canh Scene. Chon Status/Inventory/Quest/Settings de xem trang can sua; giu Live update bat. Khi ve Tilemap hoac doi Text/Rect Transform tren prefab goc, Preview tu cap nhat, khong can Play va khong can bat SettingsCanvas tren prefab goc. Preview dung mot ban sao rieng; khong thay doi trang thai menu, Time.timeScale hay prefab dang sua. Chi so Status trong preview la du lieu mau.

`View` cho phep xem Page, Open Book, Close Book, Flip Left, Flip Right. Dung thanh Animation de keo tung khung hoac bam Play de xem chuyen dong trong Editor. `Save PNG` luu anh xem thu. `Refresh` cap nhat thu cong khi can. Ve/sua trong Scene va Inspector goc, khong ve truc tiep trong cua so Preview.

1. Mo prefab `SettingsMenu` bang double-click trong Project.
2. Chon `PaintedLayout/Background`, `Decorations` hoac `Icons`. Bam F trong Scene de dua camera toi bo cuc; bat che do 2D.
3. Mo `Window > 2D > Tile Palette`, chon `PaperBook_Settings`.
4. Dat Active Target la dung Tilemap dang chon. Dung Brush de ve, Eraser de xoa. Grid co o 16 pixel; moi tile la mot sprite nguyen ven, nen sach va panel lon se phu nhieu o. Chi can dong mot lan, khong to lap lai nhu gach nen.
5. Save prefab. Chon goc `SettingsMenu`, mo menu ba cham cua component SettingsMenuToggle va chon `Rebuild Painted UI` neu muon cap nhat preview; bat `SettingsCanvas` de xem, sau do tat lai. Khi Play, script tu dung lai hinh UI tu cac tile.

`Background` ve truoc, `Decorations` ve sau, `Icons` ve tren cung. Khong doi pixelsPerUnit=100 hoac Grid cellSize=(0.16,0.16,1) neu khong muon thay doi ty le. Co the sua chu Title/Hint trong SettingsCanvas. Chi sua cac con cua PaintedLayout de ve; cac con cua PaintedUI duoc tao tu dong.

## Esc va scene khac

Trong Play, bam Esc de phat 5 frame mo sach; bam lan nua de phat 5 frame dong sach. Chu va bo cuc tile chi hien khi sach mo xong. `Opening Duration` (mac dinh 0.45 giay) va `Closing Duration` (0.35 giay) chinh tren SettingsMenuToggle. Hieu ung dung thoi gian unscaled, nen van chay khi game tam dung. Co the bam Esc lai giua hieu ung de doi chieu.

`Pause Game` tam dung thoi gian va khoa input di chuyen/dash/tan cong ngay khi bat dau mo; khi sach dong xong se khoi phuc trang thai truoc do. Dung `SettingsMenuToggle.Close()` cho OnClick cua nut dong neu them Button sau nay.

Chu UI dung Pixelify Sans Regular, tai `Assets/UI/Fonts/PixelifySans`, kem giay phep OFL. Cac Text Title/Hint va control moi nen dat trong SettingsCanvas/Content de an trong luc sach chuyen dong. BookAnimation chi dung de phat cac frame mo/dong; PaintedUI van duoc tao tu PaintedLayout nhu truoc.

Trong scene khac, keo `SettingsMenu.prefab` vao Hierarchy mot lan. Giu object goc hoat dong; chi SettingsCanvas an luc khoi dong. Bo cuc tile nam xa map game (10000,10000); script chuyen cac sprite thanh Image tren Canvas Screen Space Overlay nen UI luon o giua man hinh.

Tile Palette chi ve hinh anh. Cac control tuong tac (Button, Slider, Toggle) nam trong SettingsCanvas; viec luu cai dat can duoc them rieng.

## Bon muc trong pause menu

`SettingsMenu` hien co them component `PauseBookNavigation` va 4 tab: Status, Inventory, Quest, Settings. Bam icon hoac dung dieu huong UI de chuyen muc. Trang Status doc CharacterStats cua Player khi duoc mo. Inventory va Quest hien la bo cuc trong, chua ket noi he thong tui do/nhiem vu. Settings co Master Volume dieu khien AudioListener.volume trong phien choi; chua luu tuy chon ra dia.

- `SettingsCanvas/Content/Pages`: sua noi dung tung trang tai day.
- `SettingsCanvas/SectionTabs`: nut bam, icon va ten tab.
- `PageTurnAnimation`: 9 frame lat trai/phai Style1.
- `ContentRevealAnimation`: 36 frame hien noi dung Style1.
- Tabs dung 17 frame Appear/Disappear Withouticons. Script cat vung tab tu sprite goc trong bo nho, khong sua anh nguon. Source Slot 0/2/4/5 tuong ung 4 tab duoc chon.

Chinh `Tabs Duration`, `Tabs Hide Duration`, `Page Turn Duration`, `Content Reveal Duration` tren PauseBookNavigation de can bang nhip hieu ung. Cac hieu ung dung unscaled time. Nut chuyen muc tam khoa trong luc mo sach, tab dang xuat hien, lat trang hoac hien noi dung. Esc van dong duoc sach giua hieu ung va tra lai dieu khien sau khi sach dong xong. Menu ghi nho muc dang chon cho lan mo tiep theo trong cung scene.

## Inventory cuon tu dong

`Content/Pages/InventoryPage/InventoryScrollView` co ScrollRect, Viewport (RectMask2D), Content (GridLayoutGroup) va VerticalScrollbar. Thanh cuon tu an khi noi dung vua khung hoac chua co do; tu hien khi co hon 12 o. Co the lan chuot, keo noi dung hoac keo thanh cuon, ke ca khi game dang pause.

Component `InventoryGridView` tren InventoryPage quan ly so o. `Minimum Slots` mac dinh 12, `Columns` mac dinh 4; `Item Count` mac dinh 0. Chinh Item Count trong Inspector de xem thu qua Live Preview. Khi co he thong tui do, goi `SetItemCount(count)` hoac `SetItems(itemIcons)`; `SetItemIcon(index, sprite)` cap nhat icon cho tung o. Khong can tao lai ScrollRect khi them vat pham. `SlotTemplate` la mau an; noi dung Content duoc tao tu dong, nen sua mau de doi giao dien tat ca o.

He thong nay moi la phan hien thi; chua them logic nhat, su dung hay luu vat pham.

Giao dien thanh cuon dung Holders_22/23/24 lam dau tren, than lap lai va dau duoi. Nut keo dung Buttons_08, duoc sao chep sang Sprites/Scrollbar/ScrollbarHandle.png de cau hinh 9-slice rieng. Anh goc khong bi sua. Co the doi sprite/color tren VerticalScrollbar/TrackTop, TrackMiddle, TrackBottom va SlidingArea/Handle; tranh xoa cac tham chieu ScrollRect/Scrollbar.


