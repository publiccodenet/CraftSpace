using UnityEngine;
using TMPro;

// Add namespace to avoid collision with the existing ItemView
namespace CraftSpace.Collections
{
    [RequireComponent(typeof(ItemLoader))]
    public class CollectionItemView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Material loadingMaterial;
        
        // Public accessor for loadingMaterial
        public Material LoadingMaterial => loadingMaterial;
        
        private ItemLoader _itemLoader;
        
        private void Start()
        {
            _itemLoader = GetComponent<ItemLoader>();
            UpdateLabel();
        }
        
        private void UpdateLabel()
        {
            if (titleText != null && _itemLoader != null)
            {
                titleText.text = _itemLoader.ItemTitle;
            }
        }
        
        // Public method for other scripts to update the label
        public void SetText(string newText)
        {
            if (titleText != null)
            {
                titleText.text = newText;
            }
        }
    }
} 