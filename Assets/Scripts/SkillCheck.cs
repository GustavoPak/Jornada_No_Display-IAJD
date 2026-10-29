using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SkillCheck : MonoBehaviour
{
    public GameObject painelSkillCheck;

    public RectTransform ponteiro;
    public RectTransform areaAcerto;

    public float velocidade = 500f;

    private bool ativo = false;
    private float direcao = 1f;

    public AudioSource audioSource;
    public AudioClip somSucesso;
    public AudioClip somFalha;

    private MotherboardRepair reparoAtual;

    void Start()
    {
        painelSkillCheck.SetActive(false);
    }

    void Update()
    {
        if (!ativo)
            return;

        MoverPonteiro();

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            VerificarAcerto();
        }
    }

    public void IniciarSkillCheck(MotherboardRepair reparo)
    {
        reparoAtual = reparo;

        painelSkillCheck.SetActive(true);

        ativo = true;

        direcao = 1f;

        ponteiro.anchoredPosition = new Vector2(-300f, ponteiro.anchoredPosition.y);
    }

    void MoverPonteiro()
    {
        Vector2 posicao = ponteiro.anchoredPosition;

        posicao.x += velocidade * direcao * Time.deltaTime;

        if (posicao.x >= 300f)
        {
            direcao = -1f;
        }

        if (posicao.x <= -300f)
        {
            direcao = 1f;
        }

        ponteiro.anchoredPosition = posicao;
    }

    void VerificarAcerto()
    {
        float posPonteiro = ponteiro.anchoredPosition.x;

        float centroArea = areaAcerto.anchoredPosition.x;

        float metadeArea = areaAcerto.rect.width / 2f;

        bool acertou =
            posPonteiro >= centroArea - metadeArea &&
            posPonteiro <= centroArea + metadeArea;

        ativo = false;

        painelSkillCheck.SetActive(false);

        if (acertou)
        {
            audioSource.PlayOneShot(somSucesso);

            reparoAtual.AcertouSkillCheck();
        }
        else
        {
            audioSource.PlayOneShot(somFalha);

            reparoAtual.ErrouSkillCheck();
        }
    }

    public void Fechar()
    {
        ativo = false;
        painelSkillCheck.SetActive(false);
    }
}