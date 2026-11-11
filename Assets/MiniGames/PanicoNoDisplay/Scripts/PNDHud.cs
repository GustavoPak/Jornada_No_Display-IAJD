using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PanicoNoDisplay
{
    /// <summary>
    /// Conversão de ui/hud.gd + ui/hud.tscn: tempo, pontos, vidas, música, botões de
    /// som/pause, tela de pause, janela "Salvar Arquivo" estilo Windows e tela de fim.
    /// </summary>
    public class PNDHud : MonoBehaviour
    {
        public float timeLeft = 60f;
        public int points;
        public int lives = 3;
        public bool gameEnded;
        public bool isPaused;
        public bool isSaveDialogOpen;

        public PNDGame game;

        TextMeshProUGUI timeLabel, pointsLabel, livesLabel, titleLabel, hintLabel, warningLabel;
        GameObject endScreen, pauseScreen, windowsSaveDialog;
        RectTransform windowPanel;
        PNDButton continueButton, restartButton, quitButton;
        PNDButton btnSalvar, btnNaoSalvar, btnCancelar, btnCloseX;
        PNDButton muteButton, pauseButton;
        AudioSource musicPlayer, collectSound;
        Coroutine shake;
        float panelOrigX;

        // ==========================================
        // Montagem (hud.tscn)
        // ==========================================
        public static PNDHud Create(Transform canvasRoot, PNDGame game)
        {
            var rt = PNDUI.Full(PNDUI.Rect("HUD", canvasRoot));
            var hud = rt.gameObject.AddComponent<PNDHud>();
            hud.game = game;
            hud.Build(rt);
            return hud;
        }

        void Build(RectTransform root)
        {
            // ---------- Barra superior ----------
            // MarginContainer (topo, altura 50, margens 20) > HBoxContainer
            var hbox = PNDUI.G(PNDUI.Rect("HBoxContainer", root), 0, 0, 1, 0, 20, 20, -20, 50);
            PNDUI.HBox(hbox, 4f, TextAnchor.MiddleLeft);

            livesLabel = PNDUI.Label("LivesLabel", hbox, "", 24, new Color(1f, 0.2f, 0.2f), TextAlignmentOptions.Left, 4, Color.black);
            timeLabel = PNDUI.Label("TimeLabel", hbox, "TEMPO: 60", 24, Color.white, TextAlignmentOptions.Left, 4, Color.black);
            PNDUI.Layout(timeLabel, flexW: 1); // size_flags_horizontal = EXPAND
            pointsLabel = PNDUI.Label("PointsLabel", hbox, "PONTOS: 0", 24, Color.white, TextAlignmentOptions.Right, 4, Color.black);

            SetupTopButtons(hbox);

            // ---------- Janela estilo Windows de Salvar Arquivo ----------
            var dlg = PNDUI.Full(PNDUI.Rect("WindowsSaveDialog", root));
            windowsSaveDialog = dlg.gameObject;
            PNDUI.ColorRect("Dimmer", dlg, new Color(0f, 0f, 0f, 0.4f));

            var winStyle = new PNDStyle
            {
                bg = new Color(0.933333f, 0.933333f, 0.933333f, 1f),
                radius = 4,
                shadowSize = 8,
                shadowColor = new Color(0f, 0f, 0f, 0.588235f),
            }.Border(new Color(0.25f, 0.25f, 0.25f, 1f), 1);
            var panelImg = PNDUI.StyledBox("WindowPanel", dlg, winStyle);
            windowPanel = PNDUI.G(panelImg.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, -210, -95, 210, 95);
            panelOrigX = windowPanel.anchoredPosition.x;

            var titleStyle = new PNDStyle { bg = new Color(0.0627451f, 0.313726f, 0.690196f, 1f), radius = 3, radiusBottom = 0 };
            var titleBar = PNDUI.StyledBox("TitleBar", windowPanel, titleStyle);
            PNDUI.G(titleBar.rectTransform, 0, 0, 1, 0, 0, 0, 0, 28);
            var wtitle = PNDUI.Label("TitleLabel", titleBar.transform, "Microsoft Word - Salvar Arquivo", 12, Color.white, TextAlignmentOptions.MidlineLeft);
            PNDUI.G(wtitle.rectTransform, 0, 0.5f, 0, 0.5f, 10, -9, 320, 9);

            btnCloseX = PNDUI.Button("CloseButton", titleBar.transform, "✕", 11);
            PNDUI.G((RectTransform)btnCloseX.transform, 1, 0.5f, 1, 0.5f, -26, -11, -4, 11);

            var vbox = PNDUI.G(PNDUI.Rect("VBox", windowPanel), 0, 0, 1, 1, 16, 36, -16, -12);
            PNDUI.VBox(vbox, 10f);

            var contentRow = PNDUI.Rect("ContentRow", vbox);
            PNDUI.HBox(contentRow, 12f, TextAnchor.UpperLeft);
            var iconRt = PNDUI.Rect("Icon", contentRow);
            var icon = iconRt.gameObject.AddComponent<Image>();
            var iconSpr = PND.Spr("art/ui/save_dialog_icon");
            icon.sprite = iconSpr;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            PNDUI.Layout(icon, 32, 32, 32, 32);
            var msg = PNDUI.Label("MessageLabel", contentRow,
                "Deseja salvar as alterações feitas em:\n\"Relatório_Final_v2_DEFINITIVO.docx\"?", 13, new Color(0.12f, 0.12f, 0.12f));
            PNDUI.Layout(msg, flexW: 1);

            warningLabel = PNDUI.Label("WarningLabel", vbox, "⚠ O tempo no laboratório continua correndo!", 12,
                new Color(0.85f, 0.1f, 0.1f), TextAlignmentOptions.Center);

            var buttonRow = PNDUI.Rect("ButtonRow", vbox);
            PNDUI.HBox(buttonRow, 14f, TextAnchor.MiddleCenter);

            var styleBtn = new PNDStyle { bg = new Color(0.88f, 0.88f, 0.88f, 1f), radius = 3 }.Border(new Color(0.45f, 0.45f, 0.45f, 1f), 1);
            var styleBtnSave = new PNDStyle { bg = new Color(0.85f, 0.9f, 0.98f, 1f), radius = 3 }.Border(new Color(0.15f, 0.45f, 0.85f, 1f), 2);

            btnSalvar = PNDUI.Button("BtnSalvar", buttonRow, "Salvar", 13, styleBtnSave, styleBtnSave, styleBtnSave);
            btnSalvar.fontNormal = btnSalvar.fontHover = btnSalvar.fontFocus = btnSalvar.fontPressed = btnSalvar.fontHoverPressed = Color.black;
            PNDUI.Layout(btnSalvar, 90, 28, 90, 28);

            // Só o estado "normal" foi personalizado no Godot; os outros usam o tema padrão
            btnNaoSalvar = PNDUI.Button("BtnNaoSalvar", buttonRow, "Não Salvar", 13, styleBtn);
            btnNaoSalvar.fontNormal = new Color(0.15f, 0.15f, 0.15f);
            PNDUI.Layout(btnNaoSalvar, 100, 28, 100, 28);

            btnCancelar = PNDUI.Button("BtnCancelar", buttonRow, "Cancelar", 13, styleBtn);
            btnCancelar.fontNormal = new Color(0.15f, 0.15f, 0.15f);
            PNDUI.Layout(btnCancelar, 85, 28, 85, 28);

            // ---------- Tela de pause ----------
            var pause = PNDUI.Full(PNDUI.Rect("PauseScreen", root));
            pauseScreen = pause.gameObject;
            PNDUI.ColorRect("ColorRect", pause, new Color(0f, 0f, 0f, 0.784314f));
            var pv = PNDUI.G(PNDUI.Rect("VBoxContainer", pause), 0.5f, 0.5f, 0.5f, 0.5f, -120, -110, 120, 110);
            PNDUI.VBox(pv, 14f);
            PNDUI.Label("PauseTitle", pv, "PAUSADO", 30, new Color(1f, 0.85f, 0.2f), TextAlignmentOptions.Center, 4, Color.black);
            continueButton = PNDUI.Button("ContinueButton", pv, "CONTINUAR", 20);
            restartButton = PNDUI.Button("RestartButton", pv, "REINICIAR", 20);
            quitButton = PNDUI.Button("QuitButton", pv, "SAIR", 20);

            // ---------- Tela de fim ----------
            var end = PNDUI.Full(PNDUI.Rect("EndScreen", root));
            endScreen = end.gameObject;
            PNDUI.ColorRect("ColorRect", end, new Color(0f, 0f, 0f, 0.784314f));
            var ev = PNDUI.G(PNDUI.Rect("VBoxContainer", end), 0.5f, 0.5f, 0.5f, 0.5f, -200, -50, 200, 50);
            PNDUI.VBox(ev, 20f, TextAnchor.MiddleCenter);
            // O conteúdo pode passar de 100 px de altura: cresce para os dois lados (grow_vertical = both)
            var fitter = ev.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            titleLabel = PNDUI.Label("TitleLabel", ev, "FIM DE JOGO", 32, Color.white, TextAlignmentOptions.Center);
            hintLabel = PNDUI.Label("HintLabel", ev, "Pressione ESPAÇO para reiniciar\nESC para voltar ao laboratório", 16, Color.white, TextAlignmentOptions.Center);

            // ---------- Sons ----------
            collectSound = PND.Audio(gameObject, "audio/sfx/powerUp2");
            SetupMusic();

            continueButton.onClick.AddListener(OnContinuePressed);
            restartButton.onClick.AddListener(OnRestartPressed);
            quitButton.onClick.AddListener(OnQuitPressed);

            // Conexões dos botões da janela de salvar estilo Windows
            btnSalvar.onClick.AddListener(OnSalvarPressed);
            btnNaoSalvar.onClick.AddListener(OnNaoSalvarPressed);
            btnCancelar.onClick.AddListener(OnCancelarPressed);
            btnCloseX.onClick.AddListener(OnCancelarPressed);

            UpdateLabels();
            endScreen.SetActive(false);
            pauseScreen.SetActive(false);
            windowsSaveDialog.SetActive(false);
        }

        // ==========================================
        // Entrada (P = pause, M = som) — funciona mesmo pausado
        // ==========================================
        void Update()
        {
            var kb = Keyboard.current;

            if (!gameEnded && kb != null)
            {
                if (kb.pKey.wasPressedThisFrame)
                    TogglePause();
                else if (kb.mKey.wasPressedThisFrame)
                    ToggleMute();
            }

            if (gameEnded)
            {
                if (kb != null && kb.spaceKey.isPressed)
                {
                    RestartGame();
                    return;
                }
                // Integração com o jogo principal: ESC volta para o laboratório
                if (kb != null && kb.escapeKey.wasPressedThisFrame)
                    game.ExitToMainGame();
                return;
            }

            if (isPaused)
                return;

            // O cronômetro NÃO para quando a janela de salvar abre!
            if (timeLeft > 0f)
            {
                timeLeft -= Time.deltaTime;
                UpdateLabels();
                if (isSaveDialogOpen)
                    warningLabel.text = "CORRA! Restam apenas " + (int)timeLeft + " segundos para salvar!";
                if (timeLeft <= 0f)
                {
                    if (isSaveDialogOpen)
                        windowsSaveDialog.SetActive(false);
                    ShowGameOver("Tempo Esgotado!\nO laboratório foi danificado e o relatório se perdeu!");
                }
            }
        }

        public void TogglePause()
        {
            if (gameEnded)
                return;
            isPaused = !isPaused;
            SetTreePaused(isPaused);
            if (isPaused)
            {
                pauseScreen.SetActive(true);
                PNDUI.Select(continueButton);
            }
            else
            {
                pauseScreen.SetActive(false);
                RestoreFocusAfterOverlay();
            }
        }

        void RestoreFocusAfterOverlay()
        {
            // Controles escondidos perdem o foco; se a janela de salvar estiver aberta, o foco volta para ela
            if (isSaveDialogOpen && windowsSaveDialog.activeSelf)
                PNDUI.Select(btnSalvar);
            else
                PNDUI.Select(null);
        }

        void OnContinuePressed()
        {
            if (isPaused)
                TogglePause();
        }

        void OnRestartPressed()
        {
            isPaused = false;
            RestartGame();
        }

        void OnQuitPressed()
        {
            isPaused = false;
            SetTreePaused(false);
            // No Godot fechava o jogo; integrado, volta para o jogo principal
            game.ExitToMainGame();
        }

        public void ShowWindowsSaveDialog()
        {
            if (gameEnded) return;
            isSaveDialogOpen = true;
            windowsSaveDialog.SetActive(true);
            PNDUI.Select(btnSalvar);
            // O cronômetro continua correndo em tempo real!
        }

        void OnSalvarPressed()
        {
            if (!isSaveDialogOpen || gameEnded) return;
            isSaveDialogOpen = false;
            windowsSaveDialog.SetActive(false);
            AddPoints(200); // Bônus especial de salvar com sucesso!
            ShowVictory();
        }

        void OnNaoSalvarPressed()
        {
            if (!isSaveDialogOpen || gameEnded) return;
            isSaveDialogOpen = false;
            windowsSaveDialog.SetActive(false);
            ShowGameOver("Você clicou em 'Não Salvar'!\nSeu relatório de pesquisa foi perdido!");
        }

        void OnCancelarPressed()
        {
            if (!isSaveDialogOpen || gameEnded) return;
            warningLabel.text = "ERRO: Cancelar não é uma opção! O tempo está acabando!";
            warningLabel.color = new Color(1f, 0.1f, 0.1f);

            // Efeito de tremor na janela
            if (shake != null) StopCoroutine(shake);
            shake = StartCoroutine(ShakePanel());
        }

        IEnumerator ShakePanel()
        {
            float orig = panelOrigX;
            float[] targets = { orig + 8f, orig - 8f, orig + 5f, orig };
            float from = windowPanel.anchoredPosition.x;
            foreach (var to in targets)
            {
                float t = 0f;
                while (t < 0.05f)
                {
                    t += Time.unscaledDeltaTime;
                    var p = windowPanel.anchoredPosition;
                    p.x = Mathf.Lerp(from, to, Mathf.Clamp01(t / 0.05f));
                    windowPanel.anchoredPosition = p;
                    yield return null;
                }
                from = to;
            }
            shake = null;
        }

        void UpdateLabels()
        {
            timeLabel.text = "TEMPO: " + (int)timeLeft;
            pointsLabel.text = "PONTOS: " + points;
            if (livesLabel != null)
                livesLabel.text = "VIDAS: " + lives + " ❤ ";
        }

        public void AddTime(float amount)
        {
            timeLeft += amount;
            UpdateLabels();
        }

        public void AddPoints(int amount)
        {
            points += amount;
            UpdateLabels();
            if (collectSound != null)
                collectSound.Play();
        }

        public bool TakeDamage()
        {
            lives -= 1;
            UpdateLabels();
            if (lives <= 0)
            {
                ShowGameOver("Ficou sem vidas!");
                return true;
            }
            return false;
        }

        public void ShowGameOver(string reason = "Você bateu!")
        {
            if (gameEnded) return;
            gameEnded = true;
            isPaused = false;
            isSaveDialogOpen = false;
            windowsSaveDialog.SetActive(false);
            pauseScreen.SetActive(false);
            titleLabel.text = "FIM DE JOGO!\n" + reason;
            titleLabel.color = new Color(1f, 0.2f, 0.2f);
            endScreen.SetActive(true);
            PNDUI.Select(null);
            SetTreePaused(true);
        }

        public void ShowVictory()
        {
            if (gameEnded) return;
            gameEnded = true;
            isPaused = false;
            isSaveDialogOpen = false;
            windowsSaveDialog.SetActive(false);
            pauseScreen.SetActive(false);

            // Recompensa no jogo principal: cada ponto vale 0.1 de dinheiro
            int reward = PanicoNoDisplayLauncher.RegisterVictory(points);

            titleLabel.text = "VITÓRIA!\nRelatório salvo antes do lab apagar!\nPontos: " + points + "\nRecompensa: $ " + reward;
            titleLabel.color = new Color(0.2f, 1f, 0.2f);
            endScreen.SetActive(true);
            PNDUI.Select(null);
            SetTreePaused(true);
        }

        void RestartGame()
        {
            SetTreePaused(false);
            game.RestartLevel();
        }

        /// <summary>get_tree().paused: para física, animações, timers da fase e a música.</summary>
        void SetTreePaused(bool paused)
        {
            Time.timeScale = paused ? 0f : 1f;
            if (musicPlayer != null)
            {
                if (paused) musicPlayer.Pause();
                else musicPlayer.UnPause();
            }
            if (game != null) game.SetLevelAudioPaused(paused);
        }

        // ==========================================
        // MÚSICA DA CORRIDA
        // ==========================================
        void SetupMusic()
        {
            musicPlayer = PND.Audio(gameObject, "audio/music/musica-corrida", -6f, loop: true); // Repete enquanto o jogo durar
            musicPlayer.Play(); // O personagem começa a correr assim que a fase carrega
        }

        // ==========================================
        // BOTÕES DE SOM E PAUSE (canto superior direito)
        // ==========================================
        void SetupTopButtons(RectTransform hbox)
        {
            // Sem foco: assim o ESPAÇO (pulo) não "aperta" o botão sem querer
            muteButton = PNDUI.Button("MuteButton", hbox, "", 18, focusable: false);
            PNDUI.Layout(muteButton, 36, 32, 36, 32);
            muteButton.tooltip = "Ligar/desligar o som (M)";
            muteButton.onClick.AddListener(ToggleMute);

            pauseButton = PNDUI.Button("PauseButton", hbox, "⏸", 18, focusable: false);
            PNDUI.Layout(pauseButton, 36, 32, 36, 32);
            pauseButton.tooltip = "Pausar (P)";
            pauseButton.onClick.AddListener(TogglePause);

            UpdateMuteButton();
        }

        public void ToggleMute()
        {
            // Muta tudo (música e efeitos) e continua mutado mesmo ao reiniciar a fase
            PND.Muted = !PND.Muted;
            PND.ApplyMute();
            UpdateMuteButton();
        }

        void UpdateMuteButton()
        {
            if (muteButton == null) return;
            muteButton.label.text = PND.Muted ? "🔇" : "🔊";
        }
    }
}
