"""Layer Kenney CC0 Foley recordings into short combat cues. No Epidemic samples used."""
from pathlib import Path
import json, wave
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
DECODED = ROOT.parents[1] / 'Library/DeveloperTools/AudioSources/Decoded'
OUT = ROOT / 'Resources/CombatSFX/SwordFoley'
OUT.mkdir(parents=True, exist_ok=True)
RATE = 44100
index = {e['name']: e for e in json.loads((DECODED/'index.json').read_text())}
report = []

def sample(name, seconds, speed=1, impact=True):
    entry = index[name]
    x = np.fromfile(DECODED/(name+'.f32'), dtype='<f4').astype(float)
    x = x.reshape(-1, entry['channels']).mean(axis=1)
    # Align impacts on their strongest short contact, not the recording's lead silence.
    if impact:
        window=max(1,int(entry['rate']*.006))
        envelope=np.convolve(x*x,np.ones(window)/window,mode='same')
        start=max(0,int(np.argmax(envelope)-entry['rate']*.004))
    else:
        active=np.flatnonzero(abs(x)>max(.002,float(max(abs(x)))*.035))
        start=max(0,int(active[0]-entry['rate']*.003)) if len(active) else 0
    x=x[start:]
    count=round(seconds*RATE)
    positions=np.arange(count)*entry['rate']/RATE*speed
    x=np.interp(positions,np.arange(len(x)),x,left=0,right=0)
    peak=max(.001,float(max(abs(x))))
    x=x/peak
    return x

def mix(name, seconds, layers):
    x=np.zeros(round(seconds*RATE))
    details=[]
    for source,gain,speed,delay,impact in layers:
        layer=sample(source,seconds,speed,impact)
        offset=round(delay*RATE)
        x[offset:] += layer[:len(x)-offset]*gain
        details.append(dict(source=source,gain=gain,speed=speed,delay=delay))
    x-=np.mean(x)
    x=np.tanh(x*1.25)
    attack,release=round(.0007*RATE),round(.025*RATE)
    x[:attack]*=np.linspace(0,1,attack)
    x[-release:]*=np.linspace(1,0,release)
    rms=float(np.sqrt(np.mean(x*x)))
    # Similar perceived body across variations, with a hard headroom ceiling.
    x*=min(.15/max(.001,rms),.78/max(.001,float(max(abs(x)))))
    assert np.isfinite(x).all() and float(max(abs(x)))<.8
    with wave.open(str(OUT/(name+'.wav')),'wb') as w:
        w.setparams((1,2,RATE,0,'NONE','not compressed'))
        w.writeframes(np.round(x*32767).astype('<i2').tobytes())
    report.append(dict(name=name,duration=seconds,rms=round(float(np.sqrt(np.mean(x*x))),4),
                       peak=round(float(max(abs(x))),4),layers=details))

for i in range(3):
    suffix=f'{i:03}'
    blade='knifeSlice' if i%2==0 else 'knifeSlice2'
    mix('Hit'+str(i+1),.27,[
        ('impactSoft_medium_'+suffix,.65,1.05,0,True),
        ('impactPunch_heavy_'+suffix,.50,1.12,.002,True),
        ('impactMetal_medium_'+suffix,.28,1.12,.004,True),
        (blade,.15,1.6,.008,False)])
    mix('HeavyHit'+str(i+1),.34,[
        ('impactPunch_heavy_'+suffix,.95,.88,0,True),
        ('impactSoft_medium_'+suffix,.43,.94,.003,True),
        ('impactMetal_heavy_'+suffix,.40,.94,.005,True),
        (blade,.18,1.3,.007,False)])
    mix('Swing'+str(i+1),.18+i*.008,[
        (blade,.9,1.7+i*.045,0,False),
        ('drawKnife'+str(i+1),.09,1.6,.003,False)])
    mix('HeavySwing'+str(i+1),.235+i*.008,[
        (blade,.95,1.3+i*.035,0,False),
        ('drawKnife'+str(i+1),.12,1.25,.005,False)])
mix('Parry',.34,[
    ('impactMetal_heavy_001',.9,1.17,0,True),
    ('impactMetal_light_000',.48,1.08,.003,True),
    ('drawKnife3',.18,1.3,.005,False),
    ('impactPunch_heavy_001',.18,1.25,0,True)])
(OUT/'MixReport.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('Mixed',len(report),'CC0 Foley cues into',OUT)
