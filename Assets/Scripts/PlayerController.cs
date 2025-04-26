using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f; // Karakterin hareket hızı
    public GameObject deflectShieldPrefab; // Mermi sektirme kalkanı prefabı
    private GameObject activeShield;
    private Camera mainCamera;
    private float screenHalfWidthInWorldUnits;


    void Start()
    {
        mainCamera = Camera.main;
        // Instantiate the deflect shield and keep it inactive initially
        if (deflectShieldPrefab != null)
        {
            activeShield = Instantiate(deflectShieldPrefab, transform.position, Quaternion.identity, transform);
            activeShield.SetActive(false);
        }
        // Calculate the x-coordinate of the screen center in world units
        Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, mainCamera.nearClipPlane);
        Vector3 screenCenterInWorld = mainCamera.ScreenToWorldPoint(screenCenter);
        screenHalfWidthInWorldUnits = screenCenterInWorld.x; // This is the x-coord of the center line
    }

    void Update()
    {
        MovePlayer();
        HandleDeflectInput();
    }

    void MovePlayer()
    {
        // Yatay ve dikey girişleri al
        float moveX = Input.GetAxis("Horizontal"); // Sağ (-1) ve sol (1) için giriş
        float moveY = Input.GetAxis("Vertical");   // Yukarı (1) ve aşağı (-1) için giriş

        // Karakterin pozisyonunu güncelle
        Vector3 movement = new Vector3(moveX, moveY, 0) * moveSpeed * Time.deltaTime;
        transform.position += movement;

    }

    void HandleDeflectInput()
    {
        if (activeShield != null)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                activeShield.SetActive(true);
                Debug.Log("Deflect Shield Activated");
            }
            if (Input.GetKeyUp(KeyCode.Space))
            {
                activeShield.SetActive(false);
                Debug.Log("Deflect Shield Deactivated");
            }
        }
    }
}
