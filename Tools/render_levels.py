#!/usr/bin/env python3
"""
FAÍSCA — validador e renderizador dos mapas das fases.

Lê Assets/Resources/Levels/*.txt (o mesmo arquivo que a Unity carrega),
verifica regras de consistência e gera imagens de preview em
Docs/LevelDesign/ usando os sprites reais do jogo.

Uso:  python3 Tools/render_levels.py
"""
import glob
import os
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
LEVELS = os.path.join(ROOT, "Assets", "Resources", "Levels")
SPR = os.path.join(ROOT, "Assets", "Art", "Sprites")
BG = os.path.join(ROOT, "Assets", "Art", "Backgrounds")
OUT = os.path.join(ROOT, "Docs", "LevelDesign")
FONT = os.path.join(ROOT, "Assets", "Art", "Fonts", "PixelifySans-Bold.ttf")

LEGEND = {
    ".": "vazio", "#": "chão", "B": "bloco metálico", "I": "isolador (sólido)",
    "-": "passarela (atravessável por baixo)", "^": "sucata (dano)", "C": "célula de energia",
    "E": "Curto (inimigo)", "K": "checkpoint", "P": "início", "G": "transformador (objetivo)",
    "M": "plataforma móvel horizontal", "m": "fim do trajeto de M", "V": "plataforma móvel vertical",
    "v": "fim do trajeto de V", "~": "arco elétrico fase A", "!": "arco elétrico fase B",
}
SOLID = set("#BI")


def parse(path):
    meta, rows = {}, []
    with open(path, encoding="utf-8") as f:
        for line in f:
            line = line.rstrip("\n\r")
            if not line:
                continue
            if line.startswith("@"):
                k, v = line[1:].split("=", 1)
                meta[k.strip()] = v.strip()
            else:
                rows.append(line)
    return meta, rows


def validate(name, meta, rows):
    errs = []
    w = len(rows[0])
    for i, r in enumerate(rows):
        if len(r) != w:
            errs.append(f"linha {i} tem largura {len(r)} (esperado {w})")
        for ch in r:
            if ch not in LEGEND and not ch.isdigit():
                errs.append(f"caractere desconhecido '{ch}' na linha {i}")
    flat = "".join(rows)
    for ch, n in (("P", 1), ("G", 1)):
        if flat.count(ch) != n:
            errs.append(f"deve haver exatamente {n} '{ch}' (há {flat.count(ch)})")
    cells = flat.count("C")
    goal = int(meta.get("meta", 0))
    if not (0 < goal <= cells):
        errs.append(f"meta {goal} inválida para {cells} células")
    for y, r in enumerate(rows):
        for x, ch in enumerate(r):
            if ch == "M" and "m" not in r:
                errs.append(f"M em ({x},{y}) sem 'm' na mesma linha")
            if ch == "V" and not any(rows[yy][x] == "v" for yy in range(len(rows))):
                errs.append(f"V em ({x},{y}) sem 'v' na mesma coluna")
            if ch.isdigit() and f"dica{ch}" not in meta:
                errs.append(f"gatilho {ch} sem @dica{ch}")
            if ch in "EKPG" and y + 1 < len(rows) and rows[y + 1][x] not in SOLID | {"-"}:
                errs.append(f"'{ch}' em ({x},{y}) não está apoiado no chão")
    return errs, cells, goal


def load(rel):
    return Image.open(os.path.join(SPR, rel)).convert("RGBA")


def ground_sprite(rows, x, y):
    def solid(xx, yy):
        if xx < 0 or xx >= len(rows[0]):
            return True
        if yy < 0:
            return False
        if yy >= len(rows):
            return True
        return rows[yy][xx] == "#"
    mask = (0 if solid(x, y - 1) else 1) | (0 if solid(x - 1, y) else 2) | (0 if solid(x + 1, y) else 4)
    return load(f"Tiles/ground_{mask}.png")


