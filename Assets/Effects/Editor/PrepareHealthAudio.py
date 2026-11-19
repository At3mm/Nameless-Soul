import json,wave,zipfile
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Editor/HoveHealth/Source'
OUT=ROOT/'Audio/HoveHealth/Clips'
SOURCE.mkdir(parents=True,exist_ok=True)
OUT.mkdir(parents=True,exist_ok=True)
RATE=44100
report=[]
def prepare(name,source,max_duration):
    with wave.open(str(SOURCE/source)) as w:
        rate,channels,width=w.getframerate(),w.getnchannels(),w.getsampwidth()
        raw=w.readframes(w.getnframes())
    if width==3:
        x=np.frombuffer(raw,dtype=np.uint8).reshape(-1,3).astype(np.int32)
        x=x[:,0]|(x[:,1]<<8)|(x[:,2]<<16)
        x=((x^0x800000)-0x800000)/8388608
    elif width in (2,4):
        x=np.frombuffer(raw,dtype='<i'+str(width))/float(2**(width*8-1))
    else: raise ValueError('Unsupported PCM width')
    x=x.reshape(-1,channels).mean(axis=1)
    active=np.flatnonzero(abs(x)>max(.001,float(max(abs(x)))*.018))
    if not len(active):raise ValueError('Silent source '+source)
    start=max(0,int(active[0]-rate*.002))
    end=min(len(x),int(active[-1]+rate*.025),start+round(max_duration*rate))
    x=x[start:end]
    if rate>RATE:
        n=np.arange(-64,65);ratio=RATE/rate*.9
        kernel=np.sinc(n*ratio)*np.hanning(len(n));kernel/=kernel.sum()
        x=np.convolve(x,kernel,mode='same')
    x=np.interp(np.arange(round(len(x)*RATE/rate))*rate/RATE,np.arange(len(x)),x)
    x-=x.mean()
    attack=min(len(x),round(.0008*RATE));release=min(len(x)//4,round(.02*RATE))
    x[:attack]*=np.linspace(0,1,attack);x[-release:]*=np.linspace(1,0,release)
    x*=min(2,.7/max(.001,float(max(abs(x)))))
    assert len(x)>RATE*.025 and np.isfinite(x).all() and max(abs(x))<1
    with wave.open(str(OUT/(name+'.wav')),'wb') as w:
        w.setparams((1,2,RATE,0,'NONE','not compressed'))
        w.writeframes(np.round(x*32767).astype('<i2').tobytes())
    report.append(dict(name=name,source=source,start=round(start/rate,5),duration=round(len(x)/RATE,4)))


choices=[('Heal', 'Healing/Riser/DSGNRise_HealthRegen_HA_HealthDmg_01.wav', 0.8), ('Cleanse', 'Healing/Tone/4th_01.wav', 0.65), ('BuffDamage', 'Healing/Tone/3rd_01.wav', 0.55), ('BuffSpeed', 'Healing/Tone/5th_01.wav', 0.55), ('BuffResistance', 'Healing/Tone/4th_and_octave_01.wav', 0.65), ('BuffStamina', 'Healing/Synthetic/DSGNSynth_HealingSynthetic_HA_HealthDmg_01.wav', 0.65), ('BuffReduction', 'Healing/Tone/4th_and_3rd_01.wav', 0.65), ('Burn', 'HealthLowering/Falling/DSGNSynth_HealthFalling_HA_HealthDmg_01.wav', 0.6), ('Poison', 'HealthLowering/Synthetic/DSGNSynth_HealthFallingSynth_HA_HealthDmg_03.wav', 0.6), ('Stress', 'Heartbeat/HMNHart_HeartBeat_HA_HealthDmg_01.wav', 1.1)]
with zipfile.ZipFile('C:/Users/Quytsus/Downloads/HealthAndDamageSystemSFX.zip') as z:
 for name,tail,duration in choices:
  matches=[n for n in z.namelist() if n.endswith('/'+tail) and not n.startswith('__MACOSX')]
  assert len(matches)==1,(tail,matches)
  source=name+'_'+Path(tail).name
  (SOURCE/source).write_bytes(z.read(matches[0]))
  prepare(name,source,duration)
(OUT.parent/'Cuts.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
