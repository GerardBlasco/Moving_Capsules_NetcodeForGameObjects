using UnityEngine;
using Unity.Netcode;
using DG.Tweening;

public class NetworkGrabbable : NetworkBehaviour
{
    Rigidbody rb;
    [SerializeField] Canvas proximityIcon;
    private Tween shakeTween;
    private Vector3 initialPos;

    private readonly NetworkVariable<bool> isGrabbed = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        isGrabbed.OnValueChanged += OnGrabbedChange;

        if (IsServer)
        {
            rb.isKinematic = false;
        }
        else
        {
            rb.isKinematic = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.transform.CompareTag("Player") && other.GetComponent<NetworkObject>())
        {
            ToggleProximityIconClientRpc(true, RpcParamsToSingleClient(other.GetComponent<NetworkObject>().OwnerClientId));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;

        if (other.transform.CompareTag("Player") && other.GetComponent<NetworkObject>())
        {
            ToggleProximityIconClientRpc(false, RpcParamsToSingleClient(other.GetComponent<NetworkObject>().OwnerClientId));
        }
    }

    private ClientRpcParams RpcParamsToSingleClient(ulong targetClientId)
    {
        ulong[] target = new ulong[1];

        target[0] = targetClientId;

        ClientRpcParams rpcParams = new ClientRpcParams();
        rpcParams.Send.TargetClientIds = target;

        return rpcParams;
    }

    public void GrabObject(ulong clientID, Transform parent, Vector3 grabbedPos)
    {
        if (isGrabbed.Value) return;

        Debug.Log($"Objeto agarrado por {clientID}");

        bool parentedSuccessfully = NetworkObject.TrySetParent(parent.transform, false);

        NetworkObject.ChangeOwnership(clientID);
        Debug.Log($"[RED] El objeto '{name}' ahora tiene de owner a '{clientID}'.");

        transform.localPosition = grabbedPos;
        rb.isKinematic = true;
        isGrabbed.Value = true;

        if (!parentedSuccessfully)
        {
            Debug.LogError($"[RED] Error al emparentar '{name}' a '{parent.name}'. Comprueba que ambos tengan NetworkObject y estén spawneados.");
            rb.isKinematic = false;
            isGrabbed.Value = false;

            ToggleProximityIconClientRpc(true, RpcParamsToSingleClient(clientID));

            return;
        }
    }

    public void DropObject()
    {
        NetworkObject.TryRemoveParent();
        NetworkObject.RemoveOwnership();

        rb.isKinematic = false;
        isGrabbed.Value = false;
    }

    public Rigidbody GetRigidBody()
    {
        return rb;
    }

    [ClientRpc]
    private void ToggleProximityIconClientRpc(bool value, ClientRpcParams rpcParams = default)
    {
        proximityIcon.gameObject.SetActive(value);
    }

    private void OnGrabbedChange(bool prevState, bool currentState)
    {
        if (currentState)
        {
            proximityIcon.enabled = false;
        } else
        {
            proximityIcon.enabled = true;
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void StartShakeClientRpc()
    {
        initialPos = transform.localPosition;
        shakeTween = transform.DOShakePosition(999f, 0.1f, 30, 90, false, false);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void StopShakeClientRpc()
    {
        shakeTween.Kill();
        transform.localPosition = initialPos;
    }
}
