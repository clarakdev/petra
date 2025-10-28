using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;      // Drag your Player object here in Inspector
    public float smoothSpeed = 5f;
    public Vector3 offset;        // Optional offset (like (0, 0, -10) for 2D)

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 targetPosition = player.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        transform.position = new Vector3(smoothedPosition.x, smoothedPosition.y, transform.position.z);
    }
}
