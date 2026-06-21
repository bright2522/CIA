using Fusion;
using TMPro;
using UnityEngine;

public class PlayerData : NetworkBehaviour
{
    public static PlayerData LocalPlayer;

    [Networked] public bool IsReady { get; set; }

    [Networked]
    public NetworkString<_16> Role { get; set; }

    [Header("UI")]
    public TMP_Text statusText;
    public TMP_Text roleText;
    public TMP_Text timerText;

    public GameObject endGameUI;
    public GameObject playerCanvas;

    private void Update()
    {
        Render();
    }
    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            LocalPlayer = this;
        }
        else
        {
            playerCanvas.SetActive(false);
        }
    }

    public void Ready()
    {
        if (HasInputAuthority)
        {
            RPC_SetReady();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_SetReady()
    {
        IsReady = true;
    }

    public override void Render()
    {
        if (roleText != null)
        {
            roleText.text = $"Role : {Role}";
        }

        if (GameManager.Instance != null)
        {
            if (statusText != null)
            {
                statusText.text =
                    $"Players : {GameManager.Instance.PlayerCount}\n" +
                    $"Ready : {GameManager.Instance.ReadyCount}/{GameManager.Instance.PlayerCount}";
            }

            if (timerText != null &&
                GameManager.Instance.GameStarted &&
                GameManager.Instance.MatchTimer.IsRunning)
            {
                float remain =
                    GameManager.Instance.MatchTimer.RemainingTime(Runner) ?? 0;

                int min = Mathf.FloorToInt(remain / 60);
                int sec = Mathf.FloorToInt(remain % 60);

                timerText.text = $"{min:00}:{sec:00}";
            }
        }
    }
}