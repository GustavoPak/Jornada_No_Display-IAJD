using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PanicoNoDisplay
{
    /// <summary>
    /// Conversão de scenes/cutscene/intro_cutscene.gd + .tscn.
    /// O discente derruba café no servidor, curto-circuito, explosão, alarme de 60 segundos
    /// e ele sai correndo para salvar o relatório. Pode ser pulada com ESPAÇO / ENTER / ESC / clique.
    /// </summary>
    public class PNDCutscene : MonoBehaviour
    {
        bool hasSkipped;
        bool started;
        float shakeIntensity;
        bool alarmActive;

        PNDGame game;
        Camera cam;

        SpriteRenderer playerSprite, deskSprite, monitorSprite, keyboardSprite, mugSprite, smokeSprite, sirenSprite;
        Image redAlarmRect, fadeRect;
        GameObject dialoguePanel, alertBanner;
        TextMeshProUGUI dialogueLabel, alertSublabel;

        AudioSource sfxTyping, sfxCup, sfxZap, sfxExplosion, sfxAlarm, sfxRun;

        // Texturas do Monitor e Caneca
        Sprite TEX_SCREEN_WORKING, TEX_SCREEN_GLITCH, TEX_SCREEN_ALERT, TEX_MUG_UP, TEX_MUG_SPILL;
        // Texturas do Player
        Sprite TEX_PLAYER_STAND, TEX_PLAYER_TALK, TEX_PLAYER_HURT, TEX_PLAYER_JUMP, TEX_PLAYER_WALK1, TEX_PLAYER_WALK2;

        GameObject uiRoot;

        public static PNDCutscene Create(Transform worldRoot, Transform canvasRoot, PNDGame game, Camera cam)
        {
            var c = worldRoot.gameObject.AddComponent<PNDCutscene>();
            c.game = game;
            c.cam = cam;
            c.Build(worldRoot, canvasRoot);
            return c;
        }

        // Godot: Sprite2D em (x, y) centralizado
        SpriteRenderer Spr(string name, Transform parent, string path, float x, float y, int order, float scale = 1f)
        {
            var sr = PND.MakeSprite(name, parent, PND.Spr(path), PND.G(x, y), order);
            sr.transform.localScale = new Vector3(scale, scale, 1f);
            return sr;
        }

        void Build(Transform world, Transform canvas)
        {
            TEX_SCREEN_WORKING = PND.Spr("art/cutscene/screen_working");
            TEX_SCREEN_GLITCH = PND.Spr("art/cutscene/screen_glitch");
            TEX_SCREEN_ALERT = PND.Spr("art/cutscene/screen_alert");
            TEX_MUG_UP = PND.Spr("art/cutscene/coffee_mug_up");
            TEX_MUG_SPILL = PND.Spr("art/cutscene/coffee_spill");
            TEX_PLAYER_STAND = PND.Spr("art/player/hero/player_stand");
            TEX_PLAYER_TALK = PND.Spr("art/player/hero/player_talk");
            TEX_PLAYER_HURT = PND.Spr("art/player/hero/player_hurt");
            TEX_PLAYER_JUMP = PND.Spr("art/player/hero/player_jump");
            TEX_PLAYER_WALK1 = PND.Spr("art/player/hero/player_run1");
            TEX_PLAYER_WALK2 = PND.Spr("art/player/hero/player_run4");

            // ----- Cenário (mesma ordem de desenho do .tscn) -----
            PND.MakeSpriteTopLeft("Background", world, PND.Spr("art/environment/display_lab_background_clean"), 0, -12, new Vector2(0.5f, 0.5f), 0);

            // Floor: region 640x128 a partir de (0, 275), canto superior esquerdo
            var floor = PND.MakeSprite("Floor", world, PND.Spr("art/environment/lab_floor_tile_128"), PND.G(320, 275 + 64), 1);
            floor.drawMode = SpriteDrawMode.Tiled;
            floor.tileMode = SpriteTileMode.Continuous;
            floor.size = new Vector2(640, 128);

            sirenSprite = Spr("SirenSprite", world, "art/cutscene/alarm_siren", 320, 12, 2, 1.5f);
            deskSprite = Spr("DeskSprite", world, "art/cutscene/lab_desk", 370, 240, 3);
            monitorSprite = Spr("MonitorSprite", world, "art/cutscene/screen_working", 385, 186, 4);
            keyboardSprite = Spr("KeyboardSprite", world, "art/cutscene/keyboard", 335, 222, 5);
            mugSprite = Spr("MugSprite", world, "art/cutscene/coffee_mug_up", 308, 218, 6);
            smokeSprite = Spr("SmokeSprite", world, "art/cutscene/explosion_puff", 385, 180, 7);
            playerSprite = Spr("PlayerSprite", world, "art/player/hero/player_stand", 240, 252, 8, 0.5f);

            // ----- Sons -----
            var sounds = PND.Node("Sounds", world, Vector3.zero);
            sfxTyping = PND.Audio(sounds, "audio/sfx/footstep_wood_000", -6f);
            sfxCup = PND.Audio(sounds, "audio/sfx/impactGlass_light_000", 2f);
            sfxZap = PND.Audio(sounds, "audio/sfx/zapTwoTone", 4f);
            sfxExplosion = PND.Audio(sounds, "audio/sfx/impactMetal_heavy_000", 6f);
            sfxAlarm = PND.Audio(sounds, "audio/sfx/twoTone1", 3f);
            sfxRun = PND.Audio(sounds, "audio/sfx/phaseJump1", 0f);

            // ----- CanvasLayer -----
            var ui = PNDUI.Full(PNDUI.Rect("CutsceneUI", canvas));
            uiRoot = ui.gameObject;

            redAlarmRect = PNDUI.ColorRect("RedAlarmOverlay", ui, new Color(1f, 0f, 0f, 0f), false);

            var dp = PNDUI.StyledBox("DialoguePanel", ui, PNDUI.GodotPanel, false);
            PNDUI.G(dp.rectTransform, 0.5f, 1f, 0.5f, 1f, -260, -75, 260, -15);
            dialoguePanel = dp.gameObject;
            dialogueLabel = PNDUI.Label("Label", dp.transform,
                "Relatório de pesquisa quase pronto lá no meu PC...\nVou só dar uma olhada no servidor do lab e tomar um café.",
                15, Color.white, TextAlignmentOptions.Center, 4, Color.black, true);
            PNDUI.Full(dialogueLabel.rectTransform);

            var banner = PNDUI.ColorRect("AlertBanner", ui, new Color(0.12f, 0.02f, 0.02f, 0.9f), false);
            PNDUI.G(banner.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, -270, -80, 270, 45);
            alertBanner = banner.gameObject;
            var bv = PNDUI.G(PNDUI.Rect("VBox", banner.transform), 0, 0, 1, 1, 10, 10, -10, -10);
            PNDUI.VBox(bv, 8f);
            PNDUI.Label("TitleLabel", bv, "⚠ ALERTA: CURTO-CIRCUITO NO LABORATÓRIO ⚠", 20, new Color(1f, 0.2f, 0.2f),
                TextAlignmentOptions.Center, 6, Color.black);
            alertSublabel = PNDUI.Label("SubLabel", bv,
                "O CURTO-CIRCUITO ESTÁ SE ESPALHANDO PELO LABORATÓRIO!\nEM 60 SEGUNDOS O LAB VAI SER TOTALMENTE DANIFICADO!",
                14, new Color(1f, 0.9f, 0.4f), TextAlignmentOptions.Center, 4, Color.black, true);

            var skip = PNDUI.Label("SkipLabel", ui, "[ESPAÇO / ENTER] Pular Introdução", 12, new Color(0.9f, 0.9f, 0.9f, 0.7f),
                TextAlignmentOptions.TopRight, 3, new Color(0f, 0f, 0f, 0.8f));
            PNDUI.G(skip.rectTransform, 1, 0, 1, 0, -220, 10, -12, 35);

            fadeRect = PNDUI.ColorRect("FadeRect", ui, new Color(0f, 0f, 0f, 0f), false);

            // _ready()
            smokeSprite.gameObject.SetActive(false);
            alertBanner.SetActive(false);
            dialoguePanel.SetActive(true);
        }

        void Start()
        {
            StartCoroutine(PlayCutscene());
        }

        void OnDestroy()
        {
            if (uiRoot != null) Destroy(uiRoot);
        }

        void Update()
        {
            // _input: pular a introdução
            if (!hasSkipped)
            {
                var kb = Keyboard.current;
                var mouse = Mouse.current;
                bool key = kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame);
                bool click = mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame);
                if (key || click)
                    SkipCutscene();
            }

            // Camera shake suave
            Vector2 offset = Vector2.zero;
            if (shakeIntensity > 0f)
            {
                offset = new Vector2(Random.Range(-shakeIntensity, shakeIntensity), Random.Range(-shakeIntensity, shakeIntensity));
                shakeIntensity = Mathf.Max(0f, shakeIntensity - Time.deltaTime * 15f);
            }
            if (cam != null)
                cam.transform.position = new Vector3(320f + offset.x, -(180f + offset.y), -10f);

            // Piscar da luz vermelha de emergência
            if (alarmActive)
            {
                float pulse = (Mathf.Sin(Time.realtimeSinceStartup * 1000f * 0.012f) + 1f) * 0.5f;
                var c = redAlarmRect.color;
                c.a = pulse * 0.4f;
                redAlarmRect.color = c;
                PND.SetTint(sirenSprite, new Color(1f, 0.4f + 0.6f * pulse, 0.4f + 0.6f * pulse));
            }
        }

        IEnumerator PlayCutscene()
        {
            // --- CENA 1 (~4.0s): O discente mexendo no servidor do lab, com o café ao lado ---
            playerSprite.sprite = TEX_PLAYER_STAND;
            monitorSprite.sprite = TEX_SCREEN_WORKING;
            mugSprite.sprite = TEX_MUG_UP;
            dialogueLabel.text = "Relatório de pesquisa quase pronto lá no meu PC...\nVou só dar uma olhada no servidor do lab e tomar um café.";

            sfxTyping.Play();
            yield return Wait(2.0f);
            if (hasSkipped) yield break;

            playerSprite.sprite = TEX_PLAYER_TALK;
            yield return Wait(2.0f);
            if (hasSkipped) yield break;
            sfxTyping.Stop();

            // --- CENA 2 (~5.0s): O discente derrama café no servidor ---
            dialogueLabel.text = "Eita, bati o braço! O CAFÉ NO SERVIDOR NÃO...!!";
            playerSprite.sprite = TEX_PLAYER_JUMP;
            MoveY(playerSprite, -12f);

            // Caneca tomba
            mugSprite.sprite = TEX_MUG_SPILL;
            mugSprite.transform.localPosition = PND.G(330, 246);
            sfxCup.Play();

            // Tempo para ler o susto da caneca caindo
            yield return Wait(2.0f);
            if (hasSkipped) yield break;

            MoveY(playerSprite, 12f);
            playerSprite.sprite = TEX_PLAYER_HURT;
            shakeIntensity = 3.0f;

            // Curto elétrico e pedido de socorro
            sfxZap.Play();
            monitorSprite.sprite = TEX_SCREEN_GLITCH;
            dialogueLabel.text = "CURTO-CIRCUITO NO SERVIDOR!!\nAS FAÍSCAS ESTÃO SE ESPALHANDO!!";

            // Monitor pisca enquanto o jogador lê a fala
            for (int i = 0; i < 7; i++)
            {
                PND.SetTint(monitorSprite, new Color(2f, 2f, 0.5f));
                yield return Wait(0.2f);
                if (hasSkipped) yield break;
                PND.SetTint(monitorSprite, Color.white);
                yield return Wait(0.2f);
                if (hasSkipped) yield break;
            }

            // --- CENA 3 (~3.5s): A explosão do servidor ---
            shakeIntensity = 10.0f;
            sfxExplosion.Play();
            smokeSprite.gameObject.SetActive(true);
            smokeSprite.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            StartCoroutine(SmokeTween());

            monitorSprite.sprite = TEX_SCREEN_ALERT;
            playerSprite.sprite = TEX_PLAYER_HURT;
            dialogueLabel.text = "KABOOOOM! O SERVIDOR EXPLODIU!!";

            // Tempo para ler o susto da explosão
            yield return Wait(3.4f);
            if (hasSkipped) yield break;

            // --- CENA 4 (~4.5s): Alarme: o curto-circuito vai danificar o lab em 60 segundos ---
            alarmActive = true;
            sfxAlarm.Play();
            dialoguePanel.SetActive(false);
            alertBanner.SetActive(true);
            alertSublabel.text = "O CURTO-CIRCUITO ESTÁ SE ESPALHANDO PELO LABORATÓRIO!\nEM 60 SEGUNDOS O LAB VAI SER TOTALMENTE DANIFICADO!";

            playerSprite.sprite = TEX_PLAYER_HURT;
            shakeIntensity = 2.0f;

            // Tempo para ler com calma todo o banner de emergência
            yield return Wait(4.2f);
            if (hasSkipped) yield break;

            // --- CENA 5 (~3.5s): O discente lembra do relatório não salvo e sai correndo ---
            alertBanner.SetActive(false);
            dialoguePanel.SetActive(true);
            dialogueLabel.text = "MEU RELATÓRIO TÁ ABERTO NO PC E EU NÃO SALVEI!!\nPRECISO CHEGAR LÁ ANTES QUE O LAB APAGUE!!";

            // Dá 2 segundos para ler a decisão antes do arranque da corrida
            yield return Wait(2.0f);
            if (hasSkipped) yield break;

            sfxRun.Play();
            // Olha para a direita (flip_h = false)

            float startX = playerSprite.transform.localPosition.x;
            float targetX = 690f;
            float runDuration = 1.4f;
            float elapsed = 0f;

            while (elapsed < runDuration && !hasSkipped)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / runDuration;
                var p = playerSprite.transform.localPosition;
                p.x = Mathf.LerpUnclamped(startX, targetX, t);
                playerSprite.transform.localPosition = p;
                // Alterna os frames de corrida
                playerSprite.sprite = ((int)(elapsed * 10f)) % 2 == 0 ? TEX_PLAYER_WALK1 : TEX_PLAYER_WALK2;
                yield return null;
            }

            StartGame();
        }

        static void MoveY(SpriteRenderer sr, float godotDy)
        {
            var p = sr.transform.localPosition;
            p.y -= godotDy; // Y do Godot é para baixo
            sr.transform.localPosition = p;
        }

        IEnumerator SmokeTween()
        {
            // scale 0.5 -> 1.5 em 0.8s, em paralelo com o alpha 1 -> 0 em 1.2s
            float t = 0f;
            while (t < 1.2f)
            {
                t += Time.deltaTime;
                float s = Mathf.Lerp(0.5f, 1.5f, Mathf.Clamp01(t / 0.8f));
                smokeSprite.transform.localScale = new Vector3(s, s, 1f);
                PND.SetTint(smokeSprite, new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, Mathf.Clamp01(t / 1.2f))));
                yield return null;
            }
        }

        IEnumerator Wait(float sec)
        {
            if (hasSkipped) yield break;
            float t = 0f;
            while (t < sec)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        void SkipCutscene()
        {
            if (hasSkipped)
                return;
            hasSkipped = true;
            StartGame();
        }

        void StartGame()
        {
            if (started) return;
            started = true;
            StartCoroutine(FadeAndStart());
        }

        IEnumerator FadeAndStart()
        {
            // Fade suave para a transição
            if (sfxAlarm.isPlaying)
                sfxAlarm.Stop();
            float t = 0f;
            float from = fadeRect.color.a;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                var c = fadeRect.color;
                c.a = Mathf.Lerp(from, 1f, Mathf.Clamp01(t / 0.3f));
                fadeRect.color = c;
                yield return null;
            }
            game.OnCutsceneFinished();
        }
    }
}
