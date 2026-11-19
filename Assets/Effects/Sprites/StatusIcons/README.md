Debuff icons: Zlacc, [FREE] 25+ Pixel Art Status Icons Pack.
Source: https://zlacc.itch.io/25-free-pixel-art-status-icons-pack
Commercial use and modifications allowed; attribution optional. Do not redistribute raw files as assets.
Buff icons: Softgrid Tiny Pixel Status Icons Pack, https://softgrid.itch.io/tiny-pixel-status-icons-pack . Original license included in Softgrid/LICENSE.txt. Damage=attack_up; movement=boot; resistance=shield; Stamina efficiency=lightning; damage reduction=armor. Assigned in DefaultStatusHUD.
HUD uses gold duration bars for buffs and colored bars for debuffs; dim icons with growing bars indicate pending buildup. Active icons have a rising dark veil as remaining duration expires.
Status buildup configuration
- CharacterDebuffs: threshold 100; recovery starts after 1.5 seconds and removes 12 points/second. RES and resistance buffs reduce incoming buildup.
- PoisonZone: 30 buildup/second by default. Continuous exposure triggers poison at the threshold. Active poison ignores fresh buildup and does not refresh endlessly.
- SkillData: burnBuildup, poisonBuildup, stressBuildup apply only when the hit actually reduces HP. Values default to 0; elemental/status sources can be authored without changing all existing attacks. Parried, invulnerable and missed hits add nothing.
- Apply(Kind) adds default buildup (35); AddBuildup allows a per-source amount. ApplyBurn/ApplyPoison/ApplyStress remain explicit force-apply tools for existing tests and scripted effects.
- Remove and ClearAll remove pending buildup as well as active status. Curative medicines can be consumed against either.
- Stress retains the game's movement/Stamina penalty; it is not Sekiro's lethal Terror.


Update: continuous exposure now holds the active status timer (without resetting its original duration or interrupting DOT). PoisonZone registers/unregisters exposure per actor; leaving all sources resumes countdown.
