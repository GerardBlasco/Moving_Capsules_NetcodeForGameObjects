using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class RocketController : NetworkBehaviour
{
    private float timeToExplode = 1f;
    float speed = 20f;
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

        StartCoroutine(AutomaticExplosionTimer());
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        rb.linearVelocity = transform.forward * speed;
    }

    public IEnumerator AutomaticExplosionTimer()
    {
        yield return new WaitForSeconds(timeToExplode);

        Destroy(gameObject);
        GetComponent<NetworkObject>().Despawn();
    }
}
