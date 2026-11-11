using System.Collections.Generic;
using UnityEngine;

namespace PanicoNoDisplay
{
    /// <summary>Uma animação do SpriteFrames do Godot.</summary>
    public class PNDAnimation
    {
        public string name;
        public Sprite[] frames;
        public float[] durations; // "duration" relativo de cada quadro (Godot)
        public float speed;       // FPS
        public bool loop;

        public PNDAnimation(string name, float speed, bool loop, params (string path, float duration)[] frames)
        {
            this.name = name;
            this.speed = speed;
            this.loop = loop;
            this.frames = new Sprite[frames.Length];
            durations = new float[frames.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                this.frames[i] = PND.Spr(frames[i].path);
                durations[i] = frames[i].duration;
            }
        }
    }

    /// <summary>
    /// Equivalente ao AnimatedSprite2D do Godot 4, com a mesma regra do play():
    /// chamar Play() com a animação atual não reinicia, exceto se ela já terminou
    /// (animações sem loop), aí recomeça do primeiro quadro.
    /// </summary>
    public class PNDSpriteAnimator : MonoBehaviour
    {
        public SpriteRenderer sr;
        readonly Dictionary<string, PNDAnimation> anims = new Dictionary<string, PNDAnimation>();

        public string Current { get; private set; }
        int frame;
        float progress; // segundos dentro do quadro atual
        bool playing;
        bool finished;

        public void Add(PNDAnimation a) => anims[a.name] = a;
        public bool Has(string n) => n != null && anims.ContainsKey(n);
        public int FrameCount(string n) => anims.TryGetValue(n, out var a) ? a.frames.Length : 0;
        public bool IsPlaying => playing;

        public int Frame
        {
            get => frame;
            set
            {
                if (Current == null) return;
                var a = anims[Current];
                frame = Mathf.Clamp(value, 0, a.frames.Length - 1);
                progress = 0f;
                finished = false;
                Apply();
            }
        }

        public void Play(string n)
        {
            if (!anims.TryGetValue(n, out var a)) return;
            if (n != Current)
            {
                Current = n;
                frame = 0;
                progress = 0f;
                finished = false;
            }
            else if (finished && frame == a.frames.Length - 1)
            {
                frame = 0;
                progress = 0f;
                finished = false;
            }
            playing = true;
            Apply();
        }

        void Update()
        {
            if (!playing || Current == null) return;
            var a = anims[Current];
            if (a.speed <= 0f || a.frames.Length == 0) return;

            progress += Time.deltaTime;
            float frameDur = a.durations[frame] / a.speed;
            int guard = 0;
            while (progress >= frameDur && guard++ < 64)
            {
                if (frame < a.frames.Length - 1)
                {
                    progress -= frameDur;
                    frame++;
                }
                else if (a.loop)
                {
                    progress -= frameDur;
                    frame = 0;
                }
                else
                {
                    progress = frameDur;
                    finished = true;
                    playing = false;
                    break;
                }
                frameDur = a.durations[frame] / a.speed;
            }
            Apply();
        }

        void Apply()
        {
            if (sr == null || Current == null) return;
            var a = anims[Current];
            if (a.frames.Length > 0) sr.sprite = a.frames[frame];
        }

        /// <summary>flip_h do Godot (feito pela escala, para funcionar com qualquer shader).</summary>
        public bool FlipH
        {
            get => sr != null && sr.transform.localScale.x < 0f;
            set
            {
                if (sr == null) return;
                var s = sr.transform.localScale;
                s.x = Mathf.Abs(s.x) * (value ? -1f : 1f);
                sr.transform.localScale = s;
            }
        }

        /// <summary>Cria SpriteRenderer + animador como filho (AnimatedSprite2D).</summary>
        public static PNDSpriteAnimator Create(string name, Transform parent, Vector3 localPos, int order, params PNDAnimation[] animations)
        {
            var sr = PND.MakeSprite(name, parent, null, localPos, order);
            var anim = sr.gameObject.AddComponent<PNDSpriteAnimator>();
            anim.sr = sr;
            foreach (var a in animations) anim.Add(a);
            return anim;
        }
    }
}
