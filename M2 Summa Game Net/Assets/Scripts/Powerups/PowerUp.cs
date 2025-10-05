using UnityEngine;
using Photon.Pun;

public enum PowerUpType 
{ 
    SpeedBoost, 
    JumpBoost 
}

public class PowerUp : MonoBehaviourPun
{
    public PowerUpType type;

    [Header("Speed Boost Settings")]
    public float speedMultiplier = 2f;
    public float speedDuration = 5f;

    [Header("Jump Boost Settings")]
    public float jumpMultiplier = 2f;
    public float jumpDuration = 5f;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PhotonView pv = other.GetComponent<PhotonView>();
        if (pv != null && pv.IsMine)
        {
            PlayerMovement3D player = other.GetComponent<PlayerMovement3D>();
            if (player != null)
            {
                if (type == PowerUpType.SpeedBoost)
                {
                    player.ApplySpeedBoost(speedMultiplier, speedDuration);
                }
                else if (type == PowerUpType.JumpBoost)
                {
                    player.ApplyJumpBoost(jumpMultiplier, jumpDuration);
                }
            }

            PhotonNetwork.Destroy(gameObject); // synced removal
        }
    }
}
