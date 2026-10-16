using UnityEngine;
using Unity.Netcode;

public class NetworkGrabbable : NetworkBehaviour
{
    Rigidbody rb;

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

        Debug.Log("Ha hecho trigger");
    }
}
