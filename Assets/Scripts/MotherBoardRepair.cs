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
        Debug.Log("MotherboardRepair iniciou!");
    }

    void Update()
    {
        if (concluido)
            return;

        if (playerPerto && !reparando && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Debug.Log("E apertado perto da placa-mãe");
            ComecarReparo();
        }
    }

    void ComecarReparo()
    {
        reparando = true;

        playerController.enabled = false;

        Debug.Log("Começou o reparo");

        skillCheck.IniciarSkillCheck(this);
    }

    public void AcertouSkillCheck()
    {
        progresso += 20f;
        Debug.Log("Acertou! Progresso: " + progresso);

        VerificarProgresso();
    }

    public void ErrouSkillCheck()
    {
        progresso -= 5f;

        if (progresso < 0)
            progresso = 0;

        Debug.Log("Errou! Progresso: " + progresso);

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

        Debug.Log("PLACA-MÃE CONSERTADA!");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Algo entrou no trigger: " + other.name);

        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entrou no trigger da placa-mãe");
            playerPerto = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player saiu do trigger");
            playerPerto = false;
        }
    }
}