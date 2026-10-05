#!/usr/bin/env python3
"""
FAÍSCA — gerador de áudio (músicas, ambiência e efeitos sonoros próprios).

Síntese subtrativa simples no estilo chiptune: ondas quadrada, triangular,
seno e ruído, com envelopes ADSR. Gera WAV 16 bits mono 44,1 kHz em
Assets/Audio. Sementes fixas → resultado reprodutível.

Uso:  python3 Tools/gen_audio.py
Requer: numpy
"""
import os
import wave

import numpy as np

SR = 44100
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
OUT = os.path.join(ROOT, "Assets", "Audio")
RNG = np.random.default_rng(42)

NOTE_INDEX = {"C": 0, "C#": 1, "D": 2, "D#": 3, "E": 4, "F": 5, "F#": 6,
              "G": 7, "G#": 8, "A": 9, "A#": 10, "B": 11}


def hz(name):
    """'A4' -> 440.0"""
    n, octv = name[:-1], int(name[-1])
    midi = 12 * (octv + 1) + NOTE_INDEX[n]
    return 440.0 * 2 ** ((midi - 69) / 12)


def ns(dur):
    """Duração em segundos -> número de amostras (arredondado)."""
    return int(round(SR * dur))


# ------------------------------------------------------------ osciladores --
def t_axis(dur):
    return np.arange(ns(dur)) / SR


def phase_of(freq, dur):
    f = np.broadcast_to(np.asarray(freq, dtype=float), (ns(dur),))
    return np.cumsum(f) / SR


def square(freq, dur, duty=0.5):
    ph = phase_of(freq, dur) % 1.0
    return np.where(ph < duty, 1.0, -1.0)


def triangle(freq, dur):
    ph = phase_of(freq, dur) % 1.0
    return 4 * np.abs(ph - 0.5) - 1


def sine(freq, dur):
    return np.sin(2 * np.pi * phase_of(freq, dur))


def noise(dur):
    return RNG.uniform(-1, 1, ns(dur))


def sweep(f0, f1, dur, curve=1.0):
    t = np.linspace(0, 1, ns(dur))
    return f0 + (f1 - f0) * t ** curve


def env(n, a=0.005, d=0.05, s=0.6, r=0.05, total=None):
    """ADSR em amostras (n = comprimento)."""
    a_n, d_n, r_n = ns(a), ns(d), ns(r)
    s_n = max(0, n - a_n - d_n - r_n)
    e = np.concatenate([
        np.linspace(0, 1, a_n, endpoint=False) if a_n else np.zeros(0),
        np.linspace(1, s, d_n, endpoint=False) if d_n else np.zeros(0),
        np.full(s_n, s),
        np.linspace(s, 0, r_n) if r_n else np.zeros(0),
    ])
    if len(e) < n:
        e = np.concatenate([e, np.zeros(n - len(e))])
    return e[:n]


def decay(n, tau):
    return np.exp(-np.arange(n) / (SR * tau))


def lowpass(x, cutoff):
    """Filtro passa-baixas de um polo."""
    alpha = 1 - np.exp(-2 * np.pi * cutoff / SR)
    y = np.zeros_like(x)
    acc = 0.0
    for i in range(len(x)):
        acc += alpha * (x[i] - acc)
        y[i] = acc
    return y


def mix_into(buf, sig, start):
    s = int(start)
    if s >= len(buf):
        return
    e = min(len(buf), s + len(sig))
    buf[s:e] += sig[: e - s]


def normalize(x, peak_db=-1.0):
    peak = np.max(np.abs(x)) or 1.0
    return x / peak * (10 ** (peak_db / 20))


def write_wav(x, *parts):
    path = os.path.join(OUT, *parts)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    data = (np.clip(x, -1, 1) * 32767).astype(np.int16)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())
    return path


