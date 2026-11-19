# Witchcraft — MakotoHiramatsu

Source: https://makotohiramatsu.itch.io/witchcraft

License: CC BY 4.0 — https://creativecommons.org/licenses/by/4.0/

Original license is included as LICENSE.txt. In-game credit appears on MainMenu; the full notice ships in StreamingAssets/ThirdPartyNotices/Witchcraft.txt.

DefaultGameMusic (Resources) selects:
- Menu: Witchcraft
- Exploration / normal combat / Elite: Hidden Path (loop version)
- Boss combat: Ritual (loop version)

Forest and Forgotten Words are also imported for optional replacement. Files retain the original audio; Unity imports them as stereo Vorbis streaming. Original ZIP and original filenames remain in References/Music/Witchcraft.

GameMusicController automatically starts, crossfades tracks and returns to exploration after disengaging or boss death. Configure volume, crossfade, pause ducking and disengage delay in Assets/Resources/DefaultGameMusic.asset. For a particular boss, add BossMusicOverride to its prefab and assign Combat Music. No scene placement of the music controller is required.

Verified in Play Mode: menu/exploration/boss selection, Elite exclusion, per-boss override, disengagement, boss death, pause volume and streaming import settings. See Library/MusicSetup/test-results.txt.
