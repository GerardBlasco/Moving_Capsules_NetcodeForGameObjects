using UnityEngine;
using Unity.Netcode;
using System.Collections;
using static UnityEngine.ParticleSystem;

public class ExplosionController : NetworkBehaviour
{
    private float timer = 1f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            StartCoroutine(AutomaticDestruction());
            ExplodeClientRpc();
        }
    }

    public IEnumerator AutomaticDestruction()
    {
        yield return new WaitForSeconds(timer);

        Destroy(gameObject);
        GetComponent<NetworkObject>().Despawn();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExplodeClientRpc()
    {
        ParticleManager.instance.SpawnParticles("ExplosionEffect", transform.position, Quaternion.identity);
    }
}
