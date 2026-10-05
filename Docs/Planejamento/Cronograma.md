# Cronograma e entregas

Cada entrega corresponde a uma **tag** no repositório (`git tag -a vX.Y`), publicada também como *Release*.

| Data | Tag | Etapa | Critério de "pronto" (Definition of Done) | Principais artefatos |
|---|---|---|---|---|
| 05/10 | `v0.1` | Concepção e GDD inicial | GDD com conceito, core loop, controles e escopo aprovado pela equipe | `Docs/GDD.md`, `Docs/Planejamento/*` |
| 05/10 | `v0.2` | Direção visual e level design | Paleta, concept arts, model sheets e mapas das 3 fases | `Docs/Arte/*`, `Docs/LevelDesign/*`, `Assets/Art/*`, `Assets/Resources/Levels/*` |
| 05/10 | `v0.3` | Protótipo jogável | Faísca se move, pula e usa o Pulso numa fase de teste; colisões funcionando | `Assets/Scripts/Player/*`, `LevelLoader` |
| 06/10 | `v0.4` | Vertical slice | Fase 1 completa: arte, animação, som, HUD, checkpoint, vitória/derrota | prefabs, Animator, `AudioManager`, `HUD` |
| 06/10 | `v0.5` | Beta / feature lock | Todas as fases e sistemas; nenhuma feature nova depois desta data | fases 2 e 3, menu, opções, cena final, playtests |
| 07/10 | `v1.0` | Versão final | Bugs críticos zerados, créditos, build testada em outro PC | `CHANGELOG.md`, `Docs/Testes/*`, build |

## Rotina semanal da equipe
- **Nn tem equipe Kkkkk**

## Fluxo de versionamento
- Ramo `main` sempre jogável; trabalho em ramos `feature/<tema>` (ex.: `feature/pulso`, `feature/fase2`).
- Mensagens de commit no imperativo, com prefixo: `feat:`, `fix:`, `art:`, `audio:`, `level:`, `docs:`, `test:`.
- Binários grandes (PNG, WAV, TTF) marcados em `.gitattributes`; usar **Git LFS** se o repositório passar de ~500 MB.
- Os arquivos `.meta` da Unity **sempre** são versionados junto com o asset correspondente.
