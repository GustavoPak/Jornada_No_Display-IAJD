using System.Collections.Generic;
using UnityEngine;

namespace PanicoNoDisplay
{
    /// <summary>
    /// Utilitários centrais do minijogo: carregamento de artes/sons (Resources),
    /// conversão de coordenadas do Godot (Y para baixo) para Unity (Y para cima),
    /// "modulate" de sprites e estado global de som.
    ///
    /// Escala: 1 unidade Unity = 1 pixel do jogo original (viewport 640x360),
    /// assim todos os números (velocidade 200, pulo 400, gravidade 980...) ficam
    /// exatamente iguais aos do Godot.
    /// </summary>
    public static class PND
    {
        public const string ResRoot = "PanicoNoDisplay/";

        // Godot: gravidade padrão de 2D (physics/2d/default_gravity)
        public const float GodotGravity = 980f;

        // ---------------- Coordenadas ----------------

        /// <summary>Converte uma posição do Godot (x, y para baixo) para Unity.</summary>
        public static Vector3 G(float x, float y) => new Vector3(x, -y, 0f);
        public static Vector2 G2(float x, float y) => new Vector2(x, -y);

        // ---------------- Sprites ----------------

        static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        /// <summary>Carrega um sprite, ex.: "art/obstacles/a4_papers".</summary>
        public static Sprite Spr(string path)
        {
            if (spriteCache.TryGetValue(path, out var cached) && cached != null)
                return cached;

            Sprite s = Resources.Load<Sprite>(ResRoot + path);

            // Segurança: se a textura não foi importada como sprite com 1 pixel por unidade,
            // recria o sprite em tempo de execução com as configurações certas.
            if (s == null || Mathf.Abs(s.pixelsPerUnit - 1f) > 0.001f)
            {
                Texture2D tex = s != null ? s.texture : Resources.Load<Texture2D>(ResRoot + path);
                if (tex == null)
                {
                    Debug.LogError("[Pânico no Display] Arte não encontrada: Resources/" + ResRoot + path);
                    return null;
                }
                s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
                s.name = path;
            }

            spriteCache[path] = s;
            return s;
        }

        // ---------------- Áudio ----------------

        static readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();

        public static AudioClip Clip(string path)
        {
            if (clipCache.TryGetValue(path, out var c) && c != null) return c;
            c = Resources.Load<AudioClip>(ResRoot + path);
            if (c == null) Debug.LogError("[Pânico no Display] Áudio não encontrado: Resources/" + ResRoot + path);
            clipCache[path] = c;
            return c;
        }

        /// <summary>Cria um AudioStreamPlayer equivalente (volume em dB como no Godot).</summary>
        public static AudioSource Audio(GameObject go, string clipPath, float volumeDb = 0f, bool loop = false)
        {
            var a = go.AddComponent<AudioSource>();
            a.clip = Clip(clipPath);
            a.playOnAwake = false;
            a.loop = loop;
            a.spatialBlend = 0f;
            a.volume = Mathf.Clamp01(Mathf.Pow(10f, volumeDb / 20f));
            return a;
        }

        // Som mutado (equivale a AudioServer.set_bus_mute(0) do Godot: vale para tudo
        // e continua mutado ao reiniciar a fase).
        public static bool Muted;

        public static void ApplyMute()
        {
            AudioListener.volume = Muted ? 0f : 1f;
        }

        // ---------------- Material / Modulate ----------------

        static Material spriteMaterial;

        /// <summary>
        /// Material padrão de sprite do pipeline do projeto (o mesmo que um SpriteRenderer novo recebe no URP).
        /// Usar o material padrão evita qualquer problema de textura trocada.
        /// </summary>
        public static Material SpriteMaterial
        {
            get
            {
                if (spriteMaterial == null)
                {
                    var tmp = new GameObject("PND_tmp") { hideFlags = HideFlags.HideAndDontSave };
                    var sr = tmp.AddComponent<SpriteRenderer>();
                    spriteMaterial = sr.sharedMaterial;
                    Object.DestroyImmediate(tmp);
                    if (spriteMaterial == null)
                    {
                        var sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                        if (sh == null) sh = Shader.Find("Sprites/Default");
                        if (sh != null) spriteMaterial = new Material(sh) { name = "PND_Sprite" };
                    }
                }
                return spriteMaterial;
            }
        }

        /// <summary>Equivalente ao "modulate" do Godot.</summary>
        public static void SetTint(SpriteRenderer r, Color c)
        {
            if (r == null) return;
            r.color = c;
        }

        /// <summary>Cria um Sprite2D do Godot (centralizado por padrão).</summary>
        public static SpriteRenderer MakeSprite(string name, Transform parent, Sprite sprite, Vector3 localPos, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            var mat = SpriteMaterial;
            if (mat != null) sr.sharedMaterial = mat;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>
        /// Sprite com "centered = false" do Godot: a posição informada é o canto superior esquerdo.
        /// O pivô dos sprites é o centro, então desloca metade do tamanho (já considerando a escala).
        /// </summary>
        public static SpriteRenderer MakeSpriteTopLeft(string name, Transform parent, Sprite sprite, float gx, float gy, Vector2 scale, int order)
        {
            float w = sprite != null ? sprite.rect.width : 0f;
            float h = sprite != null ? sprite.rect.height : 0f;
            var sr = MakeSprite(name, parent, sprite, G(gx + w * scale.x * 0.5f, gy + h * scale.y * 0.5f), order);
            sr.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            return sr;
        }

        public static GameObject Node(string name, Transform parent, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go;
        }

        public static Color Rgb(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
    }
}
