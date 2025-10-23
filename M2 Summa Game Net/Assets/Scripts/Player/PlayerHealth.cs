using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Simple player HP system. Default max HP = 150.
/// Assign Head and Body Colliders in the inspector so bullets can detect headshots.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("HP")]
    [Tooltip("Maximum health. Default 150.")]
    public int maxHealth = 150;

    private int currentHealth;

    [Header("Body parts (assign Colliders)")]
    [Tooltip("Collider used to detect headshots.")]
    public Collider headCollider;
    [Tooltip("Optional: collider used for the body (not strictly required).")]
    public Collider bodyCollider;

    [Header("Events (optional)")]
    public UnityEvent onDamage;
    public UnityEvent onDeath;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Apply damage to this player.
    /// </summary>
    /// <param name="amount">Damage amount (already adjusted for headshot, if any).</param>
    /// <param name="isHeadHit">True when this damage was from the head collider.</param>
    public void TakeDamage(int amount, bool isHeadHit = false)
    {
        currentHealth -= amount;
        Debug.Log($"{name} took {amount} damage{(isHeadHit ? " (HEADSHOT)" : "")}. HP: {Mathf.Max(currentHealth,0)}/{maxHealth}");

        onDamage?.Invoke();

        if (currentHealth <= 0)
            Die();
    }

    public int GetCurrentHealth() => currentHealth;
    public float GetHealthNormalized() => (float)currentHealth / maxHealth;

    void Die()
    {
        Debug.Log($"{name} died.");
        onDeath?.Invoke();
        // Default behavior: disable the GameObject. Adjust to your game (destroy, play animation, etc.)
        // gameObject.SetActive(false);
    }
}
