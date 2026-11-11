using PanicoNoDisplay;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gatilho do minijogo "Pânico no Display" no jogo principal.
/// Aparece como um cubo visível (girando devagar). Quando o jogador chega perto,
/// mostra uma mensagem discreta "Aperte E para jogar" e, ao apertar E, abre o minijogo.
/// Ao sair, o jogador volta exatamente para onde estava e, se venceu, recebe pontos x 0.1 em dinheiro.
/// </summary>
public class IniciarPanicoNoDisplay : MonoBehaviour
{
    [Tooltip("Transform do jogador. Se ficar vazio, procura o objeto com a tag \"Player\".")]
    public Transform jogador;

    [Tooltip("Distância máxima (em metros, só na horizontal) para poder apertar E.")]
    public float distanciaInteracao = 3f;

    [Tooltip("Mensagem mostrada quando o jogador está perto.")]
    public string mensagem = "Aperte E para jogar";

    public bool mostrarMensagem = true;

    [Header("Cubo visível")]
    public bool criarCuboVisivel = true;
    public Color corDoCubo = new Color(1f, 0.75f, 0.15f);
    public float tamanhoDoCubo = 1f;
    public bool girarCubo = true;

    [Header("Debug")]
    public bool mostrarLogs = true;

    bool perto;
    bool pertoAnterior;
    Transform visual;
    Vector3 visualBase;
    GUIStyle estilo;
    Texture2D fundo;

    void Log(string msg)
    {
        if (mostrarLogs) Debug.Log("[Pânico no Display] " + msg, this);
    }

    void Awake()
    {
        if (criarCuboVisivel && GetComponentInChildren<Renderer>() == null)
            CriarCubo();
    }

    void Start()
    {
        ProcurarJogador();
        Log("Gatilho ativo em " + transform.position + " | distância de interação: " + distanciaInteracao + " m" +
            (jogador != null ? " | jogador: " + jogador.name + " em " + jogador.position +
                               " (distância atual: " + DistanciaAteJogador().ToString("0.0") + " m)"
                             : " | jogador AINDA NÃO encontrado (precisa da tag \"Player\" ou arrastar no campo 'jogador')"));
    }

    void CriarCubo()
    {
        var cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubo.name = "CuboVisivel";
        cubo.transform.SetParent(transform, false);
        cubo.transform.localScale = Vector3.one * tamanhoDoCubo;
        // Fica apoiado no chão (a base do cubo na posição do gatilho)
        cubo.transform.localPosition = new Vector3(0f, tamanhoDoCubo * 0.5f, 0f);

        var sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Standard");
        if (sh != null)
        {
            var mat = new Material(sh) { name = "PND_Gatilho" };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", corDoCubo);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", corDoCubo);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", corDoCubo * 0.6f);
            }
            cubo.GetComponent<Renderer>().sharedMaterial = mat;
        }

        visual = cubo.transform;
        visualBase = cubo.transform.localPosition;
    }

    void ProcurarJogador()
    {
        if (jogador != null) return;
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            jogador = p.transform;
            Log("Jogador encontrado pela tag: " + p.name);
        }
    }

    float DistanciaAteJogador()
    {
        if (jogador == null) return float.PositiveInfinity;
        // Só na horizontal: assim a altura do pivô do gatilho/jogador não atrapalha
        Vector3 a = jogador.position, b = transform.position;
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    void Update()
    {
        // Animação do cubo (gira e flutua um pouco para chamar atenção)
        if (visual != null && girarCubo)
        {
            visual.Rotate(0f, 45f * Time.deltaTime, 0f, Space.World);
            visual.localPosition = visualBase + Vector3.up * (Mathf.Sin(Time.time * 2f) * 0.08f);
        }

        if (PanicoNoDisplayLauncher.IsRunning)
        {
            perto = false;
            return;
        }

        if (jogador == null)
        {
            ProcurarJogador();
            if (jogador == null) return;
        }

        float dist = DistanciaAteJogador();
        perto = dist <= distanciaInteracao;

        if (perto != pertoAnterior)
        {
            Log(perto ? "Jogador entrou na área (distância " + dist.ToString("0.0") + " m). Aperte E."
                      : "Jogador saiu da área.");
            pertoAnterior = perto;
        }

        var kb = Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame)
        {
            if (perto)
            {
                Log("E apertado: abrindo o minijogo...");
                perto = pertoAnterior = false;
                PanicoNoDisplayLauncher.Launch();
            }
            else
            {
                Log("E apertado, mas o jogador está longe: " + dist.ToString("0.0") + " m (máximo " + distanciaInteracao + " m). " +
                    "Gatilho em " + transform.position + ", jogador em " + jogador.position);
            }
        }
    }

    void OnGUI()
    {
        if (!mostrarMensagem || !perto || PanicoNoDisplayLauncher.IsRunning) return;

        if (estilo == null)
        {
            fundo = new Texture2D(1, 1);
            fundo.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.45f));
            fundo.Apply();
            estilo = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(13, Screen.height / 48),
                padding = new RectOffset(14, 14, 6, 6),
            };
            estilo.normal.textColor = new Color(1f, 1f, 1f, 0.9f);
            estilo.normal.background = fundo;
        }

        var texto = new GUIContent("[E]  " + mensagem);
        Vector2 tam = estilo.CalcSize(texto);
        var rect = new Rect((Screen.width - tam.x) * 0.5f, Screen.height * 0.82f, tam.x, tam.y);
        GUI.Label(rect, texto, estilo);
    }

    void OnDrawGizmos()
    {
        // Mostra o cubo também no modo de edição (antes de dar Play)
        if (GetComponentInChildren<Renderer>() == null)
        {
            Gizmos.color = new Color(corDoCubo.r, corDoCubo.g, corDoCubo.b, 0.5f);
            Gizmos.DrawCube(transform.position + Vector3.up * tamanhoDoCubo * 0.5f, Vector3.one * tamanhoDoCubo);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, distanciaInteracao);
    }
}
