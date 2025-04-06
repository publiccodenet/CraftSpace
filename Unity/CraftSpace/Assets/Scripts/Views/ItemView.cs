using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

/// <summary>
/// Displays a single Item model as a 3D object in the scene.
/// Pure presentation component that doesn't handle any loading or serialization.
/// </summary>
[AddComponentMenu("Views/Item View")]
public class ItemView : MonoBehaviour, IModelView<Item>
{
    [Header("Model Reference")]
    [SerializeField] private Item model;
    
    [Header("UI References")]
    [SerializeField] private ItemLabel itemLabel;
    
    [Header("Item Display")]
    [SerializeField] private float itemWidth = 1.4f;
    [SerializeField] private float itemHeight = 1.0f;

    [Header("Materials")]
    [SerializeField] private Material loadingMaterial;
    
    [Header("Highlighting")]
    [SerializeField] private GameObject highlightMesh; // Dedicated mesh for highlighting
    [SerializeField] private Material highlightMaterial; // Material for hover state
    [SerializeField] private Material selectionMaterial; // Material for selection state
    [SerializeField] private float highlightElevation = 0.1f;
    [SerializeField] private Color highlightColor = new Color(1f, 0.8f, 0.2f, 0.7f);
    [SerializeField] private Color selectionColor = new Color(1f, 0.5f, 0f, 0.9f);
    
    // State
    private bool isHighlighted = false;
    private bool isSelected = false;
    private Material originalMaterial;
    private Vector3 originalPosition;
    
    // Cached component references
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private BoxCollider boxCollider;
    
    // Collection context
    [SerializeField] private string collectionId;
    
    // Unity event for item changes
    [SerializeField] private UnityEvent<Item> onItemChanged = new UnityEvent<Item>();
    public UnityEvent<Item> OnItemChanged => onItemChanged;
    
    // Property to get/set the model (implementing IModelView)
    public Item Model 
    {
        get => model;
        set => SetModel(value);
    }
    
    // Parent reference
    public CollectionView ParentCollectionView { get; set; }
    
    // Public accessors for state
    public bool IsHighlighted => isHighlighted;
    public bool IsSelected => isSelected;
    
    private void Awake()
    {
        // Cache component references
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();
            
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            
        boxCollider = GetComponent<BoxCollider>();
        
        // Store original position
        originalPosition = transform.position;
        
        // Create highlight mesh if it doesn't exist
        if (highlightMesh == null)
        {
            CreateHighlightMesh();
        }
        
        // Initially disable highlight mesh
        if (highlightMesh != null)
        {
            highlightMesh.SetActive(false);
        }
        
        if (model != null)
        {
            // Register with model on awake
            model.RegisterView(this);
        }
    }
    
