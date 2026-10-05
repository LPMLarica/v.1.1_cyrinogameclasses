# FAÍSCA — Game Design Document

| Campo | Valor |
|---|---|
| Título | **Faísca** |
| Gênero | Plataforma 2D de ação, *pixel art* |
| Plataforma | PC (Windows / Linux / macOS) — teclado ou controle |
| Engine | Unity 6 LTS (compatível com 2022.3 LTS), C# |
| Público | 10+ anos, jogadores casuais e fãs de plataforma de precisão |
| Duração de uma partida | 8 a 15 minutos (3 fases) |
| Versão do documento | 1.0 (final) — histórico ao fim |

---

## 1. Visão geral

### 1.1 High concept
Uma pequena centelha elétrica precisa atravessar uma subestação danificada por uma tempestade, recolhendo **Células de Energia** para religar o **Transformador Principal** e devolver a luz à cidade.

### 1.2 Pilares de design
1. **Movimento gostoso de controlar** — aceleração responsiva, *coyote time*, *jump buffer*, pulo variável e um *dash* ("Pulso") preciso.
2. **Desafio legível** — todo perigo tem cor, som e ritmo próprios; o jogador sempre entende por que errou.
3. **Pequeno e polido** — três fases curtas, sem conteúdo de enchimento; cada fase ensina, testa e combina.

### 1.3 Diferencial
O tema (energia elétrica / subestações) é tratado de forma lúdica: arcos elétricos com ritmo, "curtos-circuitos" como inimigos, isoladores e transformadores como cenário — com identidade visual própria.

---

## 2. Narrativa

**Premissa.** Após um raio atingir a Subestação Vale Verde, a cidade ficou às escuras. Faísca, a última centelha ainda acesa no sistema, decide percorrer o pátio, as linhas e a casa de controle para recolher a energia espalhada e religar o transformador.

**Estrutura.** A narrativa é ambiental: o cenário vai do pátio escuro (Fase 1) ao transformador (Fase 3). A tela final mostra a cidade iluminada.

---

## 3. Personagens e entidades

| Entidade | Papel | Comportamento |
|---|---|---|
| **Faísca** | Protagonista | Corre, pula e usa o Pulso. Perde uma carga (vida) ao tocar em perigos. |
| **Curto** | Inimigo comum | Patrulha plataformas, vira ao encontrar parede ou borda. Derrotado ao ser pisado ou atingido pelo Pulso. |
| **Célula de Energia** | Coletável | Necessária para ativar o transformador da fase. |
| **Sucata pontiaguda** | Perigo estático | Causa dano sempre, inclusive durante o Pulso. |
| **Arco elétrico** | Perigo rítmico | Liga e desliga em ciclo (fase A `~` e fase B `!`, alternadas). Pode ser atravessado durante o Pulso. |
| **Plataforma móvel** | Obstáculo/ajuda | Move-se horizontal ou verticalmente, carregando o jogador. |
| **Poste de checkpoint** | Progresso | Ao ser tocado, acende e passa a ser o ponto de retorno. |
| **Transformador** | Objetivo | Exige um número mínimo de células; fica piscando quando está pronto. |

---

## 4. Gameplay

### 4.1 Core loop
```
Explorar a fase → Superar obstáculos (pular / Pulso / pisar) → Coletar células
      ↑                                                          ↓
  Próxima fase  ←  Ativar o transformador  ←  Atingir a meta de células
```

### 4.2 Mecânica principal — movimento + Pulso
| Ação | Teclado | Controle |
|---|---|---|
| Mover | A / D ou ← / → | Analógico esquerdo |
| Pular (segurar = mais alto) | Espaço, W, ↑ ou Z | Botão A / Cruz |
| **Pulso** (dash) | Shift, X, J ou K | Botão X / Quadrado |
| Pausar | Esc ou P | Start |

**Pulso:** impulso horizontal curto (≈2,7 tiles) sem gravidade. Um uso no ar, recarregado ao tocar o chão. Durante o Pulso a Faísca **atravessa arcos elétricos e derrota Curtos**, mas **não** ignora a sucata.

### 4.3 Parâmetros de movimento (valores no Inspector do `PlayerController`)
| Parâmetro | Valor | Motivo |
|---|---|---|
| Velocidade máxima | 7 u/s | Atravessa a tela em ~4 s |
| Aceleração / desaceleração | 70 / 90 u/s² | Resposta quase imediata, sem "patinar" |
| Velocidade de pulo | 16 u/s (gravidade ×3,5) | Altura ≈ 3,5 tiles; alcance ≈ 5,8 tiles |
| Gravidade na queda | ×1,7 | Queda mais rápida que a subida (sensação de peso) |
| Corte do pulo | ×0,5 | Soltar o botão encurta o pulo |
| Coyote time / jump buffer | 0,10 s / 0,12 s | Perdoa pequenos erros de timing |
| Pulso | 18 u/s por 0,15 s, recarga 0,3 s | Vão máximo com pulo + Pulso ≈ 8 tiles |
| Invulnerabilidade pós-dano | 1,0 s | Evita dano em cadeia |

