using UnityEngine;
using TMPro;

public class MoneyUI : MonoBehaviour
{
    public PlayerWallet playerWallet;
    public TMP_Text moneyText;
    public AudioClip EarnMoney;

    void Update()
    {
        moneyText.text = "$ " + playerWallet.dinheiro;
    }
}