def render(name, meta, rows, scale=2):
    T = 16
    W, H = len(rows[0]), len(rows)
    img = Image.new("RGBA", (W * T, H * T), (20, 16, 40, 255))
    sky = Image.open(os.path.join(BG, "bg_sky.png")).convert("RGBA")
    for x0 in range(0, W * T, sky.size[0]):
        img.alpha_composite(sky.crop((0, 0, sky.size[0], min(sky.size[1], H * T))), (x0, 0))
    lay = Image.open(os.path.join(BG, "bg_station.png")).convert("RGBA")
    for x0 in range(0, W * T, lay.size[0]):
        img.alpha_composite(lay, (x0, H * T - lay.size[1]))
    sprites = {
        "B": load("Tiles/metal.png"), "I": load("Tiles/insulator.png"), "-": load("Tiles/oneway.png"),
        "^": load("Hazards/spikes.png"), "C": load("Items/cell_0.png"), "E": load("Enemy/curto_walk_0.png"),
        "K": load("Items/checkpoint_off_0.png"), "P": load("Player/player_idle_0.png"),
        "~": load("Hazards/arc_on_0.png"), "!": load("Hazards/arc_on_1.png"),
    }
    d = ImageDraw.Draw(img)
    font = ImageFont.truetype(FONT, 10)
    for y, r in enumerate(rows):
        for x, ch in enumerate(r):
            px, py = x * T, y * T
            if ch == "#":
                img.alpha_composite(ground_sprite(rows, x, y), (px, py))
            elif ch in sprites:
                img.alpha_composite(sprites[ch], (px, py))
                if ch == "!":
                    d.rectangle([px, py, px + 15, py + 15], outline=(255, 107, 107, 255))
            elif ch == "G":
                goal = load("Items/goal_off_0.png")
                img.alpha_composite(goal, (px, py - 16))
            elif ch in "MV":
                plat = load("Tiles/moving_platform.png")
                img.alpha_composite(plat, (px - 16, py + 4))
            elif ch in "mv":
                d.rectangle([px + 4, py + 4, px + 11, py + 11], outline=(255, 210, 63, 255))
            elif ch.isdigit():
                d.ellipse([px + 2, py + 2, px + 13, py + 13], outline=(155, 231, 255, 255), fill=(27, 31, 59, 255))
                d.text((px + 5, py + 1), ch, fill=(155, 231, 255, 255), font=font)
    # trajetos das plataformas
    for y, r in enumerate(rows):
        for x, ch in enumerate(r):
            if ch == "M":
                x2 = r.index("m", x) if "m" in r[x:] else r.index("m")
                d.line([(x * T + 8, y * T + 8), (x2 * T + 8, y * T + 8)], fill=(255, 210, 63, 200), width=1)
            if ch == "V":
                y2 = next(yy for yy in range(H) if rows[yy][x] == "v")
                d.line([(x * T + 8, y * T + 8), (x * T + 8, y2 * T + 8)], fill=(255, 210, 63, 200), width=1)
    # grade a cada 10 tiles
    for x in range(0, W, 10):
        d.line([(x * T, 0), (x * T, 4)], fill=(255, 255, 255, 120))
        d.text((x * T + 2, 2), str(x), fill=(255, 255, 255, 160), font=font)
    img = img.resize((img.size[0] * scale // 2, img.size[1] * scale // 2), Image.NEAREST) if scale != 2 else img
    return img


def main():
    os.makedirs(OUT, exist_ok=True)
    ok = True
    for path in sorted(glob.glob(os.path.join(LEVELS, "*.txt"))):
        name = os.path.splitext(os.path.basename(path))[0]
        meta, rows = parse(path)
        errs, cells, goal = validate(name, meta, rows)
        status = "OK" if not errs else "ERRO"
        print(f"{name}: {meta.get('nome')} — {len(rows[0])}x{len(rows)}, células {cells}, meta {goal} → {status}")
        for e in errs:
            print("   -", e)
        ok &= not errs
        img = render(name, meta, rows)
        img.save(os.path.join(OUT, f"{name}_mapa.png"))
    sys.exit(0 if ok else 1)


if __name__ == "__main__":
    main()
