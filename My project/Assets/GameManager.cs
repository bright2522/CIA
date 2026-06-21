using Fusion;
using UnityEngine;
using System.Collections.Generic;
using StarterAssets;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("Roles")]
    public RoleData[] roles;

    [Networked]
    public bool GameStarted { get; set; }

    [Networked]
    public TickTimer MatchTimer { get; set; }

    [Networked]
    public int PlayerCount { get; set; }

    [Networked]
    public int ReadyCount { get; set; }

    private void Awake()
    {
        Instance = this;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        UpdatePlayerInfo();

        if (!GameStarted)
        {
            CheckReady();
        }
        else
        {
            if (MatchTimer.Expired(Runner))
            {
                EndGame();
            }
        }
    }

    void UpdatePlayerInfo()
    {
        PlayerData[] players =
            FindObjectsOfType<PlayerData>();

        PlayerCount = players.Length;

        int ready = 0;

        foreach (var p in players)
        {
            if (p.IsReady)
                ready++;
        }

        ReadyCount = ready;
    }

    void CheckReady()
    {
        PlayerData[] players =
            FindObjectsOfType<PlayerData>();

        if (players.Length < 2)
            return;

        foreach (var p in players)
        {
            if (!p.IsReady)
                return;
        }

        StartGame(players);
    }

    void StartGame(PlayerData[] players)
    {
        GameStarted = true;

        List<int> rolePool =
            new List<int>();

        for (int i = 0; i < roles.Length; i++)
        {
            rolePool.Add(i);
        }

        foreach (var player in players)
        {
            int randomIndex =
                Random.Range(0, rolePool.Count);

            int roleID =
                rolePool[randomIndex];

            player.RoleID = roleID;

            rolePool.RemoveAt(randomIndex);

            Debug.Log(
                $"{player.Object.InputAuthority} ได้ Role : {roles[roleID].roleName}"
            );

            CharacterController cc =
                player.GetComponent<CharacterController>();

            if (cc != null)
                cc.enabled = false;

            player.transform.position = Vector3.zero;

            if (cc != null)
                cc.enabled = true;
        }

        MatchTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                300f
            );

        RPC_GameStarted();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_GameStarted()
    {
        Debug.Log("Game Started");
    }

    void EndGame()
    {
        GameStarted = false;

        RPC_EndGame();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_EndGame()
    {
        PlayerData[] players =
            FindObjectsOfType<PlayerData>();

        foreach (var player in players)
        {
            ThirdPersonController controller =
                player.GetComponent<ThirdPersonController>();

            if (controller != null)
            {
                controller.enabled = false;
            }

            if (player.HasInputAuthority)
            {
                player.endGameUI.SetActive(true);
            }
        }
    }
}