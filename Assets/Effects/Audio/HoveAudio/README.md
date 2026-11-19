# Hove Audio - Sword Combat Free

Active sword/impact source selected by the user from the first recommended free pack.

Source: https://hoveaudio.itch.io/sword-combat-sound-effects-pack-free-version
Creator: Hove Audio
Pack: Sword Combat Sound Effects Pack FREE VERSION.zip, downloaded free through itch.io.

The creator describes this as a royalty-free sword combat pack for game/film use and states in the page comments that credit is not necessary. Source/comment: https://itch.io/post/13522085 . This is not a CC0 claim. Do not redistribute the pack as a standalone sound library.

11 prepared clips: 3 gore/body hits for normal attacks; 3 stab-with-ring impacts for Skill2; BaseMetal_03 for parry; 2 standalone whoosh variations, prepared at two lengths for ordinary/heavy swings. Combo files containing whooshes were not used as impact clips to avoid duplicating the animation-event swing sounds. Voice lines are retained only in the Editor source folder and are not activated.

Original 41 WAVs are preserved under Assets/Effects/Editor/HoveAudio/Source. Preparation removes source lead silence, converts to mono 44.1 kHz PCM, limits duration, adds tiny edge fades and adjusts peaks. Cuts.json records exact source names and excerpt timings. PrepareHoveAudio.py reproduces these files from the downloaded archive. Archive paths containing colons were flattened to Windows-safe leaf filenames.

DefaultCombatAudio now references these clips in Editor and builds. Previous configuration: Assets/Effects/Editor/CombatAudioBeforeHove.asset. Existing buffs/debuffs, dash, healing and skill cast sounds are retained.
