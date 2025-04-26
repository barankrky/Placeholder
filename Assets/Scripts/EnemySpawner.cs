using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab; // Oluşturulacak düşman prefabı
    public float spawnRate = 2f; // Saniyede kaç düşman oluşturulacağı
    public float spawnAreaHeight = 5f; // Düşmanların spawn olacağı dikey alanın yüksekliği

    private float nextSpawnTime;
    private Camera mainCamera;
    private float screenRightBoundaryInWorldUnits;

    void Start()
    {
        mainCamera = Camera.main;
        // Calculate the right screen boundary in world units
        Vector3 screenRight = new Vector3(Screen.width, Screen.height / 2, mainCamera.nearClipPlane);
        Vector3 screenRightInWorld = mainCamera.ScreenToWorldPoint(screenRight);
        screenRightBoundaryInWorldUnits = screenRightInWorld.x + 1f; // Add a small buffer outside the screen

        nextSpawnTime = Time.time + spawnRate; // Set the initial spawn time
    }

    void Update()
    {
        if (Time.time >= nextSpawnTime)
        {
            SpawnEnemy();
            nextSpawnTime = Time.time + spawnRate;
        }
    }

    void SpawnEnemy()
    {
        if (enemyPrefab != null)
        {
            // Calculate a random y position within the spawn area
            float randomY = Random.Range(-spawnAreaHeight / 2f, spawnAreaHeight / 2f);
            Vector3 spawnPosition = new Vector3(screenRightBoundaryInWorldUnits, randomY, 0);

            // Instantiate the enemy at the calculated position
            Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            Debug.Log("Enemy spawned at: " + spawnPosition);
        }
    }
}