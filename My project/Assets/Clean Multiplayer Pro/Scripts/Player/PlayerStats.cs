#if CMPSETUP_COMPLETE
using UnityEngine;
using Fusion;
using TMPro;
using System;
using UnityEngine.SceneManagement;
using System.Collections;

namespace AvocadoShark
{
    // กำหนดสถานะผู้เล่น (สไตล์ Dead by Daylight / Identity V)
    public enum PlayerStatus { Healthy, Injured, Downed }

    public class PlayerStats : NetworkBehaviour
    {
        private ChangeDetector _changeDetector;
        [Networked] public bool IsDisconnecting { get; set; }
        [Networked] public TickTimer VoteTime { get; set; }
        [Networked] public NetworkString<_32> PlayerName { get; set; }
        [Networked] public NetworkString<_32> VoteInitiatorPlayerName { get; set; }
        [Networked] public NetworkBool VoteKick { get; set; }
        [Networked] public int PositiveVotes { get; set; }
        [Networked] public int NegativeVotes { get; set; }
        [Networked] public PlayerStatus CurrentStatus { get; set; }
        // --- เพิ่มตัวแปรสำหรับคูลดาวน์ดาเมจตรงนี้ ---
        [Networked] public TickTimer DamageCooldownTimer { get; set; }
        [SerializeField] private float damageCooldownDuration = 2.0f; // ตั้งเวลาคูลดาวน์ตรงนี้ (เช่น 2 วินาที)
                                                                      // ----------------------------------------
        public Action<PlayerStatus> OnStatusChanged;
        // ---------------------------------

        public int maxVoteTime = 15;

        public bool isVoteInitiator = false;
        public Action<int> OnPositiveVotesChanged, OnNegativeVotesChanged, OnVoteTimeUpdated;
        public Action<bool> OnSpeaking;

        [SerializeField] TextMeshPro playerNameLabel;

        public static PlayerStats instance;

        public Action<string> OnPlayerStatsReady;

        public override void Spawned()
        {
            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
            GetComponent<PlayerWorldUIManager>().OnSpeaking += Speaking;

            if (HasStateAuthority)
            {
                PlayerName = FusionConnection.Instance._playerName;
                CurrentStatus = PlayerStatus.Healthy; // เริ่มต้นเกมให้เป็นสถานะปกติ (Healthy)
                OnPlayerStatsReady?.Invoke(PlayerName.ToString());
                playerNameLabel.text = !HasStateAuthority ? PlayerName.ToString() : "";
                Debug.Log(PlayerName + " Has state authority");
                if (instance == null)
                {
                    instance = this;
                }
            }
            else
            {
                SessionPlayers.instance.AddPlayer(this);
                playerNameLabel.text = !HasStateAuthority ? PlayerName.ToString() : "";
            }

            // แจ้งเตือนให้ UI Manager ทำการวาดหรือสร้างไอคอนผู้เล่นใหม่ขึ้นมาบนจอ
            PlayerHUDManager.Instance?.RefreshHUD();
        }

        public override void Render()
        {
            foreach (var change in _changeDetector.DetectChanges(this, out var previousBuffer, out var currentBuffer))
            {
                switch (change)
                {
                    case nameof(PlayerName):
                        HandleChangeDetection<NetworkString<_32>>(nameof(PlayerName), previousBuffer, currentBuffer,
                            UpdatePlayerName);
                        break;
                    case nameof(VoteKick):
                        HandleChangeDetection<NetworkBool>(nameof(VoteKick), previousBuffer, currentBuffer,
                            OnVoteKickStateChanged);
                        break;
                    case nameof(PositiveVotes):
                        HandleChangeDetection<int>(nameof(PositiveVotes), previousBuffer, currentBuffer,
                            OnPositiveVote);
                        break;
                    case nameof(NegativeVotes):
                        HandleChangeDetection<int>(nameof(NegativeVotes), previousBuffer, currentBuffer,
                            OnNegativeVote);
                        break;
                    // --- เพิ่มเติม: ดักจับการเปลี่ยนสถานะผู้เล่นเพื่อส่งข้อมูลไปอัปเดต UI ---
                    case nameof(CurrentStatus):
                        HandleChangeDetection<PlayerStatus>(nameof(CurrentStatus), previousBuffer, currentBuffer,
                            OnStatusUpdate);
                        break;
                        // -------------------------------------------------------------
                }
            }
        }

