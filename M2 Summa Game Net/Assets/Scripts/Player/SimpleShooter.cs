// SimpleShooter.cs
// Put both classes in one file for convenience. You can split them into separate files if you prefer.
// Behavior update: pool now has a fixed "poolSize". If all pooled bullets are in use, the shooter will
// create **temporary non-pooled bullets** that are NOT added to the pool and will be destroyed when
// their lifetime ends. This allows you to fire beyond the pool limit (temporarily) while keeping the
// pool capped at the configured size; once temporary bullets die the pool returns to its original count.

using System.Collections.Generic;
using UnityEngine;

public class SimpleShooter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Camera used as the source / forward direction for bullets. If left empty the script will use Camera.main at Awake.")]
    public Camera sourceCamera;

    [Tooltip("Prefab for the bullet. Prefab should contain at least a Collider and (preferably) a Rigidbody.")]
    public GameObject bulletPrefab;

    [Tooltip("Optional explicit spawn point. If empty the camera's transform will be used (bullets spawn at camera position).")]
    public Transform spawnPoint;

    [Header("Bullet Settings")]
    public float bulletSpeed = 40f;
    [Tooltip("If 0 or less, shooting is single-shot (click to fire). If > 0, holding Fire1 will fire repeatedly at this interval (seconds).")]
    public float fireRate = 0.2f;
    public float bulletLifetime = 5f;

    [Header("Pooling (optional)")]
    [Tooltip("Enable a simple pool to avoidAllocations from frequent Instantiate/Destroy.")]
    public bool usePooling = true;
    [Tooltip("Number of pooled bullets to keep around. The pool is fixed at this size; extra bullets will be temporary.")]
    public int poolSize = 20;

    // internal
    private float nextFireTime = 0f;
    private List<GameObject> pool;
    private Transform poolParent;

    void Awake()
    {
        if (sourceCamera == null)
            sourceCamera = Camera.main;

        if (spawnPoint == null && sourceCamera != null)
            spawnPoint = sourceCamera.transform;

        if (usePooling)
            InitPool();
    }

    void InitPool()
    {
        pool = new List<GameObject>(poolSize);
        poolParent = new GameObject("BulletPool").transform;
        poolParent.SetParent(transform, true);

        if (bulletPrefab == null)
        {
            Debug.LogWarning("SimpleShooter: bulletPrefab is null - cannot initialize pool.");
            return;
        }

        for (int i = 0; i < poolSize; i++)
        {
            GameObject g = Instantiate(bulletPrefab, poolParent);
            g.SetActive(false);
            // Ensure Bullet script exists so the pool can properly deactivate on lifetime/hit
            if (g.GetComponent<Bullet>() == null)
                g.AddComponent<Bullet>();
            pool.Add(g);
        }
    }

    // Returns a bullet GameObject and indicates whether the returned object is part of the pool.
    GameObject GetBulletFromPool(out bool isPooled)
    {
        // If pooling disabled: create non-pooled bullet (will be destroyed when lifetime ends)
        if (!usePooling)
        {
            isPooled = false;
            GameObject newB = Instantiate(bulletPrefab);
            if (newB.GetComponent<Bullet>() == null)
                newB.AddComponent<Bullet>();
            newB.SetActive(false);
            return newB;
        }

        // Try to find an inactive pooled bullet
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].activeInHierarchy)
            {
                isPooled = true;
                return pool[i];
            }
        }

        // No pooled bullet available: create a temporary non-pooled bullet.
        // IMPORTANT: we do NOT add this to the pool, so the pool size remains fixed.
        isPooled = false;
        GameObject temp = Instantiate(bulletPrefab);
        if (temp.GetComponent<Bullet>() == null)
            temp.AddComponent<Bullet>();
        temp.SetActive(false);
        return temp;
    }

    void Update()
    {
        // Fire when left mouse button / Fire1 is pressed.
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

        bool isPooled;
        GameObject bulletObj = GetBulletFromPool(out isPooled);

        bulletObj.transform.position = origin.position;
        // Make bullet point in camera forward direction
        bulletObj.transform.rotation = Quaternion.LookRotation(sourceCamera.transform.forward);
        bulletObj.SetActive(true);

        // Parent temporary bullets under this GameObject for cleanliness (optional)
        if (!isPooled)
            bulletObj.transform.SetParent(transform, true);

        // Ensure there's a Rigidbody to control motion
        Rigidbody rb = bulletObj.GetComponent<Rigidbody>();
        if (rb == null)
            rb = bulletObj.AddComponent<Rigidbody>();

        // If pooled, ensure physics state is reset. If you prefer kinematic pooled bullets when inactive,
        // you can toggle rb.isKinematic accordingly when deactivating/enabling.
        rb.velocity = sourceCamera.transform.forward * bulletSpeed;

        // Tell the bullet about lifetime and pooling so it cleans up correctly
        Bullet b = bulletObj.GetComponent<Bullet>();
        if (b != null)
            b.Launch(bulletLifetime, isPooled);
        else
            Destroy(bulletObj, bulletLifetime);
    }
}

// Simple bullet helper: handles lifetime and deactivation on hit (works with pooling)
public class Bullet : MonoBehaviour
{
    private float lifetime = 5f;
    private float spawnTime;
    private bool pooled = false;

    public void Launch(float life, bool isPooled)
    {
        lifetime = life;
        pooled = isPooled;
        spawnTime = Time.time;
        // Cancel any previous invoke if reused
        CancelInvoke();
        if (!pooled)
            Invoke(nameof(DestroySelf), lifetime);
    }

    void OnEnable()
    {
        spawnTime = Time.time;
    }

    void Update()
    {
        if (pooled && Time.time - spawnTime >= lifetime)
            Deactivate();
    }

    void OnCollisionEnter(Collision other)
    {
        // On hit, deactivate / destroy immediately. You can add damage logic here.
        Deactivate();
    }

    void Deactivate()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.velocity = Vector3.zero;

        if (pooled)
            gameObject.SetActive(false);
        else
            Destroy(gameObject);
    }

    void DestroySelf()
    {
        Destroy(gameObject);
    }
}
