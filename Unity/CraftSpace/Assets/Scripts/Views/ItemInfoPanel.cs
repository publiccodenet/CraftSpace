using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

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