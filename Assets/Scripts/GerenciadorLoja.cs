
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

[Serializable]
public class ItemLoja
{
    public ProdutoLoja produto;
    public GameObject objetoLaboratorio;
    public GameObject objetoLaboratorio1;
    public GameObject objetoLaboratorio2;
    public GameObject objetoLaboratorio3;
    public GameObject objetoLaboratorio4;

}

public class GerenciadorLoja : MonoBehaviour
{
    [Header("Produtos disponíveis")]
    public ItemLoja[] itens;

    [Header("Interface da loja")]
    public Transform content;
    public Button botaoProdutoPrefab;

    [Header("Dinheiro do jogador")]
    public PlayerWallet playerWallet;

    void Start()
    {
        AtualizarCatalogo();
    }

    void AtualizarCatalogo()
    {
        foreach (Transform filho in content)
        {
            Destroy(filho.gameObject);
        }

        foreach (ItemLoja item in itens)
        {
            if (item == null || item.produto == null) continue;

            Button novoBotao = Instantiate(botaoProdutoPrefab, content);

            TMP_Text texto = novoBotao.GetComponentInChildren<TMP_Text>();

            if (texto != null)
            {
                texto.text = item.produto.nome + " - $" + item.produto.preco;
            }

            ItemLoja itemSelecionado = item;

            novoBotao.onClick.AddListener(() =>
            {
                ComprarProduto(itemSelecionado);
            });
        }
    }

    void ComprarProduto(ItemLoja item)
    {
        if (item.objetoLaboratorio == null)
        {
            Debug.LogError("Objeto do laboratório não configurado!");
            return;
        }

        if (item.objetoLaboratorio.activeSelf)
        {
            Debug.Log("Este objeto já está ativo!");
            return;
        }

        if (playerWallet == null)
        {
            Debug.LogError("PlayerWallet não configurado!");
            return;
        }

        if (playerWallet.GastarDinheiro(item.produto.preco))
        {
            item.objetoLaboratorio.SetActive(true);
            item.objetoLaboratorio1.SetActive(true);
            item.objetoLaboratorio2.SetActive(true);
            item.objetoLaboratorio3.SetActive(true);
            //item.objetoLaboratorio4.SetActive(true);
            Debug.Log("Compra realizada: " + item.produto.nome);
        }
        else
        {
            Debug.Log("Dinheiro insuficiente!");
        }
    }
}
