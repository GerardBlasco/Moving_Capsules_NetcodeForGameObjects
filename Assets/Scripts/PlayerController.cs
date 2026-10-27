using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : NetworkBehaviour
{
    private float speed = 5f;

    private float rotationSpeed = 500f;

    private float jumpForce = 5f;

    private Vector2 inputVector;

    private bool tryiedToJump;

    private bool tryiedToGrabItem;
    private bool tryiedToThrowItem;
    private bool preparingToThrow;

    [SerializeField] private float minThrowPower = 1f;
    [SerializeField] private float maxThrowPower = 20f;
    private float throwPowerChargeSpeed = 10f;

    private Rigidbody rb;

    private AudioSource audioSource;

    private bool onKnockback;

    bool isGrounded;

    NetworkGrabbable grabbable;

    [SerializeField] Transform grabbablePivot;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
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

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                InteractServerRpc();
            }

            SubmitThrowPreparationServerRpc(Keyboard.current.eKey.isPressed);

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

        if (onKnockback)
        {
            rb.linearVelocity = rb.linearVelocity;
        }
        else
        {
            rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
        }

        // Control de la rotación
        if (moveDirection.sqrMagnitude != 0f)
        {
            Quaternion rotationDirection = Quaternion.LookRotation(moveDirection, Vector3.up);
            Quaternion targetRotation = Quaternion.RotateTowards(rb.rotation, rotationDirection, rotationSpeed * Time.fixedDeltaTime);

            rb.MoveRotation(targetRotation);
        }

        // Control del estado de si el jugador esta en contacto con el suelo
        Vector3 raycastPoint = new Vector3(transform.position.x, transform.position.y - 0.6f, transform.position.z);
        
        if (Physics.Raycast(raycastPoint, transform.TransformDirection(Vector3.down), 0.5f))
        {
            Debug.DrawRay(raycastPoint, transform.TransformDirection(Vector3.down) * 0.5f, Color.yellow);
            //Debug.Log("Hit something");
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
            InvokeSoundClientRpc();
        }

        tryiedToJump = false;

        if (tryiedToThrowItem)
        {
            tryiedToThrowItem = false;
            StartCoroutine(DynamicThrow());
        }
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

    /*[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SubmitGrabServerRpc()
    {
        tryiedToGrabItem = true;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SubmitThrowServerRpc()
    {
        Debug.Log("THROW RPC");
        tryiedToThrowItem = true;
    }*/

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SubmitThrowPreparationServerRpc(bool value)
    {
        preparingToThrow = value;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void InvokeSoundClientRpc()
    {
        audioSource.Play();
    }

    public void AddKnockback(Vector3 knockback, float knockbackDuration)
    {
        if (!IsServer) return;

        onKnockback = true;
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(knockback, ForceMode.Impulse);
        StartCoroutine(KnockbackTimer(knockbackDuration));
    }

    public IEnumerator KnockbackTimer(float duration)
    {
        if (!isGrounded)
        {
            yield return new WaitUntil(() => isGrounded);
            yield return new WaitForSeconds(duration * 0.2f);
        }
        else
        {
            yield return new WaitForSeconds(duration);
        }
        
        onKnockback = false;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsServer) return;

        if (other.transform.CompareTag("Grabbable") && other.GetComponent<NetworkObject>() && tryiedToGrabItem)
        {
            tryiedToGrabItem = false;

            grabbable = other.GetComponent<NetworkGrabbable>();

            grabbable.GrabObject(GetComponent<NetworkObject>().OwnerClientId, transform, new Vector3(0, 0, 1.5f));
        }
    }

    private void ThrowGrabbable(float throwPower)
    {
        grabbable.DropObject();
        grabbable.GetRigidBody().AddForce((grabbable.transform.position - transform.position).normalized * throwPower, ForceMode.Impulse);
        grabbable = null;
    }

    public IEnumerator DynamicThrow()
    {
        //yield return new WaitUntil(() => !preparingToThrow);

        float throwPower = minThrowPower;

        grabbable.StartShakeClientRpc();

        while (preparingToThrow)
        {
            throwPower += throwPowerChargeSpeed * Time.deltaTime;
            throwPower = Mathf.Clamp(throwPower, minThrowPower, maxThrowPower);

            Debug.Log("Current throw power: " + throwPower);

            yield return null;
        }

        grabbable.StopShakeClientRpc();
        ThrowGrabbable(throwPower);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void InteractServerRpc()
    {
        if (grabbable != null)
        {
            tryiedToThrowItem = true;
        }
        else
        {
            tryiedToGrabItem = true;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (grabbable)
        {
            grabbable.DropObject();
            grabbable.transform.position = transform.position;
        }
    }
}
