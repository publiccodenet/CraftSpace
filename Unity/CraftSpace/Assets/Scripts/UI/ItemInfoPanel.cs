using UnityEngine;
using TMPro;

namespace CraftSpace.UI
{
    public class ItemInfoPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _infoText;

        public void UpdateInfo(string title)
        {
            if (_infoText != null)
            {
                _infoText.text = title;
            }
        }

        public void Clear()
        {
            if (_infoText != null)
            {
                _infoText.text = "";
            }
        }
    }
} 