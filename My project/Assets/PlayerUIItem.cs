using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AvocadoShark
{
    public class PlayerUIItem : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image statusIconImage;
        [SerializeField] private TextMeshProUGUI nameText;

        [Header("Status Sprites")]
        [SerializeField] private Sprite healthySprite;
        [SerializeField] private Sprite injuredSprite;
        [SerializeField] private Sprite downedSprite;

        private PlayerStats targetPlayer;

        public void Setup(PlayerStats player)
        {
            targetPlayer = player;
            nameText.text = player.PlayerName.ToString();

            // สมัครรับอีเวนต์เมื่อสถานะเปลี่ยน
            targetPlayer.OnStatusChanged += UpdateUI;

            // อัปเดตหน้าตาครั้งแรก
            UpdateUI(targetPlayer.CurrentStatus);
        }

        private void UpdateUI(PlayerStatus status)
        {
            switch (status)
            {
                case PlayerStatus.Healthy:
                    statusIconImage.sprite = healthySprite;
                    statusIconImage.color = Color.white;
                    break;
                case PlayerStatus.Injured:
                    statusIconImage.sprite = injuredSprite;
                    statusIconImage.color = Color.orange; // หรือเปลี่ยนตามธีมเกม
                    break;
                case PlayerStatus.Downed:
                    statusIconImage.sprite = downedSprite;
                    statusIconImage.color = Color.red;
                    break;
            }
        }

        private void OnDestroy()
        {
            if (targetPlayer != null)
            {
                targetPlayer.OnStatusChanged -= UpdateUI;
            }
        }
    }
}