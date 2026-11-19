"""Original deterministic synthesis; no third-party audio samples. Run with Python 3."""
import math, random, struct, wave
from pathlib import Path

RATE = 22050
ROOT = Path(__file__).resolve().parents[1] / 'Resources' / 'CombatSFX'
ROOT.mkdir(parents=True, exist_ok=True)

def write(name, duration, render, seed=1):
    rng = random.Random(seed)
    samples = []
    low = 0.0
    for i in range(round(duration * RATE)):
        t = i / RATE
        noise = rng.uniform(-1, 1)
        low += .22 * (noise - low)
        v = render(t, noise, low)
        # Short edge fades eliminate clicks; leave headroom for simultaneous effects.
        v *= min(1, t / .003, (duration-t) / .012)
        samples.append(v)
    peak = max(abs(v) for v in samples) or 1
    gain = .65 / peak
    with wave.open(str(ROOT / (name + '.wav')), 'wb') as out:
        out.setparams((1, 2, RATE, 0, 'NONE', 'not compressed'))
        out.writeframes(b''.join(struct.pack('<h', round(v * gain * 32767)) for v in samples))

for variant in range(3):
    base = 105 + variant * 17
    write('Hit' + str(variant+1), .145, lambda t,n,l, base=base, variant=variant: (
        .48*l*math.exp(-t*48) + .42*(n-l)*math.exp(-t*150) +
        .27*math.sin(2*math.pi*(base*t-110*t*t))*math.exp(-t*55) +
        sum(a*math.sin(2*math.pi*(f+variant*43)*t)*math.exp(-t*d)
            for f,a,d in [(710,.16,42),(1283,.11,55),(2179,.05,75)])), 30+variant)

write('Parry', .255, lambda t,n,l: .48*(n-l)*math.exp(-t*150) + .24*l*math.exp(-t*60) + sum(
    math.sin(2*math.pi*f*t)*a*math.exp(-t*d)
    for f,a,d in [(1120,.33,26),(1873,.23,37),(3011,.09,55)]), 40)
write('Dash', .16, lambda t,n,l: (n-l)*math.sin(math.pi*t/.16)**1.4 * .4 +
    l*.3*math.exp(-t*18), 45)

notes = {
    'BuffDamage': [220, 330, 440],
    'BuffSpeed': [440, 660, 880],
    'BuffResistance': [330, 415.3, 660],
    'BuffStamina': [392, 493.9, 784],
    'BuffProtection': [261.6, 392, 523.3],
    'Heal': [329.6, 440, 659.3],
    'Cleanse': [523.3, 659.3, 1046.5],
}
for name, pitches in notes.items():
    def chime(t,n,l, pitches=pitches):
        value = 0
        for i, pitch in enumerate(pitches):
            age = t - i*.065
            if age >= 0:
                tone = math.sin(2*math.pi*pitch*age) + .16*math.sin(4*math.pi*pitch*age)
                value += tone * min(1, age/.006) * math.exp(-age*16) * .35
        return value + n*.03*math.exp(-t*35)
    write(name, .43, chime, 50)

write('DebuffBurn', .3, lambda t,n,l: (n-l)*.45*math.exp(-t*13) +
    .2*math.sin(2*math.pi*(260*t-200*t*t))*math.exp(-t*16), 60)
write('DebuffPoison', .36, lambda t,n,l: sum(
    (.22*math.sin(2*math.pi*(f*(t-delay)-200*(t-delay)**2))*math.exp(-(t-delay)*35))
    if t>=delay else 0 for delay,f in [(0,420),(.07,310),(.15,210)]) + l*.18*math.exp(-t*13), 61)
write('DebuffStress', .38, lambda t,n,l: (
    math.sin(2*math.pi*190*t)+.35*math.sin(2*math.pi*203*t))*.24*math.exp(-t*10)+l*.1*math.exp(-t*20), 62)
def sword(duration, heavy, variation):
    # Two moving low-pass bands make a shaped air sweep instead of a flat hiss.
    slow = fast = 0.0
    def render(t, n, low):
        nonlocal slow, fast
        u = t / duration
        crest = math.exp(-((u - (.32 + variation*.035)) / .22)**2)
        cutoff = 650 + crest * (3100 if heavy else 4200)
        a = 1 - math.exp(-2*math.pi*cutoff/RATE)
        b = 1 - math.exp(-2*math.pi*(230 if heavy else 390)/RATE)
        fast += a*(n-fast)
        slow += b*(n-slow)
        air = (fast-slow) * crest * 1.25
        body = low * .34 * math.exp(-((u-.27)/.18)**2)
        # Quiet inharmonic steel vibration trails the strongest part of the swing.
        age = max(0, t-duration*.28)
        ring = sum(math.sin(2*math.pi*freq*age + .025*math.sin(2*math.pi*31*age))*amp
                   for freq,amp in [(1170+variation*83,.05),(1867+variation*51,.024),(2931,.009)])
        ring *= min(1, age/.008)*math.exp(-age*34)
        return air + body + ring
    return render

for variation in range(3):
    name = 'AttackSwing' + (str(variation+1) if variation else '')
    duration = [.125,.115,.14][variation]
    write(name, duration, sword(duration, False, variation), 170+variation)
write('Skill2Cast', .24, lambda t,n,l: (
    .25*math.sin(2*math.pi*(180*t+700*t*t)) + .12*math.sin(2*math.pi*(360*t+1100*t*t))
    + l*.13)*math.sin(math.pi*t/.24)**1.3, 71)
for variation in range(3):
    name = 'Skill2Swing' + (str(variation+1) if variation else '')
    duration = [.17,.155,.185][variation]
    write(name, duration, sword(duration, True, variation), 180+variation)
print('Generated', len(list(ROOT.glob('*.wav'))), 'original WAV files in', ROOT)
