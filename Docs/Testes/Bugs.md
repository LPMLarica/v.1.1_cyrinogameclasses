# Registro de bugs

Severidade: **A** = impede jogar/entregar · **M** = atrapalha a experiência · **B** = cosmético.
Status: Aberto · Corrigido (versão) · Não corrigir (motivo).

| ID | Sev. | Onde | Descrição | Como reproduzir | Causa | Correção | Status |
|---|---|---|---|---|---|---|---|
| BUG-01 | A | `Tools/gen_audio.py` | Script de áudio abortava com `ValueError: operands could not be broadcast` | rodar o script | `int(SR * d)` arredondava diferente em osciladores e envelopes (50713 × 50714 amostras) | função `ns()` com `round()` para todas as durações | Corrigido (v0.4) |
| BUG-02 | B | `bg_sky.png` | Lua crescente com um disco escuro visível ao lado | olhar o fundo em tela cheia | o "recorte" da lua era pintado com uma cor fixa, diferente do degradê | crescente desenhada por máscara (círculo A menos círculo B) | Corrigido (v0.2) |
| BUG-03 | B | Model sheet da Faísca | Texto de notas sobrepondo as animações; seta "→" aparecia como quadrado | abrir a prancha | layout com altura fixa; a fonte Pixelify Sans não tem setas | layout calculado pela altura do texto; setas trocadas por ">" e ":" | Corrigido (v0.2) |
| BUG-04 | M | `CameraFollow` | Câmera "deslizava" até a posição correta no início de cada fase | iniciar qualquer fase | `GameManager.Awake` (ordem −100) chamava `SnapToTarget` antes do `Awake` da câmera, que sobrescrevia a posição | câmera obtém o `Camera` sob demanda e não sobrescreve a posição já ajustada | Corrigido (v0.3) |
| BUG-05 | M | `PlayerController` | Plataformas a 3 tiles de altura exigiam pulo "perfeito" | pular para a passarela da fase 1 (x 40) | altura do pulo 3,35 tiles: folga de só 0,35 | `jumpVelocity` 15,5 → 16 (altura ≈ 3,5) | Corrigido (v0.3) |
| BUG-06 | M | Compatibilidade | `Rigidbody2D.velocity` gera aviso de obsoleto na Unity 6 | abrir na Unity 6 | API renomeada para `linearVelocity` | extensões `Rb2D.GetVelocity/SetVelocity` com `#if UNITY_6000_0_OR_NEWER` | Corrigido (v0.3) |
| BUG-07 | B | `Parallax` | No menu, após alguns minutos o fundo rolando automaticamente acabava | deixar o menu aberto ~10 min | deslocamento acumulava sem limite | deslocamento com `Mathf.Repeat` pela largura da imagem | Corrigido (v0.5) |
| BUG-08 | B | Cena final | Transformador aparecia apagado | concluir o jogo | `Animator.Play` chamado no editor não é salvo na cena | controlador próprio `Goal_Lit` com o estado aceso | Corrigido (v0.5) |
| BUG-09 | B | Prancha de objetos | Rótulos "Off" e "On" sobrepostos | abrir a prancha | posições fixas curtas demais | novas posições e rótulo "estático" para estados de 1 quadro | Corrigido (v0.2) |

## Bugs abertos
_Nenhum conhecido na v1.0._ Registre aqui o que aparecer nos playtests (seção 5 de `Playtests.md`).

## Modelo para novos registros
```
| BUG-NN | A/M/B | arquivo ou fase (x) | o que acontece | passos | causa (quando descoberta) | o que foi feito | status |
```
