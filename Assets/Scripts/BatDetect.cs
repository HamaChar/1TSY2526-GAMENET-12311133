using Mirror;
using UnityEngine;

public class BatDetect : NetworkBehaviour
{
    [SerializeField] private float hitForceMultiplier = 2f;

    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        // Debug.Log($"Bat hit something: {other.name}");
        if (other.CompareTag("Ball"))
        {
            Debug.Log("Hit the ball!");
            var ball = other.GetComponent<BallController>();
            if (ball != null)
            {
                // Get the player who owns this bat
                GameObject player = GetComponentInParent<NetworkIdentity>().gameObject;
                ball.OnHitByPlayer(player, hitForceMultiplier);
            }
        }
    }
}