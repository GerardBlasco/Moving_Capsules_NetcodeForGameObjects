using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class RocketController : NetworkBehaviour
{
    private float timeToExplode = 1f;
    float speed = 20f;
    Rigidbody rb;

    [SerializeField] ParticleSystem particles;

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
            StartCoroutine(AutomaticExplosionTimer());
        }
        else
        {
            rb.isKinematic = true;
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        rb.linearVelocity = transform.forward * speed;
    }

    public IEnumerator AutomaticExplosionTimer()
    {
        yield return new WaitForSeconds(timeToExplode);

        DetonateRocket();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        DetonateRocket();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExplodeClientRpc(Vector3 position)
    {
        particles.transform.SetParent(null);
        particles.Stop();

        ParticleManager.instance.SpawnParticles("ExplosionEffect", position, Quaternion.identity);
    }

    private void DetonateRocket()
    {
        if (!IsServer) return;

        ExplodeClientRpc(transform.position);

        Destroy(gameObject);
        GetComponent<NetworkObject>().Despawn();
    }
}
