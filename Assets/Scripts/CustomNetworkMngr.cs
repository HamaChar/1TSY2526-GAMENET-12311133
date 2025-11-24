using Mirror;
using UnityEngine;
using System.Collections.Generic;

public class CustomNetworkMngr : NetworkRoomManager
{
    [Header("Spawn Settings")]
    [SerializeField] private float spawnRadius = 5f;
    public Vector3 spawnCenter;

    [Header("Lobby Settings")]
    [SerializeField] private GameObject lobbyCagePrefab;

    private GameObject lobbyCageInstance;
    
    [Header("Game Settings")]
    private GameObject ballInstance;

    private Dictionary<NetworkConnectionToClient, Color> playerColors =
        new Dictionary<NetworkConnectionToClient, Color>();
    
    private List<NetworkConnectionToClient> activePlayers = new List<NetworkConnectionToClient>();
    
    public override GameObject OnRoomServerCreateRoomPlayer(NetworkConnectionToClient conn)
    {
        Vector3 spawnPos = GetRandomSpawn(spawnRadius);
        GameObject roomPlayer = Instantiate(roomPlayerPrefab.gameObject, spawnPos, Quaternion.identity);
        
        if (!activePlayers.Contains(conn))
        {
            activePlayers.Add(conn);
            Debug.Log($"Added {conn.connectionId} to activePlayers");
        }
        
        Debug.Log($"Room player created for conn {conn.connectionId}; total active players: {activePlayers.Count}");
        
        // Check and spawn cage after player is created
        Invoke(nameof(CheckAndSpawnCage), 0.1f);
        
        return roomPlayer;
    }
    
    private void CheckAndSpawnCage()
    {
        int actualPlayerCount = activePlayers.Count;
        Debug.Log($"Checking cage: {actualPlayerCount} players in room (minPlayers: {minPlayers})");
        
        if (actualPlayerCount >= minPlayers && lobbyCageInstance == null)
        {
            SpawnLobbyCage();
            Debug.Log("Spawning cage - minimum players reached");
        }
    }
    
    public override void OnRoomServerPlayersReady()
    {
        Debug.Log($"OnRoomServerPlayersReady called - allPlayersReady: {allPlayersReady}, roomSlots.Count: {roomSlots.Count}");
        
        // Log each room player's ready state
        foreach (var roomPlayer in roomSlots)
        {
            if (roomPlayer != null)
            {
                Debug.Log($"Room player: {roomPlayer.name}, ready: {roomPlayer.readyToBegin}, netId: {roomPlayer.netId}");
            }
        }
        
        // Destroy cage before transitioning
        if (lobbyCageInstance != null)
        {
            NetworkServer.Destroy(lobbyCageInstance);
            lobbyCageInstance = null;
            Debug.Log("Lobby cage destroyed - players ready, transitioning to game");
        }
        
        // CRITICAL: Don't call base if not all players are actually ready
        if (!allPlayersReady)
        {
            Debug.LogWarning("allPlayersReady is false, not transitioning!");
            return;
        }
        
        base.OnRoomServerPlayersReady();
    }

    public override void OnRoomServerSceneChanged(string sceneName)
    {
        Debug.Log($"OnRoomServerSceneChanged called - sceneName: {sceneName}, roomSlots count: {roomSlots.Count}");
        
        base.OnRoomServerSceneChanged(sceneName);

        Debug.Log($"After base call - Scene changed to {sceneName}; active players: {activePlayers.Count}, GameplayScene: {GameplayScene}");

        if (sceneName == GameplayScene && activePlayers.Count >= 2)
        {
            Invoke(nameof(SpawnGameBall), 0.5f);
        }
    }
    
    public override bool OnRoomServerSceneLoadedForPlayer(NetworkConnectionToClient conn, GameObject roomPlayer, GameObject gamePlayer)
    {
        Debug.Log($"OnRoomServerSceneLoadedForPlayer - conn: {conn.connectionId}, roomPlayer: {roomPlayer?.name}, gamePlayer: {gamePlayer?.name}");
        
        // Make sure we return true to allow the player to stay connected
        return base.OnRoomServerSceneLoadedForPlayer(conn, roomPlayer, gamePlayer);
    }

