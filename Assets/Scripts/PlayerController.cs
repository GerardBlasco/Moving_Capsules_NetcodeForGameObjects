using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    private float speed = 5f;

    void Update()
    {
        if (!IsOwner) return; // Solo el jugador local controla su personaje

        float horizontal = Keyboard.current.aKey.IsPressed() ? -1 : 0 + (Keyboard.current.dKey.IsPressed() ? 1 : 0); // A/D
        float vertical = Keyboard.current.sKey.IsPressed() ? -1 : 0 + (Keyboard.current.wKey.IsPressed() ? 1 : 0); // S/W

        Vector3 movement = new Vector3(horizontal, 0, vertical);

        transform.Translate(movement * speed * Time.deltaTime);
    }
}
