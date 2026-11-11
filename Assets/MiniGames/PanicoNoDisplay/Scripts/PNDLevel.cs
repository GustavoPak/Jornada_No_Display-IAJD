using UnityEngine;

namespace PanicoNoDisplay
{
    /// <summary>
    /// Monta a fase (scenes/main/main.tscn) e as "cenas" de cada obstáculo
    /// exatamente nas mesmas posições e tamanhos do Godot.
    /// </summary>
    public static class PNDLevel
    {
        // Ordem na árvore do Godot = ordem de desenho (z_index 0). O Player tem z_index 1.
        const int OrderBackground = -100;
        const int OrderGround = 0;
        const int OrderPlayer = 5000;

        // (tipo, x, y) — copiado de main.tscn, na mesma ordem
        static readonly (string type, float x, float y)[] Layout =
        {
            ("papers", 500, 224),
            ("points", 700, 145),
            ("pencils", 900, 225),
            ("wires", 1300, 218),
            ("cooler", 1750, 194),
            ("points", 1950, 145),
            ("ac", 2200, 20),
            ("nbobs", 2300, 216),
            ("cleaner", 2800, 202),
            ("points", 3100, 145),
            ("papers", 3350, 224),
            ("wires", 3800, 218),
            ("pencils", 4250, 225),
            ("points", 4500, 145),
            ("cooler", 4700, 194),
            ("ac", 5100, 20),
            ("nbobs", 5200, 216),
            ("cleaner", 5750, 202),
            ("points", 6050, 145),
            ("wires", 6300, 218),
            ("papers", 6750, 224),
            ("pencils", 7200, 225),
            ("points", 7450, 145),
            ("cooler", 7650, 194),
            ("ac", 8000, 20),
            ("nbobs", 8150, 216),
            ("cleaner", 8700, 202),
            ("points", 9000, 145),
            ("wires", 9300, 218),
            ("papers", 9750, 224),
            ("cooler", 10200, 194),
            ("ac", 10550, 20),
            ("pencils", 10650, 225),
            ("points", 10900, 145),
            ("nbobs", 11100, 216),
            ("cleaner", 11550, 202),
            ("hwire", 1530, 0),
            ("hwire", 4020, 0),
            ("hwire", 6525, 0),
            ("hwire", 8425, 0),
            ("hwire", 9525, 0),
            ("hwire", 11325, 0),
            ("goal", 12000, 202),
        };

        public static PNDPlayer Build(Transform root, PNDHud hud, Camera cam)
        {
            // ---------- Fundo com parallax ----------
            var bgSprite = PND.Spr("art/environment/display_lab_background_clean");
            var parallax = PND.Node("ParallaxBackground", root, Vector3.zero);
            var copies = new SpriteRenderer[2];
            for (int i = 0; i < copies.Length; i++)
            {
                copies[i] = PND.MakeSprite("BackgroundSprite" + i, parallax.transform, bgSprite, Vector3.zero, OrderBackground);
                copies[i].transform.localScale = new Vector3(0.5f, 0.58f, 1f);
            }

            // ---------- Chão ----------
            var ground = PND.Node("Ground", root, PND.G(15000, 300));
            var gcol = ground.AddComponent<BoxCollider2D>();
            gcol.size = new Vector2(30000, 128);
            var floor = PND.MakeSprite("Sprite2D", ground.transform, PND.Spr("art/environment/lab_floor_tile_128"), Vector3.zero, OrderGround);
            floor.drawMode = SpriteDrawMode.Tiled;
            floor.tileMode = SpriteTileMode.Continuous;
            floor.size = new Vector2(30000, 128);

            // ---------- Player ----------
            var player = PNDPlayer.Create(root, PND.G(100, 200), hud, OrderPlayer);

            // Câmera (Camera2D filha do Player no Godot)
            var follow = root.gameObject.AddComponent<PNDCameraFollow>();
            follow.cam = cam;
            follow.target = player.transform;
            follow.backgrounds = copies;

            // ---------- Obstáculos / itens ----------
            int order = 10;
            int n = 0;
            foreach (var item in Layout)
            {
                n++;
                var pos = PND.G(item.x, item.y);
                switch (item.type)
                {
                    case "papers": SimpleObstacle("A4Papers" + n, root, pos, "art/obstacles/a4_papers", new Vector2(56, 14), 5, "slip", order); break;
                    case "pencils": SimpleObstacle("Pencils" + n, root, pos, "art/obstacles/pencils", new Vector2(52, 14), 4, "slip", order); break;
                    case "wires": SimpleObstacle("TangledWires" + n, root, pos, "art/obstacles/tangled_wires", new Vector2(54, 24), 6, "trip", order); break;
                    case "cooler": WaterCooler("WaterCooler" + n, root, pos, order); break;
                    case "nbobs": ObstacleNotebook("ObstacleNotebook" + n, root, pos, order); break;
                    case "cleaner": Cleaner("Cleaner" + n, root, pos, player, order); break;
                    case "points": DisplayPoints("DisplayPoints" + n, root, pos, hud, order); break;
                    case "ac": AirConditioner("AirConditioner" + n, root, pos, order); break;
                    case "hwire": HangingWire("HangingWire" + n, root, pos, order); break;
                    case "goal": Goal("Notebook", root, pos, hud, order); break;
                }
                order += 5;
            }

            return player;
        }

