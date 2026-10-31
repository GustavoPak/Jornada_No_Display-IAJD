
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GerenciadorLoja : MonoBehaviour
{
    [Header("Produtos disponíveis")]
    public ProdutoLoja[] produtos;

    [Header("Interface da loja")]
    public Transform content;
    public Button botaoProdutoPrefab;

    void Start()
    {
        AtualizarCatalogo();
    }

    void AtualizarCatalogo()
    {
        // Limpa os botões anteriores
        foreach (Transform filho in content)
        {
            Destroy(filho.gameObject);
        }

        // Cria um botão para cada produto
        foreach (ProdutoLoja produto in produtos)
        {
            if (produto == null) continue;

            Button novoBotao = Instantiate(
                botaoProdutoPrefab,
                content
            );

            TMP_Text texto = novoBotao.GetComponentInChildren<TMP_Text>();

            if (texto != null)
            {
                texto.text = produto.nome + " - $" + produto.preco;
            }

            novoBotao.onClick.AddListener(() =>
            {
                SelecionarProduto(produto);
            });
        }
    }

    void SelecionarProduto(ProdutoLoja produto)
    {
        Debug.Log("Produto selecionado: " + produto.nome);
        Debug.Log("Preço: " + produto.preco);
    }
}
