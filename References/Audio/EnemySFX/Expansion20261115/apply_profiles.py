from pathlib import Path
import re,shutil,uuid,json
B=Path(__file__).parent;R=Path.cwd();P=R/'Assets/Effects/Audio/Enemies/Profiles';A=R/'Assets/Effects/Audio/Enemies'
backup=B/'Before';backup.mkdir(exist_ok=True)
def clip(pack,name):
 f=A/'Expansion'/pack/name;assert f.exists(),f
 guid=re.search(r'^guid: (\w+)',f.with_suffix(f.suffix+'.meta').read_text(),re.M).group(1)
 return '{fileID: 8300000, guid: '+guid+', type: 3}'
def old(path):
 f=A/path;assert f.exists(),f
 return '{fileID: 8300000, guid: '+re.search(r'^guid: (\w+)',f.with_suffix(f.suffix+'.meta').read_text(),re.M).group(1)+', type: 3}'
def C(pack,*names):return [clip(pack,x) for x in names]
def build(name,attacks,hurt,death,phase,skills=None,weapon=False):
 f=P/(name+'.asset')
 original=f.read_text() if f.exists() else (P/'BlackKnight.asset').read_text()
 if f.exists() and not (backup/f.name).exists():shutil.copyfile(f,backup/f.name)
 header=original[:original.index('  attacks:')];header=re.sub(r'  m_Name: .*','  m_Name: '+name,header)
 tail=original[original.index('  volume:'):]
 s=header
 for key,values in [('attacks',attacks),('hurt',hurt),('death',death),('phaseTransition',phase)]:
  s+='  '+key+(':\n'+''.join('  - '+v+'\n' for v in values) if values else ': []\n')
 s+='  replaceDefaultWeaponSwing: '+str(int(weapon))+'\n'
 if skills:
  s+='  skills:\n'
  for key,values in skills.items():s+='  - skillName: '+key+'\n    clips:\n'+''.join('    - '+v+'\n' for v in values)
 else:s+='  skills: []\n'
 f.write_text(s+tail,encoding='utf-8')
 if not f.with_suffix('.asset.meta').exists():f.with_suffix('.asset.meta').write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')

orc=C('ClassicMonsters','Orc_Attack.wav');orcH=C('ClassicMonsters','Orc_Damage.wav');orcD=C('ClassicMonsters','Orc_Death.wav')
build('Orc',orc,orcH,orcD,C('ClassicMonsters','Orc_Emerge.wav'))
build('OrcRider',orc+C('ClassicMonsters','Behemoth_Attack.wav'),orcH,orcD,C('ClassicMonsters','Behemoth_Emerge.wav'),{'Skill3':C('ClassicMonsters','Behemoth_Attack.wav')})
build('Slime',C('ClassicMonsters','Slime_Attack.wav'),C('ClassicMonsters','Slime_Damage.wav'),C('ClassicMonsters','Slime_Death.wav'),C('ClassicMonsters','Slime_Emerge.wav'))
build('NecromancerBat',C('ClassicMonsters','Bat_Attack.wav'),C('ClassicMonsters','Bat_Damage.wav'),C('ClassicMonsters','Bat_Death.wav'),[])
build('Necromancer',C('ClassicMonsters','Ghost_Attack.wav'),C('ClassicMonsters','Ghost_Damage.wav'),C('ClassicMonsters','Ghost_Death.wav'),C('MagicLite','void_cast_01.wav'),{'HeavyAttack':C('MagicLite','shadow_01.wav'),'Summon':C('MagicLite','summon_01.wav')})
build('Werewolf',C('Werewolf','Werewolf_Attack.wav','Werewolf_Attack2.wav','Werewolf_Attack4.wav'),C('Werewolf','Werewolf_Hit.wav','Werewolf_Hit2.wav'),[old('Creature2/die_02.ogg')],C('Werewolf','Werewolf_ShortRoar1.wav'),{'TeleportAttack':C('MagicLite','teleport_01.wav')})
build('Werebear',C('Bear','bear_01.ogg','bear_02.ogg'),[old('Creature2/grunt_08.ogg')],[old('Creature2/die_03.ogg')],C('Bear','bear_02.ogg'))
sw=C('Medieval','sword_swing_01.wav');heavy=C('Medieval','sword_heavy_swing_01.wav');axe=C('Medieval','axe_swing_01.wav');plate=C('Medieval','hit_plate_01.wav');fall=C('Medieval','body_fall_01.wav')
build('Skeleton',sw,C('Skeleton','Ghostly_skeleton_damage_1.wav','Ghostly_skeleton_damage_3.wav'),C('Skeleton','Ghostly_skeleton_death_1.wav','Ghostly_skeleton_death_3.wav'),C('Medieval','blade_scrape_01.wav'),{'HeavyAttack':heavy},True)
build('ArmorSkeleton',sw,plate+C('Skeleton','Ghostly_skeleton_damage_3.wav'),C('Skeleton','Ghostly_skeleton_death_3.wav'),C('Medieval','sword_draw_01.wav'),{'HeavyAttack':heavy},True)
build('GreatswordSkeleton',heavy,plate+C('Skeleton','Ghostly_skeleton_damage_5.wav'),C('Skeleton','Ghostly_skeleton_death_1.wav'),C('Medieval','blade_scrape_01.wav'),{'HeavyAttack':heavy,'Skill3':axe},True)
build('BlackKnight',sw,plate,fall,C('Medieval','sword_draw_01.wav'),{'Skill2':heavy,'Skill3':axe},True)
build('BlackKnightBoss',heavy,plate,fall,C('MagicLite','void_cast_01.wav'),{'Skill2':axe,'Skill3':C('MagicLite','arcane_blast_01.wav')},True)
# Boss variant has a beam, so its Skill3 requires a separate profile from the leap variant.
prefab=R/'Assets/Prefabs/Characters/BlackKnightBoss.prefab'
if not (backup/prefab.name).exists():shutil.copyfile(prefab,backup/prefab.name)
oldguid=re.search(r'^guid: (\w+)',(P/'BlackKnight.asset.meta').read_text(),re.M).group(1);newguid=re.search(r'^guid: (\w+)',(P/'BlackKnightBoss.asset.meta').read_text(),re.M).group(1)
s=prefab.read_text();s=s.replace('profile: {fileID: 11400000, guid: '+oldguid,'profile: {fileID: 11400000, guid: '+newguid);prefab.write_text(s,encoding='utf-8')
print('Applied 12 profiles; boss beam profile separated; all prefab frames/markers unchanged')
