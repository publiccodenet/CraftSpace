using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _fastMoveSpeed = 15f;
    [SerializeField] private float _rotationSpeed = 60f;
    [SerializeField] private float _zoomSpeed = 10f;
    [SerializeField] private float _minZoom = 2f;
    [SerializeField] private float _maxZoom = 20f;
    
    [Header("Smoothing")]
    [SerializeField] private float _movementSmoothTime = 0.2f;
    [SerializeField] private float _rotationSmoothTime = 0.1f;
    [SerializeField] private float _zoomSmoothTime = 0.1f;
    
    // Internal state
    private Vector3 _moveVelocity = Vector3.zero;
    private float _rotationVelocity = 0f;
    private float _zoomVelocity = 0f;
    private float _currentZoomLevel;
    
    private Transform _cameraTransform;
    
    private void Awake()
    {
        _cameraTransform = Camera.main.transform;
        _currentZoomLevel = Vector3.Distance(_cameraTransform.position, transform.position);
    }
    
    private void Update()
    {
        HandleMovementInput();
        HandleRotationInput();
        HandleZoomInput();
    }
    
    private void HandleMovementInput()
    {
        // Get input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        // Check for movement
        if (Mathf.Abs(horizontal) > 0.01f || Mathf.Abs(vertical) > 0.01f)
        {
            // Calculate movement direction
            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();
            
            Vector3 right = transform.right;
            right.y = 0f;
            right.Normalize();
            
            Vector3 moveDirection = (forward * vertical + right * horizontal).normalized;
            
            // Apply speed
            float speed = Input.GetKey(KeyCode.LeftShift) ? _fastMoveSpeed : _moveSpeed;
            Vector3 targetPosition = transform.position + moveDirection * speed * Time.deltaTime;
            
            // Apply smoothing
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, 
                ref _moveVelocity, _movementSmoothTime);
        }
    }
    
    private void HandleRotationInput()
    {
        // Rotate when right mouse button is held
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            
            if (Mathf.Abs(mouseX) > 0.01f)
            {
                // Calculate target rotation
                float rotationAmount = mouseX * _rotationSpeed * Time.deltaTime;
                float targetRotation = transform.eulerAngles.y + rotationAmount;
                
                // Apply smoothing
                float newRotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation,
                    ref _rotationVelocity, _rotationSmoothTime);
                
                transform.rotation = Quaternion.Euler(0f, newRotation, 0f);
            }
        }
    }
    
    private void HandleZoomInput()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            // Calculate target zoom
            float targetZoom = _currentZoomLevel - scrollInput * _zoomSpeed;
            targetZoom = Mathf.Clamp(targetZoom, _minZoom, _maxZoom);
            
            // Apply smoothing
            _currentZoomLevel = Mathf.SmoothDamp(_currentZoomLevel, targetZoom,
                ref _zoomVelocity, _zoomSmoothTime);
            
            // Apply zoom to camera position
            Vector3 zoomDirection = _cameraTransform.position - transform.position;
            zoomDirection.Normalize();
            _cameraTransform.position = transform.position + zoomDirection * _currentZoomLevel;
        }
    }
    
    // Jump to a specific collection view
    public void FocusOnCollection(CollectionGridLayout collectionLayout)
    {
        if (collectionLayout == null)
            return;
            
        // Calculate center position
        Vector3 targetPosition = collectionLayout.transform.position;
        
        // Calculate appropriate distance based on grid size
        Vector2 gridSize = collectionLayout.GetGridSize();
        float maxDimension = Mathf.Max(gridSize.x, gridSize.y);
        
        // Set appropriate zoom level
        _currentZoomLevel = maxDimension * 0.75f;
        _currentZoomLevel = Mathf.Clamp(_currentZoomLevel, _minZoom, _maxZoom);
        
        // Position camera
        transform.position = targetPosition;
        
        // Update camera position
        Vector3 zoomDirection = _cameraTransform.position - transform.position;
        zoomDirection.Normalize();
        _cameraTransform.position = transform.position + zoomDirection * _currentZoomLevel;
    }
} 