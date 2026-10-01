using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.InputSystem;

public class PlayerIdentity : NetworkBehaviour
{
    private Material playerMaterial;

    private bool tryingColorChange;

    private void Awake()
    {
        playerMaterial = GetComponent<MeshRenderer>().material;
    }

    private readonly NetworkVariable<FixedString32Bytes> userName = new NetworkVariable<FixedString32Bytes>(
        "Undefined",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<Color> userColor = new NetworkVariable<Color>(
        Color.white,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        userName.Value = "Jugador " + OwnerClientId;
        userColor.Value = GetRandomColor();
        playerMaterial.color = userColor.Value;

        //Debug.Log(userName.Value);
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.cKey.wasPressedThisFrame) ;
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        if (tryingColorChange)
        {

        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SubmitColorChangeInputServerRpc()
    {
        tryingColorChange = true;
    }

    public Color GetRandomColor()
    {
        return Random.ColorHSV();
    }
}
