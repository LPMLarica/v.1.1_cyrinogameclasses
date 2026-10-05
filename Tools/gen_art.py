#!/usr/bin/env python3
"""
FAÍSCA — gerador da arte do jogo, pixel art.

Todos os sprites são desenhados, pixel a pixel, a partir de uma paleta
restrita. Rodar novamente regenera os PNGs em Assets/Art com o mesmo resultado
(seeds fixas), o que deixa o histórico do Git limpo.

Uso:  python3 Tools/gen_art.py
Requer: Pillow
"""
import math
import os
import random

from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
ART = os.path.join(ROOT, "Assets", "Art")

# ---------------------------------------------------------------- paleta ----
PAL = {
    "k": (13, 11, 30),      # quase preto (contorno)
    "n": (27, 31, 59),      # azul-marinho
    "b": (43, 58, 103),     # azul
    "c": (63, 167, 214),    # ciano
    "C": (155, 231, 255),   # ciano claro
    "w": (255, 255, 255),   # branco
    "y": (255, 210, 63),    # amarelo
    "Y": (255, 243, 163),   # amarelo claro
    "o": (242, 140, 40),    # laranja
    "r": (215, 38, 61),     # vermelho
    "R": (255, 107, 107),   # vermelho claro
    "g": (92, 107, 115),    # cinza
    "G": (154, 165, 171),   # cinza claro
    "d": (57, 66, 74),      # cinza escuro
    "D": (36, 42, 48),      # cinza muito escuro
    "e": (46, 147, 60),     # verde
    "E": (123, 211, 137),   # verde claro
    "u": (124, 84, 58),     # ferrugem
}
PALETTE_ORDER = "knbcCwyYorRgGdDeEu"


def rgba(ch, a=255):
    r, g, b = PAL[ch]
    return (r, g, b, a)


def new(w, h):
    return Image.new("RGBA", (w, h), (0, 0, 0, 0))


def from_ascii(rows, w=None, h=None):
    h = h or len(rows)
    w = w or max(len(r) for r in rows)
    img = new(w, h)
    px = img.load()
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch in PAL:
                px[x, y] = rgba(ch)
    return img


def save(img, *parts):
    path = os.path.join(ART, *parts)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path)
    return path


def bottom_align(img):
    """Move o conteúdo para encostar na última linha (pés no chão)."""
    bbox = img.getbbox()
    if not bbox:
        return img
    out = new(*img.size)
    dy = img.size[1] - bbox[3]
    out.paste(img, (0, dy))
    return out


# Faísca 
def draw_spark(size=16, cx=8.0, cy=10.0, rx=5.0, ry=5.0, phase=0.0,
               trail=None, trail_len=0, eyes="open", body="normal",
               n_flames=7, flame_len=2.2, streak=0):
    """Desenha a Faísca: corpo elíptico com gradiente + chamas radiais."""
    img = new(size, size)
    px = img.load()

    def put(x, y, ch):
        if 0 <= x < size and 0 <= y < size:
            px[x, y] = rgba(ch)

    # rastro Pulso
    for i in range(streak):
        x = int(cx - rx - 1 - i)
        for yy, ch in ((int(cy) - 2, "c"), (int(cy), "C"), (int(cy) + 2, "c")):
            if (i + yy) % 2 == 0 or ch == "C":
                put(x, yy, ch)

    # chamas 
    rnd = random.Random(int(phase * 1000) + n_flames)
    for i in range(n_flames):
        a = (i / n_flames) * 2 * math.pi + phase
        length = flame_len + rnd.uniform(-0.6, 0.8)
        if trail is not None:
            # chamas do lado do rastro ficam mais longas
            align = math.cos(a - trail)
            length += max(0.0, align) * trail_len
        r0 = 1.0
        steps = int(length * 3) + 1
        for s in range(steps):
            t = s / 3.0
            ex = cx + math.cos(a) * (rx + t) - 0.5
            ey = cy + math.sin(a) * (ry + t) - 0.5
            ch = "y" if t < length * 0.5 else ("o" if t < length * 0.85 else "r")
            put(int(round(ex)), int(round(ey)), ch)

    # corpo
    hx, hy = cx - rx * 0.35, cy - ry * 0.4
    for y in range(size):
        for x in range(size):
            fx, fy = x + 0.5, y + 0.5
            d = ((fx - cx) / rx) ** 2 + ((fy - cy) / ry) ** 2
            if d <= 1.0:
                hd = math.hypot((fx - hx) / rx, (fy - hy) / ry)
                if body == "flash":
                    ch = "w" if d < 0.7 else "Y"
                elif body == "hurt":
                    ch = "R" if d < 0.6 else ("r" if d < 0.9 else "k")
                else:
                    if hd < 0.28:
                        ch = "w"
                    elif d < 0.42:
                        ch = "Y"
                    elif d < 0.78:
                        ch = "y"
                    else:
                        ch = "o"
                px[x, y] = rgba(ch)

    # olhar 
    ex0 = int(cx + rx * 0.15)
    ey0 = int(cy - ry * 0.15)
    if eyes == "open":
        for ex in (ex0, ex0 + 2):
            put(ex, ey0, "k")
            put(ex, ey0 + 1, "k")
    elif eyes == "blink":
        for ex in (ex0, ex0 + 2):
            put(ex, ey0 + 1, "k")
    elif eyes == "x":
        for ex in (ex0 - 1, ex0 + 2):
            put(ex, ey0, "k")
            put(ex + 1, ey0 + 1, "k")
            put(ex, ey0 + 2, "k")
    elif eyes == "focus":
        for ex in (ex0 + 1, ex0 + 3):
            put(ex, ey0, "k")
            put(ex, ey0 + 1, "k")
    return img