def fade_edges(x, ms=4):
    n = ns(ms / 1000)
    if len(x) > 2 * n:
        x[:n] *= np.linspace(0, 1, n)
        x[-n:] *= np.linspace(1, 0, n)
    return x


# --------------------------------------------------------------- efeitos ---
def sfx_jump():
    d = 0.14
    s = square(sweep(320, 760, d, 0.7), d, 0.25) * decay(ns(d), 0.07)
    return s


def sfx_land():
    d = 0.07
    s = lowpass(noise(d), 900) * decay(ns(d), 0.02) * 1.5
    s += triangle(sweep(140, 60, d), d) * decay(ns(d), 0.03)
    return s


def sfx_dash():
    d = 0.22
    n = lowpass(noise(d), 3500) * env(ns(d), 0.005, 0.05, 0.5, 0.12)
    z = square(sweep(1100, 180, d, 0.5), d, 0.125) * decay(ns(d), 0.06) * 0.6
    return n + z


def sfx_collect():
    notes = ["E6", "G6", "B6", "E7"]
    out = np.zeros(ns(0.32))
    for i, nm in enumerate(notes):
        d = 0.12
        s = square(hz(nm), d, 0.5) * decay(ns(d), 0.04) * 0.6
        mix_into(out, s, i * SR * 0.05)
    return out


def sfx_stomp():
    d = 0.18
    s = triangle(sweep(220, 55, d, 0.6), d) * decay(ns(d), 0.06)
    s += lowpass(noise(d), 1500) * decay(ns(d), 0.015) * 0.8
    return s


def sfx_hurt():
    d = 0.4
    t = t_axis(d)
    f = sweep(520, 90, d) * (1 + 0.06 * np.sin(2 * np.pi * 30 * t))
    s = square(f, d, 0.5) * env(len(t), 0.003, 0.1, 0.5, 0.15) * 0.7
    s += noise(d) * decay(len(t), 0.05) * 0.4
    return s


def sfx_checkpoint():
    out = np.zeros(ns(0.6))
    for i, nm in enumerate(["C6", "G6"]):
        d = 0.45
        s = (triangle(hz(nm), d) + 0.3 * sine(hz(nm) * 2, d)) * decay(ns(d), 0.15)
        mix_into(out, s, i * SR * 0.11)
    return out


def sfx_goal():
    seq = ["C5", "E5", "G5", "C6", "E6", "G6"]
    out = np.zeros(ns(1.4))
    for i, nm in enumerate(seq):
        d = 0.2
        mix_into(out, square(hz(nm), d, 0.25) * decay(ns(d), 0.08) * 0.5, i * SR * 0.06)
    d = 0.9
    shimmer = (sine(hz("C7"), d) + 0.5 * sine(hz("G7"), d)) * env(ns(d), 0.02, 0.1, 0.6, 0.6)
    t = t_axis(d)
    shimmer *= 0.7 + 0.3 * np.sin(2 * np.pi * 9 * t)
    mix_into(out, shimmer * 0.5, SR * 0.36)
    return out


def melody(seq, bpm, wave_fn, gain=0.5, tail=0.3):
    """seq: lista de (nota|None, batidas)."""
    beat = 60.0 / bpm
    total = sum(b for _, b in seq) * beat + tail
    out = np.zeros(ns(total))
    pos = 0.0
    for nm, beats in seq:
        d = beats * beat
        if nm:
            n = int(SR * (d + tail * 0.5))
            s = wave_fn(hz(nm), n / SR) * env(n, 0.005, 0.05, 0.6, min(0.12, d * 0.5)) * gain
            mix_into(out, s, pos * SR)
        pos += d
    return out


def sfx_level_complete():
    seq = [("G5", 0.5), ("C6", 0.5), ("E6", 0.5), ("G6", 1.0), ("E6", 0.5), ("G6", 2.0)]
    lead = melody(seq, 150, lambda f, d: square(f, d, 0.25))
    bass = melody([("C4", 1.5), ("G3", 1.0), ("C4", 2.5)], 150, triangle, 0.6)
    n = max(len(lead), len(bass))
    out = np.zeros(n)
    out[: len(lead)] += lead
    out[: len(bass)] += bass
    return out


