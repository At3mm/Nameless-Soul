"""Prepare authorized downloads for Editor-only audition; retain source files and cut metadata."""
import json, shutil, wave
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parent / 'EpidemicAudio'
SOURCE = ROOT / 'Originals'
CLIPS = ROOT / 'Clips'
SOURCE.mkdir(parents=True, exist_ok=True)
CLIPS.mkdir(parents=True, exist_ok=True)
DOWNLOADS = Path('C:/Users/Quytsus/Downloads')
FILES = {
    'Light': 'ES_Weapons, Sword, Light, One Handed, Attack, Hit Flesh, Impact - Epidemic Sound.wav',
    'Heavy': 'ES_Weapons, Sword, Heavy, Two Handed, Attack, Hit Flesh, Impact - Epidemic Sound.wav',
    'Parry': 'ES_Weapons, Sword, Hit Sword, Block, Parry, Impact, Ringing - Epidemic Sound.wav',
    'Swing': 'ES_Swooshes, Swish, Sword Swing, Airy Heavy - Epidemic Sound.wav',
}
report = []

def read(path):
    with wave.open(str(path)) as w:
        rate, channels, width = w.getframerate(), w.getnchannels(), w.getsampwidth()
        raw = w.readframes(w.getnframes())
    if width == 3:
        a = np.frombuffer(raw, dtype=np.uint8).reshape(-1, 3).astype(np.int32)
        a = a[:, 0] | (a[:, 1] << 8) | (a[:, 2] << 16)
        a = ((a ^ 0x800000) - 0x800000) / 8388608.0
    elif width in (2, 4):
        a = np.frombuffer(raw, dtype='<i' + str(width)) / float(2 ** (width*8-1))
    else:
        raise ValueError('Unsupported PCM width')
    a = a.reshape(-1, channels).mean(axis=1)
    # Integer decimation with a windowed-sinc anti-alias filter.
    factor = 4 if rate >= 88200 else 2 if rate >= 44100 else 1
    if factor > 1:
        taps = np.arange(-48, 49)
        kernel = np.sinc(taps / factor * .85) * np.hanning(len(taps))
        kernel /= kernel.sum()
        a = np.convolve(a, kernel, mode='same')[::factor]
    return a, rate // factor

def export(name, data, rate, start, duration, source):
    cut = data[max(0, round(start*rate)):round((start+duration)*rate)].copy()
    if len(cut) < rate*.04:
        raise ValueError('Cut too short')
    cut -= np.mean(cut)
    fade_in, fade_out = min(round(.001*rate), len(cut)), min(round(.018*rate), len(cut))
    cut[:fade_in] *= np.linspace(0, 1, fade_in)
    cut[-fade_out:] *= np.linspace(1, 0, fade_out)
    peak = np.max(np.abs(cut))
    if peak < .001:
        raise ValueError('Silent cut')
    cut *= .68 / peak
    with wave.open(str(CLIPS / (name + '.wav')), 'wb') as w:
        w.setparams((1, 2, rate, 0, 'NONE', 'not compressed'))
        w.writeframes(np.round(cut*32767).astype('<i2').tobytes())
    report.append(dict(name=name, source=source, start=round(start, 5),
                       duration=round(len(cut)/rate, 5), rate=rate, peak=.68))

for kind, filename in FILES.items():
    shutil.copy2(DOWNLOADS / filename, SOURCE / filename)
    data, rate = read(SOURCE / filename)
    if kind in ('Light', 'Heavy'):
        candidates = []
        for index in range(10):
            start = index * 2
            part = data[round(start*rate):round((start+.95)*rate)]
            peak_index = int(np.argmax(np.abs(part)))
            candidates.append((float(np.max(np.abs(part))), start+peak_index/rate))
        for index, (_, peak_time) in enumerate(sorted(candidates, reverse=True)[:3]):
            export(kind+'Hit'+str(index+1), data, rate, max(0, peak_time-.006),
                   .36 if kind == 'Heavy' else .28, filename)
    elif kind == 'Parry':
        peak = np.argmax(np.abs(data))/rate
        export('Parry', data, rate, max(0, peak-.006), .42, filename)
    else:
        window = max(1, round(rate*.015))
        energy = np.convolve(data*data, np.ones(window)/window, mode='same')
        peak = int(np.argmax(energy))/rate
        export('SwordSwing', data, rate, max(0, peak-.07), .20, filename)
        export('HeavySwing', data, rate, max(0, peak-.09), .25, filename)
(ROOT / 'Cuts.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report, indent=2))
