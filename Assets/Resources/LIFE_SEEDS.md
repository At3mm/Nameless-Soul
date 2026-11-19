# Hạt sinh mệnh

- Place `Assets/Prefabs/Items/LifeSeedPickup.prefab` in a saved gameplay scene. Set `quantity` (default 1). Scene pickup IDs are assigned automatically, including when duplicating.
- Press E near the pickup. Collected placements stay collected in the existing GameEconomy save; reset/backup uses the existing economy reset tools.
- Inventory: Vật phẩm nhiệm vụ. Cannot equip or consume as a quick item.
- Checkpoint: Hiến tế vật phẩm → E/click. Each sacrifice consumes exactly one seed and permanently adds one potion capacity, immediately supplying one charge. Pray to refill all charges.
- Capacity uses the PlayerHealthPotion prefab capacity plus saved upgrades. The prefab's original capacity remains editable.
- Default player starts with 3/3 potions. Maximum capacity is 10 (seven upgrades); at the cap, sacrificing is blocked without spending seeds.
- Every Elite drops exactly one seed on death. Normal enemies and bosses do not use this reward. Drops are persisted before recording defeat, restored when revisiting the scene, and never respawn after collection.
- Runtime grants/rewards can call `GameEconomy.AddLifeSeeds(quantity)`; unique world pickups use `CollectLifeSeed(id, quantity)` for atomic collection tracking.
- Icon reuses the installed Catalone amulet artwork; see its existing source/license notes.
