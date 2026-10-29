using UnityEngine;
using StarterAssets;

public class ShopUI : MonoBehaviour
{
    public GameObject painelLoja;
    public GameObject player;
    public ThirdPersonController playerController;

    void Start()
    {
        painelLoja.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void AbrirLoja()
    {
        painelLoja.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        playerController.enabled = false;
    }

    public void FecharLoja()
    {
        painelLoja.SetActive(false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Locked;

        playerController.enabled = true;
    }
}