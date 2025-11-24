using Mirror;
using UnityEngine;

public class BallController : NetworkBehaviour
{
    private Rigidbody rb;
    [SerializeField] private float speed = 10f;
    private GameObject targetPlayer;
    private GameObject prevPlayer;

    private float hitSlow = 1f;
    private float lastHitTime = -999f;
    private float hitCooldown = 0.7f; // Adjust as needed

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (rb == null) rb = GetComponent<Rigidbody>();


        rb = GetComponent<Rigidbody>();
        rb.WakeUp();
        Debug.Log($"Before: isKinematic = {rb.isKinematic}");
        rb.isKinematic = false;
        Debug.Log($"After: isKinematic = {rb.isKinematic}");
        rb.useGravity = false; // Assuming you don't want gravity

        Debug.Log($"Ball initialized - Sleeping: {rb.IsSleeping()}, Kinematic: {rb.isKinematic}");

        ChooseRandomTarget();
    }

    [ServerCallback]
    void FixedUpdate()
    {
        if (Time.time < lastHitTime + hitCooldown)
        {
            return;
        }
        if (hitSlow > 1f)
        {
            hitSlow -= 0.2f * Time.deltaTime;
        }
        
        if (targetPlayer == null)
        {
            ChooseRandomTarget();
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, targetPlayer.transform.position);

        
        if (distanceToTarget < 0.5f) // Adjust threshold as needed
        {
            ChooseRandomTarget();
            return;
        }

        Vector3 direction = (targetPlayer.transform.position - transform.position).normalized;

        Vector3 desiredVelocity = direction * speed;
        Vector3 velocityChange = desiredVelocity - rb.velocity;
        
        rb.AddForce(velocityChange * (1 / hitSlow), ForceMode.VelocityChange);
    }

    [Server]
    void ChooseRandomTarget()
    {
        var playerObjs = GameObject.FindGameObjectsWithTag("Player");

        if (playerObjs.Length == 0)
        {
            Debug.LogWarning("No players found!");
            return;
        }


        GameObject newTarget;
        int attempts = 0;

        do
        {
            newTarget = playerObjs[Random.Range(0, playerObjs.Length)];

            attempts++;
            if (attempts > 10) break;
        } while (newTarget == prevPlayer && prevPlayer != null);

        targetPlayer = newTarget;
        prevPlayer = targetPlayer;
        Debug.Log($"Ball targeting: {targetPlayer.name}");

    }

    [ServerCallback]
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            
            Vector3 bounceDir = (transform.position - collision.transform.position).normalized;
            rb.AddForce(bounceDir * speed * 1.5f, ForceMode.Impulse);

            lastHitTime = Time.time;
            if (collision.gameObject == targetPlayer)
            {
                ChooseRandomTarget();
            }
        }
    }

    [Server]
    public void OnHitByPlayer(GameObject hittingPlayer, float forceMultiplier = 1f)
    {
        
        Vector3 bounceDir = (transform.position - hittingPlayer.transform.position).normalized;
        rb.AddForce(bounceDir * speed * forceMultiplier);

        hitSlow = 4f;
        lastHitTime = Time.time;
        if (hittingPlayer == targetPlayer)
        {
            ChooseRandomTarget();
        }
    }
    
    
    
}