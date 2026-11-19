# Village NPCs

All six prefab roots now include World.NpcStationary and a Static Rigidbody2D (zero gravity, FreezeAll). Collisions cannot push them. Existing colliders, dialogue, shop and child idle animation are retained. Decorative NPCs without colliders keep their previous collision behavior. NpcInteractable also installs NpcStationary at runtime for newly configured dialogue NPCs and older scene-only instances.

Drag a prefab from this folder into a scene. Each prefab contains only Transform, SpriteRenderer and Animator. Idle starts automatically and loops at 6 fps. No movement, combat, collision, dialogue or shop logic is attached.

- NPC_Merchant: hooded merchant with belt pouch.
- NPC_VillagerFarmer: farmer with straw hat.
- NPC_VillagerWoman: villager with blue dress and apron.
- NPC_VillagerElder: elderly villager with gray beard.
- NPC_Wizard: Wizard from Tiny RPG Character Asset Pack 01 v2.0.
- NPC_Priest: Priest from Tiny RPG Character Asset Pack 01 v2.0.

Wizard/Priest use the supplied six-frame 100x100 strips, 25 PPU. Four civilian characters use VillageNPCs_Idle_Pixel25.png: nearest-neighbor sampled from the generated source into 100x100 cells, 20-21 visible pixels tall, 25 PPU. All six NPCs now use the same world-space pixel size. All textures use Point filtering, no mipmaps, no compression. Pivots align with feet; sorting layer Characters.

Animations/controllers: Assets/Animations/NPCs.
Sprite sources and generation prompt: Assets/Sprites/NPCs.
These prefabs have not been placed in WastelandScene.


Civilian Idle updated: fixed first sprite on Visual child; stepped vertical bob of one pixel (0.04 world units), 1-second loop. Root transform stays stationary. Wizard and Priest retain their original Idle strips.

NPC_Wizard now includes World.NpcInteractable. A test instance NPC_Wizard_Test is placed near the starting player in WastelandScene. Approach and press E to talk/advance; Enter/Space also advance, Esc closes. Configure displayName, dialogueLines, dialogueFont and interactionRadius in Inspector. Dialogue leaves world time running, temporarily suspends player actions, blocks the pause menu and restores previously enabled controls on close or disable. Other NPC prefab roles are unchanged.


Dialogue artwork: existing Assets/UI/Checkpoint/CheckpointParchment.asset (FantasyUI atlas), Pixelify menu font and PaperBook selection arrow. Parchment uses 9-slice scaling and dark brown text above the HUD. World AI, effects and damage continue; player movement, attacks, dash, parry, potions and other interactions are unavailable until dialogue closes. Death closes the conversation automatically.

## Wizard shop prototype
After the final dialogue line, press E to open the shop. Left/Right or A/D selects Buy, Sell, or Load; E/Enter/Space confirms. Mouse clicks also work. Esc leaves the shop and closes the entire NPC conversation immediately.

One product: spare health potion. Defaults: buy 25 Gold, sell 10 Gold; NPC_Wizard's NpcShop component exposes prices and itemIcon. Effective sell price cannot exceed buy price. Buy/Sell transfers one unit; Load consumes one spare to restore one missing charge in PlayerHealthPotion, without increasing capacity. Full charges reject Load. Rechargeable charges supplied by prayer are never sellable.

SpareHealthPotions is persisted in GameEconomy.SaveKey alongside Gold, so save/load and the existing reset/backup tool include shop stock automatically. EXP/stat upgrades are not used for shop trades. The reserve count is currently displayed in the shop; this prototype does not introduce a general-purpose inventory system.

Play verification: talk-to-shop; successful buy/sell; reject empty stock and insufficient Gold; reject full charge refill; consume exactly one reserve on refill; reload persistence; unchanged EXP; world pauses in the shop and controls/time restore on leaving. Tests backed up and restored the original economy save.

Shop pause behavior: dialogue keeps the world running; opening the shop captures Time.timeScale and sets it to zero. Exit restores the captured speed exactly once and closes both shop and dialogue. The close-frame pause guard prevents Esc from also opening the settings menu. Verified frozen world time and restoration from a nondefault timeScale of 0.5; Console has no errors.


## Current shop layout and transitions
Sekiro-inspired structure: Purchase/Sell tabs, selectable item list with icon/price/stock on the left, item description and carried/reserve quantities on the right, Gold at top right, and quantity/total-price confirmation before a transaction. The existing Load option is retained as a third tab. Only the existing spare health potion product is available.
Reference: https://www.neoseeker.com/sekiro-shadows-die-twice/walkthrough/Ashina_Outskirts_Part_2

Q/R or Left/Right changes shop mode. E/Enter opens quantity confirmation; Left/Right adjusts quantity; E/Enter confirms. Mouse buttons work as well. Huy/Backspace cancels confirmation without spending. Esc exits the entire shop from either the catalog or confirmation, with a fade.

NpcInteractable openFadeDuration (.2s) fades in dialogue; closeFadeDuration (.22s) fades out the entire conversation/shop. NpcShop backgroundDim (.68) darkens the world; openFadeDuration (.18s) fades in the shop. All transitions use unscaledDeltaTime. World time and player controls stay locked until closing fade finishes. HUD is hidden only during shop and its prior alpha/interactivity/raycast flags are restored. Disable/death cleans up immediately. Original paper artwork and pixel font retained.

Bulk purchase/sale updates Gold and reserve quantity in one save commit; UI quantity limit is 99 and respects affordability/owned stock. Verification covered fade at timeScale zero, control/HUD restore, bulk buy/sell, confirmation/cancel, full refill, save/load and overflow protection; original economy save restored after tests.

Current shop update: spare health potion is no longer sold or bought here; Load/Refill tab removed. Shop lists only eight consumable medicines and has two tabs (Buy/Sell). R healing flask remains unchanged. Validated both catalog pages, buy/sell, removed UI text and rejection of the former refill action.


## Hàng hóa riêng theo NPC
- NPC_Priest: chỉ bán Hương thanh hồn (150 vàng), dùng tại tượng thần → Thanh tẩy để chữa Ăn hồn. Vẫn cần hoàn thành và trả nhiệm vụ Werebear để mở shop.
- NPC_Wizard: bán 8 loại bình thuốc hỗ trợ (giải độc, chữa bỏng, an thần, cường lực, nhanh nhẹn, kháng lực, bền sức, hộ thể).
- NpcShop: bật Use Custom Stock rồi sửa Consumable Stock để chọn hàng; Include Custom Items cho phép thêm vật phẩm từ Item Manager. Để Use Custom Stock tắt giữ danh mục chung.
- Elite và Boss: mỗi encounter tự nhận 1 Hạt sinh mệnh vào túi; hiến tế tại tượng để tăng sức chứa bình máu (từ 3 đến tối đa 10). Hạt chưa nhặt trong save cũ tự chuyển vào túi khi tải game; không nhận trùng khi xử lý lại cùng encounter.