        private void HandleChangeDetection<T>(string propertyName, NetworkBehaviourBuffer previousBuffer,
            NetworkBehaviourBuffer currentBuffer, Action<T, T> callback) where T : unmanaged
        {
            var reader = GetPropertyReader<T>(propertyName);
            var (previous, current) = reader.Read(previousBuffer, currentBuffer);
            callback(previous, current);
        }

        private void Update()
        {
            if (!VoteKick)
                return;
            OnVoteTimeUpdated?.Invoke(Mathf.RoundToInt(VoteTime.RemainingTime(Runner).GetValueOrDefault()));
        }

        public override void FixedUpdateNetwork()
        {
            if (VoteTime.Expired(Runner) && VoteKick)
            {
                VoteKick = false;
            }
        }

        protected void UpdatePlayerName(NetworkString<_32> previous, NetworkString<_32> current)
        {
            SessionPlayers.instance.AddPlayer(this);
            playerNameLabel.text = !HasStateAuthority ? current.ToString() : "";
            // อัปเดต UI เมื่อชื่อจริงส่งมาถึงฝั่ง Client แล้ว
            PlayerHUDManager.Instance?.RefreshHUD();
        }

        // --- เพิ่มเติม: ฟังก์ชันสำหรับการเปลี่ยนสถานะของผู้เล่น (เรียกใช้จากฝั่งคิลเลอร์ หรือเหตุการณ์โดนโจมตี) ---
        public void ChangeStatus(PlayerStatus newStatus)
        {
            if (Object.HasStateAuthority)
            {
                CurrentStatus = newStatus;
            }
        }

        private void OnStatusUpdate(PlayerStatus previous, PlayerStatus current)
        {
            OnStatusChanged?.Invoke(current);
        }
        // -----------------------------------------------------------------------------------------

        public void InitializeVoteKick()
        {
            Debug.Log("InitializeVoteKick");

            if (Object.HasStateAuthority)
            {
                PositiveVotes = PositiveVotes + 1;
            }

            if (NotEnoughPlayers())
            {
                Debug.Log("Not enough players for vote kick");
                return;
            }

            if (IsDisconnecting)
            {
                Debug.Log("Player disconnection in process");
                return;
            }

            if (VoteKick == true)
            {
                Debug.Log($"Votekick already in process");
                return;
            }

            if (Runner.GameMode == GameMode.Shared)
            {
                Debug.Log($"Initializing Vote kick for {Runner.GameMode} Mode");
                if (Object.HasStateAuthority)
                {
                    VoteKick = true;
                    VoteInitiatorPlayerName = PlayerName;
                    VoteTime = TickTimer.CreateFromSeconds(Runner, maxVoteTime);
                }
                else
                {
                    RPC_BeginVoteKick();
                    isVoteInitiator = true;
                }
            }
            else
            {
                Debug.Log($"Initializing Vote kick for {Runner.GameMode} Mode");
            }
        }

        public void OnVoteKickStateChanged(NetworkBool previous, NetworkBool current)
        {
            Debug.Log("Vote kick state changed");
            Debug.Log($"positive votes {PositiveVotes}");
            if (current)
            {
                SessionPlayers.instance.AddVoteKick(this);
                PositiveVotes = 0;
                NegativeVotes = 0;
            }
            else
            {
                if (HasStateAuthority)
                {
                    if (PositiveVotes > NegativeVotes)
                    {
                        IsDisconnecting = true;
                        RemovePlayer();
                        RPC_PlayerVoteResultMessage(
                            $"Vote kick for {PlayerName} has passed");
                    }
                    else
                    {
                        RPC_PlayerVoteResultMessage(
                            $"Vote kick for {PlayerName} has failed");
                    }

                    PositiveVotes = 0;
                    NegativeVotes = 0;
                }

                SessionPlayers.instance.RemoveVoteKick(this);
            }
        }

        public int GetNegativeVotes()
        {
            return (SessionPlayers.instance.activePlayers.Count - PositiveVotes - 1);
        }

        public bool NotEnoughPlayers()
        {
            return (SessionPlayers.instance.activePlayers.Count <= 2);
        }

        public void AddPositiveVote()
        {
            if (!VoteKick)
                return;
            if (Object.HasStateAuthority)
                PositiveVotes += 1;
            else
                RPC_AddPositiveVote();
        }

        public void AddNegativeVote()
        {
            if (!VoteKick)
                return;
            if (Object.HasStateAuthority)
                NegativeVotes += 1;
            else
                RPC_AddNegativeVote();
        }

