# Expanded enemy audio

Six additional sources applied 2026-11-16; 41 selected runtime clips. Original archives and extracted files remain under `References/Audio/EnemySFX/Expansion20261115`. Credits and license texts ship under `StreamingAssets/ThirdPartyNotices`; in-game credits are available from Main menu > Thiết lập > Thông tin > Âm thanh quái.

- Ghostly Skeleton (iDarklinkZ): hurt/death for Skeleton, Armor Skeleton, Greatsword Skeleton. Commercial/non-commercial game use permitted, credit optional, per source page.
- Medieval Combat SFX Free (heyheythere): weapon swings, armor hits, draws and body falls. CC BY 4.0.
- Werewolf Sound Effects Vol. 1 (Atelier Magicae): attacks, hurt and phase roar. Creator's bundled license permits game use; no redistribution of the raw pack.
- Bear Growls (AntumDeluge / USFWS): Werebear growls. CC0.
- Magic Spells SFX Lite (Willosuna): Necromancer shadow/summon, Werewolf teleport and Black Knight Boss beam. Creator's bundled license permits commercial use and modification, with no raw-pack redistribution or Content ID registration.
- Free Monster Sounds (Creature Sounds): Orc/Orc Rider, Slime, summoned Bat and Necromancer voices. CC BY 4.0.

WAV clips are processed to mono PCM with short fades and moderate peak normalization; source files are untouched. See the runtime manifest for exact source filenames and changes. Bear OGG files are unmodified. Existing CC0 death cues remain where the new pack has no death cue for that creature.

Twelve profiles serve thirteen main character prefabs (Orc/EliteOrc share a profile). Necromancer encounter variants inherit the shared Necromancer profile. BlackKnightBoss uses a separate profile so its beam cue does not replace the leap sound of BlackKnight.

`replaceDefaultWeaponSwing` is enabled only for the Skeleton and Black Knight weapon profiles. SkillDamageManager then skips the shared swing and calls the existing EnemyAudio hook, avoiding two simultaneous swing layers. Creature vocals continue to layer with the shared weapon swing. Animation frames, event markers, skill timings, damage and AI are unchanged.

Adjust clips, skill overrides, volume, pitch and rate limits in `Assets/Effects/Audio/Enemies/Profiles`. The opt-in regression tool is Tools > Enemies > Test Expanded Audio (Ctrl+Shift+F8); it uses temporary actors and restores save/start-scene settings. Signal/playback checks do not constitute a full subjective listening or final mix review.
