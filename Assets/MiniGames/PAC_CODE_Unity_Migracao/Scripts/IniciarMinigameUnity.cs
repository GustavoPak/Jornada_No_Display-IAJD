using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum MinigameDisponivel
{
    PAC_CODE,
    PANICO_NO_LABORATORIO
}

public class IniciarMinigameUnity : MonoBehaviour
{
    [Header("Referências")]
    public Transform jogador;

    [Header("Interação")]
    public float distanciaInteracao = 10f;

    [Header("Minigames deste computador")]
    public List<MinigameDisponivel> minigamesDisponiveis =
        new List<MinigameDisponivel> { MinigameDisponivel.PAC_CODE };

    private bool menuAberto;

    private PACCodeGame jogo;
    private PanicoNoLaboratorioGame panicoJogo;

    private GameObject objetoJogo;

    private float escalaAnterior;
    private CursorLockMode cursorAnterior;
    private bool cursorVisivelAnterior;

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (jogo != null || panicoJogo != null)
            return;

        if (menuAberto)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
                FecharMenu();

            return;
        }

        if (jogador == null)
            return;

        float distancia = Vector3.Distance(
            jogador.position,
            transform.position
        );

        if (distancia <= distanciaInteracao &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            AbrirMenu();
        }
    }

    private void AbrirMenu()
    {
        escalaAnterior = Time.timeScale;
        cursorAnterior = Cursor.lockState;
        cursorVisivelAnterior = Cursor.visible;

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        menuAberto = true;

        Debug.Log("Menu de minigames aberto.");
    }

    private void IniciarPACCode()
    {
        objetoJogo = new GameObject("PAC-CODE - Minigame Unity");

        jogo = objetoJogo.AddComponent<PACCodeGame>();
        jogo.ExitRequested += VoltarAoMenu;

        Debug.Log("PAC-CODE iniciado dentro da Unity.");
    }

    private void IniciarPanicoNoLaboratorio()
    {
        // O minigame utiliza Time.deltaTime.
        // Por isso, precisamos descongelar o tempo.
        Time.timeScale = 1f;

        objetoJogo = new GameObject(
            "Panico no Laboratorio - Minigame Unity"
        );

        panicoJogo = objetoJogo.AddComponent<PanicoNoLaboratorioGame>();
        panicoJogo.ExitRequested += VoltarAoMenu;

        Debug.Log("Pânico no Laboratório iniciado dentro da Unity.");
    }

    private void VoltarAoMenu()
    {
        if (objetoJogo != null)
            Destroy(objetoJogo);

        objetoJogo = null;

        jogo = null;
        panicoJogo = null;

        // Congela novamente o jogo enquanto o seletor está aberto.
        Time.timeScale = 0f;

        menuAberto = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Retornou ao menu de minigames.");
    }

    private void FecharMenu()
    {
        menuAberto = false;

        Time.timeScale = escalaAnterior;

        Cursor.lockState = cursorAnterior;
        Cursor.visible = cursorVisivelAnterior;

        Debug.Log("Menu fechado. Retornando ao laboratório.");
    }

    private void OnGUI()
    {
        if (!menuAberto || jogo != null || panicoJogo != null)
            return;

        float largura = 520;
        float altura = 360;

        Rect painel = new Rect(
            (Screen.width - largura) / 2,
            (Screen.height - altura) / 2,
            largura,
            altura
        );

        GUI.Box(painel, "");

        GUI.Label(
            new Rect(painel.x + 20, painel.y + 20, 480, 45),
            "CENTRAL DE MINIGAMES"
        );

        GUI.Label(
            new Rect(painel.x + 20, painel.y + 65, 480, 30),
            "Escolha uma atividade:"
        );

        float posicaoY = painel.y + 115;

        foreach (MinigameDisponivel minigame in minigamesDisponiveis)
        {
            string nome = minigame.ToString().Replace("_", " ");

            if (GUI.Button(
                new Rect(painel.x + 40, posicaoY, 440, 55),
                nome))
            {
                switch (minigame)
                {
                    case MinigameDisponivel.PAC_CODE:
                        IniciarPACCode();
                        break;

                    case MinigameDisponivel.PANICO_NO_LABORATORIO:
                        IniciarPanicoNoLaboratorio();
                        break;
                }
            }

            posicaoY += 70;
        }

        if (minigamesDisponiveis.Count == 0)
        {
            GUI.Label(
                new Rect(painel.x + 40, posicaoY, 440, 40),
                "Nenhum minigame disponível."
            );
        }

        GUI.Label(
            new Rect(painel.x + 20, painel.y + altura - 45, 480, 30),
            "ESC - Voltar ao laboratório"
        );
    }

    private void OnDisable()
    {
        if (objetoJogo != null)
            Destroy(objetoJogo);

        objetoJogo = null;

        jogo = null;
        panicoJogo = null;

        if (menuAberto)
        {
            Time.timeScale = escalaAnterior;

            Cursor.lockState = cursorAnterior;
            Cursor.visible = cursorVisivelAnterior;

            menuAberto = false;
        }
    }
}