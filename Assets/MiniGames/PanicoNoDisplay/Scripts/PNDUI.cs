using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PanicoNoDisplay
{
    /// <summary>Equivalente simplificado ao StyleBoxFlat do Godot.</summary>
    public class PNDStyle
    {
        public Color bg = Color.clear;
        public bool drawCenter = true;
        public Color border = Color.clear;
        public int borderL, borderT, borderR, borderB;
        public int radius;
        public int radiusBottom = -1; // -1 = igual a radius (Godot permite cantos diferentes)
        public int shadowSize;
        public Color shadowColor = Color.clear;
        public float expand; // expand_margin (todos os lados)

        public static PNDStyle Flat(Color bg, int radius = 0) => new PNDStyle { bg = bg, radius = radius };

        public PNDStyle Border(Color c, int w)
        {
            border = c;
            borderL = borderT = borderR = borderB = w;
            return this;
        }

        Sprite cached;
        public Sprite Sprite => cached != null ? cached : (cached = PNDUI.MakeStyleSprite(this));
    }

    /// <summary>
    /// Botão com os estados visuais do Godot (normal / hover / pressed / focus),
    /// incluindo o contorno de foco ao navegar pelo teclado.
    /// </summary>
    public class PNDButton : Button
    {
        [System.NonSerialized] public PNDStyle styleNormal, styleHover, stylePressed, styleFocus;
        public Color fontNormal = new Color(0.875f, 0.875f, 0.875f);
        public Color fontHover = new Color(0.95f, 0.95f, 0.95f);
        public Color fontPressed = Color.white;
        public Color fontHoverPressed = Color.white;
        public Color fontFocus = new Color(0.95f, 0.95f, 0.95f);
        public Image bgImage;
        public Image focusImage;
        public TextMeshProUGUI label;
        public string tooltip; // tooltip_text do Godot (aparece após 0,5s com o mouse em cima)
        bool pointerInside, pointerDown;
        float hoverTimer;
        RectTransform tooltipRt;
        PNDStyle lastStyle;

        public override void OnPointerEnter(PointerEventData eventData) { base.OnPointerEnter(eventData); pointerInside = true; }
        public override void OnPointerExit(PointerEventData eventData) { base.OnPointerExit(eventData); pointerInside = false; }
        public override void OnPointerDown(PointerEventData eventData) { base.OnPointerDown(eventData); if (eventData.button == PointerEventData.InputButton.Left) pointerDown = true; }
        public override void OnPointerUp(PointerEventData eventData) { base.OnPointerUp(eventData); if (eventData.button == PointerEventData.InputButton.Left) pointerDown = false; }

        protected override void OnDisable()
        {
            base.OnDisable();
            pointerInside = pointerDown = false;
        }

        void LateUpdate()
        {
            var es = EventSystem.current;
            bool focused = es != null && es.currentSelectedGameObject == gameObject;
            bool pressed = pointerDown && pointerInside;

            PNDStyle st = pressed ? stylePressed : pointerInside ? styleHover : styleNormal;
            if (bgImage != null && st != lastStyle)
            {
                lastStyle = st;
                bgImage.sprite = st != null ? st.Sprite : null;
                bgImage.enabled = st != null;
                if (st != null) PNDUI.ApplyExpand(bgImage.rectTransform, st.expand);
            }
            if (focusImage != null)
            {
                focusImage.enabled = focused && styleFocus != null;
            }
            if (!string.IsNullOrEmpty(tooltip))
            {
                hoverTimer = pointerInside && !pointerDown ? hoverTimer + Time.unscaledDeltaTime : 0f;
                bool show = hoverTimer >= 0.5f;
                if (show && tooltipRt == null) tooltipRt = PNDUI.Tooltip(transform, tooltip);
                if (tooltipRt != null && tooltipRt.gameObject.activeSelf != show) tooltipRt.gameObject.SetActive(show);
            }
            if (label != null)
            {
                label.color = pressed ? (pointerInside ? fontHoverPressed : fontPressed)
                    : pointerInside ? fontHover
                    : focused ? fontFocus
                    : fontNormal;
            }
        }
    }

    public static class PNDUI
    {
        public const int UILayer = 5;

        // ---------------- Tema padrão do Godot 4 ----------------
        public static readonly PNDStyle GodotButtonNormal = new PNDStyle { bg = new Color(0.1f, 0.1f, 0.1f, 0.6f), radius = 3 };
        public static readonly PNDStyle GodotButtonHover = new PNDStyle { bg = new Color(0.225f, 0.225f, 0.225f, 0.6f), radius = 3 };
        public static readonly PNDStyle GodotButtonPressed = new PNDStyle { bg = new Color(0f, 0f, 0f, 0.6f), radius = 3 };
        public static readonly PNDStyle GodotButtonFocus = new PNDStyle { drawCenter = false, radius = 3, expand = 2f }.Border(new Color(1f, 1f, 1f, 0.75f), 2);
        public static readonly PNDStyle GodotPanel = new PNDStyle { bg = new Color(0.1f, 0.1f, 0.1f, 0.6f) };

        // ---------------- Fonte ----------------
        static TMP_FontAsset font;
        static bool fontTried;

        /// <summary>
        /// Fonte com suporte aos símbolos usados no jogo (❤ ⏸ 🔊 🔇 ⚠ ✕ ➜ ◀ ▶ 💡 ↑ ↓).
        /// Usa as fontes do sistema (Segoe UI no Windows) e cai para a fonte padrão do TMP.
        /// </summary>
        public static TMP_FontAsset Font
        {
            get
            {
                if (fontTried) return font != null ? font : TMP_Settings.defaultFontAsset;
                fontTried = true;

                font = FromOS("segoeui.ttf", "Segoe UI", "Regular")
                       ?? FromOS("arial.ttf", "Arial", "Regular")
                       ?? FromOS("DejaVuSans.ttf", "DejaVu Sans", "Book");

                var fallbacks = new List<TMP_FontAsset>();
                void Add(TMP_FontAsset f) { if (f != null && f != font && !fallbacks.Contains(f)) fallbacks.Add(f); }
                Add(FromOS("seguisym.ttf", "Segoe UI Symbol", "Regular"));
                Add(FromOS("seguiemj.ttf", "Segoe UI Emoji", "Regular"));
                Add(FromOS("arialuni.ttf", "Arial Unicode MS", "Regular"));
                Add(FromOS("DejaVuSans.ttf", "DejaVu Sans", "Book"));
                Add(FromOS("NotoSansSymbols2-Regular.ttf", "Noto Sans Symbols2", "Regular"));
                Add(FromOS("Apple Symbols.ttf", "Apple Symbols", "Regular"));
                Add(TMP_Settings.defaultFontAsset);

                if (font != null)
                {
                    font.fallbackFontAssetTable = fallbacks;
                    return font;
                }
                return TMP_Settings.defaultFontAsset;
            }
        }

        static TMP_FontAsset FromOS(string fileName, string family, string style)
        {
            // 1) Pelo arquivo da fonte instalada no sistema
            try
            {
                foreach (var path in UnityEngine.Font.GetPathsToOSFonts())
                {
                    if (!string.Equals(Path.GetFileName(path), fileName, System.StringComparison.OrdinalIgnoreCase)) continue;
                    var f = new UnityEngine.Font(path);
                    var fa = TMP_FontAsset.CreateFontAsset(f);
                    if (fa != null)
                    {
                        fa.name = "PND_" + family;
                        return fa;
                    }
                }
            }
            catch (System.Exception) { }

            // 2) Pelo nome da família (versões do TMP que suportam)
            try
            {
                var m = typeof(TMP_FontAsset).GetMethod("CreateFontAsset", new[] { typeof(string), typeof(string), typeof(int) });
                if (m != null)
                {
                    var fa = m.Invoke(null, new object[] { family, style, 90 }) as TMP_FontAsset;
                    if (fa != null)
                    {
                        fa.name = "PND_" + family;
                        return fa;
                    }
                }
            }
            catch (System.Exception) { }
            return null;
        }

        // ---------------- RectTransform estilo Godot ----------------

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = UILayer;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>
        /// Aplica âncoras e offsets como no Godot (anchor_left/top/right/bottom e
        /// offset_left/top/right/bottom, com Y para baixo).
        /// </summary>
        public static RectTransform G(RectTransform rt, float aL, float aT, float aR, float aB, float oL, float oT, float oR, float oB)
        {
            rt.anchorMin = new Vector2(aL, 1f - aB);
            rt.anchorMax = new Vector2(aR, 1f - aT);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(oL, -oB);
            rt.offsetMax = new Vector2(oR, -oT);
            return rt;
        }

        public static RectTransform Full(RectTransform rt) => G(rt, 0, 0, 1, 1, 0, 0, 0, 0);

        public static void ApplyExpand(RectTransform rt, float e)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-e, -e);
            rt.offsetMax = new Vector2(e, e);
        }

        public static Image ColorRect(string name, Transform parent, Color color, bool blocksMouse = true)
        {
            var rt = Full(Rect(name, parent));
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = blocksMouse;
            return img;
        }

        /// <summary>
        /// Painel com StyleBoxFlat. Retorna a Image raiz (transparente, bloqueia o mouse);
        /// a sombra e o fundo ficam como primeiros filhos, então o conteúdo adicionado depois fica por cima.
        /// </summary>
        public static Image StyledBox(string name, Transform parent, PNDStyle st, bool blocksMouse = true)
        {
            var rt = Rect(name, parent);
            var rootImg = rt.gameObject.AddComponent<Image>();
            rootImg.color = Color.clear;
            rootImg.raycastTarget = blocksMouse;

            if (st.shadowSize > 0)
            {
                var sh = Rect("Shadow", rt);
                ApplyExpand(sh, st.shadowSize);
                var shImg = sh.gameObject.AddComponent<Image>();
                shImg.sprite = MakeShadowSprite(st);
                shImg.type = Image.Type.Sliced;
                shImg.raycastTarget = false;
            }
            var bgRt = Rect("Style", rt);
            ApplyExpand(bgRt, st.expand);
            var img = bgRt.gameObject.AddComponent<Image>();
            img.sprite = st.Sprite;
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            return rootImg;
        }

        public static TextMeshProUGUI Label(string name, Transform parent, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.TopLeft, float outlineSize = 0f, Color? outlineColor = null, bool wrap = false)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false; // Label no Godot ignora o mouse
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.richText = false;
            if (outlineSize > 0f) SetOutline(t, outlineSize, outlineColor ?? Color.black);
            return t;
        }

        public static void SetOutline(TextMeshProUGUI t, float outlineSize, Color c)
        {
            // outline_size do Godot é a espessura total em pixels; no TMP a espessura é relativa ao tamanho da fonte.
            float w = Mathf.Clamp(outlineSize / (t.fontSize * 0.2f) * 0.5f, 0.05f, 0.6f);
            t.outlineWidth = w;
            t.outlineColor = c;
            t.fontMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, w * 0.5f);
        }

        public static PNDButton Button(string name, Transform parent, string text, float fontSize,
            PNDStyle normal = null, PNDStyle hover = null, PNDStyle pressed = null, PNDStyle focus = null, bool focusable = true)
        {
            var rt = Rect(name, parent);
            var bgRt = Rect("Background", rt);
            ApplyExpand(bgRt, 0);
            var bg = bgRt.gameObject.AddComponent<Image>();
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = true;

            var focusRt = Rect("Focus", rt);
            var focusSt = focus ?? GodotButtonFocus;
            ApplyExpand(focusRt, focusSt.expand);
            var fimg = focusRt.gameObject.AddComponent<Image>();
            fimg.sprite = focusSt.Sprite;
            fimg.type = Image.Type.Sliced;
            fimg.raycastTarget = false;
            fimg.enabled = false;

            var label = Label("Label", rt, text, fontSize, new Color(0.875f, 0.875f, 0.875f), TextAlignmentOptions.Center);
            ApplyExpand(label.rectTransform, 0);

            // O Button precisa de um Graphic no próprio objeto para receber o mouse
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;
            bg.raycastTarget = false;

            var b = rt.gameObject.AddComponent<PNDButton>();
            b.transition = Selectable.Transition.None;
            b.targetGraphic = hit;
            b.styleNormal = normal ?? GodotButtonNormal;
            b.styleHover = hover ?? GodotButtonHover;
            b.stylePressed = pressed ?? GodotButtonPressed;
            b.styleFocus = focusSt;
            b.bgImage = bg;
            b.focusImage = fimg;
            b.label = label;
            if (!focusable)
            {
                // focus_mode = FOCUS_NONE: o mouse não "seleciona" o botão (ESPAÇO não o aperta)
                var nav = b.navigation;
                nav.mode = Navigation.Mode.None;
                b.navigation = nav;
            }
            bg.sprite = b.styleNormal.Sprite;

            // Tamanho mínimo como no Godot: texto + margens do StyleBox
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.minHeight = Mathf.Ceil(fontSize * 1.4f) + 8f;
            le.preferredHeight = le.minHeight;
            le.preferredWidth = label.GetPreferredValues(text).x + 16f;
            return b;
        }

        static readonly PNDStyle TooltipStyle = new PNDStyle { bg = new Color(0.12f, 0.12f, 0.12f, 0.95f), radius = 3 }.Border(new Color(0.4f, 0.4f, 0.4f, 1f), 1);

        /// <summary>Balão de dica embaixo do botão (fora do layout do contêiner).</summary>
        public static RectTransform Tooltip(Transform button, string text)
        {
            var box = StyledBox("Tooltip", button, TooltipStyle, false);
            var rt = box.rectTransform;
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
            var label = Label("Label", rt, text, 12, new Color(0.9f, 0.9f, 0.9f), TextAlignmentOptions.Center);
            Vector2 pref = label.GetPreferredValues(text);
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(pref.x + 12f, pref.y + 6f);
            rt.anchoredPosition = new Vector2(0f, -4f);
            ApplyExpand(label.rectTransform, 0f);
            // Garante que fique por cima dos outros itens da barra
            var cv = rt.gameObject.AddComponent<Canvas>();
            cv.overrideSorting = true;
            cv.sortingOrder = 32100;
            return rt;
        }

        public static void Select(Selectable s)
        {
            var es = EventSystem.current;
            if (es == null) return;
            es.SetSelectedGameObject(s != null ? s.gameObject : null);
        }

        // ---------------- Geração das texturas de StyleBox ----------------

        static float SdRoundRect(float px, float py, float x0, float y0, float x1, float y1, float r)
        {
            float cx = Mathf.Clamp(px, x0 + r, x1 - r);
            float cy = Mathf.Clamp(py, y0 + r, y1 - r);
            float dx = px - cx, dy = py - cy;
            bool inCornerX = px < x0 + r || px > x1 - r;
            bool inCornerY = py < y0 + r || py > y1 - r;
            if (inCornerX && inCornerY && r > 0f)
                return Mathf.Sqrt(dx * dx + dy * dy) - r;
            // distância a um retângulo comum (negativa dentro)
            float ddx = Mathf.Max(x0 - px, px - x1);
            float ddy = Mathf.Max(y0 - py, py - y1);
            return Mathf.Max(ddx, ddy);
        }

        public static Sprite MakeStyleSprite(PNDStyle st)
        {
            int l = Mathf.Max(st.radius, st.borderL) + 1;
            int r = Mathf.Max(st.radius, st.borderR) + 1;
            int t = Mathf.Max(st.radius, st.borderT) + 1;
            int b = Mathf.Max(st.radius, st.borderB) + 1;
            int w = l + r + 2, h = t + b + 2;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Y do Godot (para baixo): linha 0 da textura é a de baixo
                    float gx = x + 0.5f, gy = (h - 1 - y) + 0.5f;
                    int rad = (gy > h * 0.5f && st.radiusBottom >= 0) ? st.radiusBottom : st.radius;
                    float outer = SdRoundRect(gx, gy, 0, 0, w, h, rad);
                    float rin = Mathf.Max(0f, rad - Mathf.Max(Mathf.Max(st.borderL, st.borderR), Mathf.Max(st.borderT, st.borderB)));
                    float inner = SdRoundRect(gx, gy, st.borderL, st.borderT, w - st.borderR, h - st.borderB, rin);
                    float ao = Mathf.Clamp01(0.5f - outer);
                    float ai = Mathf.Clamp01(0.5f - inner);
                    float k = ao > 0f ? Mathf.Clamp01(ai / ao) : 0f;
                    Color c;
                    if (st.drawCenter)
                    {
                        c = Color.Lerp(st.border, st.bg, k);
                        c.a = ao * Mathf.Lerp(st.border.a, st.bg.a, k);
                    }
                    else
                    {
                        c = st.border;
                        c.a = ao * st.border.a * (1f - k);
                    }
                    px[y * w + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(l, b, r, t));
        }

        static Sprite MakeShadowSprite(PNDStyle st)
        {
            int s = st.shadowSize;
            int c = st.radius + s + 1;
            int w = c * 2 + 2, h = c * 2 + 2;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = SdRoundRect(x + 0.5f, y + 0.5f, s, s, w - s, h - s, st.radius);
                    float a = d <= 0f ? 1f : Mathf.Clamp01(1f - d / s);
                    a = a * a;
                    var col = st.shadowColor;
                    col.a *= a;
                    px[y * w + x] = col;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(c, c, c, c));
        }

        // ---------------- Contêineres estilo Godot ----------------

        public static VerticalLayoutGroup VBox(RectTransform rt, float separation, TextAnchor align = TextAnchor.UpperLeft)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = separation;
            v.childAlignment = align;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return v;
        }

        public static HorizontalLayoutGroup HBox(RectTransform rt, float separation, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = separation;
            h.childAlignment = align;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            return h;
        }

        public static LayoutElement Layout(Component c, float minW = -1, float minH = -1, float prefW = -1, float prefH = -1, float flexW = -1, float flexH = -1)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            le.minWidth = minW;
            le.minHeight = minH;
            le.preferredWidth = prefW;
            le.preferredHeight = prefH;
            le.flexibleWidth = flexW;
            le.flexibleHeight = flexH;
            return le;
        }
    }
}
