# Boss victory presentation

Configure `DefaultBossVictory.asset`: pixel font, parchment sprite, ink/gold palette, reveal/hold/fade timings, animation frame rate and three sound cues.

Flow: low impact -> stepped ink/banner reveal and stamped Vietnamese title -> held victory screen -> fade -> parchment reward card and chime. The notification does not pause gameplay or capture clicks. Separate encounters queue; the same encounter is presented once per play session. Bosses with item rewards disabled still receive the victory banner.

Preview without awarding an item or changing saves: enter Play Mode, then use `Tools > Diagnostics > Preview Boss Victory (Play Mode)`.

Audio is original procedural work; its generator and source notes are in `Tools/Audio/GenerateBossVictory.ps1` and `Assets/Effects/Audio/BossVictory/SOURCE.md`. Cues respect menu/master volume.
