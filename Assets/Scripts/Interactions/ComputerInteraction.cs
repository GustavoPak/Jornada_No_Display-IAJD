using UnityEngine;
using UnityEngine.InputSystem;

public class ComputerInteraction : MonoBehaviour
{
    public ShopUI shopUI;

    private bool playerPerto;

    void Update()
    {
        if (playerPerto && Keyboard.current.eKey.wasPressedThisFrame)
        {
            shopUI.AbrirLoja();
        }
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