using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float moveSpeed = 3f; // Düşmanın hareket hızı
    public GameObject projectilePrefab; // Düşmanın atacağı mermi prefabı
    public float fireRate = 2f; // Saniyede atılan mermi sayısı
    private float nextFireTime;

    private float screenLeftBoundaryInWorldUnits;

    void Start()
    {
        // Calculate the screen boundary in world units to know when to destroy the enemy
        Camera mainCamera = Camera.main;
        Vector3 screenLeft = new Vector3(0, Screen.height / 2, mainCamera.nearClipPlane);
        Vector3 screenLeftInWorld = mainCamera.ScreenToWorldPoint(screenLeft);
        screenLeftBoundaryInWorldUnits = screenLeftInWorld.x - 1f; // Add a small buffer

        nextFireTime = Time.time + Random.Range(0.5f, 1.5f); // Initial random delay before first shot
    }

    void Update()
    {
        Move();
        CheckFire();
        CheckBounds();
    }

    void Move()
    {
        // Move the enemy to the left
        transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);
    }

    void CheckFire()
    {
        if (Time.time >= nextFireTime)
        {
            FireProjectile();
            nextFireTime = Time.time + 1f / fireRate + Random.Range(-0.5f, 0.5f); // Add some randomness to fire rate
        }
    }

    void FireProjectile()
    {
        if (projectilePrefab != null)
        {
            // Instantiate the projectile at the enemy's position
            Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        }
    }

    void CheckBounds()
    {
        // Destroy the enemy if it moves off the left side of the screen
        if (transform.position.x < screenLeftBoundaryInWorldUnits)
        {
            Destroy(gameObject);
            Debug.Log("Enemy moved off-screen and was destroyed.");
        }
    }

    // TODO: Implement logic for being destroyed by a deflected player projectile

    public void Die()
    {
        Debug.Log("Enemy Died!");
        Destroy(gameObject); // Destroy the enemy GameObject
    }
}