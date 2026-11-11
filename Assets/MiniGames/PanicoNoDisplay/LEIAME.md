# Pânico no Display — versão Unity

Conversão completa do minijogo feito em Godot (pasta `PanicoNoDisplay/` na raiz do projeto) para Unity, rodando **dentro** do jogo principal.

## Como usar

O minijogo abre pela **Central de Minigames** do computador do laboratório
(`IniciarMinigameUnity`, em `Assets/MiniGames/PAC_CODE_Unity_Migracao`): aperte **E** perto do
computador e escolha **PANICO NO LABORATORIO**. Ao sair do minijogo, volta para esse menu.

Também dá para usar um gatilho próprio (cubo amarelo, tecla E):
**Tools → Pânico no Display → Colocar gatilho ao lado do jogador**. Não use os dois no mesmo lugar.

Se alguma arte aparecer borrada ou com tamanho errado: **Tools → Pânico no Display → Reimportar artes do minijogo**.

## Fluxo

Menu (JOGAR / COMO JOGAR / SAIR) → cutscene (só na primeira vez, sempre pode pular) → fase.

- **SAIR** no menu, **SAIR** na pausa ou **ESC** na tela de fim → volta ao jogo principal, no mesmo lugar.
- **Vitória** → recompensa de `pontos × 0.1` em dinheiro no `PlayerWallet` ao voltar.
- Para ver a cutscene de novo: apague a chave `PanicoNoDisplay_CutsceneVista` do PlayerPrefs.

## Arquivos

| Godot | Unity |
|---|---|
| `scripts/player/player.gd` | `Scripts/PNDPlayer.cs` |
| `scripts/obstacles/*.gd`, `scripts/environment/*.gd` | `Scripts/PNDObstacles.cs` |
| `scenes/main/main.tscn` e cenas dos obstáculos | `Scripts/PNDLevel.cs` (posições idênticas) |
| `ui/hud.gd` + `hud.tscn` | `Scripts/PNDHud.cs` |
| `ui/main_menu.gd` | `Scripts/PNDMainMenu.cs` |
| `ui/how_to_play.gd` | `Scripts/PNDHowToPlay.cs` |
| `scenes/cutscene/intro_cutscene.gd` | `Scripts/PNDCutscene.cs` |
| AnimatedSprite2D | `Scripts/PNDSpriteAnimator.cs` |
| — (integração) | `Scripts/PanicoNoDisplayLauncher.cs`, `Scripts/IniciarPanicoNoDisplay.cs`, `Scripts/PNDGame.cs` |

O minijogo é montado por código numa cena criada na hora, então não há cena nem prefab para configurar.
Escala: 1 unidade = 1 pixel do original (640×360), então todos os números são os mesmos do Godot.
