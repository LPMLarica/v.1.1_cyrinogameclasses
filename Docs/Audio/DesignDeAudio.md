# Design de áudio

Todo o áudio foi **sintetizado pela equipe** em `Tools/gen_audio.py` (ondas quadrada/triangular/seno/ruído + envelopes ADSR, estilo chiptune). WAV 16 bits mono 44,1 kHz. Rodar o script de novo regenera os mesmos arquivos.

## Música
| Arquivo | Uso | Características | Loop |
|---|---|---|---|
| `music_menu.wav` | menu e cena final | Lá menor, 90 BPM, *pad* triangular, arpejo, sino | 42,7 s, sem emenda |
| `music_game.wav` | fases | Lá menor, 140 BPM, melodia em quadrada, baixo, bateria; 2ª passada com eco | 54,9 s, sem emenda |

Loop sem emenda: a cauda das notas que passa do fim é somada ao início do arquivo (`render_loop`).

## Ambientação
`ambience_substation.wav` — zumbido de 60 Hz com harmônicos (o som real de uma subestação), vento filtrado e estalos esporádicos. Toca em volume baixo por baixo da música das fases.

## Efeitos sonoros
| Evento | Arquivo | Ideia sonora |
|---|---|---|
| Pulo | `sfx_jump` | varredura ascendente curta |
| Pouso | `sfx_land` | baque grave (só em quedas altas) |
| Pulso | `sfx_dash` | sopro de ruído + "zap" descendente |
| Coleta | `sfx_collect` | arpejo brilhante (Mi maior) |
| Pisão | `sfx_stomp` | baque com varredura grave |
| Dano | `sfx_hurt` | queda de tom com vibrato e ruído |
| Checkpoint | `sfx_checkpoint` | sino de duas notas |
| Transformador | `sfx_goal` | arpejo ascendente + brilho sustentado |
| Fase concluída | `sfx_level_complete` | vinheta curta |
| Derrota | `sfx_game_over` | melodia descendente abafada |
| Vitória | `sfx_victory` | fanfarra |
| UI | `sfx_ui_click`, `sfx_ui_hover` | bipes curtos |
| Acesso negado | `sfx_denied` | dois zumbidos graves |
| Arco ligando | `sfx_arc` | estalos de ruído (só toca se o arco estiver perto da Faísca) |

## Implementação
- `AudioManager` (singleton persistente): *crossfade* entre duas fontes de música, fonte de ambiência e 10 vozes de efeito em rodízio, com pequena variação de *pitch* para evitar repetição.
- `AudioLibrary` (ScriptableObject): liga cada evento a um clipe — trocar um som não exige mexer em código.
- Volumes de música e efeitos ajustáveis no menu Opções e salvos em `PlayerPrefs`.
- Música abaixa ("ducking") quando o jogo é pausado.
- Importação: músicas em *streaming* (Vorbis), efeitos descomprimidos na memória (`ArtImportPostprocessor`).
