# Changelog

## [v1.0] — Versão final (08/10)
- README, créditos, checklist de requisitos e roteiro da apresentação.
- Registro de bugs consolidado (`Docs/Testes/Bugs.md`): 9 corrigidos, 0 abertos conhecidos.
- Checklist da build e da entrega no AVA.

## [v0.5] — Beta / feature lock (01/10)
- Protocolo e fichas de playtest; lista de verificação manual na Unity.
- Testes técnicos automatizados (validação das fases, simulação do pulo).
- Correções: rolagem infinita do parallax no menu (BUG-07); transformador apagado na cena final (BUG-08).
- **Feature lock:** nenhuma mecânica nova a partir desta versão.

## [v0.4] — Vertical slice (24/09)
- Áudio próprio: 2 músicas em loop, ambiência de subestação e 15 efeitos (`Tools/gen_audio.py`).
- `AudioManager` com crossfade, ducking na pausa e volumes salvos.
- Design de áudio documentado.
- Correção: arredondamento de amostras no gerador de áudio (BUG-01).

## [v0.3] — Protótipo jogável (17/09)
- Todo o código C#: jogador (corrida, pulo com coyote/buffer, Pulso), fases a partir de texto, inimigos, perigos, arcos rítmicos, plataformas móveis, checkpoint, transformador, HUD, menu, cena final.
- `ProjectBootstrapper`: gera prefabs, AnimatorControllers, cenas e build pelo menu **Faísca**.
- `ArtImportPostprocessor`: importação automática da pixel art.
- Correções: câmera deslizando no início (BUG-04), folga do pulo (BUG-05), compatibilidade Unity 6 (BUG-06).

## [v0.2] — Direção visual e level design (10/09)
- Paleta de 18 cores e regra de cores; exploração de silhuetas; concept da tela de jogo.
- Sprites, tiles com auto-tile, fundos para parallax, UI e logotipo (`Tools/gen_art.py`).
- Model sheets da Faísca, do Curto e dos objetos.
- Formato de mapa em texto, 3 fases, validador e mapas renderizados (`Tools/render_levels.py`).
- Correções cosméticas nas pranchas (BUG-02, BUG-03, BUG-09).

## [v0.1] — Concepção e GDD inicial (03/09)
- GDD: conceito, pilares, core loop, controles, regras, progressão.
- Cronograma, escopo MoSCoW com riscos, papéis da equipe.
- Repositório com `.gitignore` e `.gitattributes` para Unity.
