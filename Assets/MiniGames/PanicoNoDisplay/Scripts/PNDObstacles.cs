using System.Collections;
using UnityEngine;

namespace PanicoNoDisplay
{
    static class PNDTrigger
    {
        public static PNDPlayer PlayerFrom(Collider2D other)
        {
            if (other == null) return null;
            return other.GetComponentInParent<PNDPlayer>();
        }
    }

    /// <summary>scripts/obstacles/obstacle.gd — papéis A4, lápis, fios embolados, bebedouro.</summary>
    public class PNDObstacle : MonoBehaviour
    {
        public string damageType = "slip"; // "slip", "trip" ou "hurt"

        void OnTriggerEnter2D(Collider2D other)
        {
            // Se o corpo que colidiu é o player, chama o dano com o tipo de animação
            var p = PNDTrigger.PlayerFrom(other);
            if (p != null) p.TakeDamage(damageType);
        }
    }

    /// <summary>scripts/obstacles/obstacle_notebook.gd — notebook no chão que trinca.</summary>
    public class PNDObstacleNotebook : MonoBehaviour
    {
        public bool isCracked;
        public PNDSpriteAnimator anim;

        void Start()
        {
            anim.Play("intact");
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var p = PNDTrigger.PlayerFrom(other);
            if (p == null) return;
            if (!isCracked)
            {
                isCracked = true;
                anim.Play("crack");
            }
            p.TakeDamage("trip");
        }
    }

    /// <summary>scripts/obstacles/cleaner.gd — faxineira que patrulha e dá vassouradas.</summary>
    public class PNDCleaner : MonoBehaviour
    {
        public float patrolDistance = 80f;
        public float patrolSpeed = 55f;
        public float attackTriggerDistance = 95f;

        float startX;
        int direction = -1;
        public bool isAttacking;

        public PNDSpriteAnimator anim;
        public PNDPlayer player;
        float x;

        void Start()
        {
            startX = transform.position.x;
            x = startX;
            anim.Play("sweep");
        }

        void FixedUpdate()
        {
            float delta = Time.fixedDeltaTime;
            if (player != null)
            {
                float distX = player.transform.position.x - transform.position.x;
                float absDistX = Mathf.Abs(distX);

                // Quando o player se aproxima, ela vira para o lado dele e bate com a vassoura
                if (absDistX <= attackTriggerDistance)
                {
                    isAttacking = true;
                    anim.FlipH = distX > 0f; // Sprite original olha para a esquerda
                    if (anim.Current != "attack")
                        anim.Play("attack");
                    return;
                }
                isAttacking = false;
            }

            // Patrulha varrendo para lá e para cá
            if (anim.Current != "sweep")
                anim.Play("sweep");

            x += direction * patrolSpeed * delta;
            if (x <= startX - patrolDistance)
                direction = 1;
            else if (x >= startX + patrolDistance)
                direction = -1;

            transform.position = new Vector3(x, transform.position.y, transform.position.z);

            // Na arte original ela olha para a esquerda
            anim.FlipH = direction > 0;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var p = PNDTrigger.PlayerFrom(other);
            if (p == null) return;
            anim.Play("attack");
            p.TakeDamage("hurt");
        }
    }

    /// <summary>
    /// scripts/obstacles/hanging_wire.gd — FIO DESENCAPADO PENDURADO DO TETO.
    /// Fica na altura da cabeça soltando faíscas. Não dá para pular:
    /// o jogador precisa DESLIZAR por baixo (seta ↓).
    /// </summary>
    public class PNDHangingWire : MonoBehaviour
    {
        public float swayDegrees = 2.5f; // Quanto o fio balança
        public float swaySpeed = 1.8f;   // Velocidade do balanço

        float time;
        float phase;

        public Transform visual;
        public PNDSpriteAnimator anim;
        public PNDParticles sparks;
        public AudioSource zapSound;
        Coroutine flash;

        void Start()
        {
            phase = Random.value * Mathf.PI * 2f;
            anim.Play("spark");
            anim.Frame = Random.Range(0, anim.FrameCount("spark"));
        }

        void Update()
        {
            // Balanço suave (só o visual; a área de dano fica parada)
            time += Time.deltaTime;
            // Godot gira no sentido horário com ângulo positivo; Unity no anti-horário
            float deg = swayDegrees * Mathf.Sin(time * swaySpeed + phase);
            visual.localRotation = Quaternion.Euler(0f, 0f, -deg);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var p = PNDTrigger.PlayerFrom(other);
            if (p == null) return;
            if (p.isHurt) return; // Já está machucado/invencível
            zapSound.Play();
            // Rajada de faíscas e flash no fio
            if (sparks != null)
            {
                sparks.Restart();
            }
            if (flash != null) StopCoroutine(flash);
            flash = StartCoroutine(Flash());
            p.TakeDamage("hurt");
        }

        IEnumerator Flash()
        {
            Color from = Color.white;
            Color bright = new Color(2f, 2f, 1.2f, 1f);
            yield return Tween(from, bright, 0.05f);
            yield return Tween(bright, Color.white, 0.25f);
            flash = null;
        }

        IEnumerator Tween(Color a, Color b, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                PND.SetTint(anim.sr, Color.LerpUnclamped(a, b, Mathf.Clamp01(t / dur)));
                yield return null;
            }
            PND.SetTint(anim.sr, b);
        }
    }

    /// <summary>scripts/environment/air_conditioner.gd — vento que deixa o jogador lento.</summary>
    public class PNDAirConditioner : MonoBehaviour
    {
        public float slowdownMultiplier = 0.4f; // Reduz a velocidade para 40%

        void OnTriggerEnter2D(Collider2D other)
        {
            var p = PNDTrigger.PlayerFrom(other);
            if (p != null) p.ApplySlowdown(slowdownMultiplier);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            // Quando o Player sair do vento, volta ao normal
            var p = PNDTrigger.PlayerFrom(other);
            if (p != null) p.RemoveSlowdown();
        }
    }

    /// <summary>scripts/environment/collectible_time.gd — DISPLAY POINTS: pontos e segundos extras.</summary>
    public class PNDCollectibleTime : MonoBehaviour
    {
        public float timeBonus = 5f;
        public int pointsBonus = 10;
        public PNDHud hud;
        bool collected;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (collected) return;
            // Checa se o corpo que tocou é o jogador
            var p = PNDTrigger.PlayerFrom(other);
            if (p == null || p.gameObject.name != "Player") return;
            collected = true;
            if (hud != null)
            {
                hud.AddTime(timeBonus);
                hud.AddPoints(pointsBonus);
            }
            // Remove o Display Point após ser coletado
            Destroy(gameObject);
        }
    }

    /// <summary>scripts/environment/notebook.gd — o PC do fim da fase (abre a janela de Salvar).</summary>
    public class PNDGoalNotebook : MonoBehaviour
    {
        public bool hasTriggered;
        public PNDHud hud;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (hasTriggered) return;
            var p = PNDTrigger.PlayerFrom(other);
            if (p == null || p.gameObject.name != "Player") return;
            // Congela o jogador e abre a tela de Salvar do Windows!
            hasTriggered = true;
            p.FreezeAtPc();
            if (hud != null) hud.ShowWindowsSaveDialog();
        }
    }
}
