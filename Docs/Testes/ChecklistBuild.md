# Checklist da build final

## Gerar
1. Unity ▸ menu **Faísca ▸ Gerar build ▸ Windows (64 bits)** (ou Linux/macOS).
   - Requer o módulo de build da plataforma instalado no Unity Hub.
   - Saída: `Builds/Windows/` (pasta ignorada pelo Git).
2. Conferir no Console: `[Faísca] Build concluída`.

## Testar em **outro computador** (obrigatório)
- [ ] Copiar a pasta inteira `Builds/Windows/` para um pendrive ou nuvem.
- [ ] No outro PC, executar `Faisca.exe` sem a Unity instalada.
- [ ] Jogar do menu até o fim da Fase 1, pausar, voltar ao menu, sair.
- [ ] Testar em resolução diferente (janela redimensionável).
- [ ] Anotar sistema operacional e resultado abaixo.

| Data | Computador / SO | Resultado | Observações |
|---|---|---|---|
| | | | |

## Montar a entrega no AVA
```
Faisca/                 ← pasta com o nome do jogo
├── Faisca.exe
├── Faisca_Data/
├── MonoBleedingEdge/
└── UnityPlayer.dll     (e demais .dll gerados)
```
- **Não** enviar: projeto da Unity, código-fonte, GDD, artes ou assets (avaliados pelo repositório).
- **Não** enviar a pasta `Faisca_BurstDebugInformation_DoNotShip`, se existir.
- Enviar o **link do repositório** na atividade do AVA.
- Conferir que a tag `v1.0` está publicada: `git push origin v1.0`.