def player_frames():
    F = {}
    left = math.pi  # rastro para trás 
    F["idle"] = [
        draw_spark(cy=10.0, phase=0.0),
        draw_spark(cy=10.0, phase=0.45, flame_len=2.6),
        draw_spark(cy=10.5, ry=4.6, phase=0.9),
        draw_spark(cy=10.0, phase=1.35, eyes="blink", flame_len=2.6),
    ]
    bobs = [0.0, -0.5, -1.0, 0.0, -0.5, -1.0]
    F["run"] = [
        draw_spark(cy=10.0 + b, rx=5.2, ry=4.8 + (0.2 if b == 0 else 0),
                   phase=i * 0.5, trail=left, trail_len=2.0 + (i % 3) * 0.6,
                   eyes="focus")
        for i, b in enumerate(bobs)
    ]
    F["jump"] = [
        draw_spark(cy=9.5, rx=4.2, ry=5.6, phase=0.2, trail=math.pi / 2, trail_len=2.5, eyes="focus"),
        draw_spark(cy=9.5, rx=4.4, ry=5.4, phase=0.7, trail=math.pi / 2, trail_len=3.0, eyes="focus"),
    ]
    F["fall"] = [
        draw_spark(cy=10.5, rx=5.4, ry=4.6, phase=0.3, trail=-math.pi / 2, trail_len=2.5),
        draw_spark(cy=10.5, rx=5.5, ry=4.5, phase=0.8, trail=-math.pi / 2, trail_len=3.0),
    ]
    F["dash"] = [
        draw_spark(cx=10.0, cy=10.0, rx=5.8, ry=3.8, phase=0.0, trail=left, trail_len=3.0, eyes="focus", streak=3, n_flames=6),
        draw_spark(cx=10.0, cy=10.0, rx=6.0, ry=3.6, phase=0.5, trail=left, trail_len=3.5, eyes="focus", streak=4, n_flames=6),
        draw_spark(cx=10.0, cy=10.0, rx=5.6, ry=4.0, phase=1.0, trail=left, trail_len=3.0, eyes="focus", streak=3, n_flames=6),
    ]
    F["hurt"] = [
        draw_spark(cy=10.0, phase=0.0, eyes="x", body="flash", flame_len=1.2),
        draw_spark(cy=10.0, phase=0.6, eyes="x", body="hurt", flame_len=1.0),
    ]
    return F


