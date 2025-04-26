using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public float moveSpeed = 7f; // Merminin hareket hızı
    private bool isDeflected = false; // Merminin sektirilip sektirilmediği
    private Vector3 moveDirection; // Merminin hareket yönü
    private float screenLeftBoundaryInWorldUnits;

    void Start()
    {
        moveDirection = Vector3.left; // Başlangıçta sola doğru hareket et
        // Calculate the screen boundary in world units to know when to destroy the projectile
        Camera mainCamera = Camera.main;
        Vector3 screenLeft = new Vector3(0, Screen.height / 2, mainCamera.nearClipPlane);
        Vector3 screenLeftInWorld = mainCamera.ScreenToWorldPoint(screenLeft);
        screenLeftBoundaryInWorldUnits = screenLeftInWorld.x - 1f; // Add a small buffer
    }

    void Update()
    {
        Move();
        CheckBounds();
    }

    void Move()
    {
        // Move the projectile in the current direction
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime);
    }

    void CheckBounds()
    {
        // Destroy the projectile if it moves off the left side of the screen
        if (transform.position.x < screenLeftBoundaryInWorldUnits)
        {
            Destroy(gameObject);
            Debug.Log("Projectile moved off-screen and was destroyed.");
        }
    }

    // Called when another collider enters a trigger collider attached to this object
    void OnTriggerEnter2D(Collider2D other)
    {
        // If not already deflected and collides with the deflect shield
        if (!isDeflected && other.CompareTag("DeflectShield"))
        {
            Deflect();
        }
        // If deflected and collides with an Enemy
        else if (isDeflected && other.CompareTag("Enemy"))
        {
            // Find the Enemy script and call a method to handle being hit
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Die(); // Assume Enemy script has a Die() method
                Destroy(gameObject); // Destroy the projectile after hitting the enemy
            }
        }
        // If not deflected and collides with the Player
        else if (!isDeflected && other.CompareTag("Player"))
        {
            // Find the PlayerHealth script and call TakeDamage
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                int projectileDamage = 20; // Define damage amount for projectile hit
                playerHealth.TakeDamage(projectileDamage);
                Destroy(gameObject); // Destroy the projectile after hitting the player
            }
        }
    }

    void Deflect()
    {
        isDeflected = true;
        moveDirection = Vector3.right; // Reverse direction to move right
        // Optionally change tag/layer here to avoid colliding with player shield again
        // gameObject.tag = "DeflectedProjectile"; // Example: change tag
        Debug.Log("Projectile deflected!");
    }
}