        public void RemovePlayer()
        {
            if (Object.HasStateAuthority)
            {
                Debug.Log("Shutting Down");
                StartCoroutine(RemovePlayerAfterDelay(3f));
            }
        }

        private IEnumerator RemovePlayerAfterDelay(float time)
        {
            yield return new WaitForSeconds(time);
            Runner.Shutdown();
            SceneManager.LoadScene(0);
        }

        public void OnPositiveVote(int previous, int current)
        {
            OnPositiveVotesChanged?.Invoke(current);
        }

        public void OnNegativeVote(int previous, int current)
        {
            OnNegativeVotesChanged?.Invoke(current);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            SessionPlayers.instance.RemovePlayer(this);
            // เมื่อผู้เล่นหลุดหรือออกจากเกม ให้ลบไอคอน UI ของคนนั้นออกด้วย
            PlayerHUDManager.Instance?.RefreshHUD();

            if (VoteKick)
            {
                SessionPlayers.instance.RemoveVoteKick(this);
                RPC_PlayerVoteResultMessage($"Vote kick failed");
            }
        }

        [Rpc(sources: RpcSources.Proxies, targets: RpcTargets.StateAuthority)]
        public void RPC_BeginVoteKick()
        {
            Debug.Log("RPC_BeginVoteKick");
            VoteKick = true;
            VoteTime = TickTimer.CreateFromSeconds(Runner, maxVoteTime);
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPC_PlayerVoteResultMessage(string message)
        {
            Debug.Log("RPC_PlayerVoteResultMessage");
            InfoPanel.instance.AddMessage(message);
        }

        [Rpc(sources: RpcSources.Proxies, targets: RpcTargets.StateAuthority)]
        public void RPC_AddPositiveVote()
        {
            PositiveVotes += 1;
        }

        [Rpc(sources: RpcSources.Proxies, targets: RpcTargets.StateAuthority)]
        public void RPC_AddNegativeVote()
        {
            Debug.Log("RPC_AddNegativeVote");
            NegativeVotes += 1;
        }

        private void Speaking(bool value)
        {
            OnSpeaking?.Invoke(value);
        }
        // ลากไปแปะเพิ่มไว้ด้านในคลาส PlayerStats (ต่อท้ายฟังก์ชันอื่นๆ ก็ได้ครับ)

        private void OnTriggerEnter(Collider other)
        {
            // 1. เช็คสิทธิ์ควบคุมเครื่องตัวเอง
            if (!Object.HasStateAuthority) return;

            // 2. เช็ค Tag ของวัตถุทำดาเมจ
            if (other.CompareTag("DamageToPlayer"))
            {
                // 3. ตรวจสอบว่ายังมีคูลดาวน์เหลืออยู่ไหม (ถ้ายังไม่หมดเวลา ให้ข้ามไปเลย ไม่โดนดาเมจ)
                if (DamageCooldownTimer.ExpiredOrNotRunning(Runner) == false)
                {
                    Debug.Log($"[Damage Cooldown] ยังอยู่ในช่วงอมตะ! คูลดาวน์เหลือ: {DamageCooldownTimer.RemainingTime(Runner):F1} วินาที");
                    return;
                }

                // 4. ถ้าไม่มีคูลดาวน์ (หรือคูลดาวน์หมดแล้ว) ถึงจะเริ่มคำนวณหักเลือด
                switch (CurrentStatus)
                {
                    case PlayerStatus.Healthy:
                        ChangeStatus(PlayerStatus.Injured);
                        // สั่งเริ่มนับเวลาคูลดาวน์ทันทีหลังจากโดนตีขั้นแรก
                        DamageCooldownTimer = TickTimer.CreateFromSeconds(Runner, damageCooldownDuration);
                        Debug.Log($"[Damage] {PlayerName} บาดเจ็บ! เปิดใช้งานอมตะชั่วคราว {damageCooldownDuration} วิ");
                        break;

                    case PlayerStatus.Injured:
                        ChangeStatus(PlayerStatus.Downed);
                        // เมื่อล้มแล้วก็เปิดคูลดาวน์ไว้เผื่อระบบอื่นๆ
                        DamageCooldownTimer = TickTimer.CreateFromSeconds(Runner, damageCooldownDuration);
                        Debug.Log($"[Damage] {PlayerName} ล้มลง!");
                        break;

                    case PlayerStatus.Downed:
                        Debug.Log($"[Damage] {PlayerName} ล้มอยู่แล้ว");
                        break;
                }
            }
        }
    }
}
#endif