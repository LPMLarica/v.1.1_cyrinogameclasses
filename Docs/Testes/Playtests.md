# Testes e playtests

## 1. Testes técnicos automatizados (rodam sem a Unity)
| Teste | Comando | O que verifica |
|---|---|---|
| Validação das fases | `python3 Tools/render_levels.py` | largura das linhas, `P`/`G` únicos, meta ≤ células, pares `M/m` e `V/v`, dicas definidas, entidades apoiadas no chão |
| Regeneração da arte | `python3 Tools/gen_art.py && git diff --stat` | os PNGs não mudam sem intenção |
| Regeneração do áudio | `python3 Tools/gen_audio.py` | todos os clipes são gerados; picos ≤ −1 dBFS |

### Registro — teste técnico da v1.0
| Data | Versão | Resultado |
|---|---|---|
| 05/10 | v1.0 | 3/3 fases válidas (fase1 10 células/meta 7; fase2 12/9; fase3 14/11) |
| 05/10 | v1.0 | Simulação da física: pulo 3,5 tiles / alcance 5,8 / pulo + Pulso 8,3 → todos os vãos dentro da regra do level design |
| 05/10 | v1.0 | Scripts C# compilados nas variantes Unity 6 (`linearVelocity`) e 2022.3 (`velocity`) |

## 2. Protocolo de playtest com jogadores
1. **Quem:** pessoas de fora da equipe (mínimo 3 por rodada), de preferência sem experiência com o jogo.
2. **Como:** o jogador recebe apenas "use o teclado, o jogo explica o resto". A equipe **não ajuda**; só observa e anota.
3. **Duração:** até 15 minutos ou até terminar a Fase 3.
4. **Durante:** anotar onde o jogador hesita, morre, se perde ou fala algo em voz alta.
5. **Depois:** 4 perguntas — (a) O que você precisava fazer? (b) O que foi mais difícil? (c) Algo pareceu injusto? (d) Nota de 1 a 5 para o controle.
6. **Métricas (registrar na tabela):** tempo por fase, quedas por fase, onde ocorreram, se atingiu a meta de células.

## 3. Fichas de playtest
> Preencher uma linha por jogador. As fases e os trechos (coluna "x") seguem os mapas de `Docs/LevelDesign/`.

### Rodada 1 — Protótipo (v0.3)
| Jogador | Fase | Tempo | Quedas (onde, x) | Células | Observações | Ação decidida |
|---|---|---|---|---|---|---|
| | | | | | | |

### Rodada 2 — Vertical slice (v0.4)
| Jogador | Fase | Tempo | Quedas (onde, x) | Células | Observações | Ação decidida |
|---|---|---|---|---|---|---|
| | | | | | | |

### Rodada 3 — Beta (v0.5)
| Jogador | Fase | Tempo | Quedas (onde, x) | Células | Observações | Ação decidida |
|---|---|---|---|---|---|---|
| | | | | | | |

## 4. Exemplo de preenchimento (ilustrativo)
> Linha de exemplo para mostrar o nível de detalhe esperado — **não** é um teste real.

| Jogador | Fase | Tempo | Quedas (onde, x) | Células | Observações | Ação decidida |
|---|---|---|---|---|---|---|
| J1 (colega de outra turma) | 1 | 02:40 | 3 (vão do Pulso, x 65) | 8/10 | Não leu a dica do Pulso; tentou só pular | Aumentar duração da dica 6; célula no meio do vão como "isca" |

## 5. Lista de verificação manual na Unity (antes de cada tag)
- [ ] Menu: todos os botões funcionam com mouse, teclado e controle.
- [ ] Opções: volumes alteram o som e ficam salvos ao reabrir o jogo.
- [ ] Fase 1–3: é possível concluir cada fase atingindo a meta.
- [ ] Transformador recusa a entrada sem a meta ("Faltam N células").
- [ ] Dano: sucata, arco ligado, Curto e queda tiram 1 carga e voltam ao checkpoint.
- [ ] Pulso atravessa arcos e derrota Curtos; não protege da sucata.
- [ ] Pausa congela tudo (inclusive o cronômetro) e abaixa a música.
- [ ] Game Over → "Tentar de novo" recomeça a fase com 5 cargas.
- [ ] Cena final mostra tempo, células e quedas; recorde é salvo.
- [ ] Nenhum erro no Console durante uma partida completa.
