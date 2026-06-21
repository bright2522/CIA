using Fusion;
using TMPro;
using UnityEngine;
using StarterAssets;

public class PlayerData : NetworkBehaviour
{
    public static PlayerData LocalPlayer;

    [Networked]
    public bool IsReady { get; set; }

    [Networked]
    public int RoleID { get; set; }

    [Header("UI")]
    public TMP_Text statusText;
    public TMP_Text roleText;
    public TMP_Text timerText;

    public GameObject endGameUI;
    public GameObject playerCanvas;

    private bool roleApplied;

    private GameObject currentRoleUI;
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

    public override void Render()
    {

        if (!roleApplied &&
            GameManager.Instance != null &&
            GameManager.Instance.GameStarted)
        {
            ApplyRole();
            roleApplied = true;
        }

        if (GameManager.Instance == null)
            return;

        statusText.text =
            $"Players : {GameManager.Instance.PlayerCount}\n" +
            $"Ready : {GameManager.Instance.ReadyCount}/{GameManager.Instance.PlayerCount}";

        if (roleText != null)
        {
            roleText.text =
                GameManager.Instance.roles[RoleID].roleName;
        }

        if (GameManager.Instance.GameStarted)
        {
            float remain =
                GameManager.Instance.MatchTimer.RemainingTime(Runner) ?? 0;

            int min = Mathf.FloorToInt(remain / 60);
            int sec = Mathf.FloorToInt(remain % 60);

            timerText.text = $"{min:00}:{sec:00}";
        }
    }

    public void ApplyRole()
    {

        RoleData role =
            GameManager.Instance.roles[RoleID];

        ThirdPersonController controller =
            GetComponent<ThirdPersonController>();

        controller.MoveSpeed = role.walkSpeed;
        controller.SprintSpeed = role.sprintSpeed;

        if (currentRoleUI != null)
        {
            Destroy(currentRoleUI);
        }

        if (role.roleUIPrefab != null)
        {
            currentRoleUI = Instantiate(
    role.roleUIPrefab,
    playerCanvas.transform,
    false
);
        }
    }

    public void Ready()
    {
        RPC_SetReady();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_SetReady()
    {
        IsReady = true;
    }
}