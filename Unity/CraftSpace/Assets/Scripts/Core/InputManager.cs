using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class InputManager : MonoBehaviour
{
    [Header("Camera References")]
    public Transform cameraRig;
    public Camera camera;

    [Header("Pan Settings")]
    public float panSpeed = 3f;
    public float minX = -12f;
    public float maxX = 12f;
    public float minZ = -12f;
    public float maxZ = 12f;
    public bool invertDrag = false;

    [Header("Zoom Settings")]
    public float zoomSpeed = 1f;
    public float minZoom = 0.1f;
    public float maxZoom = 100f;
    public float keyboardZoomSpeed = 5f;

    [Header("Physics Settings")]
    public float baseVelocityThreshold = 0.2f;
    public float velocitySmoothingFactor = 0.1f;
    public float frictionFactor = 0.999f;
    public float bounceFactor = 0.9f;

    [Header("UI References")]
    public ItemInfoPanel itemInfoPanel;
    public LayerMask itemLayer;
    public TextMeshProUGUI infoText;
    public CollectionDisplay collectionDisplay;

    [Header("UI Settings")]
    public float descriptionScale = 0.8f;
    
    [Header("Selection Settings")]
    public float maxSelectionDistance = 100f;
    
    // Selection events
    public UnityEvent<ItemView> OnItemSelected = new UnityEvent<ItemView>();
    public UnityEvent<ItemView> OnItemDeselected = new UnityEvent<ItemView>();
    public UnityEvent<ItemView> OnItemHoverStart = new UnityEvent<ItemView>();
    public UnityEvent<ItemView> OnItemHoverEnd = new UnityEvent<ItemView>();

    // State variables
    // Runtime state that changes during execution
    private bool isDragging = false;
    private Vector3 previousMousePosition;
    private Vector3 cameraVelocity = Vector3.zero;
    private Vector3 filteredVelocity = Vector3.zero;
    private bool physicsEnabled = true;
    private float lastDragTime;
    
    // Item tracking state - made public so it's visible in inspector
    public ItemView hoveredItem;
    public ItemView currentHighlightedItem;
    public ItemView selectedItem;

    private void Start()
    {
        if (camera == null) 
        {
            Debug.LogError("No camera assigned to InputManager in the Inspector!");
            enabled = false;
        }
    }

    private void Update()
    {
        // Only handle input capture in Update
        HandleInput();
        UpdateHoveredItem();
        
        // Handle selection clicks
        if (Input.GetMouseButtonDown(0) && hoveredItem != null && hoveredItem != selectedItem)
        {
            SelectItem(hoveredItem);
        }
        else if (Input.GetMouseButtonDown(0) && hoveredItem == null && selectedItem != null)
        {
            DeselectItem();
        }
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Only start dragging if not clicking on an item
            if (hoveredItem == null)
            {
                isDragging = true;
                previousMousePosition = Input.mousePosition;
                lastDragTime = Time.realtimeSinceStartup;
                physicsEnabled = false;
                cameraVelocity = Vector3.zero;
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            if (filteredVelocity.magnitude > GetScaledVelocityThreshold())
            {
                cameraVelocity = filteredVelocity;
                physicsEnabled = true;
            }
        }
        
        // Handle active dragging AND velocity calculation in ONE place
        if (isDragging)
        {
            float currentTime = Time.realtimeSinceStartup;
            float deltaTime = currentTime - lastDragTime;
            
            if (deltaTime > 0.001f)
            {
                // Get ACTUAL world positions
                Vector3 oldWorldPos = GetMouseWorldPosition(previousMousePosition);
                Vector3 newWorldPos = GetMouseWorldPosition(Input.mousePosition);
                Vector3 worldDelta = oldWorldPos - newWorldPos;  // Direction matches camera movement
                
                // Apply inversion if needed
                if (invertDrag)
                {
                    worldDelta = -worldDelta;
                }
                
                // Update tracking
                previousMousePosition = Input.mousePosition;
                lastDragTime = currentTime;
                
                // Calculate velocity from actual world movement
                Vector3 instantVelocity = worldDelta / deltaTime;
                filteredVelocity = Vector3.Lerp(filteredVelocity, instantVelocity, velocitySmoothingFactor);
                
                // Move camera
                Vector3 newPosition = cameraRig.position + worldDelta;
                newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
                newPosition.z = Mathf.Clamp(newPosition.z, minZ, maxZ);
                cameraRig.position = newPosition;
            }
        }

        HandleKeyboardPan();
        HandleZoom();
        
        if (physicsEnabled)
        {
            ApplyPhysics();
        }
    }

    // Physics simulation for momentum and bouncing
    private void ApplyPhysics()
    {
        // Stop if velocity is very small
        if (cameraVelocity.sqrMagnitude < 0.0001f)
        {
            cameraVelocity = Vector3.zero;
            return;
        }
        
        // Apply friction
        cameraVelocity *= frictionFactor;
        
        // Calculate new position
        Vector3 newPosition = cameraRig.position + cameraVelocity * Time.deltaTime;
        
        // Check boundaries and bounce on X axis
        if (newPosition.x < minX)
        {
            newPosition.x = minX;
            cameraVelocity.x = -cameraVelocity.x * bounceFactor;
        }
        else if (newPosition.x > maxX)
        {
            newPosition.x = maxX;
            cameraVelocity.x = -cameraVelocity.x * bounceFactor;
        }
        
        // Check boundaries and bounce on Z axis
        if (newPosition.z < minZ)
        {
            newPosition.z = minZ;
            cameraVelocity.z = -cameraVelocity.z * bounceFactor;
        }
        else if (newPosition.z > maxZ)
        {
            newPosition.z = maxZ;
            cameraVelocity.z = -cameraVelocity.z * bounceFactor;
        }
        
        // Apply new position
        cameraRig.position = newPosition;
    }

    private void HandleKeyboardPan()
    {
        Vector3 moveDirection = Vector3.zero;

        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            moveDirection.x -= 1;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            moveDirection.x += 1;
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            moveDirection.z += 1;
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            moveDirection.z -= 1;

        if (moveDirection != Vector3.zero)
        {
            // Disable physics when using keyboard controls
            physicsEnabled = false;
            cameraVelocity = Vector3.zero;
            
            moveDirection.Normalize();
            float moveSpeed = panSpeed * Time.deltaTime * camera.orthographicSize * 0.5f;
            Vector3 newPosition = cameraRig.position + moveDirection * moveSpeed;
            
            newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
            newPosition.z = Mathf.Clamp(newPosition.z, minZ, maxZ);
            
            cameraRig.position = newPosition;
        }
        else if (!isDragging && !physicsEnabled)
        {
            // Re-enable physics when keyboard controls are released
            physicsEnabled = true;
        }
    }

    private void HandleZoom()
    {
        // Mouse wheel zoom
        float scrollAmount = Input.GetAxis("Mouse ScrollWheel");
        if (scrollAmount != 0)
        {
            ApplyZoomAroundCursor(-scrollAmount * zoomSpeed * camera.orthographicSize);
        }

        // Keyboard zoom
        float keyboardZoomInput = 0f;
        if (Input.GetKey(KeyCode.Comma)) keyboardZoomInput += 1f;
        if (Input.GetKey(KeyCode.Period)) keyboardZoomInput -= 1f;
        
        if (keyboardZoomInput != 0)
        {
            float zoomAmount = keyboardZoomInput * keyboardZoomSpeed * Time.deltaTime;
            ApplyZoomAroundCursor(zoomAmount);
        }
    }

    // Helper method to apply zoom around cursor position
    private void ApplyZoomAroundCursor(float zoomAmount)
    {
        // Get world position at mouse before zoom
        Vector3 mouseWorldPosBefore = GetMouseWorldPosition();
        
        // Track velocity before zoom to scale it
        Vector3 velocityBeforeZoom = cameraVelocity;
        float oldSize = camera.orthographicSize;
        
        // Apply zoom
        float newSize = Mathf.Clamp(camera.orthographicSize + zoomAmount, minZoom, maxZoom);
        camera.orthographicSize = newSize;
        
        // Scale velocity to maintain world speed
        if (oldSize > 0 && physicsEnabled)
        {
            float velocityScale = newSize / oldSize;
            cameraVelocity = velocityBeforeZoom * velocityScale;
        }
        
        // Get world position at mouse after zoom
        Vector3 mouseWorldPosAfter = GetMouseWorldPosition();
        Vector3 adjustment = mouseWorldPosBefore - mouseWorldPosAfter;
        
        // Apply adjustment to camera position with strict boundary enforcement
        Vector3 newPosition = cameraRig.position + adjustment;
        newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
        newPosition.z = Mathf.Clamp(newPosition.z, minZ, maxZ);
        
        cameraRig.position = newPosition;
        
        // One final boundary check to be certain we're within limits
        EnforceBoundaries();
    }

    // Utility method to ensure camera is always within boundaries
    private void EnforceBoundaries()
    {
        Vector3 position = cameraRig.position;
        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.z = Mathf.Clamp(position.z, minZ, maxZ);
        cameraRig.position = position;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        
        if (plane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }
        
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = camera.nearClipPlane;
        return camera.ScreenToWorldPoint(mousePos);
    }

    private float GetScaledVelocityThreshold()
    {
        return baseVelocityThreshold * (camera.orthographicSize / minZoom);
    }

    // Add overload to handle arbitrary screen positions
    private Vector3 GetMouseWorldPosition(Vector3 screenPos)
    {
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = camera.ScreenPointToRay(screenPos);
        
        if (plane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }
        
        screenPos.z = camera.nearClipPlane;
        return camera.ScreenToWorldPoint(screenPos);
    }

    private void UpdateHoveredItem()
    {
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, maxSelectionDistance, itemLayer))
        {
            ItemView itemView = hit.collider.GetComponentInParent<ItemView>();
            if (itemView != null && itemView.Model != null)
            {
                // Only update the UI if we hover over a new item
                if (hoveredItem != itemView)
                {
                    // Fire hover end for previous item
                    if (hoveredItem != null)
                    {
                        OnItemHoverEnd?.Invoke(hoveredItem);
                    }
                    
                    hoveredItem = itemView;
                    
                    // Fire hover start
                    OnItemHoverStart?.Invoke(hoveredItem);
                }
                
                // Highlight the item if it's not already highlighted
                if (currentHighlightedItem != itemView)
                {
                    // Remove highlight from previous item if not selected
                    if (currentHighlightedItem != null && currentHighlightedItem != selectedItem)
                    {
                        currentHighlightedItem.SetHighlighted(false);
                    }
                    
                    // Highlight new item if not selected (selection takes priority)
                    currentHighlightedItem = itemView;
                    if (currentHighlightedItem != selectedItem)
                    {
                        currentHighlightedItem.SetHighlighted(true);
                    }
                }
            }
            else
            {
                ClearItemState();
            }
        }
        else
        {
            ClearItemState();
        }
    }
    
    private void ClearItemState()
    {
        if (hoveredItem != null)
        {
            OnItemHoverEnd?.Invoke(hoveredItem);
            hoveredItem = null;
        }
        
        if (currentHighlightedItem != null && currentHighlightedItem != selectedItem)
        {
            currentHighlightedItem.SetHighlighted(false);
            currentHighlightedItem = null;
        }
    }
    
    // Selection methods
    public void SelectItem(ItemView itemView)
    {
        // Don't reselect same item
        if (selectedItem == itemView)
            return;
            
        // Deselect current item if exists
        if (selectedItem != null)
        {
            DeselectItem();
        }
        
        // Set new selection
        selectedItem = itemView;
        selectedItem.SetSelected(true);
        OnItemSelected?.Invoke(selectedItem);
    }
    
    public void DeselectItem()
    {
        if (selectedItem != null)
        {
            selectedItem.SetSelected(false);
            
            // If this was also the hovered item, reapply hover effect
            if (hoveredItem == selectedItem || currentHighlightedItem == selectedItem)
            {
                selectedItem.SetHighlighted(true);
            }
            
            OnItemDeselected?.Invoke(selectedItem);
            selectedItem = null;
        }
    }
    
    // Get currently selected item
    public ItemView GetSelectedItem()
    {
        return selectedItem;
    }
} 