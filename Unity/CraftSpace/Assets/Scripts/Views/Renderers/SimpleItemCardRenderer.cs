using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(ItemView))]
public class SimpleItemCardRenderer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image _coverImage;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _authorText;
    
    private ItemView _itemView;
    
    private void Awake()
    {
        _itemView = GetComponent<ItemView>();
        _itemView.OnModelUpdated += UpdateRenderer;
    }
    
    private void OnDestroy()
    {
        if (_itemView != null)
        {
            _itemView.OnModelUpdated -= UpdateRenderer;
        }
    }
    
    private void UpdateRenderer()
    {
        var model = _itemView.Model;
        if (model == null)
            return;
            
        // Update cover image
        if (_coverImage != null && model.cover != null)
        {
            _coverImage.sprite = Sprite.Create(
                model.cover,
                new Rect(0, 0, model.cover.width, model.cover.height),
                Vector2.one * 0.5f
            );
            _coverImage.enabled = true;
        }
        else if (_coverImage != null)
        {
            _coverImage.enabled = false;
        }
        
        // Update text fields
        if (_titleText != null)
            _titleText.text = model.title;
            
        if (_authorText != null)
            _authorText.text = model.creator;
    }
} 