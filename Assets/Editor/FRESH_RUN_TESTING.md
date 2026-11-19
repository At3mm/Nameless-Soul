# Test from beginning

Open **Tools → Game Testing → Test From Beginning** and click **Chạy thử từ đầu**.

The tool stops any current Play session, backs up the current progression, opens MainMenu through the Editor's temporary Play Mode start-scene setting, and uses a clean save. If there are unsaved scene changes Unity asks whether to save them before the test.

Stopping Play automatically restores the old checkpoint, unlocked statues, economy (including seeds/potion upgrades), defeated enemies, boss relics, previous-run backup, master volume and original Play Mode start-scene setting. Test progress is discarded.

The separate backup is stored at `Library/DeveloperTools/FreshRunBackup.json`. **Dừng test và khôi phục save** also allows recovery from this file if a test was interrupted.

Scene transitions through ScreenFader now show the loading overlay. Its bar follows AsyncOperation progress (asset preparation to 90%, activation to 100%), with a minimum presentation time of 0.7 seconds. Same-scene checkpoint travel keeps its existing fade.
