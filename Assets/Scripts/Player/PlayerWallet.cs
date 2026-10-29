using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    public int dinheiro = 0;
    public AudioSource audioSource;
    public AudioClip somGanharDinheiro;
    public AudioClip somGastarDinheiro;

    public void AdicionarDinheiro(int quantidade)
    {
        dinheiro += quantidade;

        Debug.Log("Dinheiro atual: " + dinheiro);

        audioSource.PlayOneShot(somGanharDinheiro);
    }

    public bool GastarDinheiro(int quantidade)
    {
        if (dinheiro >= quantidade)
        {
            dinheiro -= quantidade;
            audioSource.PlayOneShot(somGastarDinheiro);
            return true;
        }

        return false;
    }
}