        static BoxCollider2D Box(GameObject go, Vector2 size, float gOffsetX, float gOffsetY, bool trigger = true)
        {
            var c = go.AddComponent<BoxCollider2D>();
            c.size = size;
            c.offset = new Vector2(gOffsetX, -gOffsetY);
            c.isTrigger = trigger;
            return c;
        }

        static void SimpleObstacle(string name, Transform root, Vector3 pos, string sprite, Vector2 size, float offY, string damage, int order)
        {
            var go = PND.Node(name, root, pos);
            PND.MakeSprite("Sprite2D", go.transform, PND.Spr(sprite), Vector3.zero, order);
            Box(go, size, 0, offY);
            go.AddComponent<PNDObstacle>().damageType = damage;
        }

        static void WaterCooler(string name, Transform root, Vector3 pos, int order)
        {
            var go = PND.Node(name, root, pos);
            var anim = PNDSpriteAnimator.Create("AnimatedSprite2D", go.transform, Vector3.zero, order,
                new PNDAnimation("spill", 5f, true, ("art/obstacles/water_cooler_1", 1f), ("art/obstacles/water_cooler_2", 1f)));
            anim.Play("spill");
            Box(go, new Vector2(64, 14), 10, 36); // a poça d'água
            go.AddComponent<PNDObstacle>().damageType = "slip";
        }

        static void ObstacleNotebook(string name, Transform root, Vector3 pos, int order)
        {
            var go = PND.Node(name, root, pos);
            var anim = PNDSpriteAnimator.Create("AnimatedSprite2D", go.transform, Vector3.zero, order,
                new PNDAnimation("crack", 6f, false, ("art/obstacles/notebook_cracking", 1f), ("art/obstacles/notebook_cracked", 1f)),
                new PNDAnimation("intact", 5f, false, ("art/obstacles/notebook_intact", 1f)));
            anim.Play("intact");
            Box(go, new Vector2(50, 26), 0, 7);
            go.AddComponent<PNDObstacleNotebook>().anim = anim;
        }

        static void Cleaner(string name, Transform root, Vector3 pos, PNDPlayer player, int order)
        {
            var go = PND.Node(name, root, pos);
            var anim = PNDSpriteAnimator.Create("AnimatedSprite2D", go.transform, Vector3.zero, order,
                new PNDAnimation("attack", 7f, true, ("art/obstacles/cleaner_attack_1", 1f), ("art/obstacles/cleaner_attack_2", 1f)),
                new PNDAnimation("sweep", 4f, true, ("art/obstacles/cleaner_sweep_1", 1f), ("art/obstacles/cleaner_sweep_2", 1f)));
            anim.Play("sweep");
            Box(go, new Vector2(46, 48), 0, 10);
            var c = go.AddComponent<PNDCleaner>();
            c.anim = anim;
            c.player = player;
        }

        static void DisplayPoints(string name, Transform root, Vector3 pos, PNDHud hud, int order)
        {
            var go = PND.Node(name, root, pos);
            PND.MakeSprite("Sprite2D", go.transform, PND.Spr("art/obstacles/display_points"), Vector3.zero, order);
            var c = go.AddComponent<CircleCollider2D>();
            c.radius = 16f;
            c.isTrigger = true;
            go.AddComponent<PNDCollectibleTime>().hud = hud;
        }

        static void AirConditioner(string name, Transform root, Vector3 pos, int order)
        {
            var go = PND.Node(name, root, pos);
            PND.MakeSprite("ACUnit", go.transform, PND.Spr("art/obstacles/air_conditioner"), Vector3.zero, order);
            Box(go, new Vector2(128, 240), 0, 120);
            go.AddComponent<PNDAirConditioner>();

            // WindParticles (CPUParticles2D)
            var wind = PND.Node("WindParticles", go.transform, PND.G(0, 20)).AddComponent<PNDParticles>();
            wind.amount = 32;
            wind.lifetime = 0.75f;
            wind.sprite = PND.Spr("art/obstacles/wind_particle");
            wind.emissionRectExtents = new Vector2(54f, 2f);
            wind.direction = Vector2.down;      // (0, 1) no Godot
            wind.spreadDegrees = 6f;
            wind.gravity = Vector2.zero;
            wind.speedMin = 260f;
            wind.speedMax = 320f;
            wind.scaleMin = 0.5f;
            wind.scaleMax = 0.9f;
            wind.color = new Color(0.75f, 0.95f, 1f, 0.75f);
            wind.sortingOrder = order + 1;
        }