def sfx_game_over():
    seq = [("G4", 0.75), ("E4", 0.75), ("C4", 0.75), ("B3", 0.5), ("A#3", 0.5), ("A3", 2.0)]
    s = melody(seq, 110, lambda f, d: square(f, d, 0.5), 0.5, 0.4)
    return lowpass(s, 2500)


def sfx_victory():
    seq = [("C5", 0.5), ("C5", 0.5), ("C5", 0.5), ("C5", 1.0), ("G#4", 1.0), ("A#4", 1.0),
           ("C5", 0.66), ("A#4", 0.33), ("C5", 3.0)]
    lead = melody(seq, 140, lambda f, d: square(f, d, 0.25), 0.5, 0.4)
    harm = melody([(n and (n[:-1] + str(int(n[-1]) - 1)), b) for n, b in seq], 140, triangle, 0.35, 0.4)
    n = max(len(lead), len(harm))
    out = np.zeros(n)
    out[: len(lead)] += lead
    out[: len(harm)] += harm
    return out


def sfx_ui_click():
    d = 0.05
    return square(1250, d, 0.5) * decay(ns(d), 0.015)


def sfx_ui_hover():
    d = 0.03
    return square(880, d, 0.25) * decay(ns(d), 0.01) * 0.6


def sfx_denied():
    out = np.zeros(ns(0.3))
    for i in range(2):
        d = 0.1
        mix_into(out, square(150, d, 0.5) * env(ns(d), 0.002, 0.02, 0.8, 0.03) * 0.6, i * SR * 0.13)
    return lowpass(out, 1800)


def sfx_arc():
    d = 0.35
    n = ns(d)
    s = noise(d) * (RNG.random(n) < 0.08)  # estalos esparsos
    s = s * 1.2 + lowpass(noise(d), 5000) * 0.3
    s *= env(n, 0.005, 0.05, 0.7, 0.15)
    s += square(sweep(2400, 1600, d), d, 0.1) * decay(n, 0.05) * 0.15
    return s


# ---------------------------------------------------------------- música ---
def kick(d=0.18):
    return sine(sweep(140, 45, d, 0.4), d) * decay(ns(d), 0.05)


def snare(d=0.16):
    return (lowpass(noise(d), 6000) * 0.8 + triangle(190, d) * 0.3) * decay(ns(d), 0.04)


def hat(d=0.04):
    n = noise(d)
    hp = n - lowpass(n, 7000)
    return hp * decay(ns(d), 0.012)


def parse_bar(bar):
    """'A4 - C5 . ...' (8 colcheias) -> lista (nota|None, colcheias)."""
    toks = bar.split()
    out = []
    for tk in toks:
        if tk == "-" and out:
            out[-1] = (out[-1][0], out[-1][1] + 1)
        elif tk == ".":
            out.append((None, 1))
        else:
            out.append((tk, 1))
    return out


CHORDS = {
    "Am": ["A3", "C4", "E4"], "F": ["F3", "A3", "C4"], "C": ["C4", "E4", "G4"],
    "G": ["G3", "B3", "D4"], "E": ["E3", "G#3", "B3"], "Em": ["E3", "G3", "B3"],
    "Dm": ["D3", "F3", "A3"],
}


def render_loop(total_s, painter, tail_s=1.0):
    """Renderiza com cauda e dobra a cauda para o início → loop sem emenda."""
    n = ns(total_s)
    buf = np.zeros(n + ns(tail_s))
    painter(buf)
    out = buf[:n].copy()
    out[: len(buf) - n] += buf[n:]
    return out


