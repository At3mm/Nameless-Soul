"""Original noise/modal synthesis. Reference files are measured, never mixed into output."""
import json, math, wave
from pathlib import Path
import numpy as np

RATE = 44100
ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Resources/CombatSFX/ForgedSteel'
OUT.mkdir(parents=True, exist_ok=True)
REPORT = []

def noise(rng, count, low, high):
    x = rng.normal(size=count)
    frequencies = np.fft.rfftfreq(count, 1/RATE)
    shape = (1-np.exp(-(frequencies/max(1,low))**4))*np.exp(-(frequencies/high)**4)
    x = np.fft.irfft(np.fft.rfft(x)*shape, n=count)
    return x / max(.001, np.std(x))

def save(name, x):
    x = x - np.mean(x)
    # Tiny attack and soft release prevent artificial clicks, retain the transient.
    attack, release = round(.0007*RATE), min(round(.022*RATE), len(x)//4)
    x[:attack] *= np.linspace(0,1,attack)
    x[-release:] *= np.linspace(1,0,release)
    x *= .68 / max(.001, np.max(np.abs(x)))
    assert np.isfinite(x).all() and np.max(np.abs(x)) < 1
    with wave.open(str(OUT/(name+'.wav')), 'wb') as w:
        w.setparams((1,2,RATE,0,'NONE','not compressed'))
        w.writeframes(np.round(x*32767).astype('<i2').tobytes())
    REPORT.append(dict(name=name, duration=round(len(x)/RATE,4), peak=round(float(np.max(abs(x))),3),
                       rms=round(float(np.sqrt(np.mean(x*x))),4)))

def impact(name, variant, heavy=False, parry=False):
    rng = np.random.default_rng(8400+variant+(100 if heavy else 200 if parry else 0))
    duration = .43 if parry else .34 if heavy else .26
    t = np.arange(round(duration*RATE))/RATE
    # Broadband contact, a low body and a granular scrape have separate decays.
    contact = noise(rng,len(t),800,11500)*np.exp(-t/(.006 if parry else .009))*.95
    body = noise(rng,len(t),55,650 if heavy else 1100)*np.exp(-t/(.026 if heavy else .018))*.55
    scrape = noise(rng,len(t),450,6800)*np.exp(-t/.052)*(.5+.5*np.cos(2*np.pi*(83+variant*13)*t))*.32
    # A dense set of inharmonic steel modes avoids a single electronic whistle.
    modes = np.zeros(len(t))
    for i in range(22 if parry else 14):
        frequency = (430 if heavy else 690) * (1+i*.46+rng.uniform(-.12,.12))
        frequency *= 1+variant*.027
        decay = (.105 if parry else .04) / (1+i*.07)
        phase = rng.uniform(-math.pi,math.pi)
        modes += np.sin(2*math.pi*frequency*t+phase)*np.exp(-t/decay)/(1+i*.35)
    modes *= .20 if parry else .075
    thump = np.sin(2*math.pi*((115 if heavy else 175)*t - 210*t*t))*np.exp(-t/.018)*(.35 if heavy else .14)
    # Very short reflections suggest a real blade/room, not a sustained reverb wash.
    x = contact+body+scrape+modes+thump
    for delay, gain in [(.011,.12),(.023,.065)]:
        n=round(delay*RATE)
        x[n:] += (contact+scrape)[:-n]*gain
    save(name,np.tanh(x*(1.1 if parry else 1.8)))

def swing(name, variant, heavy=False):
    rng=np.random.default_rng(9200+variant+(30 if heavy else 0))
    duration=(.205 if heavy else .155)+variant*.009
    t=np.arange(round(duration*RATE))/RATE
    u=t/duration
    crest=np.exp(-((u-(.29+variant*.025))/.19)**2)
    front=noise(rng,len(t),250,1800 if heavy else 2600)
    air=noise(rng,len(t),100,1100)
    hiss=noise(rng,len(t),3200,10500)
    x=(front*.55+air*(.40 if heavy else .22))*crest
    x += hiss*.045*np.exp(-((u-.47)/.24)**2)
    # Sparse stochastic steel chatter trails the sweep, never a pitched laser.
    chatter=noise(rng,len(t),1500,5500)
    x += chatter*.055*np.exp(-np.maximum(0,t-.04)/.035)*(t>.04)
    save(name,x)

for i in range(3):
    impact('Hit'+str(i+1),i)
    impact('HeavyHit'+str(i+1),i,heavy=True)
    swing('Swing'+str(i+1),i)
    swing('HeavySwing'+str(i+1),i,heavy=True)
impact('Parry',0,parry=True)

# Coarse reference comparison only: no source PCM is used by synthesis above.
references=[]
for path in (ROOT/'Editor/EpidemicAudio/Clips').glob('*.wav'):
    with wave.open(str(path)) as w:
        if w.getsampwidth()!=2: continue
        rate=w.getframerate()
        x=np.frombuffer(w.readframes(w.getnframes()),dtype='<i2').astype(float)/32768
    spec=abs(np.fft.rfft(x))**2
    freq=np.fft.rfftfreq(len(x),1/rate)
    references.append(dict(name=path.stem, duration=round(len(x)/rate,4),
        rms=round(float(np.sqrt(np.mean(x*x))),4),
        centroidHz=round(float(np.sum(spec*freq)/max(1e-12,np.sum(spec))))))
(OUT/'SynthesisReport.json').write_text(json.dumps(dict(generated=REPORT,referenceMeasurements=references,
    method='Fresh seeded noise and independently chosen modal frequencies; source PCM never mixed, copied or resynthesized.'),indent=2),encoding='utf-8')
print(json.dumps(REPORT,indent=2))
