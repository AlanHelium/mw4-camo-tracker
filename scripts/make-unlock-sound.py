"""Original short 'Camo Hunter' reward cue. Pure synthesis, no sampled audio.
Only used during development; the app plays the pre-rendered WAV.
"""
import math, struct, wave
from pathlib import Path

RATE = 44100
SECONDS = .92
samples = [0.0] * round(RATE * SECONDS)
# Soft E5 -> B5 -> E6 glass notes, each with its own transient and decay.
for start, frequency, decay, gain in [(0.0, 659.255, .13, .50), (.085, 987.767, .17, .48), (.19, 1318.510, .22, .68)]:
    for index in range(round(start * RATE), len(samples)):
        t = index / RATE - start
        attack = 1 - math.exp(-t * 850)
        fundamental = math.sin(math.tau * frequency * t)
        partials = .20 * math.sin(math.tau * frequency * 2.003 * t) * math.exp(-t / .07)
        partials += .09 * math.sin(math.tau * frequency * 3.987 * t) * math.exp(-t / .04)
        body = fundamental + partials
        samples[index] += gain * attack * math.exp(-t / decay) * body
        # One quiet, baked-in reflection; no live DSP or background playback.
        delay = round(.052 * RATE)
        if index + delay < len(samples):
            samples[index + delay] += .08 * gain * attack * math.exp(-t / decay) * body
peak = max(abs(s) for s in samples)
for index in range(len(samples)):
    end_fade = min(1.0, (len(samples) - index - 1) / (.06 * RATE))
    samples[index] = max(-1, min(1, samples[index] / peak * .40 * end_fade))
output = Path(__file__).resolve().parents[1] / 'src/Assets/unlock.wav'
with wave.open(str(output), 'wb') as writer:
    writer.setnchannels(1)
    writer.setsampwidth(2)
    writer.setframerate(RATE)
    writer.writeframes(b''.join(struct.pack('<h', round(s * 32767)) for s in samples))
print(f'Original cue: {SECONDS}s / mono PCM16 / {output.stat().st_size} bytes')
