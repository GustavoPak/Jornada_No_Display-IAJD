
using UnityEngine;

[CreateAssetMenu(fileName = "NovoProduto", menuName = "Loja/Produto")]
public class ProdutoLoja : ScriptableObject
{
    public string nome;
    public int preco;
    public GameObject prefab;
}