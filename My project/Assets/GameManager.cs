using Fusion;
using UnityEngine;
using System.Collections.Generic;
using StarterAssets;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Networked] public bool GameStarted { get; set; }

    [Networked] public TickTimer MatchTimer { get; set; }

    [Networked] public int PlayerCount { get; set; }

    [Networked] public int ReadyCount { get; set; }

    private readonly string[] roles =
    {
        "A",
        "B",
        "C",
        "D"
    };

    private void Awake()
    {
        Instance = this;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        UpdatePlayerCounts();

        if (!GameStarted)
        {
            CheckAllReady();
        }
        else
        {
            if (MatchTimer.Expired(Runner))
            {
                EndGame();
            }
        }
    }

    void UpdatePlayerCounts()
    {
        PlayerData[] players =
            FindObjectsOfType<PlayerData>();

        PlayerCount = players.Length;

        int ready = 0;

        foreach (var player in players)
        {
            if (player.IsReady)
                ready++;
        }

        ReadyCount = ready;
    }

    void CheckAllReady()
    {
        PlayerData[] players =
            FindObjectsOfType<PlayerData>();

        if (players.Length < 2)
            return;

        foreach (var player in players)
        {
            if (!player.IsReady)
                return;
        }

        StartGame(players);
    }

    void StartGame(PlayerData[] players)
    {
        GameStarted = true;

        List<string> rolePool =
            new List<string>(roles);

        foreach (var player in players)
        {
            int index =
                Random.Range(0, rolePool.Count);

            player.Role = rolePool[index];

            rolePool.RemoveAt(index);

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

    void EndGame()
    {
        GameStarted = false;

        RPC_ShowEndGame();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_GameStarted()
    {
        Debug.Log("Game Started");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_ShowEndGame()
    {
        PlayerData[] players =
            FindObjectsOfType<PlayerData>();

        foreach (var player in players)
        {
            var controller =
                player.GetComponent<ThirdPersonController>();

            if (controller != null)
            {
                controller.enabled = false;
            }

            if (player.HasInputAuthority &&
                player.endGameUI != null)
            {
                player.endGameUI.SetActive(true);
            }
        }
    }
}