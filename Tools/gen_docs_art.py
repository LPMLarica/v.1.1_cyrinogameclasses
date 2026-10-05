#!/usr/bin/env python3
"""
FAÍSCA — gera as pranchas de documentação de arte (Docs/Arte):
paleta, model sheets, exploração de silhuetas e concept da cena.
Usa os mesmos desenhos de Tools/gen_art.py, então a documentação
nunca fica desatualizada em relação aos sprites do jogo.

Uso:  python3 Tools/gen_docs_art.py   (rodar depois de gen_art.py)
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, os.path.dirname(__file__))
import gen_art as A  # noqa: E402

ROOT = A.ROOT
DOCS = os.path.join(ROOT, "Docs", "Arte")
SPR = os.path.join(ROOT, "Assets", "Art", "Sprites")
FONT_B = os.path.join(ROOT, "Assets", "Art", "Fonts", "PixelifySans-Bold.ttf")
FONT_R = os.path.join(ROOT, "Assets", "Art", "Fonts", "PixelifySans-Regular.ttf")
BG = (27, 31, 59, 255)
FG = (255, 243, 163, 255)
SUB = (155, 231, 255, 255)
TXT = (230, 230, 240, 255)


def f(size, bold=False):
    return ImageFont.truetype(FONT_B if bold else FONT_R, size)


def up(img, s):
    return img.resize((img.size[0] * s, img.size[1] * s), Image.NEAREST)


def checker(w, h, s=8):
    img = Image.new("RGBA", (w, h))
    d = ImageDraw.Draw(img)
    for y in range(0, h, s):
        for x in range(0, w, s):
            c = (44, 50, 86, 255) if (x // s + y // s) % 2 else (36, 41, 74, 255)
            d.rectangle([x, y, x + s - 1, y + s - 1], fill=c)
    return img


def header(d, title, subtitle):
    d.text((40, 28), title, font=f(44, True), fill=FG)
    d.text((42, 82), subtitle, font=f(20), fill=SUB)


def frame_row(canvas, d, y, label, frames, scale, fps, x0=40):
    info = "estático" if len(frames) == 1 else f"{len(frames)} quadros @ {fps} fps"
    d.text((x0, y), f"{label}  ({info})", font=f(20, True), fill=TXT)
    x = x0
    for i, fr in enumerate(frames):
        big = up(fr, scale)
        bg = checker(big.size[0], big.size[1])
        canvas.alpha_composite(bg, (x, y + 30))
        canvas.alpha_composite(big, (x, y + 30))
        d.text((x + 4, y + 32 + big.size[1]), str(i), font=f(14), fill=SUB)
        x += big.size[0] + 12
    return y + 30 + frames[0].size[1] * scale + 34


def palette_sheet():
    keys = A.PALETTE_ORDER
    W, H = 1200, 420
    img = Image.new("RGBA", (W, H), BG)
    d = ImageDraw.Draw(img)
    header(d, "Paleta — Faísca", "18 cores. Quente = Faísca e coletáveis · Ciano = eletricidade perigosa · Cinza = cenário sólido")
    for i, k in enumerate(keys):
        x = 40 + (i % 9) * 125
        y = 130 + (i // 9) * 140
        d.rectangle([x, y, x + 100, y + 80], fill=A.rgba(k), outline=(0, 0, 0, 255))
        r, g, b = A.PAL[k]
        d.text((x, y + 86), f"#{r:02X}{g:02X}{b:02X}", font=f(16), fill=TXT)
        d.text((x, y + 106), f"'{k}'", font=f(14), fill=SUB)
    img.save(os.path.join(DOCS, "Paleta.png"))


def model_sheet_player():
    pf = A.player_frames()
    W, H = 1500, 1560
    img = Image.new("RGBA", (W, H), BG)
    d = ImageDraw.Draw(img)
    header(d, "FAÍSCA — Model Sheet", "Protagonista · sprite 16×16 px · 16 px = 1 unidade Unity · pivot no centro")
    # proporções
    s = 20
    big = up(pf["idle"][0], s)
    ox, oy = 40, 140
    img.alpha_composite(checker(big.size[0], big.size[1], s), (ox, oy))
    img.alpha_composite(big, (ox, oy))
    for i in range(17):
        d.line([(ox + i * s, oy), (ox + i * s, oy + 16 * s)], fill=(255, 255, 255, 40))
        d.line([(ox, oy + i * s), (ox + 16 * s, oy + i * s)], fill=(255, 255, 255, 40))
    # colisor (0,62 x 0,62 u, offset y -0,125)
    cx, cy = ox + 8 * s, oy + 8 * s + int(0.125 * 16 * s)
    hw = int(0.31 * 16 * s)
    d.rectangle([cx - hw, cy - hw, cx + hw, cy + hw], outline=(123, 211, 137, 255), width=3)
    tx = ox + 16 * s + 30
    notes = [
        "Proporções e leitura",
        "• Silhueta circular + chamas radiais: lê bem em 16 px.",
        "• Corpo: núcleo branco > amarelo claro > amarelo > aro laranja.",
        "• Olhos sempre voltados para a direção do movimento",
        "  (o sprite é espelhado com SpriteRenderer.flipX).",
        "• Chamas crescem no lado oposto ao movimento (rastro).",
        "• Contorno verde = BoxCollider2D 0,62 × 0,62 u",
        "  (offset y = −0,125), menor que o desenho para",
        "  perdoar colisões 'por um pixel'.",
        "",
        "Squash & stretch",
        "• Pulo: corpo estica na vertical (ry 5,6).",
        "• Queda: corpo achata (rx 5,4).",
        "• Pulso: corpo estica na horizontal + rastro ciano.",
        "• Também aplicado por código no pouso (escala 1,25 × 0,75).",
    ]
    y = oy
    for line in notes:
        bold = line and not line.startswith(("•", " "))
        d.text((tx, y), line, font=f(22 if bold else 19, bold), fill=FG if bold else TXT)
        y += 30
    y = max(oy + 16 * s, y) + 40
    fps = {"idle": 8, "run": 14, "jump": 10, "fall": 10, "dash": 20, "hurt": 12}
    names = {"idle": "Idle — parada", "run": "Run — correndo", "jump": "Jump — subindo",
             "fall": "Fall — caindo", "dash": "Dash — Pulso", "hurt": "Hurt — dano"}
    col2 = 760
    left = ["idle", "run", "jump"]
    right = ["fall", "dash", "hurt"]
    yl = y
    for k in left:
        yl = frame_row(img, d, yl, names[k], pf[k], 6, fps[k], 40)
    yr = y
    for k in right:
        yr = frame_row(img, d, yr, names[k], pf[k], 6, fps[k], col2)
    # paleta usada
    yy = max(yl, yr) + 10
    d.text((40, yy), "Cores da personagem", font=f(22, True), fill=FG)
    for i, k in enumerate("wYyorkR"):
        d.rectangle([40 + i * 70, yy + 36, 100 + i * 70, yy + 76], fill=A.rgba(k), outline=(0, 0, 0, 255))
    img = img.crop((0, 0, W, yy + 100))
    img.save(os.path.join(DOCS, "ModelSheets", "Faisca_ModelSheet.png"))


def model_sheet_enemy():
    ef = A.enemy_frames()
    W, H = 1300, 900
    img = Image.new("RGBA", (W, H), BG)
    d = ImageDraw.Draw(img)
    header(d, "CURTO — Model Sheet", "Inimigo comum · sprite 16×16 px · patrulha e vira em paredes/bordas")
    s = 18
    big = up(ef["walk"][0], s)
    img.alpha_composite(checker(big.size[0], big.size[1], s), (40, 140))
    img.alpha_composite(big, (40, 140))
    notes = [
        "Conceito",
        "• Besouro-plugue: carapaça cinza (cenário) + LED",
        "  vermelho (perigo) + pinos de tomada na frente.",
        "• Cinza o integra ao cenário industrial; o LED",
        "  vermelho é o único ponto de cor: leitura imediata.",
        "• Patas alternadas a cada quadro, corpo sobe 1 px",
        "  nos quadros 1 e 3 (bob).",
        "",
        "Interação",
        "• Pisão por cima (velocidade y < 0) = Die.",
        "• Pulso = Die.",
        "• Qualquer outro contato  = dano na Faísca.",
    ]
    y = 140
    for line in notes:
        bold = line and not line.startswith(("•", " "))
        d.text((380, y), line, font=f(22 if bold else 19, bold), fill=FG if bold else TXT)
        y += 30
    y = 140 + big.size[1] + 40
    y = frame_row(img, d, y, "Walk — andando", ef["walk"], 7, 8)
    y = frame_row(img, d, y, "Die — desligando", ef["die"], 7, 10)
    img = img.crop((0, 0, W, y + 10))
    img.save(os.path.join(DOCS, "ModelSheets", "Curto_ModelSheet.png"))


def objects_sheet():
    W, H = 1500, 1500
    img = Image.new("RGBA", (W, H), BG)
    d = ImageDraw.Draw(img)
    header(d, "Objetos, perigos e tiles", "Todos os sprites interativos do jogo e seus estados de animação")
    cells = A.cell_frames()
    y = 130
    y = frame_row(img, d, y, "Célula de Energia — Spin", cells, 6, 10)
    arcs, arc_off = A.arc_frames()
    y2 = frame_row(img, d, y, "Arco elétrico — On", arcs, 6, 14)
    frame_row(img, d, y, "Arco — Off", [arc_off], 6, 1, 760)
    y = y2
    off, on = A.checkpoint_frames()
    frame_row(img, d, y, "Checkpoint — Off", [off], 6, 1, 40)
    y = frame_row(img, d, y, "Checkpoint — On", on, 6, 4, 420)
    g_off, g_ready, g_active = A.goal_frames()
    frame_row(img, d, y, "Transformador — Off", [g_off], 4, 1, 40)
    frame_row(img, d, y, "Ready", g_ready, 4, 3, 420)
    y = frame_row(img, d, y, "Active", g_active, 4, 10, 780)
    d.text((40, y), "Tiles (16×16) — chão com auto-tile por máscara (topo=1, esquerda=2, direita=4)", font=f(20, True), fill=TXT)
    x = 40
    for m in range(8):
        t = up(A.ground_tile(m), 5)
        img.alpha_composite(t, (x, y + 32))
        d.text((x + 30, y + 32 + 84), str(m), font=f(16), fill=SUB)
        x += 96
    for t, lab in ((A.metal_tile(), "B"), (A.insulator_tile(), "I"), (A.platform_tile(), "-"), (A.spikes_tile(), "^")):
        tt = up(t, 5)
        img.alpha_composite(checker(80, 80), (x, y + 32))
        img.alpha_composite(tt, (x, y + 32))
        d.text((x + 30, y + 32 + 84), lab, font=f(16), fill=SUB)
        x += 96
    y += 150
    mp = up(A.moving_platform(), 5)
    d.text((40, y), "Plataforma móvel (48×8)", font=f(20, True), fill=TXT)
    img.alpha_composite(mp, (40, y + 32))
    img = img.crop((0, 0, W, y + 32 + mp.size[1] + 30))
    img.save(os.path.join(DOCS, "ModelSheets", "Objetos_ModelSheet.png"))


def silhouette_exploration():
    W, H = 1300, 620
    img = Image.new("RGBA", (W, H), BG)
    d = ImageDraw.Draw(img)
    header(d, "Exploração de silhuetas — protagonista", "Três propostas avaliadas na v0.2. Critério: legibilidade em 16 px sobre fundo escuro")
    variants = [
        ("A — Centelha redonda", A.draw_spark(), "ESCOLHIDA: silhueta simples,\nexpressiva e fácil de animar."),
        ("B — Chama alta", A.draw_spark(cy=11, rx=4.0, ry=4.5, n_flames=9, flame_len=3.6),
         "Rejeitada: chamas longas se\nconfundem com o arco elétrico."),
        ("C — Cristal ciano", None, "Rejeitada: ciano é a cor do\nperigo (quebra a regra de cor)."),
    ]
    x = 40
    for title, spr, note in variants:
        if spr is None:
            spr = A.draw_spark(n_flames=4, flame_len=1.5)
            px = spr.load()
            for yy in range(16):
                for xx in range(16):
                    r, g, b, a = px[xx, yy]
                    if a:
                        px[xx, yy] = (int(b * 0.5), min(255, int(g * 0.95)), min(255, r), a)
        big = up(spr, 16)
        img.alpha_composite(checker(256, 256, 16), (x, 140))
        img.alpha_composite(big, (x, 140))
        sil = Image.new("RGBA", spr.size, (0, 0, 0, 0))
        sil.paste((13, 11, 30, 255), (0, 0), spr)
        img.alpha_composite(up(sil, 6), (x + 270, 140))
        d.text((x, 410), title, font=f(22, True), fill=FG)
        d.multiline_text((x, 445), note, font=f(18), fill=TXT, spacing=6)
        if title.startswith("A"):
            d.rectangle([x - 8, 132, x + 264, 404], outline=(123, 211, 137, 255), width=4)
        x += 420
    img.save(os.path.join(DOCS, "ConceptArt", "Exploracao_Silhuetas.png"))


def concept_scene():
    """Mock-up de tela de jogo (480×270 → ×3) montado com os assets reais."""
    W, H = 480, 270
    img = Image.new("RGBA", (W, H))
    sky = Image.open(os.path.join(ROOT, "Assets", "Art", "Backgrounds", "bg_sky.png")).convert("RGBA").resize((W, H), Image.NEAREST)
    img.alpha_composite(sky)
    for name, yoff in (("bg_towers", 30), ("bg_station", 70)):
        lay = Image.open(os.path.join(ROOT, "Assets", "Art", "Backgrounds", name + ".png")).convert("RGBA")
        for x0 in (0, 320):
            img.alpha_composite(lay, (x0 - 40, yoff))
    ground_y = 200
    for x in range(0, W, 16):
        mask = 1
        img.alpha_composite(A.ground_tile(mask), (x, ground_y))
        for yy in range(ground_y + 16, H, 16):
            img.alpha_composite(A.ground_tile(0), (x, yy))
    for x in range(176, 240, 16):
        img.alpha_composite(A.platform_tile(), (x, 150))
    arcs, _ = A.arc_frames()
    for i, yy in enumerate((152, 168, 184)):
        img.alpha_composite(arcs[i % 3], (320, yy))
    img.alpha_composite(A.insulator_tile(), (320, 136))
    for yy in range(0, 136, 16):
        img.alpha_composite(A.metal_tile(), (320, yy))
    pf = A.player_frames()
    img.alpha_composite(pf["dash"][1], (282, 176))
    img.alpha_composite(A.enemy_frames()["walk"][0], (120, 184))
    img.alpha_composite(A.cell_frames()[0], (200, 130))
    img.alpha_composite(A.cell_frames()[1], (372, 176))
    img.alpha_composite(A.spikes_tile(), (60, 184))
    off, on = A.checkpoint_frames()
    img.alpha_composite(on[0], (24, 184))
    _, ready, _ = A.goal_frames()
    img.alpha_composite(ready[0], (430, 168))
    # HUD mock
    d = ImageDraw.Draw(img)
    life = A.draw_spark(size=16, cx=8, cy=8.5, rx=4.2, ry=4.2, phase=0.3, flame_len=2.0)
    img.alpha_composite(life, (8, 6))
    d.text((27, 7), "x5", font=f(12, True), fill=FG)
    icell = Image.open(os.path.join(ROOT, "Assets", "Art", "UI", "icon_cell.png")).convert("RGBA")
    img.alpha_composite(icell, (60, 6))
    d.text((79, 7), "3/7", font=f(12, True), fill=(123, 211, 137, 255))
    d.text((W // 2 - 60, 7), "Pátio de Manobras", font=f(12, True), fill=TXT)
    d.text((W - 48, 7), "01:24", font=f(12, True), fill=TXT)
    big = up(img, 3)
    canvas = Image.new("RGBA", (big.size[0] + 80, big.size[1] + 200), BG)
    dd = ImageDraw.Draw(canvas)
    dd.text((40, 24), "Concept — tela de jogo (mock-up)", font=f(40, True), fill=FG)
    dd.text((42, 76), "Composição com os assets finais: parallax em 3 camadas, HUD mínimo no topo, perigos em ciano.",
            font=f(20), fill=SUB)
    canvas.alpha_composite(big, (40, 120))
    canvas.save(os.path.join(DOCS, "ConceptArt", "Concept_TelaDeJogo.png"))


def main():
    os.makedirs(os.path.join(DOCS, "ModelSheets"), exist_ok=True)
    os.makedirs(os.path.join(DOCS, "ConceptArt"), exist_ok=True)
    palette_sheet()
    model_sheet_player()
    model_sheet_enemy()
    objects_sheet()
    silhouette_exploration()
    concept_scene()
    print("Pranchas geradas em", DOCS)


if __name__ == "__main__":
    main()
