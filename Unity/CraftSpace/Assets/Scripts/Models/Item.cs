using System;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    // Methods related to items
    
    public static ItemData FindByItemId(string collectionId, string itemId)
    {
        return Brewster.Instance.GetItem(collectionId, itemId);
    }
} 