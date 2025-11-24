using Mirror;
using UnityEngine;

public class LobbyPlayer : NetworkRoomPlayer
{
    [Header("Visual Settings")]
    [SerializeField] private Renderer playerRenderer;

    [SerializeField] private Color unreadyColor = Color.gray;
    [SerializeField] private Color[] readyColors = new Color[]
    {
        Color.red,
        Color.blue,
        Color.green, 
        Color.yellow,
        Color.cyan, 
        Color.magenta
    };

    [SyncVar(hook = nameof(OnColorChanged))]
    public Color playerColor = Color.gray;
    
    
    public override void Start()
    {
        if (playerRenderer != null)
        {
            playerRenderer.material.color = unreadyColor;
        }
    }

    [Command]
    public void CmdSetReady()
    {
        Debug.Log($"CmdSetReady called - isServer: {isServer}, hasAuthority: {isOwned}, readyToBegin: {readyToBegin}");
        
        if (readyToBegin)
        {
            Debug.Log($"{netId} is already ready");
            return;
        }

        // Assign color on server
        Color newColor = readyColors[Random.Range(0, readyColors.Length)];
        playerColor = newColor;

        // Change ready state
        Debug.Log($"About to call CmdChangeReadyState(true)");
        TargetSetReadyState(true);

        Debug.Log($"Player {netId} is now ready with color {newColor}");
    }

    [Command]
    public void CmdSetUnready()
    {
        if (!readyToBegin)
        {
            Debug.Log($"{netId} is already unready");
            return;
        }
        
        // Assign unready color on server
        playerColor = unreadyColor;
        
        // Change ready state
        TargetSetReadyState(false);
        
        Debug.Log($"Player {netId} is now unready");
    }

    // Server-side method that can be called by the cage
    [Server]
    public void ServerToggleReady()
    {
        // Just tell the client to toggle - let the client handle color selection
        TargetToggleReadyState();
        Debug.Log($"Player {netId} told to toggle ready state (server)");
    }

    [TargetRpc]
    void TargetToggleReadyState()
    {
        // This runs on the owning client, which has authority to call the command
        if (readyToBegin)
        {
            CmdSetUnready();
        }
        else
        {
            CmdSetReady();
        }
    }

    [TargetRpc]
    private void TargetSetReadyState(bool ready)
    {
        CmdChangeReadyState(ready);
    }
    
    void OnColorChanged(Color oldColor, Color newColor)
    {
        if (playerRenderer != null)
        {
            playerRenderer.material.color = newColor;
            Debug.Log($"Player color changed to {newColor}");
        }
    }
}