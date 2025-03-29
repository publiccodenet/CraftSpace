//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by CraftSpace Schema Generator.
//     Runtime Version: 1.0
//
//     This is an auto-generated Unity ScriptableObject class for handling
//     Internet Archive metadata. For more information, see:
//     Assets/Editor/SchemaGenerator/README.md
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using CraftSpace;

#nullable enable

[CreateAssetMenu(fileName = "ItemSchema", menuName = "CraftSpace/ItemSchema", order = 0)]
public class ItemSchema : SchemaGeneratedObject
{
[Serializable]
public class ExtraFields
{
}

    /// <summary>
    /// Unique Internet Archive identifier.
    /// </summary>
    [JsonProperty("id")]
    [SerializeField] private string _id;
    public string Id { get => _id; set => _id = value; }

    /// <summary>
    /// Title of this item.
    /// </summary>
    [JsonProperty("title")]
    [SerializeField] private string _title;
    public string Title { get => _title; set => _title = value; }

    /// <summary>
    /// Description of the item.
    /// </summary>
    [JsonProperty("description")]
    [SerializeField] private System.Object _description;
    public System.Object Description { get => _description; set => _description = value; }

    /// <summary>
    /// Creator(s) of this item.
    /// </summary>
    [JsonProperty("creator")]
    [SerializeField] private System.Object _creator;
    public System.Object Creator { get => _creator; set => _creator = value; }

    /// <summary>
    /// Subject tags for this item.
    /// </summary>
    [JsonProperty("subject")]
    [SerializeField] private System.Object _subject;
    public System.Object Subject { get => _subject; set => _subject = value; }

    /// <summary>
    /// Collections this item belongs to.
    /// </summary>
    [JsonProperty("collection")]
    [SerializeField] private System.Object _collection;
    public System.Object Collection { get => _collection; set => _collection = value; }

    /// <summary>
    /// Type of media (texts, movies, audio, etc).
    /// </summary>
    [JsonProperty("mediatype")]
    [SerializeField] private string _mediatype;
    public string Mediatype { get => _mediatype; set => _mediatype = value; }

    /// <summary>
    /// URL or path to the cover image.
    /// </summary>
    [JsonProperty("coverImage")]
    [SerializeField] private string _coverImage;
    public string CoverImage { get => _coverImage; set => _coverImage = value; }

}
