"""Retain the free Hove pack and prepare short, separate sword impact/swing clips."""
import json, re, wave, zipfile
from pathlib import Path
import numpy as np

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Editor/HoveAudio/Source'
OUT=ROOT/'Audio/HoveAudio/Clips'
SOURCE.mkdir(parents=True,exist_ok=True)
OUT.mkdir(parents=True,exist_ok=True)
archive=Path('C:/Users/Quytsus/Downloads/Sword Combat Sound Effects Pack FREE VERSION.zip')
with zipfile.ZipFile(archive) as z:
    for entry in z.infolist():
        path=Path(entry.filename)
        if entry.is_dir() or entry.filename.startswith('__MACOSX') or '.DS_Store' in entry.filename:
            continue
        # Flatten known media/text leaves; no archive paths are executed or extracted verbatim.
        if path.suffix.lower() not in ('.wav','.txt'): continue
        leaf=re.sub(r'[<>:"/\\|?*\x00-\x1f]','_',path.name)
        (SOURCE/leaf).write_bytes(z.read(entry))

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

for i,suffix in enumerate(['01','11','17']):
    prepare('Hit'+str(i+1),'GOREStab_SwordStabGore_HoveAud_SwordCombat_'+suffix+'.wav',.34)
    prepare('HeavyHit'+str(i+1),'WEAPSwrd_SwordStabwRing_HoveAud_SwordCombat_'+suffix+'.wav',.43)
prepare('Parry','WEAPSwrd_BaseMetal_HoveAud_SwordCombat_03.wav',.43)
prepare('ParryReady','WHSH_Whoosh_HoveAud_SwordCombat_07.wav',.13)
for i,suffix in enumerate(['07','26']):
    file='WHSH_Whoosh_HoveAud_SwordCombat_'+suffix+'.wav'
    prepare('Swing'+str(i+1),file,.22)
    prepare('HeavySwing'+str(i+1),file,.27)
(OUT.parent/'Cuts.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
