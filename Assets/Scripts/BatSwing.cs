using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

public class BatSwing : NetworkBehaviour
{
    [SerializeField] private Transform batPivot;
    // [SerializeField] private float swingSpeed = 500f;
    [SerializeField] private float swingCooldown = 0.5f;
    
    private float lastSwingTime;
    private bool isSwinging;

    void Update()
    {
        if (!isLocalPlayer) return;

        if (Input.GetKeyDown(KeyCode.Mouse0) && Time.time >= lastSwingTime + swingCooldown)
        {
            CmdSwing();
        }
    }

    [Command]
    void CmdSwing()
    {
        
        if (Time.time < lastSwingTime + swingCooldown)
        {
            Debug.Log("Swing on cooldown");
            return;
        }
        
        lastSwingTime = Time.time;
        RpcPlaySwingAnimation(); // Tell all clients to animate
    }

    [ClientRpc]
    void RpcPlaySwingAnimation()
    {
        // Simple rotation swing (you can replace with animation later)
        if (!isSwinging)
        {
            StartCoroutine(SwingAnimation());
        }
        else
        {
        }
    }

    private System.Collections.IEnumerator SwingAnimation()
    {
        isSwinging = true;
        
        if (batPivot == null)
        {
            isSwinging = false;
            yield break;
        }
        
        Quaternion playerRotation = transform.rotation; 
        Quaternion swingOffset = Quaternion.Euler(0, -130f, 0);
        Quaternion endRot = playerRotation * swingOffset;
        
        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration)
        {
            Quaternion currentPlayerRot = transform.rotation;
            batPivot.rotation = Quaternion.Lerp(currentPlayerRot, endRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Swing back
        elapsed = 0f;
        while (elapsed < duration)
        {
            Quaternion currentPlayerRot = transform.rotation;
            batPivot.rotation = Quaternion.Lerp(endRot, currentPlayerRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        batPivot.rotation = transform.rotation;
        isSwinging = false;
    }
}