# Health and menu audio

Health/status source: Hove Audio, Health & Damage System Sound Effects Pack (free download).
https://hoveaudio.itch.io/health-damage-system-sound-effects-pack
Selected original WAV files are retained under `Assets/Effects/Editor/HoveHealth/Source`.
Prepared short mono PCM clips and exact cut information are in this folder. Run `Assets/Effects/Editor/PrepareHealthAudio.py` to reproduce them from the downloaded archive.
These are healing/state notification sounds, not literal potion drinking or fire recordings. The free archive did not include a separate license document; keep the source attribution and consult the author's pack terms for distribution.

Menu source: Kenney RPG Audio, CC0.
https://kenney.nl/assets/rpg-audio
Original OGG files and supplied License.txt are in `Assets/Effects/Audio/Menu`.

Runtime settings: `Assets/Effects/Resources/DefaultCombatAudio.asset` (heal, cleanse, five buffs, three debuffs), and `DefaultMenuAudio.asset` (eight menu cues and master menu gain).
Sword combat assignments are preserved. The preceding combat configuration is backed up at `Assets/Effects/Editor/CombatAudioBeforeHealthAndMenus.asset`.
Menu sources use unscaled cooldowns and ignore listener pause so menus remain audible while gameplay is paused; ordinary master volume still applies. Initial selection and repeated selection do not emit extra navigation sounds.
