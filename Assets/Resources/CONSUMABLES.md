# Consumable items

Config: Assets/Resources/DefaultConsumables.asset. Eight item entries with existing FantasyUI bottle sprites, buy/sell prices, duration and strength. Stock and selected C item are stored in GameEconomy.SaveKey; old wallet saves load with zero consumables. The existing reset/backup tool includes this data.

Cures (25 Gold buy / 8 sell): antidote removes Poison only; burn remedy removes Burning only; calming medicine removes Stress only. A cure with no matching active status costs no item. These cures do not grant immunity; remaining in a poison pool can immediately poison again.

Buffs (60 Gold buy / 20 sell), duration 30 seconds:
- Power: +25% damage.
- Haste: +20% movement speed.
- Resistance: +20 percentage points of existing resistance (knockback/effect resistance; not poison immunity).
- Endurance: -25% stamina cost.
- Protection: -20% incoming damage.

Different buff kinds coexist. Same kind refreshes duration and keeps the strongest potency rather than stacking. Existing CharacterBuffs timing/stat integration is reused.

Buy and sell at the Wizard's shop: Up/Down or W/S selects an item, scroll also works; Q/R changes Purchase/Sell/Refill. Nine products including the existing spare health potion, shown in pages of five. Quantity confirmation, dark backdrop, pause/fade/ESC behavior are retained. Refill tab remains for spare health potions only.

Pause menu > Inventory: click an owned item to equip it. In gameplay, [ / ] cycles through owned medicines; C uses the selected medicine (or the medicine slot button). HUD shows the selected bottle, name and quantity. Shared use cooldown is 1 second. Cannot use while dead, action-locked, dashing, paused or talking/shopping; failed use does not consume inventory. No inventory is granted automatically.

Implementation: ConsumableCatalog, PlayerConsumables, InventoryConsumablesView; MedicineQuickSlot binds the existing C slot. CharacterStats attaches PlayerConsumables only for Player-tagged actors.

Verification: all eight effects/magnitudes and icons, targeted cures, inactive cure rejection, nonstacking reapplication, shop catalog pagination and buy/sell quantities, pause gates, C-slot use, inventory selection, overflow rejection, save/reload and compatibility with EXP/spare potions. Original saves restored after tests.

Current shop: only the eight debuff-removal/buff medicines are listed, indexed 0–7. Purchase and Sell are the only tabs. The spare health potion product and Load/Refill tab were removed. The player's R healing flask and checkpoint healing are unchanged. Existing reserve data is retained for save compatibility, not exposed for shop transactions.
