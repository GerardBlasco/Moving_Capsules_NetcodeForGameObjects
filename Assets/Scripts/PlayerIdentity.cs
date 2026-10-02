using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

public class PlayerIdentity : NetworkBehaviour
{
    private List<Material> playerMaterials = new List<Material>();

    private bool tryingColorChange;

    // Almacena todos los materiales de los mesh renderers del player
    private void Awake()
    {
        List<MeshRenderer> renderers = GetComponentsInChildren<MeshRenderer>().ToList();

        for (int i = 0; i < renderers.Count; i++)
        {
            playerMaterials.Add(renderers[i].material);
        }
    }

    private readonly NetworkVariable<FixedString32Bytes> userName = new NetworkVariable<FixedString32Bytes>(
        "Jugador Desconocido",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<Color> userColor = new NetworkVariable<Color>(
        Color.white,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Suscribe las funciones cuando se unen al servidor
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        userColor.OnValueChanged += OnUserColorChanged;
        userName.OnValueChanged += OnUserNameChanged;

        // Da valor a las variables al unirse al servidor
        userName.Value = "Jugador " + OwnerClientId;

        userColor.Value = GetRandomColor();
        ApplyColor(userColor.Value);
    }

    // Desuscribe las funciones cuando deja el servidor
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        userColor.OnValueChanged -= OnUserColorChanged;
        userName.OnValueChanged -= OnUserNameChanged;
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.cKey.wasPressedThisFrame) SubmitColorChangeInputServerRpc();
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        if (tryingColorChange)
        {
            userColor.Value = GetRandomColor();
            tryingColorChange = false;
        }
    }

    // El server recibe la peticion de cambio de color
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SubmitColorChangeInputServerRpc()
    {
        tryingColorChange = true;
    }

    // Llama a ApplyColor cuando detecta un cambio en la network variable "usercolor"
    void OnUserColorChanged(Color prevColor, Color newColor)
    {
        ApplyColor(newColor);
        //Debug.Log($"[RED] El color del {userName.Value} ha cambiado a: {newColor.ToString()}, era: {prevColor.ToString()} ");
    }

    // Genera un mensaje cuando detecta un cambio en la network variable "username"
    void OnUserNameChanged(FixedString32Bytes prevName, FixedString32Bytes newName)
    {
        Debug.Log($"[RED] El usuario del {prevName} ha cambiado a: {newName}");
    }

    // Aplica el color al material
    private void ApplyColor(Color newColor)
    {
        for (int i = 0; i < playerMaterials.Count; i++)
        {
            playerMaterials[i].color = newColor;
        }
    }

    // Devuelve un tono de color random
    public Color GetRandomColor()
    {
        return Random.ColorHSV();
    }
}
