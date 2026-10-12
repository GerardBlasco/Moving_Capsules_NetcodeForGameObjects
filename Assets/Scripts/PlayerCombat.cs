using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

public class PlayerCombat : NetworkBehaviour
{
    [SerializeField] GameObject weaponPrefab;

    [SerializeField] Transform weaponPos;

    private Weapon currentWeapon;

    private bool shootWasPressed;

    private Rigidbody rb;

    [SerializeField] GameObject rightHand;
    [SerializeField] GameObject leftHand;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (weaponPrefab != null)
        {
            weaponPrefab = Instantiate(weaponPrefab, weaponPos);
            currentWeapon = weaponPrefab.GetComponent<Weapon>();

            currentWeapon.SetPlayerHands(rightHand, leftHand);
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.fKey.wasPressedThisFrame) ShootServerRpc();
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        if (shootWasPressed)
        {
            shootWasPressed = false;

            if (!currentWeapon.GetCanShoot()) return;

            ShootAnimationClientRpc();
            rb.AddForce(-transform.forward * currentWeapon.GetRecoilForce(), ForceMode.Impulse);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ShootServerRpc()
    {
        shootWasPressed = true;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ShootAnimationClientRpc()
    {
        currentWeapon.PlayShootAnimation();
    }
}
