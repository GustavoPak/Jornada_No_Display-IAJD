
using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;
using System.Diagnostics;

public class IniciarMinigame : MonoBehaviour
{
    public Transform jogador;
    public float distanciaInteracao = 3f;

    private Process jogoGodot;
    private bool avisoExibido;

    void Update()
    {
        if (jogoGodot != null)
        {
            if (jogoGodot.HasExited)
            {
                jogoGodot.Dispose();
                jogoGodot = null;

                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                UnityEngine.Debug.Log("Minigame encerrado.");
            }

            return;
        }

        if (jogador == null)
            return;

        float distancia = Vector3.Distance(
            jogador.position,
            transform.position
        );

        if (distancia <= distanciaInteracao)
        {
            if (!avisoExibido)
            {
                UnityEngine.Debug.Log("Pressione E para jogar!");
                avisoExibido = true;
            }

            if (Keyboard.current != null &&
                Keyboard.current.eKey.wasPressedThisFrame)
            {
                AbrirMinigame();
            }
        }
        else
        {
            avisoExibido = false;
        }
    }

    void AbrirMinigame()
    {
        string pasta = Path.Combine(
            Application.streamingAssetsPath,
            "MiniGames",
            "Pac_Code"
        );

        string executavel = Path.Combine(
            pasta,
            "PAC-CODE_Jornada no Display.x86_64"
        );

        if (!File.Exists(executavel))
        {
            UnityEngine.Debug.LogError(
                "Executável não encontrado: " + executavel
            );
            return;
        }

        try
        {
            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = executavel,
                WorkingDirectory = pasta,
                UseShellExecute = false
            };

            jogoGodot = Process.Start(info);

            if (jogoGodot != null)
            {
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                UnityEngine.Debug.Log("Minigame iniciado!");
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError(
                "Erro ao iniciar: " + e.Message
            );
        }
    }
}
