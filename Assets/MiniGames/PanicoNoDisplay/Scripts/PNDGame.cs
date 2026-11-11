using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PanicoNoDisplay
{
    /// <summary>
    /// Raiz do minijogo. Cria câmera, Canvas (640x360, como o viewport do Godot),
    /// EventSystem e troca entre as "cenas" originais:
    /// menu (main_menu.tscn) → cutscene (intro_cutscene.tscn) → fase (main.tscn).
    /// </summary>
    public class PNDGame : MonoBehaviour
    {
        public const string CutsceneSeenKey = "PanicoNoDisplay_CutsceneVista";
        const float ViewW = 640f, ViewH = 360f;

        public Camera cam;
        Camera letterboxCam;
        Canvas canvas;
        RectTransform canvasRoot;

        GameObject currentWorld;
        GameObject currentUI;

        /// <summary>Quando true, as setas não movem o foco (usado no "Como Jogar" para trocar abas).</summary>
        public bool ConsumeArrows;
        GameObject pendingAccept;

        void Awake()
        {
            // ----- Câmeras -----
            var lb = new GameObject("LetterboxCamera");
            lb.transform.SetParent(transform, false);
            letterboxCam = lb.AddComponent<Camera>();
            letterboxCam.clearFlags = CameraClearFlags.SolidColor;
            letterboxCam.backgroundColor = Color.black;
            letterboxCam.cullingMask = 0;
            letterboxCam.depth = -100;
            letterboxCam.orthographic = true;

            var camGo = new GameObject("Camera2D");
            camGo.transform.SetParent(transform, false);
            cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = ViewH * 0.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f); // cor de fundo padrão do Godot
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cam.depth = 0;
            cam.transform.position = new Vector3(ViewW * 0.5f, -ViewH * 0.5f, -10f);
            camGo.AddComponent<AudioListener>();

            // ----- Canvas (CanvasLayer do Godot) -----
            var canvasGo = new GameObject("Canvas");
            canvasGo.layer = PNDUI.UILayer;
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 5f;
            canvas.sortingOrder = 32000;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ViewW, ViewH);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
            canvasGo.AddComponent<GraphicRaycaster>();
            canvasRoot = (RectTransform)canvasGo.transform;

            // ----- EventSystem -----
            var esGo = new GameObject("EventSystem");
            esGo.transform.SetParent(transform, false);
            esGo.AddComponent<EventSystem>();
            var module = esGo.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
            // A navegação por teclado é feita aqui (igual ao Godot: ESPAÇO/ENTER apertam o botão com foco)
            module.move = null;
            module.submit = null;
            module.cancel = null;

            PND.ApplyMute();
            UpdateLetterbox();
        }

        void Start()
        {
            ShowMenu();
        }

        void Update()
        {
            UpdateLetterbox();
            KeyboardNavigation();
        }

        // Mantém a proporção 16:9 do jogo original (stretch aspect "keep")
        void UpdateLetterbox()
        {
            float target = ViewW / ViewH;
            float win = Screen.height > 0 ? (float)Screen.width / Screen.height : target;
            Rect r;
            if (win > target)
            {
                float w = target / win;
                r = new Rect((1f - w) * 0.5f, 0f, w, 1f);
            }
            else
            {
                float h = win / target;
                r = new Rect(0f, (1f - h) * 0.5f, 1f, h);
            }
            if (cam.rect != r) cam.rect = r;
        }

        // Foco de teclado dos botões (ui_up/ui_down/ui_left/ui_right e ui_accept do Godot)
        void KeyboardNavigation()
        {
            var es = EventSystem.current;
            var kb = Keyboard.current;
            if (es == null || kb == null) return;

            var sel = es.currentSelectedGameObject;
            if (sel != null && !sel.activeInHierarchy)
            {
                es.SetSelectedGameObject(null);
                sel = null;
            }
            if (sel == null)
            {
                pendingAccept = null;
                return;
            }

            var s = sel.GetComponent<Selectable>();
            if (s == null || !s.IsInteractable()) return;

            if (!ConsumeArrows)
            {
                Selectable next = null;
                if (kb.upArrowKey.wasPressedThisFrame) next = s.FindSelectableOnUp();
                else if (kb.downArrowKey.wasPressedThisFrame) next = s.FindSelectableOnDown();
                else if (kb.leftArrowKey.wasPressedThisFrame) next = s.FindSelectableOnLeft();
                else if (kb.rightArrowKey.wasPressedThisFrame) next = s.FindSelectableOnRight();
                if (next != null)
                {
                    es.SetSelectedGameObject(next.gameObject);
                    pendingAccept = null;
                    return;
                }
            }

            // O botão do Godot dispara ao SOLTAR a tecla (action_mode = BUTTON_RELEASE)
            if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                pendingAccept = sel;

            bool released = kb.spaceKey.wasReleasedThisFrame || kb.enterKey.wasReleasedThisFrame || kb.numpadEnterKey.wasReleasedThisFrame;
            if (released && pendingAccept == sel)
            {
                pendingAccept = null;
                var b = sel.GetComponent<Button>();
                if (b != null) b.onClick.Invoke();
            }
        }

        // ---------------- Troca de "cenas" ----------------

        void Clear()
        {
            Time.timeScale = 1f;
            PNDUI.Select(null);
            pendingAccept = null;
            ConsumeArrows = false;
            if (currentWorld != null)
            {
                currentWorld.SetActive(false);
                Destroy(currentWorld);
                currentWorld = null;
            }
            if (currentUI != null)
            {
                currentUI.SetActive(false);
                Destroy(currentUI);
                currentUI = null;
            }
            cam.transform.position = new Vector3(ViewW * 0.5f, -ViewH * 0.5f, -10f);
        }

        GameObject NewWorld(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go;
        }

        public void ShowMenu()
        {
            Clear();
            currentUI = PNDMainMenu.Create(canvasRoot, this).gameObject;
        }

        public void OnPlayPressed()
        {
            // A cutscene aparece só na primeira vez (e sempre pode ser pulada)
            if (PlayerPrefs.GetInt(CutsceneSeenKey, 0) == 0)
                ShowCutscene();
            else
                StartLevel();
        }

        public void ShowCutscene()
        {
            Clear();
            currentWorld = NewWorld("IntroCutscene");
            PNDCutscene.Create(currentWorld.transform, canvasRoot, this, cam);
        }

        public void OnCutsceneFinished()
        {
            PlayerPrefs.SetInt(CutsceneSeenKey, 1);
            PlayerPrefs.Save();
            StartLevel();
        }

        public void StartLevel()
        {
            Clear();
            currentWorld = NewWorld("Main");
            var hud = PNDHud.Create(canvasRoot, this);
            currentUI = hud.gameObject;
            PNDLevel.Build(currentWorld.transform, hud, cam);
        }

        /// <summary>get_tree().reload_current_scene()</summary>
        public void RestartLevel()
        {
            StartLevel();
        }

        /// <summary>Pausa/retoma os sons da fase (no Godot eles param junto com a árvore).</summary>
        public void SetLevelAudioPaused(bool paused)
        {
            if (currentWorld == null) return;
            foreach (var a in currentWorld.GetComponentsInChildren<AudioSource>())
            {
                if (paused) a.Pause();
                else a.UnPause();
            }
        }

        public void ExitToMainGame()
        {
            PanicoNoDisplayLauncher.Exit();
        }
    }
}