    void SpawnLobbyCage()
    {
        if (lobbyCagePrefab != null)
        {
            Vector3 cagePos = spawnCenter;
            cagePos.y += 1.0f;
            
            lobbyCageInstance = Instantiate(lobbyCagePrefab, cagePos, Quaternion.identity);
            NetworkServer.Spawn(lobbyCageInstance);
            Debug.Log("LobbyCage spawned");
        }
    }
    
    
    #region Game Scene

    public override GameObject OnRoomServerCreateGamePlayer(NetworkConnectionToClient conn, GameObject roomPlayer)
    {
        Debug.Log($"OnRoomServerCreateGamePlayer called for conn {conn.connectionId}");
        
        // Get color from lobby player BEFORE it gets destroyed
        var lobbyPlayer = roomPlayer.GetComponent<LobbyPlayer>();
        Color playerColor = Color.gray;

        if (lobbyPlayer != null)
        {
            playerColor = lobbyPlayer.playerColor;
            Debug.Log($"Got color from lobby player: {playerColor} for conn {conn.connectionId}");
        }
        else
        {
            Debug.LogWarning("No LobbyPlayer component found on roomPlayer!");
        }

        // Store color in dictionary
        if (!playerColors.ContainsKey(conn))
        {
            playerColors.Add(conn, playerColor);
        }
        else
        {
            playerColors[conn] = playerColor;
        }

        Vector3 spawnPos = GetRandomSpawn(spawnRadius);
        GameObject gamePlayer = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        
        Debug.Log($"Game player instantiated at {spawnPos} for conn {conn.connectionId}");
        
        // Apply the color to the game player
        var renderer = gamePlayer.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = playerColor;
            Debug.Log($"Applied color {playerColor} to game player");
        }
        else
        {
            Debug.LogWarning("No Renderer found on game player prefab!");
        }

        // Also store color on a component if you need it later
        var colorHolder = gamePlayer.AddComponent<PlayerColorHolder>();
        if (colorHolder != null)
        {
            colorHolder.playerColor = playerColor;
        }

        return gamePlayer;
    }
    
    void SpawnGameBall()
    {
        if (ballInstance == null && GameObject.FindGameObjectWithTag("Ball") == null)
        {
            ballInstance = Instantiate(spawnPrefabs.Find(p => p.name == "BallPrefab"), spawnCenter, Quaternion.identity);
            
            var rb = ballInstance.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false;
                rb.WakeUp();
            }
        
            NetworkServer.Spawn(ballInstance);
            Debug.Log("GameBall spawned");
        }
    }
    
    private Vector3 GetRandomSpawn(float radius)
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;
        return new Vector3(x + spawnCenter.x, 0f + spawnCenter.y, z + spawnCenter.z);
    }

    public override void OnRoomServerDisconnect(NetworkConnectionToClient conn)
    {
        // Clean up lobby cage cooldowns if it exists
        if (lobbyCageInstance != null)
        {
            var cage = lobbyCageInstance.GetComponent<LobbyCage>();
            if (cage != null)
            {
                // Find the player's netId before they're destroyed
                if (conn.identity != null)
                {
                    cage.RemovePlayerCooldown(conn.identity.netId);
                }
            }
        }
        
        activePlayers.Remove(conn);
        Debug.Log($"Removed {conn.connectionId} from activePlayers; {activePlayers.Count} remaining");
        
        base.OnRoomServerDisconnect(conn);
        
        if (playerColors.ContainsKey(conn))
        {
            playerColors.Remove(conn);
        }
    }
    
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        activePlayers.Remove(conn);
        
        base.OnServerDisconnect(conn);
        
        if (playerColors.ContainsKey(conn))
        {
            playerColors.Remove(conn);
        }
    }
    #endregion
}

// Helper component to store player color on game player
public class PlayerColorHolder : NetworkBehaviour
{
    [SyncVar]
    public Color playerColor;
}