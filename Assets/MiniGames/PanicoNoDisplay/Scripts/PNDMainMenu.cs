using TMPro;
using UnityEngine;

namespace PanicoNoDisplay
{
    /// <summary>Conversão de ui/main_menu.gd + ui/main_menu.tscn.</summary>
    public class PNDMainMenu : MonoBehaviour
    {
        public PNDGame game;
        PNDButton playButton, howToButton, quitButton;
        GameObject menuBox, title;

        public static PNDMainMenu Create(Transform canvasRoot, PNDGame game)
        {
            var rt = PNDUI.Full(PNDUI.Rect("MainMenu", canvasRoot));
            var m = rt.gameObject.AddComponent<PNDMainMenu>();
            m.game = game;
            m.Build(rt);
            return m;
        }

        void Build(RectTransform root)
        {
            PNDUI.ColorRect("ColorRect", root, new Color(0.12549f, 0.164706f, 0.282353f, 1f));

            var t = PNDUI.Label("Title", root, "PÂNICO NO LABORATÓRIO", 36, new Color(1f, 0.8f, 0.2f),
                TextAlignmentOptions.Center, 6, Color.black);
            PNDUI.G(t.rectTransform, 0.5f, 0, 0.5f, 0, -250, 60, 250, 120);
            title = t.gameObject;

            var box = PNDUI.G(PNDUI.Rect("VBoxContainer", root), 0.5f, 0.5f, 0.5f, 0.5f, -100, -20, 100, 150);
            PNDUI.VBox(box, 16f);
            menuBox = box.gameObject;

            playButton = PNDUI.Button("PlayButton", box, "JOGAR", 24);
            howToButton = PNDUI.Button("HowToButton", box, "COMO JOGAR", 24);
            quitButton = PNDUI.Button("QuitButton", box, "SAIR", 24);

            // Conecta os cliques dos botões às funções
            playButton.onClick.AddListener(OnPlayPressed);
            quitButton.onClick.AddListener(OnQuitPressed);
            howToButton.onClick.AddListener(OnHowToPressed);
        }

        void Start()
        {
            // Dá o foco automático para o botão "JOGAR", permitindo usar o teclado
            PNDUI.Select(playButton);
        }

        void OnPlayPressed()
        {
            // Ao apertar Jogar, inicia a cutscene de contexto antes da fase
            // (só na primeira vez; depois vai direto para a fase)
            game.OnPlayPressed();
        }

        void OnHowToPressed()
        {
            // Abre o manual "COMO JOGAR" por cima do menu
            menuBox.SetActive(false);
            title.SetActive(false); // Fundo só com a cor sólida, sem o nome do jogo
            var manual = PNDHowToPlay.Create(transform, game);
            manual.closed += OnHowToClosed;
        }

        void OnHowToClosed()
        {
            menuBox.SetActive(true);
            title.SetActive(true);
            PNDUI.Select(howToButton);
        }

        void OnQuitPressed()
        {
            // No Godot fechava o jogo; integrado, volta para o jogo principal
            game.ExitToMainGame();
        }
    }
}