# Curto Circuito
CURTO_BODY = [
    "....kkkkkkk.....",
    "...kgGGggggkk...",
    "..kgGggggggrRk..",
    "..kggggggggrrkGG",
    ".kdggggggggggk..",
    ".kddggggggggdkGG",
    ".kdddddddddddk..",
    "..kkkkkkkkkkkk..",
]
CURTO_LEGS = [
    ["..k.k....k.k....", ".k...k..k...k..."],
    ["...k.k..k.k.....", "...k.k..k.k....."],
    ["..k.k....k.k....", "..k.k....k.k...."],
    ["...k.k..k.k.....", "..k...kk...k...."],
]


def enemy_frames():
    walk = []
    for i, legs in enumerate(CURTO_LEGS):
        rows = ["." * 16] * (6 - (1 if i % 2 else 0)) + CURTO_BODY + legs
        rows = rows[:16] + ["." * 16] * (16 - len(rows[:16]))
        walk.append(bottom_align(from_ascii(rows, 16, 16)))
    # morte: achata + faíscas
    die = []
    squash = ["." * 16] * 11 + [
        "..kkkkkkkkkkkk..",
        ".kgGggggggggrRk.",
        "kdggggggggggggdk",
        ".kkkkkkkkkkkkkk.",
        "." * 16,
    ]
    die.append(from_ascii(squash, 16, 16))
    s2 = [list(r) for r in squash]
    for (x, y, ch) in [(2, 8, "C"), (13, 7, "w"), (7, 6, "C"), (4, 10, "w"), (11, 9, "c")]:
        s2[y][x] = ch
    die.append(from_ascii(["".join(r) for r in s2], 16, 16))
    s3 = [["."] * 16 for _ in range(16)]
    for (x, y, ch) in [(1, 6, "C"), (14, 5, "w"), (6, 3, "c"), (9, 8, "C"), (3, 12, "c"), (12, 12, "w"), (8, 14, "d"), (5, 14, "d"), (10, 14, "d")]:
        s3[y][x] = ch
    die.append(from_ascii(["".join(r) for r in s3], 16, 16))
    return {"walk": walk, "die": die}


# Célula 
CELL = [
    "...kk...",
    "..kGGk..",
    ".kkkkkk.",
    ".kwEEek.",
    ".kwEyek.",
    ".kwyyek.",
    ".kyyyyk.",
    ".kwyyek.",
    ".kwEyek.",
    ".kwEEek.",
    ".kweeek.",
    ".kkkkkk.",
]


