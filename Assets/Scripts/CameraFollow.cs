using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Kameranın takip edeceği hedef (karakter)
    public float smoothSpeed = 0.125f; // Takip yumuşaklığı
    public Vector3 offset; // Hedef ile kamera arasındaki ofset

    void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("Camera target is not assigned.");
            return;
        }

        // Hedefin pozisyonunu al, sadece x eksenini kullan
        Vector3 targetPosition = new Vector3(target.position.x + offset.x, transform.position.y + offset.y, transform.position.z + offset.z);

        // Kameranın pozisyonunu hedefe doğru yumuşak bir şekilde hareket ettir
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}