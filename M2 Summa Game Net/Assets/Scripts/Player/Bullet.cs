using UnityEngine;

/// <summary>
/// Simple bullet helper: handles lifetime and deactivation on hit (works with pooling)
/// Put this in a file named "Bullet.cs".
/// </summary>
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
        Deactivate();
    }

    void Deactivate()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.velocity = Vector3.zero;

        if (pooled)
        {
            // Re-parent to global pool if present (keeps scene hierarchy tidy)
            if (BulletPool.Instance != null && BulletPool.Instance.PoolTransform != null)
                transform.SetParent(BulletPool.Instance.PoolTransform, true);

            gameObject.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void DestroySelf()
    {
        Destroy(gameObject);
    }
}
