using System.Collections.Generic;
using UnityEngine;

namespace PanicoNoDisplay
{
    /// <summary>
    /// Equivalente simples ao CPUParticles2D do Godot, feito com SpriteRenderers comuns
    /// (assim usa o mesmo material padrão dos outros sprites e nunca troca de textura).
    /// Coordenadas globais, emissão contínua.
    /// </summary>
    public class PNDParticles : MonoBehaviour
    {
        public int amount = 8;
        public float lifetime = 1f;
        public float explosiveness;          // 0..1 (como no Godot)
        public Vector2 direction = Vector2.down; // em coordenadas Unity
        public float spreadDegrees = 45f;
        public Vector2 gravity;              // em coordenadas Unity
        public float speedMin, speedMax;
        public float scaleMin = 1f, scaleMax = 1f;
        public Vector2 emissionRectExtents;  // 0 = ponto
        public Color color = Color.white;
        public Sprite sprite;                // null = quadrado branco de 1 px
        public int sortingOrder;
        public bool emitting = true;

        class P
        {
            public SpriteRenderer sr;
            public Vector2 pos, vel;
            public float age, life;
            public bool alive;
        }

        readonly List<P> pool = new List<P>();
        float cycleTime;
        int emittedThisCycle;
        static Sprite whitePixel;

        static Sprite WhitePixel
        {
            get
            {
                if (whitePixel == null)
                {
                    var t = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                    t.SetPixel(0, 0, Color.white);
                    t.Apply();
                    whitePixel = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                    whitePixel.name = "PND_WhitePixel";
                }
                return whitePixel;
            }
        }

        void Start()
        {
            for (int i = 0; i < amount; i++)
            {
                var sr = PND.MakeSprite("p" + i, transform, sprite != null ? sprite : WhitePixel, Vector3.zero, sortingOrder);
                sr.color = color;
                sr.enabled = false;
                pool.Add(new P { sr = sr });
            }
        }

        /// <summary>restart() do Godot.</summary>
        public void Restart()
        {
            foreach (var p in pool)
            {
                p.alive = false;
                if (p.sr != null) p.sr.enabled = false;
            }
            cycleTime = 0f;
            emittedThisCycle = 0;
            emitting = true;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // Emissão: 'amount' partículas a cada 'lifetime' segundos.
            // explosiveness comprime as emissões no começo do ciclo.
            if (emitting && amount > 0 && lifetime > 0f)
            {
                cycleTime += dt;
                float window = Mathf.Max(0.0001f, lifetime * (1f - explosiveness));
                int shouldHave = Mathf.Min(amount, Mathf.FloorToInt(cycleTime / window * amount) + 1);
                while (emittedThisCycle < shouldHave)
                {
                    Spawn();
                    emittedThisCycle++;
                }
                if (cycleTime >= lifetime)
                {
                    cycleTime -= lifetime;
                    emittedThisCycle = 0;
                }
            }

            foreach (var p in pool)
            {
                if (!p.alive) continue;
                p.age += dt;
                if (p.age >= p.life)
                {
                    p.alive = false;
                    p.sr.enabled = false;
                    continue;
                }
                p.vel += gravity * dt;
                p.pos += p.vel * dt;
                p.sr.transform.position = new Vector3(p.pos.x, p.pos.y, 0f);
            }
        }

        void Spawn()
        {
            P free = null;
            foreach (var p in pool)
            {
                if (!p.alive) { free = p; break; }
            }
            if (free == null) return;

            Vector2 origin = transform.position;
            if (emissionRectExtents != Vector2.zero)
                origin += new Vector2(Random.Range(-emissionRectExtents.x, emissionRectExtents.x),
                                      Random.Range(-emissionRectExtents.y, emissionRectExtents.y));

            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float ang = (baseAngle + Random.Range(-spreadDegrees, spreadDegrees)) * Mathf.Deg2Rad;
            float speed = Random.Range(speedMin, speedMax);

            free.pos = origin;
            free.vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * speed;
            free.age = 0f;
            free.life = lifetime;
            free.alive = true;
            float s = Random.Range(scaleMin, scaleMax);
            free.sr.transform.localScale = new Vector3(s, s, 1f);
            free.sr.transform.position = new Vector3(origin.x, origin.y, 0f);
            free.sr.color = color;
            free.sr.enabled = true;
        }
    }
}