    private void CreateHighlightMesh()
    {
        // Create a highlight object as a child
        highlightMesh = new GameObject("HighlightMesh");
        highlightMesh.transform.SetParent(transform, false);
        highlightMesh.transform.localPosition = Vector3.zero;
        
        // Add mesh components
        MeshFilter highlightFilter = highlightMesh.AddComponent<MeshFilter>();
        MeshRenderer highlightRenderer = highlightMesh.AddComponent<MeshRenderer>();
        
        // Create a slightly larger quad
        Mesh highlightQuad = new Mesh();
        float padding = 0.05f; // Padding around the item
        
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-(itemWidth + padding)/2, 0.01f, -(itemHeight + padding)/2),
            new Vector3((itemWidth + padding)/2, 0.01f, -(itemHeight + padding)/2),
            new Vector3(-(itemWidth + padding)/2, 0.01f, (itemHeight + padding)/2),
            new Vector3((itemWidth + padding)/2, 0.01f, (itemHeight + padding)/2)
        };
        
        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        
        int[] triangles = new int[6]
        {
            0, 2, 1,
            2, 3, 1
        };
        
        highlightQuad.vertices = vertices;
        highlightQuad.uv = uv;
        highlightQuad.triangles = triangles;
        highlightQuad.RecalculateNormals();
        
        highlightFilter.mesh = highlightQuad;
        
        // Create default materials if not assigned
        if (highlightMaterial == null)
        {
            highlightMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            highlightMaterial.color = highlightColor;
        }
        
        if (selectionMaterial == null)
        {
            selectionMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            selectionMaterial.color = selectionColor;
        }
    }
    
    private void OnDestroy()
    {
        if (model != null)
        {
            // Unregister when view is destroyed
            model.UnregisterView(this);
        }
    }
    
    /// <summary>
    /// Sets the collection context for this view
    /// </summary>
    public void SetCollectionContext(string id)
    {
        collectionId = id;
    }
    
    // Implement the IModelView interface method
    public void HandleModelUpdated()
    {
        UpdateView();
    }
    
    // Update view based on the current model
    public void UpdateView()
    {
        if (model == null) return;
        
        // Set title label
        if (itemLabel != null)
        {
            itemLabel.SetText(model.Title);
        }
        
        // Apply texture if available
        if (model.cover != null)
        {
            ApplyTexture(model.cover);
        }
        else
        {
            // Apply placeholder and request texture loading from Brewster
            ApplyPlaceholder();
            
            // Request texture from Brewster
            Brewster.Instance.LoadItemCover(model.Id, texture => {
                // Texture will be set on the model by Brewster, and the model will notify us
                // This will trigger OnItemUpdated which calls UpdateView
            });
        }
    }
    
    // Apply texture to the mesh renderer
    private void ApplyTexture(Texture2D texture)
    {
        if (texture == null || meshRenderer == null) return;
        
        // Create material and apply texture
        Material material = new Material(Shader.Find("Unlit/Texture"));
        material.mainTexture = texture;
        
        // Store as original material
        originalMaterial = material;
        
        // Apply material to main mesh
        meshRenderer.material = material;
        
        // Update mesh to match texture dimensions
        float aspectRatio = (float)texture.width / texture.height;
        UpdateMeshForAspectRatio(aspectRatio);
        
        // Update highlight mesh size if it exists
        UpdateHighlightMeshSize();
    }
    
    // Apply placeholder material
    private void ApplyPlaceholder()
    {
        if (meshRenderer == null) return;
        
        Material material;
        if (loadingMaterial != null)
        {
            material = loadingMaterial;
        }
        else
        {
            // Create a simple placeholder material
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = Color.gray;
        }
        
        // Store as original material
        originalMaterial = material;
        
        // Apply material to main mesh
        meshRenderer.material = material;
        
        // Use standard book aspect ratio (2:3)
        UpdateMeshForAspectRatio(2f/3f);
        
        // Update highlight mesh size
        UpdateHighlightMeshSize();
    }
    
    // Update highlight mesh to match current item dimensions
    private void UpdateHighlightMeshSize()
    {
        if (highlightMesh == null || meshFilter == null || meshFilter.mesh == null) return;
        
        MeshFilter highlightFilter = highlightMesh.GetComponent<MeshFilter>();
        if (highlightFilter == null || highlightFilter.mesh == null) return;
        
        // Get current item bounds
        Bounds bounds = meshFilter.mesh.bounds;
        float width = bounds.size.x;
        float height = bounds.size.z;
        float padding = 0.05f;
        
        // Create a slightly larger quad
        Mesh highlightQuad = new Mesh();
        
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-(width + padding)/2, 0.01f, -(height + padding)/2),
            new Vector3((width + padding)/2, 0.01f, -(height + padding)/2),
            new Vector3(-(width + padding)/2, 0.01f, (height + padding)/2),
            new Vector3((width + padding)/2, 0.01f, (height + padding)/2)
        };
        
        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        
        int[] triangles = new int[6]
        {
            0, 2, 1,
            2, 3, 1
        };
        
        highlightQuad.vertices = vertices;
        highlightQuad.uv = uv;
        highlightQuad.triangles = triangles;
        highlightQuad.RecalculateNormals();
        
        highlightFilter.mesh = highlightQuad;
    }
    
    // Update mesh based on aspect ratio
    private void UpdateMeshForAspectRatio(float aspectRatio)
    {
        float width, height;
        
        if (aspectRatio >= 1f) // Landscape or square
        {
            width = itemWidth;
            height = width / aspectRatio;
            
            if (height > itemHeight)
            {
                height = itemHeight;
                width = height * aspectRatio;
            }
        }
        else // Portrait
        {
            height = itemHeight;
            width = height * aspectRatio;
            
            if (width > itemWidth)
            {
                width = itemWidth;
                height = width / aspectRatio;
            }
        }
        
        CreateOrUpdateMesh(width, height);
    }
    
    // Create or update the mesh
    private void CreateOrUpdateMesh(float width, float height)
    {
        if (meshFilter == null) return;
        if (width <= 0 || height <= 0) return;
        
        // Create a simple quad mesh
        Mesh mesh = new Mesh();
        
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-width/2, 0, -height/2),
            new Vector3(width/2, 0, -height/2),
            new Vector3(-width/2, 0, height/2),
            new Vector3(width/2, 0, height/2)
        };
        
        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        
        int[] triangles = new int[6]
        {
            0, 2, 1,
            2, 3, 1
        };
        
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        meshFilter.mesh = mesh;
        
        // Update collider if present
        if (boxCollider != null)
        {
            boxCollider.size = new Vector3(width, 0.1f, height);
        }
    }
    
    // Set the model and update the view
    public void SetModel(Item newModel)
    {
        // Don't do anything if the model is the same
        if (model == newModel)
            return;
            
        // Unregister from old model
        if (model != null)
        {
            model.UnregisterView(this);
        }
        
        model = newModel;
        
        // Register with new model
        if (model != null)
        {
            model.RegisterView(this);
        }
        
        // Update view with the new model
        UpdateView();
        
        // Trigger event for external listeners
        onItemChanged.Invoke(model);
    }
    
    /// <summary>
    /// Set this item's highlight state. This is typically used for hover effects.
    /// </summary>
    public void SetHighlighted(bool highlighted)
    {
        if (isHighlighted == highlighted) return;
        
        isHighlighted = highlighted;
        
        // Don't apply hover highlight if item is selected
        if (isSelected) return;
        
        if (highlighted)
        {
            ApplyHighlight();
        }
        else
        {
            // Hide highlight mesh
            if (highlightMesh != null)
            {
                highlightMesh.SetActive(false);
            }
            
            // Restore original position
            transform.position = originalPosition;
        }
    }
    
    /// <summary>
    /// Set this item's selection state
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (isSelected == selected) return;
        
        isSelected = selected;
        
        if (selected)
        {
            ApplySelection();
        }
        else
        {
            // If still highlighted, apply highlight, otherwise restore original
            if (isHighlighted)
            {
                ApplyHighlight();
            }
            else
            {
                // Hide highlight mesh
                if (highlightMesh != null)
                {
                    highlightMesh.SetActive(false);
                }
                
                // Restore original position
                transform.position = originalPosition;
            }
        }
    }
    
    /// <summary>
    /// Apply highlight visual effect using the highlight mesh
    /// </summary>
    private void ApplyHighlight()
    {
        if (highlightMesh == null) return;
        
        // Show the highlight mesh
        highlightMesh.SetActive(true);
        
        // Apply hover material
        MeshRenderer highlightRenderer = highlightMesh.GetComponent<MeshRenderer>();
        if (highlightRenderer != null && highlightMaterial != null)
        {
            highlightRenderer.material = highlightMaterial;
        }
        
        // Slightly elevate the item
        transform.position = originalPosition + Vector3.up * highlightElevation * 0.5f;
    }
    
    /// <summary>
    /// Apply selection visual effect using the highlight mesh
    /// </summary>
    private void ApplySelection()
    {
        if (highlightMesh == null) return;
        
        // Show the highlight mesh
        highlightMesh.SetActive(true);
        
        // Apply selection material
        MeshRenderer highlightRenderer = highlightMesh.GetComponent<MeshRenderer>();
        if (highlightRenderer != null && selectionMaterial != null)
        {
            highlightRenderer.material = selectionMaterial;
        }
        
        // Elevate the item higher than hover
        transform.position = originalPosition + Vector3.up * highlightElevation;
    }
} 