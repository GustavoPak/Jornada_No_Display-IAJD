# Pânico no Laboratório — correções de visual e câmera

## Instalação
1. Feche a Unity.
2. Remova a pasta antiga `Assets/PanicoNoLaboratorio` antes de copiar esta versão (não remova seu `IniciarMinigameUnity.cs`).
3. Copie a pasta `Assets/PanicoNoLaboratorio` deste pacote para `Assets` do projeto principal.
4. Abra a Unity e aguarde a importação.
5. No menu superior, execute **Tools > Panico no Laboratório > Reimportar artes como Sprites**.
6. Aguarde terminar e teste o minigame.

## O que foi corrigido
- As imagens agora são carregadas como `Sprite` (`Resources.Load<Sprite>`) em vez de tentar criá-las a partir de `Texture2D`.
- O importador/editor configura as PNGs em `Resources/art` como Sprite, sem compressão e com filtro Point.
- O minigame usa uma câmera própria em uma camada exclusiva (31), desenhada acima da câmera principal. A câmera do jogo hospedeiro não é modificada.
- A câmera exclusiva é destruída ao desativar/destruir o minigame.
- Corrigido um acesso sem verificação ao `floorRenderer` quando a arte do piso não é carregada.

## Integração
Mantenha apenas uma cópia do `IniciarMinigameUnity.cs` no projeto principal. O componente chamado continua sendo `PanicoNoLaboratorioGame` e o evento `ExitRequested` continua sendo usado para retornar ao menu.

## Diagnóstico
Se alguma arte ainda não aparecer, procure no Console por `Sprite ausente ou não importado como Sprite`. A mensagem informa o caminho exato que não foi encontrado.
