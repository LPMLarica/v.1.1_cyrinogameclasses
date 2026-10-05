# Requisitos obrigatórios × evidências

| Requisito do enunciado | Onde está no projeto |
|---|---|
| Core loop funcional | explorar → coletar → ativar transformador → próxima fase (`GameManager`, `Collectible`, `Goal`) — GDD §4.1 |
| Mecânica principal completamente implementada | corrida + pulo variável + **Pulso** (`PlayerController`) |
| Programação das principais funcionalidades em C# | `Assets/Scripts/` (27 scripts: Core, Player, Gameplay, Level, UI, CameraFX, Editor) |
| Física, colisões e interações | `Rigidbody2D`, colisores agrupados, passarelas com `PlatformEffector2D`, triggers de dano/coleta, plataformas cinemáticas, pisão |
| Level design com progressão de desafios | 3 fases (`Assets/Resources/Levels/`), justificativa em `Docs/LevelDesign/LevelDesign.md` |
| Arte própria nos principais elementos | todos os sprites, tiles, fundos e UI (`Assets/Art/`, gerados por `Tools/gen_art.py`) |
| Animações | 7 AnimatorControllers, 16 AnimationClips + squash & stretch, partículas (`Assets/Animations/`) |
| Uso de prefabs | 10 prefabs em `Assets/Prefabs/` instanciados pelo `LevelLoader` |
| Pelo menos duas cenas, incluindo menu e gameplay | `MainMenu`, `Game`, `Ending` (`Assets/Scenes/`) |
| Interface com informações necessárias | HUD: cargas, células/meta, fase, tempo, Pulso, mensagens; painéis de pausa, derrota e fase concluída |
| Condições claras de vitória e/ou derrota | vitória: meta + transformador (fase) e Fase 3 (jogo); derrota: 0 cargas |
| Música, ambientação e efeitos sonoros | 2 músicas, 1 ambiência, 15 efeitos (`Assets/Audio/`) |
| Build executável e funcional | menu **Faísca ▸ Gerar build**; checklist em `Docs/Testes/ChecklistBuild.md` |

## Repositório
| Item exigido | Onde |
|---|---|
| Projeto da Unity e código C# | `Assets/`, `Packages/` (+ `ProjectSettings/` após a 1ª abertura) |
| GDD e documentos de planejamento | `Docs/GDD.md`, `Docs/Planejamento/` |
| Concept arts e model sheets | `Docs/Arte/ConceptArt/`, `Docs/Arte/ModelSheets/` |
| Sprites, cenários e UI | `Assets/Art/Sprites/`, `Assets/Art/Backgrounds/`, `Assets/Art/UI/` |
| Animações | `Assets/Animations/` (gerado) e quadros em `Assets/Art/Sprites/` |
| Assets produzidos pela equipe | `Assets/Art/`, `Assets/Audio/`, `Tools/` |
| Áudios produzidos ou editados | `Assets/Audio/`, `Docs/Audio/DesignDeAudio.md` |
| Mapas e materiais de level design | `Assets/Resources/Levels/`, `Docs/LevelDesign/` |
| Registros de playtesting e bugs | `Docs/Testes/Playtests.md`, `Docs/Testes/Bugs.md` |
| Créditos dos recursos externos | `CREDITS.md` |
| Uma tag por entrega | `v0.1`, `v0.2`, `v0.3`, `v0.4`, `v0.5`, `v1.0` |