        static void HangingWire(string name, Transform root, Vector3 pos, int order)
        {
            var go = PND.Node(name, root, pos);
            var wire = go.AddComponent<PNDHangingWire>();

            var visual = PND.Node("Visual", go.transform, PND.G(0, -140));
            wire.visual = visual.transform;

            // AnimatedSprite2D com centered = false na posição (-16, 0): canto superior esquerdo
            var f0 = PND.Spr("art/obstacles/hanging_wire_0");
            float w = f0 != null ? f0.rect.width : 32f, h = f0 != null ? f0.rect.height : 356f;
            const string O = "art/obstacles/";
            var anim = PNDSpriteAnimator.Create("AnimatedSprite2D", visual.transform, PND.G(-16 + w * 0.5f, h * 0.5f), order,
                new PNDAnimation("spark", 12f, true,
                    (O + "hanging_wire_1", 1f), (O + "hanging_wire_0", 1f), (O + "hanging_wire_2", 1f), (O + "hanging_wire_0", 2f),
                    (O + "hanging_wire_3", 1f), (O + "hanging_wire_1", 1f), (O + "hanging_wire_0", 3f)));
            wire.anim = anim;

            // Sparks (CPUParticles2D)
            var sparks = PND.Node("Sparks", visual.transform, PND.G(0, 342)).AddComponent<PNDParticles>();
            sparks.amount = 10;
            sparks.lifetime = 0.45f;
            sparks.explosiveness = 0.2f;
            sparks.direction = Vector2.down;
            sparks.spreadDegrees = 75f;
            sparks.gravity = new Vector2(0f, -450f); // (0, 450) no Godot
            sparks.speedMin = 30f;
            sparks.speedMax = 85f;
            sparks.scaleMin = 1f;
            sparks.scaleMax = 2f;
            sparks.color = new Color(1f, 0.88f, 0.3f, 1f);
            sparks.sortingOrder = order + 1;
            wire.sparks = sparks;

            Box(go, new Vector2(12, 340), 0, 30);
            wire.zapSound = PND.Audio(go, "audio/sfx/zap1");
        }

        static void Goal(string name, Transform root, Vector3 pos, PNDHud hud, int order)
        {
            var go = PND.Node(name, root, pos);
            PND.MakeSprite("Sprite2D", go.transform, PND.Spr("art/obstacles/goal_pc"), Vector3.zero, order);
            Box(go, new Vector2(90, 1200), 0, -500);
            go.AddComponent<PNDGoalNotebook>().hud = hud;

            // Parede atrás do PC (o jogador não passa dele)
            var wall = PND.Node("EndWall", go.transform, PND.G(40, -500));
            var wc = wall.AddComponent<BoxCollider2D>();
            wc.size = new Vector2(30, 1200);
            wc.isTrigger = false;
        }
    }

    /// <summary>
    /// Camera2D do Player (offset (150, -50), drag horizontal com margem direita 0)
    /// + ParallaxBackground (motion_scale (0.5, 0.4), motion_mirroring 688).
    /// </summary>
    public class PNDCameraFollow : MonoBehaviour
    {
        public Camera cam;
        public Transform target;
        public SpriteRenderer[] backgrounds;

        const float HalfW = 320f, HalfH = 180f;
        static readonly Vector2 Offset = new Vector2(150f, -50f); // em coordenadas Godot
        const float DragLeft = 0.2f, DragRight = 0f;
        static readonly Vector2 MotionScale = new Vector2(0.5f, 0.4f);
        const float Mirroring = 688f;

        float camX;
        bool init;

        void LateUpdate()
        {
            if (cam == null || target == null) return;

            // Coordenadas no "espaço Godot" (Y para baixo)
            float tx = target.position.x;
            float ty = -target.position.y;

            if (!init)
            {
                camX = tx;
                init = true;
            }

            // Drag horizontal: a câmera só anda quando o player passa do centro (margem direita 0)
            // e volta se ele ficar mais de 20% de meia tela para trás.
            if (tx > camX + DragRight * HalfW) camX = tx - DragRight * HalfW;
            else if (tx < camX - DragLeft * HalfW) camX = tx + DragLeft * HalfW;

            float centerX = camX + Offset.x;
            float centerY = ty + Offset.y; // vertical sem drag: segue o player direto

            cam.transform.position = new Vector3(centerX, -centerY, -10f);

            // ----- Parallax -----
            if (backgrounds == null || backgrounds.Length == 0) return;
            float topLeftX = centerX - HalfW;
            float topLeftY = centerY - HalfH;
            float layerX = Mathf.Floor(-topLeftX) * MotionScale.x;
            float layerY = Mathf.Floor(-topLeftY) * MotionScale.y;
            layerX -= Mirroring * Mathf.Ceil(layerX / Mirroring);

            for (int i = 0; i < backgrounds.Length; i++)
            {
                var sr = backgrounds[i];
                if (sr == null || sr.sprite == null) continue;
                float w = sr.sprite.rect.width * 0.5f, h = sr.sprite.rect.height * 0.58f;
                // Sprite (centered = false) em (0, -110) dentro da camada, repetido a cada 688 px
                float sx = layerX + i * Mirroring;
                float sy = layerY - 110f;
                float gx = topLeftX + sx + w * 0.5f;
                float gy = topLeftY + sy + h * 0.5f;
                sr.transform.position = new Vector3(gx, -gy, 0f);
            }
        }
    }
}
