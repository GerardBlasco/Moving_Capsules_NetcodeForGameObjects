using System.Collections;
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

        particles.transform.SetParent(null);
        particles.Stop();

        Destroy(gameObject);
        GetComponent<NetworkObject>().Despawn();

        ParticleManager.instance.SpawnParticles("ExplosionEffect", transform.position, Quaternion.identity);
    }
}