def music_game():
    bpm = 140
    beat = 60 / bpm
    eighth = beat / 2
    A = ["A4 - C5 E5 - D5 C5 -", "A4 - C5 F5 - E5 C5 -", "G4 - C5 E5 - G5 E5 -", "D5 - - B4 - G4 B4 D5",
         "E5 - A5 - G5 E5 D5 C5", "D5 - C5 A4 - C5 F5 -", "G5 - F5 E5 - D5 B4 -", "E5 - - - G#4 - B4 -"]
    B = ["C6 - A5 - F5 - A5 C6", "B5 - G5 - D5 - G5 B5", "B5 - G5 E5 - G5 B5 -", "A5 - - E5 - C5 A4 -",
         "F5 - A5 C6 - A5 F5 -", "G5 - B5 D6 - B5 G5 -", "A5 - E5 C5 - E5 A5 -", "A5 - - - . . G5 E5"]
    chA = ["Am", "F", "C", "G", "Am", "F", "G", "E"]
    chB = ["F", "G", "Em", "Am", "F", "G", "Am", "Am"]
    bars = A + B + A + B
    chords = chA + chB + chA + chB
    total = len(bars) * 4 * beat

    def paint(buf):
        for bi, (bar, ch) in enumerate(zip(bars, chords)):
            t0 = bi * 4 * beat
            second_pass = bi >= 16
            # melodia
            pos = 0
            for nm, n8 in parse_bar(bar):
                d = n8 * eighth
                if nm:
                    n = ns(d * 0.95)
                    s = square(hz(nm), n / SR, 0.25 if not second_pass else 0.5)
                    s *= env(n, 0.004, 0.06, 0.55, 0.05) * 0.26
                    mix_into(buf, s, (t0 + pos * eighth) * SR)
                    if second_pass:  # eco uma oitava abaixo
                        low = nm[:-1] + str(int(nm[-1]) - 1)
                        e2 = square(hz(low), n / SR, 0.125) * env(n, 0.004, 0.06, 0.5, 0.05) * 0.10
                        mix_into(buf, e2, (t0 + pos * eighth + eighth / 2) * SR)
                pos += n8
            # baixo
            root = CHORDS[ch][0][:-1] + "2"
            octave = CHORDS[ch][0][:-1] + "3"
            for i, nm in enumerate([root, root, octave, root, root, octave, root, octave]):
                n = ns(eighth * 0.9)
                s = triangle(hz(nm), n / SR) * env(n, 0.003, 0.03, 0.8, 0.03) * 0.45
                mix_into(buf, s, (t0 + i * eighth) * SR)
            # arpejo
            notes = [x[:-1] + str(int(x[-1]) + 1) for x in CHORDS[ch]]
            for i in range(16):
                nm = notes[[0, 1, 2, 1][i % 4]]
                n = ns(beat / 4 * 0.8)
                s = square(hz(nm), n / SR, 0.125) * decay(n, 0.03) * 0.07
                mix_into(buf, s, (t0 + i * beat / 4) * SR)
            # bateria
            for b in range(4):
                tb = t0 + b * beat
                if b in (0, 2):
                    mix_into(buf, kick() * 0.7, tb * SR)
                else:
                    mix_into(buf, snare() * 0.35, tb * SR)
                mix_into(buf, hat() * 0.25, tb * SR)
                mix_into(buf, hat() * 0.18, (tb + eighth) * SR)
            if bi % 8 == 7:  # virada
                for k in range(4):
                    mix_into(buf, snare(0.1) * 0.25, (t0 + 3 * beat + k * beat / 4) * SR)

    return normalize(render_loop(total, paint), -3.0)


