using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

public class PlayerCombat : NetworkBehaviour
{
    private List<MeshRenderer> playerRenderers = new List<MeshRenderer>();

    [SerializeField] GameObject weaponPrefab;

    [SerializeField] Transform weaponPos;

    private Weapon currentWeapon;

    private bool shootWasPressed;

    private Rigidbody rb;

    [SerializeField] GameObject rightHand;
    [SerializeField] GameObject leftHand;

    [SerializeField] float flashDuration = 0.2f;
    [SerializeField] Material flashMaterial;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        playerRenderers = GetComponentsInChildren<MeshRenderer>().ToList();

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

            GetComponent<PlayerController>().AddKnockback((-transform.forward * currentWeapon.GetRecoilForce()) + (-transform.up * (currentWeapon.GetRecoilForce() * 0.5f)), 0.3f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Attack"))
        {
            EnableFlashClientRpc();
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void EnableFlashClientRpc()
    {
        StartCoroutine(FlashColor());
    }

    public IEnumerator FlashColor()
    {
        Material ogMaterial = playerRenderers[0].material;

        for (int i = 0; i < playerRenderers.Count; i++)
        {
            playerRenderers[i].material = flashMaterial;
        }

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < playerRenderers.Count; i++)
        {
            playerRenderers[i].material = ogMaterial;
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