def cell_frames():
    base = from_ascii(CELL, 8, 12)
    widths = [8, 6, 4, 2, 4, 6]
    frames = []
    for i, w in enumerate(widths):
        f = new(16, 16)
        part = base.resize((w, 12), Image.NEAREST)
        if i in (2, 3, 4):  # verso: mais escuro
            px = part.load()
            for y in range(part.size[1]):
                for x in range(part.size[0]):
                    r, g, b, a = px[x, y]
                    if a:
                        px[x, y] = (int(r * 0.75), int(g * 0.75), int(b * 0.75), a)
        f.paste(part, (8 - w // 2, 2), part)
        frames.append(f)
    return frames


def ground_tile(mask, seed=7):
    """mask: bit0 = topo exposto, bit1 = esquerda exposta, bit2 = direita exposta."""
    rnd = random.Random(seed)
    img = new(16, 16)
    px = img.load()
    for y in range(16):
        for x in range(16):
            px[x, y] = rgba("d")
    # tijolos 
    for y in range(16):
        for x in range(16):
            row = y // 4
            if y % 4 == 3 or (x + (4 if row % 2 else 0)) % 8 == 7:
                px[x, y] = rgba("D")
    for _ in range(10):
        px[rnd.randrange(16), rnd.randrange(16)] = rgba("g")
    top, left, right = mask & 1, mask & 2, mask & 4
    if top:
        for x in range(16):
            px[x, 0] = rgba("G")
            px[x, 1] = rgba("g")
            px[x, 2] = rgba("g") if x % 5 else rgba("G")
            px[x, 3] = rgba("D")
        for x in range(1, 16, 4):  # cascalho
            px[x, 1] = rgba("G")
    if left:
        for y in range(16):
            px[0, y] = rgba("k")
            px[1, y] = rgba("g") if y > 3 or not top else px[1, y]
    if right:
        for y in range(16):
            px[15, y] = rgba("k")
            px[14, y] = rgba("D")
    if top and left:
        px[0, 0] = (0, 0, 0, 0)
        px[1, 0] = rgba("k")
        px[0, 1] = rgba("k")
    if top and right:
        px[15, 0] = (0, 0, 0, 0)
        px[14, 0] = rgba("k")
        px[15, 1] = rgba("k")
    return img


METAL = (
    ["kkkkkkkkkkkkkkkk", "kGGGGGGGGGGGGGgk"]
    + ["kGggggggggggggdk"] * 12
    + ["kgdddddddddddddk", "kkkkkkkkkkkkkkkk"]
)


def metal_tile():
    img = from_ascii(METAL, 16, 16)
    px = img.load()
    for (x, y) in [(3, 3), (12, 3), (3, 12), (12, 12)]:
        px[x, y] = rgba("D")
        px[x + 1, y] = rgba("G")
    return img


def platform_tile():
    rows = [
        "GGGGGGGGGGGGGGGG",
        "gdgdgdgdgdgdgdgd",
        "dgdgdgdgdgdgdgdg",
        "kkkkkkkkkkkkkkkk",
        ".d............d.",
        "..d..........d..",
        "...d........d...",
    ]
    return from_ascii(rows + ["." * 16] * 9, 16, 16)


def insulator_tile():
    rows = [
        "....kkkkkkkk....",
        "...kGGGGGGGGk...",
        "....kuuuuuuk....",
        "..kkwwwwwwwwkk..",
        "..kGwwwwwwwwGk..",
        "...kkkGGGGkkk...",
        ".....kuuuuk.....",
        "..kkwwwwwwwwkk..",
        "..kGwwwwwwwwGk..",
        "...kkkGGGGkkk...",
        ".....kuuuuk.....",
        "..kkwwwwwwwwkk..",
        "..kGwwwwwwwwGk..",
        "...kkkGGGGkkk...",
        "....kuuuuuuk....",
        "...kGGGGGGGGk...",
    ]
    return from_ascii(rows, 16, 16)


def spikes_tile():
    img = new(16, 16)
    d = ImageDraw.Draw(img)
    shards = [(0, 5, 3, 9), (5, 11, 8, 7), (10, 16, 13, 9)]
    for (x0, x1, tip, h) in shards:
        d.polygon([(x0, 15), (tip, 15 - h), (x1 - 1, 15)], fill=rgba("g"), outline=rgba("k"))
        d.line([(tip, 16 - h), (tip, 14)], fill=rgba("G"))
        d.point((tip, 15 - h), fill=rgba("w"))
    for x in range(16):
        img.putpixel((x, 15), rgba("u") if x % 3 else rgba("d"))
    return img


def arc_frames():
    frames = []
    for seed in (11, 23, 37):
        rnd = random.Random(seed)
        img = new(16, 16)
        px = img.load()
        x = 8
        pts = []
        for y in range(16):
            x = max(4, min(11, x + rnd.choice([-1, 0, 0, 1])))
            pts.append((x, y))
        for (x, y) in pts:  # brililhin
            for dx in (-2, -1, 1, 2):
                if 0 <= x + dx < 16 and rnd.random() < (0.9 if abs(dx) == 1 else 0.35):
                    px[x + dx, y] = rgba("c" if abs(dx) == 2 else "C", 200 if abs(dx) == 2 else 255)
        for (x, y) in pts:
            px[x, y] = rgba("w")
        # ramificação
        by = rnd.randrange(4, 12)
        bx = pts[by][0]
        for i in range(1, 4):
            xx = bx + i * rnd.choice([-1, 1])
            if 0 <= xx < 16 and by + i < 16:
                px[xx, by + i] = rgba("C")
        frames.append(img)
    off = new(16, 16)
    for y in range(0, 16, 3):
        off.putpixel((8, y), rgba("b", 160))
    return frames, off


def checkpoint_frames():
    pole = [
        "......kkkk......",
        ".....k{L}k.....",
        ".....k{L}k.....",
        "......kkkk......",
        ".......Gd.......",
        ".......Gd.......",
        ".......Gd.......",
        ".......Gd.......",
        ".......Gd.......",
        ".......Gd.......",
        ".......Gd.......",
        ".......Gd.......",
        ".......Gd.......",
        "......kGdk......",
        ".....kGGddk.....",
        "....kkkkkkkk....",
    ]

    def make(lamp, glow=None):
        rows = [r.replace("{L}", lamp) for r in pole]
        img = from_ascii(rows, 16, 16)
        if glow:
            for (x, y) in [(4, 1), (11, 1), (4, 2), (11, 2), (6, 0), (9, 0)]:
                img.putpixel((x, y), rgba(glow, 170))
        return img

    off = make("dddd")
    on = [make("EEwE", "E"), make("eEEE", "e")]
    return off, on


def goal_frames():
    def base(sign_on, bushing, arcs=0, seed=0):
        img = new(32, 32)
        d = ImageDraw.Draw(img)
        # transformador
        d.rectangle([5, 13, 26, 30], fill=rgba("g"), outline=rgba("k"))
        for x in range(7, 26, 3):  # aletas
            d.line([(x, 15), (x, 28)], fill=rgba("d"))
        d.rectangle([3, 16, 5, 28], fill=rgba("d"), outline=rgba("k"))
        d.rectangle([26, 16, 28, 28], fill=rgba("d"), outline=rgba("k"))
        d.rectangle([4, 30, 27, 31], fill=rgba("D"))
        # aviso
        tri = [(16, 17), (11, 26), (21, 26)]
        d.polygon(tri, fill=rgba("y" if sign_on else "d"), outline=rgba("k"))
        d.line([(16, 19), (15, 22), (17, 22), (16, 25)], fill=rgba("k"))
        # isolantes
        for bx in (9, 16, 23):
            for i, y in enumerate(range(5, 13, 2)):
                d.line([(bx - 2, y), (bx + 2, y)], fill=rgba("w" if i % 2 == 0 else "G"))
                d.line([(bx - 1, y + 1), (bx + 1, y + 1)], fill=rgba("u"))
            d.rectangle([bx - 1, 2, bx + 1, 4], fill=rgba(bushing), outline=rgba("k"))
        if arcs:
            rnd = random.Random(seed)
            for (a, b) in ((9, 16), (16, 23)):
                y = 2
                pts = []
                for x in range(a + 1, b):
                    y = max(0, min(4, y + rnd.choice([-1, 0, 1])))
                    pts.append((x, y))
                for (x, y) in pts:
                    img.putpixel((x, y), rgba("w"))
                    if y + 1 < 32:
                        img.putpixel((x, y + 1), rgba("C"))
            # brilho
            for (x, y) in [(2, 10), (29, 9), (1, 20), (30, 22), (16, 0)]:
                img.putpixel((x, y), rgba("C"))
        return img

    off = base(False, "d")
    ready = [base(True, "y"), base(False, "o")]
    active = [base(True, "C", arcs=1, seed=s) for s in (3, 5, 8, 13)]
    return off, ready, active


def moving_platform():
    img = new(48, 8)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, 47, 6], fill=rgba("g"), outline=rgba("k"))
    d.line([(1, 1), (46, 1)], fill=rgba("G"))
    for x0 in (1, 41):
        for i in range(6):
            for y in range(2, 6):
                x = x0 + i
                if ((x + y) // 2) % 2 == 0:
                    img.putpixel((x, y), rgba("y"))
                else:
                    img.putpixel((x, y), rgba("k"))
    for x in (10, 24, 37):
        img.putpixel((x, 3), rgba("D"))
        img.putpixel((x + 1, 3), rgba("G"))
    d.line([(2, 7), (45, 7)], fill=rgba("D"))
    return img


# partículas 
def particles():
    spark = from_ascii([".w..", "wYw.", ".w..", "...."], 4, 4)
    spark = from_ascii(["..w...", ".wYw..", "wYYYw.", ".wYw..", "..w...", "......"], 6, 6)
    dust = from_ascii([".GG.", "GwGG", "GGGG", ".GG."], 4, 4)
    return spark, dust


def icon_bolt():
    rows = [
        "........kkkk....",
        ".......kCCck....",
        "......kCCck.....",
        ".....kCCck......",
        "....kCCck.......",
        "...kCCCCCCCk....",
        "...kkkkCCck.....",
        "......kCck......",
        ".....kCck.......",
        "....kCck........",
        "...kCck.........",
        "...kck..........",
        "...kk...........",
    ]
    return from_ascii(rows + ["." * 16] * 3, 16, 16)


def nine_slice(w, h, fill, border, outer="k", alpha=235, notch=True):
    img = new(w, h)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, w - 1, h - 1], fill=rgba(outer, 255))
    d.rectangle([1, 1, w - 2, h - 2], fill=rgba(border, 255))
    d.rectangle([2, 2, w - 3, h - 3], fill=rgba(fill, alpha))
    if notch:
        for (x, y) in [(0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)]:
            img.putpixel((x, y), (0, 0, 0, 0))
    return img


