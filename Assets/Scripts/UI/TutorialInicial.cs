using System.Collections.Generic;
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

        var painel = CriarImagem("Painel", fundo.transform, new Color32(22, 34, 49, 255));
        painel.rectTransform.sizeDelta = new Vector2(560, 300);

        var fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        CriarTexto("Título", painel.transform, fonte, titulo, 28, FontStyle.Bold,
            new Vector2(496, 64), 95);
        CriarTexto("Instruções", painel.transform, fonte, instrucoes, 22, FontStyle.Normal,
            new Vector2(496, 120), 0);

        var imagemBotao = CriarImagem("OK", painel.transform, Color.white);
        imagemBotao.rectTransform.sizeDelta = new Vector2(156, 52);
        imagemBotao.rectTransform.anchoredPosition = new Vector2(0, -101);

        var botao = imagemBotao.gameObject.AddComponent<Button>();
        botao.targetGraphic = imagemBotao;
        var cores = botao.colors;
        cores.normalColor = new Color32(165, 232, 181, 255);
        cores.highlightedColor = new Color32(204, 255, 214, 255);
        cores.pressedColor = new Color32(121, 198, 144, 255);
        cores.selectedColor = cores.normalColor;
        botao.colors = cores;
        botao.onClick.AddListener(Confirmar);
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
        EventSystem.current?.SetSelectedGameObject(botao.gameObject);
    }

    private void Confirmar()
    {
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (janela == null)
            return;

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
