# Player HP / Stamina HUD

Prefab: `Assets/Prefabs/UI/PlayerHUD.prefab`.
Behavior: `Assets/Scripts/UI/PlayerVitalsHUD.cs`.

The HUD reads the Player's CharacterStats in LateUpdate. HP uses a red fill and Stamina uses a green fill; numeric values update immediately while fill changes interpolate. Damage, dash/skill costs, running drain and Stamina recovery all use the same live stats, so no gameplay script needs separate HUD calls.

The current layout uses the compact bunny portrait panel from FantasyUI_Atlas, matching the supplied reference. Baked HP/Stamina fills and example currency digits are removed from the derived frame; live red/green fill Images and compact numeric HP/Stamina values are overlaid instead. There is no currency data model yet, so no fabricated currency balance is displayed. Portrait is a separate Image and can be replaced in the Inspector. Derived assets and source coordinates are in `Sprites/PlayerHUD/CompactSkinLayout.json`; original sheets are unchanged. The earlier DragonBar skin remains available as an alternative.

Adjust Health Color, Stamina Color, Decrease Duration and Increase Duration on PlayerVitalsHUD. The target can be assigned explicitly; Auto Find Player binds to CharacterStats on the object tagged Player and rebinds after that object is replaced. The HUD clamps its display to 0..max and handles max=0 without changing the stats themselves.

The prefab is placed in WastelandScene and CaveScene. For another gameplay scene, add one PlayerHUD prefab to its root. It uses Screen Space Overlay at sorting order 50, below the pause menu (500), and does not block mouse input. Vitals anchors it to the bottom-left with a (20,20) margin and a 231x75 reference-pixel size. Scale/offset Vitals to adjust placement; Portrait, CompactFrame, HealthFill/StaminaFill and their Value labels are separate children.

HealthFill and StaminaFill use RoundedVitalsFill (compatible with Image.fillAmount). Both ends stay rounded as values change; the top highlight and bottom shading are generated in the UI mesh. Adjust Corner Radius, Top Highlight and Bottom Brightness on each fill. The native fill origin is x=26 within the compact frame, correcting the old one-pixel offset and leftover baked red edge. The original grayscale texture remains available, but the current fills use the white UI texture and vertex colors.

Resource loss: HealthLossFill (amber) and StaminaLossFill (lime) sit behind the live fills. On loss they flash, hold 0.3 seconds, then catch up over 0.65 normalized seconds. Configure colors, Loss Hold Duration, Loss Catchup Duration, Loss Flash Duration and Loss Flash Strength on PlayerVitalsHUD. Uses unscaled time; immediate refresh and player rebinding reset the trail.

POS: POSOrb is a 42x42 UI graphic built on a 21x21 pixel grid, with stepped glass rim, reflections and blue bottom-up liquid. PlayerVitalsHUD binds currentPOS/maxPOS and animates using the existing increase/decrease durations. POSValue displays current/max. HP/Stamina and loss layers use hard pixel bands; Pixel Size defaults to 3. Canvas Pixel Perfect is enabled.
