using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

public class MotherboardRepair : MonoBehaviour
{
    public SkillCheck skillCheck;

    public PlayerWallet playerWallet;
    public int recompensa = 100;

    public float progresso = 0f;
    public float progressoMaximo = 100f;

    private bool playerPerto = false;
    private bool reparando = false;
    private bool concluido = false;

    public ThirdPersonController playerController;

    void Start()
    {
    }

    void Update()
    {
        if (concluido)
            return;

        if (playerPerto && !reparando && Keyboard.current.eKey.wasPressedThisFrame)
        {
            ComecarReparo();
        }
    }

    void ComecarReparo()
    {
        reparando = true;

        playerController.enabled = false;

        skillCheck.IniciarSkillCheck(this);
    }

    public void AcertouSkillCheck()
    {
        progresso += 20f;

        VerificarProgresso();
    }

    public void ErrouSkillCheck()
    {
        progresso -= 5f;

        if (progresso < 0)
            progresso = 0;

        ContinuarReparo();
    }

    void VerificarProgresso()
    {
        if (progresso >= progressoMaximo)
        {
            progresso = progressoMaximo;
            ConcluirReparo();
        }
        else
        {
            ContinuarReparo();
        }
    }

    void ContinuarReparo()
    {
        skillCheck.IniciarSkillCheck(this);
    }

    void ConcluirReparo()
    {
        concluido = true;
        reparando = false;

        skillCheck.Fechar();

        playerController.enabled = true;

        playerWallet.AdicionarDinheiro(recompensa);
    }

    private void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Player"))
        {
            playerPerto = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerPerto = false;
        }
    }
}