### 4.4 Regras, vitória e derrota
- O jogador começa com **5 cargas** (vidas).
- Tocar sucata, arco ativo, Curto (sem pisar) ou cair num buraco: **−1 carga** e retorno ao último checkpoint.
- **Derrota:** 0 cargas → tela *Sem Energia* (Tentar novamente / Menu).
- **Vitória da fase:** chegar ao transformador com a meta de células → fase concluída.
- **Vitória do jogo:** concluir a Fase 3 → cena final com tempo total, células e quedas; o melhor tempo é salvo.

### 4.5 Progressão e curva de dificuldade
| Fase | Nome | Ensina | Testa | Meta de células |
|---|---|---|---|---|
| 1 | Pátio de Manobras | mover, pular, sucata, Curtos, Pulso | vãos com Pulso, plataformas | 7 de 10 |
| 2 | Linhas de Transmissão | arcos rítmicos, plataformas móveis | timing + Pulso através do arco | 9 de 12 |
| 3 | Transformador Principal | arcos alternados (A/B) | combinação de tudo, escalada final | 11 de 14 |

Detalhes, mapas e justificativas: `Docs/LevelDesign/LevelDesign.md`.

---

## 5. Interface (UI/UX)
- **Menu principal:** Jogar, Como jogar, Opções (volume de música e efeitos), Créditos, Sair; exibe o melhor tempo.
- **HUD:** cargas, células (atual/meta — fica verde ao atingir a meta), nome da fase, cronômetro, indicador do Pulso.
- **Mensagens contextuais:** dicas do tutorial (gatilhos no mapa), "Checkpoint!", "Faltam N células".
- **Painéis:** Pausa, Fase concluída, Sem Energia (derrota).
- **Cena final:** estatísticas, recorde e retorno ao menu.

---

## 6. Direção de arte
- *Pixel art* 16×16 px por tile, 16 px = 1 unidade, filtro *Point*, sem compressão.
- Paleta restrita de 18 cores (ver `Docs/Arte/DirecaoVisual.md`): fundos frios (azul-marinho/roxo), elementos interativos quentes (amarelo/laranja) e perigos em ciano (arco) ou vermelho (Curto).
- Regra de leitura: **quente = Faísca/coletável, ciano = eletricidade perigosa, cinza = cenário sólido.**
- Parallax em 3 camadas: céu, torres de transmissão, equipamentos da subestação.

## 7. Animação
| Objeto | Estados |
|---|---|
| Faísca | Idle (4), Run (6), Jump (2), Fall (2), Dash (3), Hurt (2) |
| Curto | Walk (4), Die (3) |
| Célula | Spin (6) |
| Arco | On (3), Off (1) |
| Checkpoint | Off (1), On (2) |
| Transformador | Off (1), Ready (2), Active (4) |

Além das animações por quadros: *squash & stretch* procedural no pouso/pulo, partículas (faíscas e poeira) e *screen shake* no dano.

## 8. Áudio
- **Músicas:** tema do menu (calmo, *pad* + arpejo) e tema das fases (chiptune 140 BPM), ambos em loop.
- **Ambiência:** zumbido de subestação (60 Hz + harmônicos + estalos).
- **Efeitos:** pulo, Pulso, coleta, pisão, dano, checkpoint, transformador, fase concluída, derrota, vitória, cliques da UI, acesso negado, estalo do arco.
- Todo o áudio é sintetizado pela equipe (`Tools/gen_audio.py`). Detalhes: `Docs/Audio/DesignDeAudio.md`.

## 9. Arquitetura técnica (resumo)
| Sistema | Script(s) | Padrão |
|---|---|---|
| Sessão / regras | `GameSession`, `GameManager` | estado estático + *singleton* de cena com eventos C# |
| Fases | `LevelLoader`, `GameConfig` | fases em texto (ASCII) → Tilemap + colisores agrupados + prefabs |
| Jogador | `PlayerController`, `InputReader` | máquina de estados simples + física `Rigidbody2D` |
| Inimigos/perigos | `EnemyPatrol`, `Hazard`, `ArcHazard`, `MovingPlatform` | componentes independentes |
| Câmera | `CameraFollow`, `Parallax` | *SmoothDamp* + limites + *shake* |
| Áudio | `AudioManager`, `AudioLibrary` | *singleton* persistente + *ScriptableObject* |
| UI | `HUD`, `MainMenu`, `EndingScreen`, `SceneLoader` | uGUI + *fade* entre cenas |
| Ferramentas | `ProjectBootstrapper` (Editor) | gera prefabs, animações, cenas e build |

## 10. Escopo (MoSCoW)
Ver `Docs/Planejamento/Escopo.md`. Itens **cortados** conscientemente para priorizar polimento: chefe final, sistema de save por fase, colecionáveis secretos, mais de 3 fases.

## 11. Histórico do documento
| Versão | Etapa | Mudanças |
|---|---|---|
| v0.1 | Concepção | Conceito, pilares, core loop, controles, escopo inicial |
| v0.2 | Direção visual / level design | Paleta, model sheets, mapas das 3 fases |
| v0.3 | Protótipo | Ajuste de física (pulo 3,5 tiles; queda ×1,7) após primeiros testes |
| v0.4 | Vertical slice | Fase 1 completa com arte, som e UI |
| v0.5 | Beta / feature lock | Fases 2 e 3, arcos alternados A/B, balanceamento das metas |
| v1.0 | Final | Correções de bugs, polimento, créditos e build |
