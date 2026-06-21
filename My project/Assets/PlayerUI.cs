using Fusion;
using UnityEngine;

public class PlayerUI : NetworkBehaviour
{
    public GameObject playerCanvas;

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            playerCanvas.SetActive(true);
        }
        else
        {
            playerCanvas.SetActive(false);
        }
    }
}