def button_sprite():
    # tons claros: a cor final vem do ColorTint do componente Button
    img = new(32, 16)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, 31, 15], fill=(40, 40, 40, 255))
    d.rectangle([1, 1, 30, 14], fill=(255, 255, 255, 255))
    d.rectangle([2, 2, 29, 12], fill=(200, 200, 200, 255))
    d.line([(2, 13), (29, 13)], fill=(150, 150, 150, 255))
    for (x, y) in [(0, 0), (31, 0), (0, 15), (31, 15)]:
        img.putpixel((x, y), (0, 0, 0, 0))
    return img


# fonte bitmap 5x7 no logotipo
FONT5 = {
    "F": ["#####", "#....", "#....", "####.", "#....", "#....", "#...."],
    "A": [".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#"],
    "I": ["#####", "..#..", "..#..", "..#..", "..#..", "..#..", "#####"],
    "S": [".####", "#....", "#....", ".###.", "....#", "....#", "####."],
    "C": [".####", "#....", "#....", "#....", "#....", "#....", ".####"],
}


def logo():
    text = "FAISCA"
    scale = 6
    gap = 1
    cw = (5 + gap) * len(text) - gap
    W, H = cw * scale + 16, 7 * scale + 30
    img = new(W, H)
    px = img.load()
    ox, oy = 8, 22
    grad = ["Y", "Y", "y", "y", "y", "o", "o"]
    mask = set()
    for i, ch in enumerate(text):
        g = FONT5[ch]
        for gy, row in enumerate(g):
            for gx, v in enumerate(row):
                if v == "#":
                    for sy in range(scale):
                        for sx in range(scale):
                            x = ox + (i * (5 + gap) + gx) * scale + sx
                            y = oy + gy * scale + sy
                            mask.add((x, y))
                            px[x, y] = rgba(grad[gy])
    # contorno + sombra
    for (x, y) in list(mask):
        for dx, dy in ((-2, 0), (2, 0), (0, -2), (0, 2), (2, 2), (-2, 2), (2, -2), (-2, -2), (0, 4), (2, 4)):
            q = (x + dx, y + dy)
            if q not in mask and 0 <= q[0] < W and 0 <= q[1] < H:
                px[q] = rgba("k")
    # acento agudo do Í vira um raio ciano
    ix = ox + (2 * (5 + gap) + 2) * scale
    bolt = [(3, 0), (2, 1), (1, 2), (2, 2), (3, 2), (2, 3), (1, 4)]
    for (bx, by) in bolt:
        for sy in range(3):
            for sx in range(3):
                x = ix + bx * 3 + sx - 4
                y = 2 + by * 3 + sy
                if 0 <= x < W and 0 <= y < H:
                    px[x, y] = rgba("C")
    return img


