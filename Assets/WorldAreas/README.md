# Editable area names

1. In WastelandScene expand `World Areas (demo)`. Two sample regions are placed near Player's editor position: Vùng hoang địa and Lối mòn cổ (16 units to the right).
2. Select a region, open Polygon Collider 2D → Edit Collider, and drag/add vertices. Keep Is Trigger enabled. You can move/scale the object too.
3. Assign its Area asset; edit areaId, displayName, subtitle and optional entrySound on that asset. Create more through Create → Game → World Area.
4. Drag `Assets/Prefabs/World/AreaTrigger.prefab` into a scene for a new region. Several regions may share one Area asset. Higher priority wins when regions overlap.
5. For rectangular regions, disable the PolygonCollider2D and add a BoxCollider2D with Is Trigger checked. Every enabled collider on this same object contributes to the region.

AreaManager starts automatically and checks player position every 0.1 seconds, including after teleportation. A region must remain selected for 0.25 seconds to announce it. Staying outside all regions for 0.25 seconds clears the current area, so entering it again announces its name. Re-entry has a 3-second cooldown from the last announcement; if necessary, the new announcement waits until that cooldown expires while the player remains inside. Brief boundary crossings do not repeat the title.

The 3.2-second VT323 banner animates without blocking input or changing time scale. The latest area replaces any previous announcement. Menus pause the animation and bell; volume follows the existing menu audio settings. A short original synthesized bell is used when entrySound is empty.
