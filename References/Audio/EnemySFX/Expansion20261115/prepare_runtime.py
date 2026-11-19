from pathlib import Path
import wave,shutil,hashlib,json,numpy as np,re,uuid
B=Path(__file__).parent
root=B.parents[3] # References/Audio/EnemySFX/Expansion -> project
root=Path.cwd()
out=root/'Assets/Effects/Audio/Enemies/Expansion'
entries={
 'Skeleton':['Ghostly_skeleton_damage_1.wav','Ghostly_skeleton_damage_3.wav','Ghostly_skeleton_damage_5.wav','Ghostly_skeleton_death_1.wav','Ghostly_skeleton_death_3.wav'],
 'Medieval':['sword_swing_01.wav','sword_heavy_swing_01.wav','axe_swing_01.wav','hit_plate_01.wav','blade_scrape_01.wav','body_fall_01.wav','sword_draw_01.wav'],
 'Werewolf':['Werewolf_Attack.wav','Werewolf_Attack2.wav','Werewolf_Attack4.wav','Werewolf_Hit.wav','Werewolf_Hit2.wav','Werewolf_ShortRoar1.wav'],
 'MagicLite':['shadow_01.wav','summon_01.wav','teleport_01.wav','void_cast_01.wav','arcane_blast_01.wav'],
 'ClassicMonsters':['Orc_Attack.wav','Orc_Damage.wav','Orc_Death.wav','Orc_Emerge.wav','Slime_Attack.wav','Slime_Damage.wav','Slime_Death.wav','Slime_Emerge.wav','Bat_Attack.wav','Bat_Damage.wav','Bat_Death.wav','Behemoth_Attack.wav','Behemoth_Emerge.wav','Ghost_Attack.wav','Ghost_Damage.wav','Ghost_Death.wav'],
 'Bear':['bear_01.ogg','bear_02.ogg']}
manifest=[]
for pack,names in entries.items():
 for name in names:
  choices=list((B/pack/'Extracted').rglob(name));assert choices,(pack,name)
  # Avoid the stray Orc_Attack copy inside the Behemoth folder.
  src=next((f for f in choices if '/Orc/' in f.as_posix()),choices[0])
  dst=out/pack/name;dst.parent.mkdir(parents=True,exist_ok=True)
  stats={}
  if src.suffix=='.wav':
   with wave.open(str(src),'rb') as w:
    channels,width,rate=w.getnchannels(),w.getsampwidth(),w.getframerate();raw=w.readframes(w.getnframes())
   if width==2:a=np.frombuffer(raw,dtype='<i2').astype(np.float64)/32768
   elif width==3:
    x=np.frombuffer(raw,dtype=np.uint8).reshape(-1,3);v=x[:,0].astype(np.int32)|(x[:,1].astype(np.int32)<<8)|(x[:,2].astype(np.int32)<<16);v=(v^0x800000)-0x800000;a=v.astype(np.float64)/8388608
   elif width==1:a=(np.frombuffer(raw,dtype=np.uint8).astype(np.float64)-128)/128
   else:raise ValueError((name,width))
   a=a.reshape(-1,channels).mean(axis=1);peak=float(np.max(np.abs(a)));assert peak>.001,name
   active=np.flatnonzero(np.abs(a)>max(.002,peak*.006));start=max(0,int(active[0])-int(rate*.015));end=min(len(a),int(active[-1])+int(rate*.05)+1);a=a[start:end]
   gain=min(2.,.8/max(peak,.001));a*=gain;fade=min(int(rate*.005),len(a)//2);a[:fade]*=np.linspace(0,1,fade);a[-fade:]*=np.linspace(1,0,fade)
   pcm=(np.clip(a,-1,1)*32767).astype('<i2')
   with wave.open(str(dst),'wb') as w:w.setparams((1,2,rate,len(pcm),'NONE','not compressed'));w.writeframes(pcm.tobytes())
   stats={'seconds':round(len(a)/rate,3),'peak':round(float(np.max(np.abs(a))),4),'rms':round(float(np.sqrt(np.mean(a*a))),4),'rate':rate,'trim_start_seconds':round(start/rate,4),'gain':round(gain,4)}
  else:shutil.copyfile(src,dst)
  meta=dst.with_suffix(dst.suffix+'.meta')
  if not meta.exists():
   meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nAudioImporter:\n  externalObjects: {}\n  serializedVersion: 7\n  defaultSettings:\n    serializedVersion: 2\n    loadType: 0\n    sampleRateSetting: 0\n    sampleRateOverride: 44100\n    compressionFormat: 0\n    quality: 1\n    conversionMode: 0\n    preloadAudioData: 1\n  platformSettingOverrides: {}\n  forceToMono: 1\n  normalize: 0\n  ambisonic: 0\n  loadInBackground: 0\n  loadAmbisonic: 0\n  3D: 0\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')
  manifest.append({'pack':pack,'original':str(src.relative_to(root)),'runtime':str(dst.relative_to(root)),'sha256_original':hashlib.sha256(src.read_bytes()).hexdigest(),**stats})
(B/'runtime-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print('Prepared',len(manifest),'runtime clips; originals retained')