# background
def bg_sky():
    W, H = 320, 180
    img = new(W, H)
    px = img.load()
    top, mid, bot = (10, 8, 26), (36, 24, 66), (43, 58, 103)
    for y in range(H):
        t = y / (H - 1)
        if t < 0.6:
            k = t / 0.6
            c = tuple(int(top[i] + (mid[i] - top[i]) * k) for i in range(3))
        else:
            k = (t - 0.6) / 0.4
            c = tuple(int(mid[i] + (bot[i] - mid[i]) * k) for i in range(3))
        
        c = tuple((v // 6) * 6 for v in c)
        for x in range(W):
            px[x, y] = c + (255,)
    rnd = random.Random(5)
    for _ in range(140):
        x, y = rnd.randrange(W), rnd.randrange(int(H * 0.7))
        px[x, y] = rgba(rnd.choice("wCCY"), rnd.choice([120, 180, 255]))
    # lua crescente: pontos dentro do círculo A e fora do círculo B
    for y in range(14, 44):
        for x in range(246, 276):
            in_a = (x - 261) ** 2 + (y - 29) ** 2 <= 11 ** 2
            in_b = (x - 268) ** 2 + (y - 26) ** 2 <= 10 ** 2
            if in_a and not in_b:
                px[x, y] = rgba("Y")
    return img


def bg_towers():
    W, H = 320, 180
    img = new(W, H)
    d = ImageDraw.Draw(img)
    col = (47, 42, 90, 255)
    hill = (36, 29, 69, 255)
    # colinas tileáveis
    for x in range(W):
        h = 132 + int(8 * math.sin(2 * math.pi * x / W) + 5 * math.sin(4 * math.pi * x / W + 1))
        d.line([(x, h), (x, H)], fill=hill)
    towers = [60, 220]
    for tx in towers:
        base_y = 140
        top_y = 60
        d.line([(tx - 14, base_y), (tx - 3, top_y)], fill=col)
        d.line([(tx + 14, base_y), (tx + 3, top_y)], fill=col)
        for i in range(8):  # treliça
            y0 = base_y - i * 10
            y1 = y0 - 10
            w0 = 14 - i * 11 / 8
            w1 = 14 - (i + 1) * 11 / 8
            d.line([(tx - w0, y0), (tx + w1, y1)], fill=col)
            d.line([(tx + w0, y0), (tx - w1, y1)], fill=col)
        for (y, w) in ((70, 22), (84, 18)):  # mísulas
            d.line([(tx - w, y), (tx + w, y)], fill=col, width=2)
            d.line([(tx - w, y), (tx - 3, y - 8)], fill=col)
            d.line([(tx + w, y), (tx + 3, y - 8)], fill=col)
    # cabos em catenária (fecham o tile)
    for (y, w) in ((71, 22), (85, 18)):
        for side in (-1, 1):
            for seg in range(2):
                a = towers[seg] + side * w
                b = (towers[seg] + 160) + side * w
                pts = []
                for i in range(41):
                    t = i / 40
                    x = a + (b - a) * t
                    yy = y + 14 * 4 * t * (1 - t)
                    pts.append(((x) % W, yy))
                for i in range(len(pts) - 1):
                    if abs(pts[i][0] - pts[i + 1][0]) < 20:
                        d.line([pts[i], pts[i + 1]], fill=(70, 62, 120, 255))
    return img


def bg_station():
    W, H = 320, 180
    img = new(W, H)
    d = ImageDraw.Draw(img)
    col = (22, 18, 48, 255)
    hi = (34, 30, 66, 255)
    d.rectangle([0, 150, W, H], fill=col)
    rnd = random.Random(9)
    x = 4
    while x < W - 30:
        kind = rnd.choice(["breaker", "transformer", "insulator", "gap"])
        if kind == "breaker":
            d.rectangle([x, 120, x + 10, 150], fill=col)
            for i in range(3):
                d.rectangle([x + 2 + i * 3, 104, x + 3 + i * 3, 120], fill=col)
            x += 22
        elif kind == "transformer":
            d.rectangle([x, 118, x + 34, 150], fill=col)
            for i in range(3):
                d.rectangle([x + 6 + i * 10, 100, x + 9 + i * 10, 118], fill=col)
            d.line([(x + 2, 124), (x + 32, 124)], fill=hi)
            x += 44
        elif kind == "insulator":
            d.rectangle([x + 3, 110, x + 5, 150], fill=col)
            for y in range(104, 124, 4):
                d.line([(x, y), (x + 8, y)], fill=col, width=2)
            x += 16
        else:
            x += 14
    # cerca (tileável: padrão de 8 px)
    for xx in range(0, W, 8):
        d.line([(xx, 136), (xx + 8, 150)], fill=hi)
        d.line([(xx + 8, 136), (xx, 150)], fill=hi)
    d.line([(0, 136), (W, 136)], fill=hi)
    for xx in range(0, W, 40):
        d.rectangle([xx, 130, xx + 1, 150], fill=hi)
    return img


def bg_city_lit():
    W, H = 320, 180
    img = new(W, H)
    px = img.load()
    top, bot = (43, 58, 103), (242, 140, 40)
    for y in range(H):
        t = y / (H - 1)
        c = tuple((int(top[i] + (bot[i] - top[i]) * t ** 1.6) // 6) * 6 for i in range(3))
        for x in range(W):
            px[x, y] = c + (255,)
    d = ImageDraw.Draw(img)
    rnd = random.Random(21)
    x = 0
    while x < W:
        w = rnd.randrange(14, 34)
        h = rnd.randrange(40, 110)
        y0 = H - h
        d.rectangle([x, y0, x + w, H], fill=(20, 18, 40, 255))
        for wy in range(y0 + 4, H - 4, 6):
            for wx in range(x + 3, x + w - 2, 5):
                if rnd.random() < 0.7:
                    d.rectangle([wx, wy, wx + 1, wy + 2], fill=rgba(rnd.choice("yyYo")))
        x += w + rnd.randrange(1, 4)
    return img


def main():
    S = "Sprites"
    pf = player_frames()
    for state, frames in pf.items():
        for i, f in enumerate(frames):
            save(f, S, "Player", f"player_{state}_{i}.png")
    ef = enemy_frames()
    for state, frames in ef.items():
        for i, f in enumerate(frames):
            save(f, S, "Enemy", f"curto_{state}_{i}.png")
    for i, f in enumerate(cell_frames()):
        save(f, S, "Items", f"cell_{i}.png")
    for m in range(8):
        save(ground_tile(m), S, "Tiles", f"ground_{m}.png")
    save(metal_tile(), S, "Tiles", "metal.png")
    save(platform_tile(), S, "Tiles", "oneway.png")
    save(insulator_tile(), S, "Tiles", "insulator.png")
    save(spikes_tile(), S, "Hazards", "spikes.png")
    arcs, arc_off = arc_frames()
    for i, f in enumerate(arcs):
        save(f, S, "Hazards", f"arc_on_{i}.png")
    save(arc_off, S, "Hazards", "arc_off_0.png")
    cp_off, cp_on = checkpoint_frames()
    save(cp_off, S, "Items", "checkpoint_off_0.png")
    for i, f in enumerate(cp_on):
        save(f, S, "Items", f"checkpoint_on_{i}.png")
    g_off, g_ready, g_active = goal_frames()
    save(g_off, S, "Items", "goal_off_0.png")
    for i, f in enumerate(g_ready):
        save(f, S, "Items", f"goal_ready_{i}.png")
    for i, f in enumerate(g_active):
        save(f, S, "Items", f"goal_active_{i}.png")
    save(moving_platform(), S, "Tiles", "moving_platform.png")
    spark, dust = particles()
    save(spark, S, "FX", "particle_spark.png")
    save(dust, S, "FX", "particle_dust.png")

    life = draw_spark(size=16, cx=8, cy=8.5, rx=4.2, ry=4.2, phase=0.3, flame_len=2.0)
    save(life, "UI", "icon_life.png")
    icell = new(16, 16)
    cb = from_ascii(CELL, 8, 12)
    icell.paste(cb, (4, 2), cb)
    save(icell, "UI", "icon_cell.png")
    save(icon_bolt(), "UI", "icon_dash.png")
    save(nine_slice(32, 32, "n", "c"), "UI", "panel.png")
    save(button_sprite(), "UI", "button.png")
    save(logo(), "UI", "logo.png")
    white = new(4, 4)
    white.paste((255, 255, 255, 255), (0, 0, 4, 4))
    save(white, "UI", "white.png")

    save(bg_sky(), "Backgrounds", "bg_sky.png")
    save(bg_towers(), "Backgrounds", "bg_towers.png")
    save(bg_station(), "Backgrounds", "bg_station.png")
    save(bg_city_lit(), "Backgrounds", "bg_city_lit.png")
    print("Arte gerada em", ART)


if __name__ == "__main__":
    main()
