# Sword Foley mix

Active replacement for ForgedSteel synthesis: 13 mono 44.1 kHz WAV clips layered from Kenney's CC0 recordings. No Epidemic Sound samples or synthesized steel modes are included.

Sources:
- Kenney Impact Sounds: https://kenney.nl/assets/impact-sounds
- Kenney RPG Audio: https://kenney.nl/assets/rpg-audio

Both original License.txt files and the selected OGGs are preserved in Assets/Effects/Editor/KenneyFoley/Source. License: Creative Commons Zero (CC0), personal and commercial use allowed by the supplied license. Credit: Kenney / www.kenney.nl.

Mixing: impact onset alignment, transient/low-body/metal/slice layering, mild soft saturation, peak headroom and short edge fades. Three variations per hit/swing category; a dedicated parry. MixReport.json records source names, gains, speed and timing. MixSwordFoley.py reproduces the mixes from PCM exported by Unity to Library/DeveloperTools/AudioSources/Decoded. The original imported OGGs can be decoded again if Library is cleared.

DefaultCombatAudio uses these clips for Editor and builds; all other audio categories are retained. The previous configuration is saved at Assets/Effects/Editor/CombatAudioBeforeSwordFoley.asset. Subjective quality requires listening in game; automated playback checks only verify routing and asset integrity.
