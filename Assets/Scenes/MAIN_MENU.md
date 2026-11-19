# Main menu

Open `Assets/Scenes/MainMenu.unity` and press Play to preview. MainMenu is first in Build Settings, so builds launch here. Playing a gameplay scene directly in the Editor still works normally.

Edit `Main Menu → MainMenuController → Game Title` to replace the temporary NONAME title. `First Scene` configures the new-run destination.

- Continue resumes the saved checkpoint, or opens the first scene while retaining progression when no valid checkpoint exists.
- New game asks for confirmation when progress exists. Confirmation backs up the previous slot under `Noname.PreviousRun.v1`, then resets checkpoints, unlocks, currency, upgrades, boss relics and defeated enemies. A fresh run starts with 3/3 potions.
- Settings changes master volume and saves it as `Noname.MasterVolume`.
- W/S or arrows select, E/Enter confirm, Esc closes dialogs; mouse input is supported. Quit exits the build (stops Play in the Editor).

The menu uses shared VT323 and an original procedurally drawn pixel forest. No gameplay runs behind the menu. Scene changes use the existing screen fader and menu sound effects.
