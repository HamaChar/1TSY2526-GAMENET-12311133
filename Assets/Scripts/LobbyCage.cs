using Mirror;
using UnityEngine;

public class LobbyCage : NetworkBehaviour
{
    [Header("Cage Settings")]
    [SerializeField] private float hitCooldownPerPlayer = 1f; // Prevent spam hitting
    
    // Track last hit time per player to prevent spam
    private System.Collections.Generic.Dictionary<uint, float> playerHitCooldowns = 
        new System.Collections.Generic.Dictionary<uint, float>();

    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        
        if (!NetworkServer.active) return;
        
        // Check if a bat hit the cage
        if (other.CompareTag("Bat"))
        {
            // Get the player who owns this bat
            var lobbyPlayer = other.GetComponentInParent<LobbyPlayer>();
            
            if (lobbyPlayer == null)
            {
                Debug.LogWarning("Bat hit cage but no LobbyPlayer found in parent");
                return;
            }

            // Check cooldown
            uint playerId = lobbyPlayer.netId;
            if (playerHitCooldowns.ContainsKey(playerId))
            {
                if (Time.time < playerHitCooldowns[playerId] + hitCooldownPerPlayer)
                {
                    return; // Still on cooldown
                }
            }

            // Update cooldown
            playerHitCooldowns[playerId] = Time.time;

            // Toggle ready state using server method instead of Command
            
            
            if (lobbyPlayer.connectionToClient != null && lobbyPlayer.connectionToClient.isReady)
            {
                RpcCageHit(lobbyPlayer.connectionToClient);
                Debug.Log($"Player {playerId} hit cage - sending toggle request");
            }
        }
    }

    [TargetRpc]
    void RpcCageHit(NetworkConnection target)
    {
        // Find the local player and tell them to toggle
        if (NetworkClient.connection == null || NetworkClient.connection.identity == null) return;
        
        var localPlayer = NetworkClient.connection.identity.GetComponent<LobbyPlayer>();
        if (localPlayer != null)
        {
            // The local player calls the command with their own authority
            if (localPlayer.readyToBegin)
            {
                localPlayer.CmdSetUnready();
            }
            else
            {
                localPlayer.CmdSetReady();
            }
        }
    }
    
    // Clean up cooldowns when player disconnects
    public void RemovePlayerCooldown(uint netId)
    {
        if (playerHitCooldowns.ContainsKey(netId))
        {
            playerHitCooldowns.Remove(netId);
        }
    }
}