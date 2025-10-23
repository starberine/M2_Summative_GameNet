using UnityEngine;

/// <summary>
/// Attach this to a hitbox collider (head or body).
/// Set the Collider to "Is Trigger = true".
/// - If isHead == false: applies `damage` when hit by a GameObject tagged "Bullet".
/// - If isHead == true: multiplies the *body* damage by 3 when hit (falls back to own damage if body hitbox not configured).
/// - Optionally ignores bullets fired by the same player via BulletOwner.owner.
/// </summary>
public class HitboxDamage : MonoBehaviour
{
    [Tooltip("Damage to apply when this hitbox is hit (used for body; used as fallback on head).")]
    public int damage = 15;

    [Tooltip("Check if this is the head hitbox. Headshots will multiply the body damage by 3.")]
    public bool isHead = false;

    [Tooltip("If true, bullets fired by the same player will not damage that player.")]
    public bool ignoreFriendlyFire = true;

    void OnTriggerEnter(Collider other)
    {
        // Only react to bullets (set the bullet prefab's Tag to "Bullet")
        if (!other.CompareTag("Bullet"))
            return;

        // Friendly-fire check (if enabled)
        if (ignoreFriendlyFire)
        {
            BulletOwner bo = other.GetComponentInParent<BulletOwner>();
            if (bo != null)
            {
                PlayerHealth myPH = GetComponentInParent<PlayerHealth>();
                if (myPH != null && bo.owner == myPH.gameObject)
                    return;

                // Extra safeguard: compare by PlayerHealth on owner if needed
                if (myPH != null && bo.owner != null)
                {
                    PlayerHealth ownerPH = bo.owner.GetComponentInParent<PlayerHealth>();
                    if (ownerPH != null && ownerPH.gameObject == myPH.gameObject)
                        return;
                }
            }
        }

        // Find PlayerHealth for this hitbox
        PlayerHealth ph = GetComponentInParent<PlayerHealth>();
        if (ph == null)
        {
            Debug.LogWarning($"HitboxDamage: no PlayerHealth found in parents of {name}.");
            return;
        }

        // Compute damage: if head, try to read body hitbox damage and multiply by 3
        int appliedDamage;
        if (isHead)
        {
            int bodyDamage = damage; // fallback
            if (ph.bodyCollider != null)
            {
                HitboxDamage bodyHitbox = ph.bodyCollider.GetComponent<HitboxDamage>();
                if (bodyHitbox != null)
                    bodyDamage = bodyHitbox.damage;
            }
            appliedDamage = bodyDamage * 3;
        }
        else
        {
            appliedDamage = damage;
        }

        // Apply damage (pass isHead for logging/logic)
        ph.TakeDamage(appliedDamage, isHead);

        // --- Bullet cleanup: prefer deactivating if it's pooled, otherwise destroy ---
        Transform poolTransform = null;
        try
        {
            poolTransform = BulletPool.Instance != null ? BulletPool.Instance.PoolTransform : null;
        }
        catch
        {
            poolTransform = null;
        }

        GameObject bulletGo = other.gameObject;

        if (poolTransform != null && bulletGo.transform.IsChildOf(poolTransform))
        {
            // pooled bullet — just deactivate
            bulletGo.SetActive(false);
        }
        else
        {
            // temporary / no pool — destroy
            Destroy(bulletGo);
        }
    }
}
