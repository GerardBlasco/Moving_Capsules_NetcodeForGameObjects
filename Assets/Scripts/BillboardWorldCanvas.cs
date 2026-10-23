using UnityEngine;

public class BillboardWorldCanvas : MonoBehaviour
{
    Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        transform.rotation = Quaternion.LookRotation(mainCamera.transform.forward - (-transform.forward));
    }
}
