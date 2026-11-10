using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// PAC-CODE: reconstrução Unity do minigame Godot fornecido.
/// Não requer sprites, prefabs ou assets externos. A interface é desenhada por IMGUI.
/// </summary>
public class PACCodeGame : MonoBehaviour
{
    public event Action<bool, int> MinigameFinished;
    public event Action ExitRequested;

    private const int Tile = 46;
    private const int BoardX = 35;
    private const int BoardY = 117;

    private static readonly string[] Map =
    {
        "###############",
        "#.....#.......#",
        "#.###.#.#####.#",
        "#...#...#.....#",
        "###.#####.###.#",
        "#...#.....#...#",
        "#.###.#####.#.#",
        "#.....#.....#.#",
        "#.#####.###.#.#",
        "#.............#",
        "###############"
    };

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    private Vector2Int robot = new Vector2Int(1, 1);
    private Vector2Int[] bugs = { new Vector2Int(11, 3), new Vector2Int(7, 9) };
    private Vector2Int[] bugPrevious = { new Vector2Int(11, 3), new Vector2Int(7, 9) };
    private readonly Vector2Int[] itemCells =
    {
        new Vector2Int(13, 1), new Vector2Int(1, 7), new Vector2Int(13, 9)
    };
    private readonly string[] itemNames = { "CPU", "RAM", "SSD" };
    private readonly bool[] collected = new bool[3];

    private int lives = 3;
    private int score;
    private float bugClock;
    private float invulnerable = 1.5f;
    private float elapsed;
    private bool finished;
    private bool won;
    private bool resultSent;
    private float moveRepeat;
    private bool ownsPause;
    private float oldTimeScale = 1f;

    private GUIStyle titleStyle, bodyStyle, smallStyle, itemStyle, overlayTitleStyle, overlayBodyStyle;
    private Texture2D pixel;

    public float bugMoveInterval = 0.28f;
    [Range(0f, 1f)] public float pursuitChance = 0.70f;

    private void Awake()
    {
        pixel = new Texture2D(1, 1);
        pixel.SetPixel(0, 0, Color.white);
        pixel.Apply();

        ResetGame();
    }   

    private void OnDestroy()
    {
        if (pixel != null) Destroy(pixel);
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ExitRequested?.Invoke();
            return;
        }

        if (finished)
        {
            if (Keyboard.current.rKey.wasPressedThisFrame ||
                Keyboard.current.enterKey.wasPressedThisFrame)
                ResetGame();
            return;
        }

        elapsed += Time.unscaledDeltaTime;
        invulnerable = Mathf.Max(0f, invulnerable - Time.unscaledDeltaTime);

        Vector2Int step = ReadDirection();
        if (step != Vector2Int.zero && moveRepeat <= 0f)
        {
            moveRepeat = 0.12f;
            Vector2Int target = robot + step;
            if (Walkable(target))
            {
                robot = target;
                CollectAtRobot();
                CheckHit();
            }
        }
        moveRepeat -= Time.unscaledDeltaTime;

