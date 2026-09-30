using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    private float speed = 5f;

    private float rotationSpeed = 500f;

    private float jumpForce = 5f;

    private Vector2 inputVector;

    private bool tryiedToJump;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            rb.isKinematic = false;
        } else
        {
            rb.isKinematic = true;
        }
    }

    void Update()
    {
        // REGLA DE ORO DE NETCODE: SOlo el cliente dueño (IsOwner) lee sus propias entradas de teclado
        if (!IsOwner) return;

        // 1. Lectura del input local en el cliente dueño
        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vertical -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vertical += 1f;

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                // 2. Enviar la entrada de control de salto al servidor mediante un ServerRpc
                SubmitJumpServerRpc();
            }
        }

        Vector2 input = new Vector2(horizontal, vertical).normalized;

        // 3. Enviar la entrada de control de movimiento al servidor mediante un ServerRpc
        SubmitInputServerRpc(input);
    }

    private void FixedUpdate()
    {
        // REGLA DE ORO DE NETCODE: Solo el server calcula y aplica cambios de físicas autoritativas
        if (!IsServer) return;

        // Control del movimiento
        Vector3 moveDirection = new Vector3(inputVector.x, 0f, inputVector.y);

        Vector3 targetVelocity = moveDirection * speed;
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);

        // Control de la rotación
        if (moveDirection.sqrMagnitude != 0f)
        {
            Quaternion rotationDirection = Quaternion.LookRotation(-moveDirection, Vector3.up);
            Quaternion targetRotation = Quaternion.RotateTowards(rb.rotation, rotationDirection, rotationSpeed * Time.fixedDeltaTime);

            rb.MoveRotation(targetRotation);
        }

        // Control del estado de si el jugador esta en contacto con el suelo
        Vector3 raycastPoint = new Vector3(transform.position.x, transform.position.y - 0.6f, transform.position.z);
        bool isGrounded;

        if (Physics.Raycast(raycastPoint, transform.TransformDirection(Vector3.down), 0.5f))
        {
            Debug.DrawRay(raycastPoint, transform.TransformDirection(Vector3.down) * 0.5f, Color.yellow);
            Debug.Log("Hit something");
            isGrounded = true;
        }
        else
        {
            Debug.DrawRay(raycastPoint, transform.TransformDirection(Vector3.down) * 0.5f, Color.yellow);
            isGrounded = false;
        }

        // Control del salto
        if (tryiedToJump && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
        tryiedToJump = false;
    }

    // El server recibe la dirección pedida por el cliente
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SubmitInputServerRpc(Vector2 input)
    {
        inputVector = input;
    }

    // El server recibe la peticion de salto pedida por el cliente
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SubmitJumpServerRpc()
    {
        tryiedToJump = true;
    }
}
