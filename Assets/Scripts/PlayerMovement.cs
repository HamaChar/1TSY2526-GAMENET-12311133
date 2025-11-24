using Mirror;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float movementSpeed = 10f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private GameObject plyTarget = null;
    private Rigidbody rb;
    private GameObject ball;
    private Camera mainCamera;


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    void Start()
    {
        if (isLocalPlayer)
        {
            mainCamera = Camera.main;
        }
    }
    
    void FixedUpdate()
    {
        if (!isLocalPlayer) return;

        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        Vector3 movement = input.normalized * movementSpeed * Time.fixedDeltaTime;
    
        rb.velocity = Vector3.zero;
        rb.MovePosition(transform.position + movement);
        
        
        HandleRotation();
    }
    
    void HandleRotation()
    {
        // Try to find ball if we don't have reference
        if (ball == null)
        {
            ball = GameObject.FindGameObjectWithTag("Ball");
        }

        Vector3 lookTarget;

        if (ball != null)
        {
            lookTarget = ball.transform.position;
        }
        else if (plyTarget != null)
        {
            lookTarget = plyTarget.transform.position;
        }
        else if (mainCamera != null)
        {
            Vector3 mainCamAdjusted = mainCamera.transform.position;
            mainCamAdjusted.z += 40.0f;
            lookTarget = mainCamAdjusted;
        }
        else
        {
            Debug.Log("No target");
            return;
        }

        Vector3 direction = lookTarget - transform.position;
        direction.y = 0;

        if (direction.sqrMagnitude > 0.001f) // Avoid zero vector
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }
}