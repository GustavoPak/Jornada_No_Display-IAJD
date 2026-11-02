
using UnityEngine;
using StarterAssets;

public class SistemaPosicionamento : MonoBehaviour
{
    public ShopUI shopUI;
    public ThirdPersonController playerController;
    public LayerMask camadaChao;

    private GameObject objetoAtual;
    private ProdutoLoja produtoAtual;
    private PlayerWallet carteira;

    private bool posicionando = false;
    private bool podePosicionar = false;

    public void IniciarPosicionamento(
        ProdutoLoja produto,
        PlayerWallet wallet)
    {
        if (wallet.dinheiro < produto.preco)
        {
            Debug.Log("Dinheiro insuficiente!");
            return;
        }

        produtoAtual = produto;
        carteira = wallet;

        objetoAtual = Instantiate(produto.prefab);

        posicionando = true;

        // Desativa os colisores durante o posicionamento
        foreach (Collider col in objetoAtual.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        shopUI.painelLoja.SetActive(false);
        playerController.enabled = false;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void Update()
    {
        if (!posicionando) return;

        // Detecta o chão através do cursor
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, camadaChao))
        {
            objetoAtual.transform.position = hit.point;
            podePosicionar = true;
        }
        else
        {
            podePosicionar = false;
        }

        // Girar objeto
        if (Input.GetKeyDown(KeyCode.R))
        {
            objetoAtual.transform.Rotate(0, 90, 0);
        }

        // Confirmar posicionamento
        if (Input.GetMouseButtonDown(0) && podePosicionar)
        {
            ConfirmarPosicionamento();
        }

        // Cancelar
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelarPosicionamento();
        }
    }

    void ConfirmarPosicionamento()
    {
        if (!carteira.GastarDinheiro(produtoAtual.preco))
        {
            Debug.Log("Dinheiro insuficiente!");
            return;
        }

        foreach (Collider col in objetoAtual.GetComponentsInChildren<Collider>())
        {
            col.enabled = true;
        }

        objetoAtual = null;
        posicionando = false;

        shopUI.FecharLoja();
    }

    void CancelarPosicionamento()
    {
        Destroy(objetoAtual);

        objetoAtual = null;
        posicionando = false;

        shopUI.AbrirLoja();
    }
}
