using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform playerTransform;
    public float smoothTime;

    private Vector3 velocity;

    private void LateUpdate() // Late update to prevent stuttering
    {
        // Calculate target position
        Vector3 target = playerTransform.position;
        target.z = -10;

        // Move towards target position
        transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, smoothTime);
    }
}
