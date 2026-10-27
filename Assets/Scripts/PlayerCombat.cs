using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : NetworkBehaviour
{
    private List<MeshRenderer> playerRenderers = new List<MeshRenderer>();

    [SerializeField] GameObject weaponPrefab;

    [SerializeField] Transform weaponPos;
    [SerializeField] Transform foldedWeaponPos;
    [SerializeField] Transform handsParent;

    private bool tryiedToAlterWeapon = false;

    private Weapon currentWeapon;

    private bool shootWasPressed;

    private Rigidbody rb;

    [SerializeField] GameObject rightHand;
    [SerializeField] GameObject leftHand;

    [SerializeField] float flashDuration = 0.2f;
    [SerializeField] Material flashMaterial;

    private readonly NetworkVariable<bool> weaponFolded = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        weaponFolded.OnValueChanged += OnWeaponFoldedChanged;

        OnWeaponFoldedChanged(false, weaponFolded.Value);
    }

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
            if (Keyboard.current.tabKey.wasPressedThisFrame) TryAlterWeaponServerRpc();
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        if (tryiedToAlterWeapon)
        {
            tryiedToAlterWeapon = false;

            if (!currentWeapon.GetCanShoot()) return;

            //weaponFolded.Value ? UnfoldWeaponClientRpc() : FoldWeaponClientRpc();

            if (weaponFolded.Value)
            {
                weaponFolded.Value = false;
            }
            else
            {
                weaponFolded.Value = true;
            }
        }

        if (shootWasPressed)
        {
            shootWasPressed = false;

            if (!currentWeapon.GetCanShoot() || weaponFolded.Value) return;

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
    private void TryAlterWeaponServerRpc()
    {
        tryiedToAlterWeapon = true;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void FoldWeaponClientRpc()
    {
        currentWeapon.UnparentHands(handsParent);
        weaponFolded.Value = true;
        weaponPrefab.transform.position = foldedWeaponPos.position;
        weaponPrefab.transform.rotation = foldedWeaponPos.rotation;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void UnfoldWeaponClientRpc()
    {
        weaponFolded.Value = false;
        weaponPrefab.transform.position = weaponPos.position;
        weaponPrefab.transform.rotation = weaponPos.rotation;
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

    private void FoldWeaponVisual()
    {
        currentWeapon.UnparentHands(handsParent);
        weaponPrefab.transform.position = foldedWeaponPos.position;
        weaponPrefab.transform.rotation = foldedWeaponPos.rotation;
    }

    private void UnfoldWeaponVisual()
    {
        weaponPrefab.transform.position = weaponPos.position;
        weaponPrefab.transform.rotation = weaponPos.rotation;
    }

    private void OnWeaponFoldedChanged(bool previous, bool current)
    {
        if (current)
        {
            FoldWeaponVisual();
        }
        else
        {
            UnfoldWeaponVisual();
        }
    }
}
