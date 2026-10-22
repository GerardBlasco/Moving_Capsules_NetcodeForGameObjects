using UnityEngine;
using Unity.Netcode;

public class NetworkGrabbable : NetworkBehaviour
{
    Rigidbody rb;

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
            GrabObject(other.GetComponent<NetworkObject>().OwnerClientId, other.transform);
        }
    }

    private void GrabObject(ulong clientID, Transform parent)
    {
        if (isGrabbed.Value) return;

        bool parentedSuccessfully = NetworkObject.TrySetParent(parent.transform, false);

        NetworkObject.ChangeOwnership(clientID);
        Debug.Log($"[RED] El objeto '{name}' ahora tiene de owner a '{clientID}'.");

        transform.localPosition = new Vector3(0, 0, 1.5f);
        rb.isKinematic = true;
        isGrabbed.Value = true;

        if (!parentedSuccessfully)
        {
            Debug.LogError($"[RED] Error al emparentar '{name}' a '{parent.name}'. Comprueba que ambos tengan NetworkObject y estén spawneados.");
            rb.isKinematic = false;
            isGrabbed.Value = false;

            return;
        }
    }
}
