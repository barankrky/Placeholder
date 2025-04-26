using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
        Debug.Log("Player Health Initialized: " + currentHealth);
    }

    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        Debug.Log("Player took " + damageAmount + " damage. Current Health: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Player Died!");
        // TODO: Implement game over logic (e.g., restart level, show game over screen)
        gameObject.SetActive(false); // Temporarily disable the player GameObject
    }

    // Called when another collider enters a trigger collider attached to this object
    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the colliding object is a "Sidewalk"
        if (other.CompareTag("Sidewalk"))
        {
            // Define damage amount for hitting the sidewalk
            int sidewalkDamage = 10; // Example damage amount

            TakeDamage(sidewalkDamage);
        }
        // TODO: Add checks for other damage sources like enemy projectiles
    }
}