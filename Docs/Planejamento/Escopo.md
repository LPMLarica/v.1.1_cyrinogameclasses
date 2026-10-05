# Escopo — priorização MoSCoW

O enunciado recomenda um jogo **pequeno e polido**. A tabela abaixo foi a ferramenta usada para decidir o que entra.

| Prioridade | Item | Status v1.0 |
|---|---|---|
| **Must** | Movimento (correr, pular com coyote/buffer, pulo variável) | ✅ |
| **Must** | Pulso (dash) — mecânica principal | ✅ |
| **Must** | Coleta de células + meta por fase | ✅ |
| **Must** | Perigos (sucata), inimigo (Curto), dano, vidas, checkpoint | ✅ |
| **Must** | 3 fases com progressão de dificuldade | ✅ |
| **Must** | Menu, HUD, pausa, vitória/derrota | ✅ |
| **Must** | Arte, animação e áudio próprios | ✅ |
| **Must** | Build executável | ✅ (menu *Faísca ▸ Gerar build*) |
| **Should** | Arcos elétricos rítmicos e plataformas móveis | ✅ |
| **Should** | Parallax, partículas, *screen shake*, *squash & stretch* | ✅ |
| **Should** | Opções de volume e melhor tempo salvo | ✅ |
| **Could** | Dicas contextuais de tutorial | ✅ |
| **Could** | Suporte a controle (gamepad) | ✅ (eixos padrão do Input Manager) |
| **Won't (nesta versão)** | Chefe final | ❌ cortado na v0.2 |
| **Won't** | Save por fase / seleção de fases | ❌ |
| **Won't** | Colecionáveis secretos, ranking online | ❌ |

## Riscos identificados (v0.1) e mitigação
| Risco | Prob. | Impacto | Mitigação |
|---|---|---|---|
| Arte própria demorar mais que o previsto | Alta | Alto | Tiles 16×16, paleta curta, sprites desenhados com script reaproveitável |
| "Grudar" em paredes / quinas de tiles | Média | Médio | Material físico sem atrito + colisores retangulares agrupados |
| Escopo crescer depois do beta | Média | Alto | *Feature lock* rígido em 01/10 |
| Build falhar em outro PC | Baixa | Alto | Testar a build em 2 máquinas antes de 08/10 (`Docs/Testes/ChecklistBuild.md`) |
