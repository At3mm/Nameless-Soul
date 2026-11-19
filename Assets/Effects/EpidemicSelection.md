# Epidemic Sound audition imported

Four source WAVs were downloaded through the signed-in Epidemic Sound account on 2026-11-02. Source recordings and 9 prepared clips are under Assets/Effects/Editor/EpidemicAudio. Original recordings are retained; Cuts.json records each excerpt. PrepareEpidemicAudio.py reproduces mono conversion, anti-alias decimation, trimming, short edge fades and peak normalization.

Source tracks:
- Light sword impact: https://www.epidemicsound.com/sound-effects/tracks/a25e6b10-87fa-4588-93b4-ee3a42d8f2a0/
- Heavy sword impact: https://www.epidemicsound.com/sound-effects/tracks/b4cd3334-b5a0-4f2f-9552-30a6fc42a36f/
- Parry: https://www.epidemicsound.com/sound-effects/tracks/5782fadf-fd30-443b-ae8a-c5999ecbb3cd/
- Sword swing: https://www.epidemicsound.com/sound-effects/tracks/e44ae3aa-a541-4a23-8b99-5a3ae0cad43e/

Editor playback uses Editor/EpidemicAudio/PreviewCombatAudio.asset: 3 light impacts, 3 heavy impacts, parry, light swing and heavy swing. Skill2 marks successful damage as a heavy impact; HP invulnerability, parry, zero damage and DOT do not produce a false impact. Other audio categories retain their generated clips. Voices remain bounded and follow the master volume.

The account UI displayed Creator. Epidemic Sound's current SFX FAQ explicitly says Creator/Pro/Pro Plus do not cover use inside a game or app: https://www.epidemicsound.com/sound-effects/ . Game/app coverage is listed for Business Plus and Enterprise. Downloads are not evidence of game-distribution rights. These files and the audition settings are kept under Editor and are not referenced by the normal Resources/DefaultCombatAudio.asset. Builds retain the generated audio. Obtain suitable game-use rights before enabling downloaded media in a distributed game.

Validation: 15 Play Mode checks passed, including clip selection, variation, heavy-damage routing and no impact during HP invulnerability. Console had no errors. Selection used catalog descriptions and transient analysis; no direct listening comparison was available.

Superseded for playback: CombatSFX no longer auto-loads PreviewCombatAudio. DefaultCombatAudio now uses original ForgedSteel synthesis in both Editor and builds. The licensed downloads remain inactive references under Editor.

Current status 2026-11-03: user requested original recordings again. Active excerpts moved with GUIDs preserved to Assets/Effects/Audio/Epidemic/Clips and are assigned to DefaultCombatAudio for Editor/builds. See Audio/Epidemic/README.md for current source and license notes; earlier preview-only entries are historical.
