using UnityEngine;

public class SimpleShooter : MonoBehaviour
{
    [Header("References")]
    public Camera sourceCamera;
    public GameObject bulletPrefab;
    public Transform spawnPoint;

    [Header("Bullet Settings")]
    public float bulletSpeed = 40f;
    public float fireRate = 0.2f;
    public float bulletLifetime = 5f;

    [Header("Pooling (optional)")]
    [Tooltip("If checked, SimpleShooter will try to use the global BulletPool. If no BulletPool exists one will be created at runtime.")]
    public bool useGlobalPool = true;

    // These are fallback values only — if you want to control the pool globally, set them on the BulletPool instance in the scene.
    public int poolSize = 20;
    public bool poolPersistent = false;

    private float nextFireTime = 0f;

    void Awake()
    {
        if (sourceCamera == null)
            sourceCamera = Camera.main;

        if (spawnPoint == null && sourceCamera != null)
            spawnPoint = sourceCamera.transform;

        if (useGlobalPool)
        {
            // Ensure a BulletPool exists
            if (BulletPool.Instance == null)
            {
                GameObject poolGO = new GameObject("BulletPoolManager");
                var pool = poolGO.AddComponent<BulletPool>();
                pool.Setup(bulletPrefab, poolSize, true, poolPersistent);
            }
            else
            {
                // Make sure the global pool knows which prefab to use (optional)
                if (BulletPool.Instance.bulletPrefab == null)
                    BulletPool.Instance.Setup(bulletPrefab, poolSize, true, poolPersistent);
            }
        }
    }

    void Update()
    {
        if (fireRate <= 0f)
        {
            if (Input.GetButtonDown("Fire1"))
                Fire();
        }
        else
        {
            if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + fireRate;
                Fire();
            }
        }
    }

    void Fire()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("SimpleShooter: bulletPrefab not set.");
            return;
        }

        if (sourceCamera == null)
        {
            Debug.LogWarning("SimpleShooter: sourceCamera not set and Camera.main is null.");
            return;
        }

        Transform origin = spawnPoint != null ? spawnPoint : sourceCamera.transform;

        bool isPooled = false;
        GameObject bulletObj = null;

        if (useGlobalPool && BulletPool.Instance != null)
        {
            // Ask the global pool
            bulletObj = BulletPool.Instance.GetBullet(out isPooled);
            // If pool returned null because prefab was missing, fallback to instantiate
            if (bulletObj == null)
            {
                bulletObj = Instantiate(bulletPrefab);
                if (bulletObj.GetComponent<Bullet>() == null) bulletObj.AddComponent<Bullet>();
                isPooled = false;
            }
        }
        else
        {
            // No global pool: instantiate a temporary bullet
            bulletObj = Instantiate(bulletPrefab);
            if (bulletObj.GetComponent<Bullet>() == null) bulletObj.AddComponent<Bullet>();
            isPooled = false;
        }

        bulletObj.transform.position = origin.position;
        bulletObj.transform.rotation = Quaternion.LookRotation(sourceCamera.transform.forward);
        bulletObj.SetActive(true);

        // Parent everything under the pool (keeps hierarchy tidy); if using non-pooled bullets this will still parent them to the pool so they aren't on the player.
        if (BulletPool.Instance != null && BulletPool.Instance.PoolTransform != null)
            bulletObj.transform.SetParent(BulletPool.Instance.PoolTransform, true);

        Rigidbody rb = bulletObj.GetComponent<Rigidbody>();
        if (rb == null)
            rb = bulletObj.AddComponent<Rigidbody>();

        rb.velocity = sourceCamera.transform.forward * bulletSpeed;

        Bullet b = bulletObj.GetComponent<Bullet>();
        if (b != null)
            b.Launch(bulletLifetime, isPooled);
        else
            Destroy(bulletObj, bulletLifetime);
    }
}
