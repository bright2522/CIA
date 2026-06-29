using UnityEngine;
using System.Collections.Generic;

namespace AvocadoShark
{
    public class PlayerHUDManager : MonoBehaviour
    {
        public static PlayerHUDManager Instance { get; private set; }

        [SerializeField] private GameObject playerUiPrefab; // Prefab ที่มี PlayerUIItem ติดอยู่
        [SerializeField] private Transform hudContainer;   // UI Parent Object ที่มี Layout Group

        private List<GameObject> activeUiItems = new List<GameObject>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        // ฟังก์ชันรีเฟรช UI ตามรายชื่อผู้เล่นที่อยู่ในระบบ
        public void RefreshHUD()
        {
            // เคลียร์ UI เก่าออกก่อน
            foreach (var item in activeUiItems)
            {
                Destroy(item);
            }
            activeUiItems.Clear();

            // ตรวจสอบว่ามี SessionPlayers หรือไม่ (อิงตามโครงสร้างโค้ดเดิมของคุณ)
            if (SessionPlayers.instance == null || SessionPlayers.instance.activePlayers == null)
                return;

            // วนลูปสร้าง UI ให้กับผู้เล่นทุกคนที่มีอยู่ในห้อง
            foreach (PlayerStats player in SessionPlayers.instance.activePlayers)
            {
                if (player == null) continue;

                GameObject uiObj = Instantiate(playerUiPrefab, hudContainer);
                PlayerUIItem uiItem = uiObj.GetComponent<PlayerUIItem>();

                if (uiItem != null)
                {
                    uiItem.Setup(player);
                }

                activeUiItems.Add(uiObj);
            }
        }
    }
}