def music_menu():
    bpm = 90
    beat = 60 / bpm
    prog = ["Am", "F", "C", "G", "Am", "F", "G", "E"] * 2
    bell = ["E5", None, "C5", None, "A4", None, "B4", None,
            "C5", None, "A4", None, "G4", None, "B4", None,
            "E5", None, "G5", None, "E5", None, "D5", "C5",
            "C5", None, "A4", None, "B4", None, "G#4", None]
    total = len(prog) * 4 * beat

    def paint(buf):
        for bi, ch in enumerate(prog):
            t0 = bi * 4 * beat
            n = ns(4 * beat * 1.05)
            for k, nm in enumerate(CHORDS[ch]):  # pad
                f = hz(nm)
                s = (triangle(f, n / SR) + 0.5 * triangle(f * 1.004, n / SR)) * env(n, 0.6, 0.4, 0.7, 0.8) * 0.10
                mix_into(buf, s, t0 * SR)
            root = CHORDS[ch][0][:-1] + "2"
            s = sine(hz(root), n / SR) * env(n, 0.05, 0.3, 0.6, 0.6) * 0.35
            mix_into(buf, s, t0 * SR)
            notes = [x[:-1] + str(int(x[-1]) + 1) for x in CHORDS[ch]]
            for i in range(16):  # arpejo em semicolcheias
                nm = notes[[0, 1, 2, 1][i % 4]]
                m = ns(beat / 4)
                s = square(hz(nm), m / SR, 0.125) * decay(m, 0.05) * 0.035
                mix_into(buf, s, (t0 + i * beat / 4) * SR)
        for i, nm in enumerate(bell):  # sino (2 por compasso)
            if nm:
                m = ns(1.6)
                s = (sine(hz(nm), m / SR) + 0.25 * sine(hz(nm) * 3, m / SR)) * decay(m, 0.45) * 0.22
                mix_into(buf, s, i * 2 * beat * SR)

    return normalize(render_loop(total, paint, 2.0), -3.0)


def ambience():
    total = 12.0

    def paint(buf):
        n = len(buf)
        t = np.arange(n) / SR
        hum = (np.sin(2 * np.pi * 60 * t) * 0.5 + np.sin(2 * np.pi * 120 * t) * 0.3
               + np.sin(2 * np.pi * 180 * t) * 0.12 + np.sin(2 * np.pi * 240 * t) * 0.06)
        hum *= 0.75 + 0.25 * np.sin(2 * np.pi * t / total * 3)
        buf += hum * 0.25
        wind = lowpass(RNG.uniform(-1, 1, n), 400)
        wind *= 0.6 + 0.4 * np.sin(2 * np.pi * t / total * 2 + 1)
        buf += wind * 0.9
        for _ in range(9):  # estalos ocasionais
            start = RNG.uniform(0, total)
            d = RNG.uniform(0.04, 0.12)
            m = ns(d)
            c = RNG.uniform(-1, 1, m) * (RNG.random(m) < 0.15) * decay(m, d / 3) * 0.35
            mix_into(buf, c, start * SR)

    return normalize(render_loop(total, paint, 0.5), -9.0)


# ------------------------------------------------------------------ main ---
def main():
    sfx = {
        "jump": sfx_jump, "land": sfx_land, "dash": sfx_dash, "collect": sfx_collect,
        "stomp": sfx_stomp, "hurt": sfx_hurt, "checkpoint": sfx_checkpoint, "goal": sfx_goal,
        "level_complete": sfx_level_complete, "game_over": sfx_game_over, "victory": sfx_victory,
        "ui_click": sfx_ui_click, "ui_hover": sfx_ui_hover, "denied": sfx_denied, "arc": sfx_arc,
    }
    for name, fn in sfx.items():
        x = fade_edges(normalize(fn(), -1.5))
        write_wav(x, "SFX", f"sfx_{name}.wav")
        print("sfx", name, f"{len(x) / SR:.2f}s")
    write_wav(music_game(), "Music", "music_game.wav")
    write_wav(music_menu(), "Music", "music_menu.wav")
    write_wav(ambience(), "Music", "ambience_substation.wav")
    print("Áudio gerado em", OUT)


if __name__ == "__main__":
    main()
