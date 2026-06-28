using Fusion;
using UnityEngine;
using System.Collections.Generic;
using StarterAssets;
using TMPro;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("Roles")]
    public RoleData[] roles;

    [Header("UI (World or Canvas Debug Panel)")]
    public TMP_Text gameStateText;
    public TMP_Text playerCountText;
    public TMP_Text readyCountText;
    public TMP_Text timerText;
    public TMP_Text logText;

    [Networked] public bool GameStarted { get; set; }
    [Networked] public TickTimer MatchTimer { get; set; }
    [Networked] public int PlayerCount { get; set; }
    [Networked] public int ReadyCount { get; set; }

    private float cachedRemainingTime;

    private void Awake()
    {
        Instance = this;
    }

    public override void FixedUpdateNetwork()
    {
        UpdatePlayerInfoUI();

        if (!GameStarted)
        {
            CheckReady();
            UpdatePlayerWaitingIndices(); // 👈 เพิ่มฟังก์ชันจัดการลำดับอนิเมชั่นตรงนี้
        }
        else
        {
            UpdateTimer();

            if (MatchTimer.Expired(Runner))
            {
                EndGame();
            }
        }
    }

    void UpdatePlayerWaitingIndices()
    {
        // ทำเฉพาะเครื่องที่เป็น State Authority ของ GameManager (Server/Host) เท่านั้น
        if (!HasStateAuthority) return;

        PlayerData[] players = FindObjectsOfType<PlayerData>();
        List<PlayerData> sortedPlayers = new List<PlayerData>(players);

        // เรียงลำดับด้วย Network Object ID เพื่อให้ทุกเครื่องเข้าใจตรงกันแน่นอน
        sortedPlayers.Sort((a, b) => a.Object.Id.CompareTo(b.Object.Id));

        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            // เซ็ตลำดับ (1, 2, 3, 4) ลงใน Networked Property 
            // ค่านี้จะถูกส่งไปหาผู้เล่นทุกคนใน Network ทันที
            sortedPlayers[i].WaitingIndex = i + 1;
        }
    }

    // -----------------------------
    // UPDATE LOBBY STATUS
    // -----------------------------
    void UpdatePlayerInfoUI()
    {
        PlayerData[] players = FindObjectsOfType<PlayerData>();

        PlayerCount = players.Length;

        int ready = 0;

        foreach (var p in players)
        {
            if (p.IsReady)
                ready++;
        }

        ReadyCount = ready;

        UpdateLobbyUI();
    }

    void UpdateLobbyUI()
    {
        if (gameStateText != null)
            gameStateText.text = GameStarted ? "🎮 IN GAME" : "⏳ WAITING ROOM";

        if (playerCountText != null)
            playerCountText.text = $"Players : {PlayerCount}";

        if (readyCountText != null)
            readyCountText.text = $"Ready : {ReadyCount}/{PlayerCount}";
    }

    // -----------------------------
    // CHECK READY START GAME
    // -----------------------------
    void CheckReady()
    {
        PlayerData[] players = FindObjectsOfType<PlayerData>();

        if (players.Length < 2)
            return;

        foreach (var p in players)
        {
            if (!p.IsReady)
                return;
        }

        StartGame(players);
    }

    // -----------------------------
    // START GAME + ROLE SYSTEM
    // -----------------------------
    void StartGame(PlayerData[] players)
    {
        GameStarted = true;

        List<int> rolePool = new List<int>();

        for (int i = 0; i < roles.Length; i++)
            rolePool.Add(i);

        Log("🔥 GAME STARTED");

        foreach (var player in players)
        {
            int randomIndex = Random.Range(0, rolePool.Count);
            int roleID = rolePool[randomIndex];

            rolePool.RemoveAt(randomIndex);

            player.RoleID = roleID;

            string roleName = roles[roleID].roleName;

            Debug.Log($"{player.Object.InputAuthority} => {roleName}");

            Log($"🎭 {player.Object.InputAuthority} = {roleName}");

            // teleport
            CharacterController cc = player.GetComponent<CharacterController>();

            if (cc != null)
                cc.enabled = false;

            player.transform.position = Vector3.zero;

            if (cc != null)
                cc.enabled = true;
        }

        MatchTimer = TickTimer.CreateFromSeconds(Runner, 300f);

        RPC_GameStarted();
    }

    // -----------------------------
    // TIMER UPDATE
    // -----------------------------
    void UpdateTimer()
    {
        float remain = MatchTimer.RemainingTime(Runner) ?? 0;

        if (Mathf.Abs(remain - cachedRemainingTime) < 0.5f)
            return;

        cachedRemainingTime = remain;

        int min = Mathf.FloorToInt(remain / 60);
        int sec = Mathf.FloorToInt(remain % 60);

        if (timerText != null)
            timerText.text = $"⏳ {min:00}:{sec:00}";
    }

    // -----------------------------
    // END GAME
    // -----------------------------
    void EndGame()
    {
        GameStarted = false;

        Log("💀 GAME END");

        RPC_EndGame();
    }

    // -----------------------------
    // RPC START
    // -----------------------------
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_GameStarted()
    {
        Debug.Log("Game Started RPC");
    }

    // -----------------------------
    // RPC END
    // -----------------------------
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_EndGame()
    {
        PlayerData[] players = FindObjectsOfType<PlayerData>();

        foreach (var player in players)
        {
            ThirdPersonController controller =
                player.GetComponent<ThirdPersonController>();

            if (controller != null)
                controller.enabled = false;

            if (player.HasInputAuthority)
            {
                player.endGameUI.SetActive(true);
            }
        }
    }

    // -----------------------------
    // DEBUG LOG UI
    // -----------------------------
    void Log(string msg)
    {
        Debug.Log(msg);

        if (logText == null)
            return;

        logText.text += msg + "\n";
    }

    public int GetPlayerWaitingIndex(PlayerData targetPlayer)
    {
        if (targetPlayer == null) return 1;

        // ดึงผู้เล่นทุกคนที่มีอยู่ในซีนปัจจุบัน
        PlayerData[] players = FindObjectsOfType<PlayerData>();

        // แปลงเป็น List และจัดเรียงลำดับ (เช่น เรียงตาม NetworkObject.Id หรือเวลาที่เข้ามา)
        // การเรียงตาม ID จะช่วยให้ทุกคนเห็นลำดับตรงกันและนิ่งที่สุด
        List<PlayerData> sortedPlayers = new List<PlayerData>(players);
        sortedPlayers.Sort((a, b) => a.Object.Id.CompareTo(b.Object.Id));

        // ค้นหาว่า Player คนนี้อยู่ในลำดับที่เท่าไหร่ของห้อง ณ ปัจจุบัน
        int index = sortedPlayers.IndexOf(targetPlayer);

        // คืนค่ากลับไปเป็นลำดับ (เริ่มที่ 1 ถึง 4)
        // ถ้าหาไม่เจอจะคืนค่า 1 เป็น Default กันบั๊ก
        return index != -1 ? (index + 1) : 1;
    }
}