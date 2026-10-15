using UnityEngine;
using Unity.Netcode;

public class ObstacleController : NetworkBehaviour
{
    Rigidbody rb;
    float speed = 50f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        Quaternion rbRotation = Quaternion.Euler(0, speed * Time.fixedDeltaTime, 0);

        rb.MoveRotation(rb.rotation * rbRotation);
    }
}
