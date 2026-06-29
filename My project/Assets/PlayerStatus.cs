using UnityEngine;
using Fusion;

public class PlayerStatus : NetworkBehaviour
{
    [Networked] public int Health { get; set; }
    public bool IsDead => Health <= 0;
    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            Health = 100;
        }
    }

    public void TakeDamage(int damage)
    {
        if (Object.HasStateAuthority && !IsDead)
        {
            Health -= damage;
            Debug.Log($"[Player] Took {damage} damage. Remaining Health: {Health}");

            if (IsDead)
            {
                Die();
            }
        }
    }

    private void Die()
    {
        Debug.Log("[Player] Player is Dead!");
    }
}