        bugClock += Time.unscaledDeltaTime;
        if (bugClock >= bugMoveInterval)
        {
            bugClock = 0f;
            for (int i = 0; i < bugs.Length; i++)
            {
                Vector2Int old = bugs[i];
                bugs[i] = ChooseBugStep(bugs[i], bugPrevious[i]);
                bugPrevious[i] = old;
            }
            CheckHit();
        }
    }

    private Vector2Int ReadDirection()
    {
        if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame) return Vector2Int.down;
        if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame) return Vector2Int.up;
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame) return Vector2Int.left;
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame) return Vector2Int.right;
        return Vector2Int.zero;
    }

    private bool Walkable(Vector2Int c)
    {
        return c.y >= 0 && c.y < Map.Length && c.x >= 0 &&
               c.x < Map[c.y].Length && Map[c.y][c.x] != '#';
    }

    private void CollectAtRobot()
    {
        for (int i = 0; i < itemCells.Length; i++)
        {
            if (!collected[i] && robot == itemCells[i])
            {
                collected[i] = true;
                score += 100;
                if (AllCollected())
                {
                    score += 300;
                    Finish(true);
                }
                break;
            }
        }
    }

    private bool AllCollected()
    {
        foreach (bool value in collected) if (!value) return false;
        return true;
    }

    private void CheckHit()
    {
        if (finished || invulnerable > 0f) return;
        foreach (Vector2Int bug in bugs)
        {
            if (bug == robot)
            {
                lives--;
                invulnerable = 2f;
                robot = new Vector2Int(1, 1);
                if (lives <= 0) Finish(false);
                return;
            }
        }
    }

    private Vector2Int ChooseBugStep(Vector2Int current, Vector2Int previous)
    {
        List<Vector2Int> choices = new List<Vector2Int>();
        foreach (Vector2Int d in Directions)
            if (Walkable(current + d)) choices.Add(current + d);

        if (choices.Count == 0) return current;
        if (UnityEngine.Random.value < pursuitChance)
        {
            Vector2Int pathStep = ShortestStep(current, robot);
            if (pathStep != current) return pathStep;
        }

        List<Vector2Int> candidates = choices.FindAll(c => c != previous);
        if (candidates.Count == 0) candidates = choices;
        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private Vector2Int ShortestStep(Vector2Int source, Vector2Int destination)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> parents = new Dictionary<Vector2Int, Vector2Int>();
        queue.Enqueue(source);
        parents[source] = source;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (current == destination) break;
            foreach (Vector2Int d in Directions)
            {
                Vector2Int next = current + d;
                if (Walkable(next) && !parents.ContainsKey(next))
                {
                    parents[next] = current;
                    queue.Enqueue(next);
                }
            }
        }

        if (!parents.ContainsKey(destination)) return source;
        Vector2Int node = destination;
        while (parents[node] != source && node != source) node = parents[node];
        return node;
    }

    private void Finish(bool victory)
    {
        if (resultSent) return;
        finished = true;
        won = victory;
        resultSent = true;
        MinigameFinished?.Invoke(won, score);
    }

    public void ResetGame()
    {
        robot = new Vector2Int(1, 1);
        bugs[0] = new Vector2Int(11, 3);
        bugs[1] = new Vector2Int(7, 9);
        bugPrevious[0] = bugs[0];
        bugPrevious[1] = bugs[1];
        Array.Clear(collected, 0, collected.Length);
        lives = 3;
        score = 0;
        bugClock = 0f;
        invulnerable = 1.5f;
        elapsed = 0f;
        moveRepeat = 0f;
        finished = false;
        won = false;
        resultSent = false;
    }

    private void BuildStyles()
    {
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 36, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        titleStyle.normal.textColor = new Color32(100, 245, 225, 255);
        bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };
        bodyStyle.normal.textColor = new Color32(232, 243, 255, 255);
        smallStyle = new GUIStyle(bodyStyle) { fontSize = 15 };
        smallStyle.normal.textColor = new Color32(187, 202, 220, 255);
        itemStyle = new GUIStyle(bodyStyle) { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        overlayTitleStyle = new GUIStyle(titleStyle) { fontSize = 32, alignment = TextAnchor.MiddleCenter };
        overlayBodyStyle = new GUIStyle(bodyStyle) { fontSize = 19, alignment = TextAnchor.MiddleCenter };
    }

    private void OnGUI()
    {
        if (pixel == null) return;

        if (titleStyle == null)
            BuildStyles();

        float scale = Mathf.Min((float)Screen.width / 1050f, (float)Screen.height / 720f);
        float ox = (Screen.width - 1050f * scale) * 0.5f;
        float oy = (Screen.height - 720f * scale) * 0.5f;
        Matrix4x4 old = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(new Vector3(ox, oy, 0), Quaternion.identity, new Vector3(scale, scale, 1));

        DrawRect(new Rect(0, 0, 1050, 720), new Color32(8, 14, 32, 255));
        GUI.Label(new Rect(36, 18, 500, 48), "PAC-CODE", titleStyle);
        GUI.Label(new Rect(38, 67, 650, 30), "Recupere os componentes e fuja dos bugs!", bodyStyle);

        for (int y = 0; y < Map.Length; y++)
        for (int x = 0; x < Map[y].Length; x++)
        {
            Rect r = new Rect(BoardX + x * Tile, BoardY + y * Tile, Tile - 2, Tile - 2);
            if (Map[y][x] == '#')
            {
                DrawRect(r, new Color32(35, 79, 119, 255));
                DrawRect(new Rect(r.x + 4, r.y + 4, r.width - 8, 4), new Color32(78, 145, 172, 255));
            }
            else
            {
                DrawRect(r, new Color32(16, 27, 53, 255));
                DrawRect(new Rect(r.center.x - 2, r.center.y - 2, 4, 4), new Color32(30, 66, 92, 255));
            }
        }

        for (int i = 0; i < itemCells.Length; i++)
            if (!collected[i]) DrawComponent(Center(itemCells[i]), itemNames[i]);

        for (int i = 0; i < bugs.Length; i++) DrawBug(Center(bugs[i]), i);
        if (invulnerable <= 0f || Mathf.FloorToInt(elapsed * 8f) % 2 == 0)
            DrawRobot(Center(robot));

        DrawRect(new Rect(756, 116, 270, 505), new Color32(18, 30, 56, 255));
        GUI.Label(new Rect(780, 126, 220, 38), "MISSÃO", new GUIStyle(titleStyle) { fontSize = 25 });
        GUI.Label(new Rect(780, 168, 230, 28), "Recuperar peças", bodyStyle);
        GUI.Label(new Rect(780, 211, 230, 30), "COMPONENTES", new GUIStyle(bodyStyle) { fontSize = 19, fontStyle = FontStyle.Bold, normal = { textColor = new Color32(255, 208, 92, 255) } });

        for (int i = 0; i < itemNames.Length; i++)
        {
            string status = collected[i] ? "[OK] " : "[  ] ";
            GUI.Label(new Rect(780, 250 + i * 40, 220, 30), status + itemNames[i],
                new GUIStyle(bodyStyle) { fontSize = 20, normal = { textColor = collected[i] ? new Color32(106, 240, 173, 255) : Color.white } });
        }

        GUI.Label(new Rect(780, 410, 230, 30), "VIDAS: " + new string('♥', lives),
            new GUIStyle(bodyStyle) { fontSize = 19, normal = { textColor = new Color32(255, 135, 151, 255) } });
        GUI.Label(new Rect(780, 450, 230, 30), "PONTOS: " + score,
            new GUIStyle(bodyStyle) { fontSize = 20, normal = { textColor = new Color32(255, 208, 92, 255) } });
        GUI.Label(new Rect(780, 510, 230, 25), "SETAS / WASD", smallStyle);
        GUI.Label(new Rect(780, 540, 230, 25), "R = REINICIAR", smallStyle);
        GUI.Label(new Rect(780, 570, 230, 25), "ESC = VOLTAR", smallStyle);
        GUI.Label(new Rect(36, 654, 700, 32), "Os bugs se movem sozinhos pelo labirinto e podem perseguir o robô.", smallStyle);

        if (finished)
        {
            DrawRect(new Rect(195, 257, 670, 180), new Color(7f / 255f, 17f / 255f, 37f / 255f, 0.97f));
            DrawRect(new Rect(205, 267, 650, 160), new Color32(23, 60, 80, 255));
            GUI.Label(new Rect(230, 290, 600, 55), won ? "MISSÃO CONCLUÍDA!" : "FIM DE JOGO!",
                new GUIStyle(overlayTitleStyle) { normal = { textColor = won ? new Color32(135, 255, 181, 255) : new Color32(255, 140, 157, 255) } });
            GUI.Label(new Rect(230, 350, 600, 35), "Pontuação: " + score + "   |   R ou ENTER para reiniciar", overlayBodyStyle);
        }

        GUI.matrix = old;
    }

    private Vector2 Center(Vector2Int cell)
    {
        return new Vector2(BoardX + cell.x * Tile + Tile / 2f, BoardY + cell.y * Tile + Tile / 2f);
    }

    private void DrawRect(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, pixel);
        GUI.color = old;
    }

    private void DrawRobot(Vector2 p)
    {
        DrawRect(new Rect(p.x - 17, p.y - 16, 34, 33), new Color32(100, 245, 225, 255));
        DrawRect(new Rect(p.x - 13, p.y - 9, 26, 17), new Color32(16, 42, 73, 255));
        DrawRect(new Rect(p.x - 9, p.y - 3, 5, 5), new Color32(255, 243, 163, 255));
        DrawRect(new Rect(p.x + 4, p.y - 3, 5, 5), new Color32(255, 243, 163, 255));
        DrawRect(new Rect(p.x - 1.5f, p.y - 25, 3, 10), new Color32(100, 245, 225, 255));
        DrawRect(new Rect(p.x - 4, p.y - 29, 8, 8), new Color32(255, 208, 92, 255));
    }

    private void DrawBug(Vector2 p, int index)
    {
        Color c = index == 0 ? new Color32(255, 100, 125, 255) : new Color32(187, 132, 255, 255);
        DrawRect(new Rect(p.x - 18, p.y - 17, 36, 34), c);
        DrawRect(new Rect(p.x - 11, p.y - 7, 9, 10), Color.white);
        DrawRect(new Rect(p.x + 3, p.y - 7, 9, 10), Color.white);
        DrawRect(new Rect(p.x - 8, p.y - 4, 4, 5), new Color32(24, 33, 59, 255));
        DrawRect(new Rect(p.x + 6, p.y - 4, 4, 5), new Color32(24, 33, 59, 255));
        DrawRect(new Rect(p.x - 22, p.y + 10, 8, 3), c);
        DrawRect(new Rect(p.x + 14, p.y + 10, 8, 3), c);
    }

    private void DrawComponent(Vector2 p, string name)
    {
        DrawRect(new Rect(p.x - 21, p.y - 21, 42, 42), new Color32(255, 208, 92, 255));
        DrawRect(new Rect(p.x - 18, p.y - 18, 36, 36), new Color32(37, 54, 83, 255));
        if (name == "CPU")
        {
            DrawRect(new Rect(p.x - 11, p.y - 12, 22, 22), new Color32(139, 233, 226, 255));
            DrawRect(new Rect(p.x - 6, p.y - 7, 12, 12), new Color32(48, 91, 132, 255));
        }
        else if (name == "RAM")
        {
            DrawRect(new Rect(p.x - 14, p.y - 9, 28, 18), new Color32(93, 227, 161, 255));
            for (int x = -9; x <= 5; x += 7) DrawRect(new Rect(p.x + x, p.y - 5, 6, 8), new Color32(29, 59, 84, 255));
        }
        else
        {
            DrawRect(new Rect(p.x - 12, p.y - 13, 24, 26), new Color32(227, 235, 246, 255));
            DrawRect(new Rect(p.x - 8, p.y + 3, 16, 10), new Color32(92, 135, 174, 255));
        }
        DrawRect(new Rect(p.x - 22, p.y + 15, 44, 17), new Color32(9, 21, 43, 255));
        GUI.Label(new Rect(p.x - 22, p.y + 14, 44, 19), name, itemStyle);
    }
}
