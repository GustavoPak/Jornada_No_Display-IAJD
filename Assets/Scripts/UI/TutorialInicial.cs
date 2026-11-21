using System.Collections.Generic;
using System.Collections;
using StarterAssets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public sealed class TutorialInicial : MonoBehaviour
{
    [SerializeField] private string titulo = "Bem-vindo ao Jornada No Display!";
    [SerializeField, TextArea(3, 5)] private string instrucoes =
        "Use WASD para se mover e o mouse para olhar ao redor.\n\n" +
        "Aproxime-se dos equipamentos e pressione E para interagir.";

    [SerializeField] private GameObject jogador;
    private StarterAssetsInputs controles;
    [SerializeField] private Behaviour[] jogabilidade;

    private readonly List<Behaviour> pausados = new();
    private GameObject janela;
    [SerializeField] private Sprite retratoMentora;
    private RectTransform painelAnimado;
    private CanvasGroup grupoAnimado;
    private Button botaoOk;
    [SerializeField] private AudioClip somConfirmacao;
    private float tempoAnterior;
    private CursorLockMode bloqueioAnterior;
    private bool cursorVisivelAntes;
    private bool controleBloqueavaCursor;

    private void Start()
    {
        var tela = new GameObject("Tela de tutorial", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        tela.transform.SetParent(transform, false);

        var canvas = tela.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        var escala = tela.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(800, 600);
        escala.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        var fundo = CriarImagem("Fundo escuro", tela.transform, new Color(0.03f, 0.06f, 0.10f, 0.78f));
        fundo.rectTransform.anchorMin = Vector2.zero;
        fundo.rectTransform.anchorMax = Vector2.one;
        fundo.rectTransform.offsetMin = Vector2.zero;
        fundo.rectTransform.offsetMax = Vector2.zero;

        var painel = CriarImagem("Painel", fundo.transform, new Color32(15, 33, 42, 255));
        painel.rectTransform.sizeDelta = new Vector2(712, 284);
        painelAnimado = painel.rectTransform;
        grupoAnimado = fundo.gameObject.AddComponent<CanvasGroup>();

        var menta = new Color32(105, 244, 192, 255);
        var faixa = Decorar("Fundo da mentora", painel.transform, -250, 0, 212, 284,
            new Color32(36, 66, 70, 255));
        var retrato = Decorar("Mentora", faixa.transform, 0, 0, 212, 260, Color.white);
        retrato.sprite = retratoMentora;
        retrato.preserveAspect = true;
        retrato.enabled = retratoMentora != null;
        Decorar("Linha superior", painel.transform, 0, 140, 712, 4, menta);

        var fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var cabecalho = CriarTexto("Título", painel.transform, fonte, titulo, 28, FontStyle.Bold,
            new Vector2(442, 72), 78);
        cabecalho.rectTransform.anchoredPosition = new Vector2(106, 78);
        cabecalho.alignment = TextAnchor.MiddleLeft;
        var corpo = CriarTexto("Instruções", painel.transform, fonte, instrucoes, 19, FontStyle.Normal,
            new Vector2(442, 94), -5);
        corpo.rectTransform.anchoredPosition = new Vector2(106, -5);
        corpo.alignment = TextAnchor.MiddleLeft;
        corpo.color = new Color32(214, 231, 231, 255);

        var imagemBotao = CriarImagem("OK", painel.transform, Color.white);
        imagemBotao.rectTransform.sizeDelta = new Vector2(122, 42);
        imagemBotao.rectTransform.anchoredPosition = new Vector2(266, -94);

        var botao = imagemBotao.gameObject.AddComponent<Button>();
        botao.targetGraphic = imagemBotao;
        var cores = botao.colors;
        cores.normalColor = menta;
        cores.highlightedColor = new Color32(204, 255, 214, 255);
        cores.pressedColor = new Color32(121, 198, 144, 255);
        cores.selectedColor = new Color32(151, 255, 211, 255);
        botao.colors = cores;
        botao.onClick.AddListener(Confirmar);
        botaoOk = botao;
        botaoOk.interactable = false;
        CriarTexto("Rótulo", botao.transform, fonte, "OK", 23, FontStyle.Bold,
            imagemBotao.rectTransform.sizeDelta, 0).color = new Color32(13, 42, 28, 255);

        janela = tela;
        controles = jogador.GetComponent<StarterAssetsInputs>();
        tempoAnterior = Time.timeScale;
        bloqueioAnterior = Cursor.lockState;
        cursorVisivelAntes = Cursor.visible;
        if (controles != null)
        {
            controleBloqueavaCursor = controles.cursorLocked;
            controles.cursorLocked = false;
        }

        foreach (var componente in jogabilidade)
        {
            if (componente != null && componente.enabled)
            {
                componente.enabled = false;
                pausados.Add(componente);
            }
        }

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        AplicarAnimacao(0f);
        StartCoroutine(Abrir());
    }

    private IEnumerator Abrir()
    {
        yield return Animar(0f, 1f, 0.28f);
        botaoOk.interactable = true;
        EventSystem.current?.SetSelectedGameObject(botaoOk.gameObject);
    }

    private void Confirmar()
    {
        if (!botaoOk.interactable)
            return;

        botaoOk.interactable = false;
        if (somConfirmacao != null)
        {
            var som = gameObject.AddComponent<AudioSource>();
            som.playOnAwake = false;
            som.ignoreListenerPause = true;
            som.spatialBlend = 0f;
            som.PlayOneShot(somConfirmacao);
        }
        StartCoroutine(Fechar());
    }

    private IEnumerator Fechar()
    {
        yield return Animar(1f, 0f, 0.20f);
        enabled = false;
    }

    private IEnumerator Animar(float inicio, float fim, float duracao)
    {
        for (float decorrido = 0f; decorrido < duracao;)
        {
            yield return null;
            decorrido += Time.unscaledDeltaTime;
            AplicarAnimacao(Mathf.SmoothStep(inicio, fim, decorrido / duracao));
        }
    }

    private void AplicarAnimacao(float progresso)
    {
        painelAnimado.localScale = Vector3.one * Mathf.Lerp(0.72f, 1f, progresso);
        grupoAnimado.alpha = progresso;
    }

    private void OnDisable()
    {
        if (janela == null)
            return;

        StopAllCoroutines();
        Destroy(janela);
        janela = null;

        if (controles != null)
        {
            controles.cursorLocked = controleBloqueavaCursor;
            controles.LookInput(Vector2.zero);
            controles.JumpInput(false);
        }

        foreach (var componente in pausados)
        {
            if (componente != null)
                componente.enabled = true;
        }

        pausados.Clear();
        Time.timeScale = tempoAnterior;
        Cursor.lockState = bloqueioAnterior;
        Cursor.visible = cursorVisivelAntes;
    }

    private static Image Decorar(string nome, Transform pai, float x, float y,
        float largura, float altura, Color cor)
    {
        var imagem = CriarImagem(nome, pai, cor);
        imagem.rectTransform.sizeDelta = new Vector2(largura, altura);
        imagem.rectTransform.anchoredPosition = new Vector2(x, y);
        imagem.raycastTarget = false;
        return imagem;
    }

    private static Image CriarImagem(string nome, Transform pai, Color cor)
    {
        var objeto = new GameObject(nome, typeof(RectTransform), typeof(Image));
        objeto.transform.SetParent(pai, false);
        var imagem = objeto.GetComponent<Image>();
        imagem.color = cor;
        return imagem;
    }

    private static Text CriarTexto(string nome, Transform pai, Font fonte, string conteudo,
        int tamanho, FontStyle estilo, Vector2 dimensoes, float altura)
    {
        var objeto = new GameObject(nome, typeof(RectTransform), typeof(Text));
        objeto.transform.SetParent(pai, false);

        var texto = objeto.GetComponent<Text>();
        texto.font = fonte;
        texto.text = conteudo;
        texto.fontSize = tamanho;
        texto.fontStyle = estilo;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = Color.white;
        texto.raycastTarget = false;
        texto.rectTransform.sizeDelta = dimensoes;
        texto.rectTransform.anchoredPosition = new Vector2(0, altura);
        return texto;
    }
}
