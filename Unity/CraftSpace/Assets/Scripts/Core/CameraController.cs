using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CameraController : MonoBehaviour
{
    [Header("Camera Reference")]
    [SerializeField] private Camera _targetCamera; // Direct reference to camera
    [Tooltip("Set this to the camera you want to control")]
    
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
    
    [Header("Collection Focus")]
    [SerializeField] private float _collectionFocusDistance = 10f;
    [SerializeField] private float _collectionFocusHeight = 5f;
    [SerializeField] private float _focusTransitionTime = 1.0f;
    
    // Internal state
    private Vector3 _moveVelocity = Vector3.zero;
    private float _rotationVelocity = 0f;
    private float _zoomVelocity = 0f;
    private float _currentZoomLevel;
    
    private Transform _cameraTransform;
    private Coroutine _focusCoroutine;
    
    private void Awake()
    {
        // Use the directly referenced camera instead of Camera.main
        if (_targetCamera == null)
        {
            Debug.LogError("CameraController: No camera assigned! Please assign a camera in the inspector.");
            enabled = false; // Disable the script to prevent further errors
            return;
        }
        
        _cameraTransform = _targetCamera.transform;
        _currentZoomLevel = Vector3.Distance(_cameraTransform.position, transform.position);
    }
    
    private void Update()
    {
        HandleMovementInput();
        HandleRotationInput();
        HandleZoomInput();
    }
    
    /// <summary>
    /// Focus the camera on a collection
    /// </summary>
    public void FocusOnCollection(Collection collection, float transitionTime = -1f)
    {
        if (collection == null)
        {
            Debug.LogWarning("CameraController: Cannot focus on null collection");
            return;
        }
        
        // Stop any existing focus operation
        if (_focusCoroutine != null)
        {
            StopCoroutine(_focusCoroutine);
        }
        
        // Use default transition time if not specified
        if (transitionTime < 0)
        {
            transitionTime = _focusTransitionTime;
        }
        
        // Start focus coroutine
        _focusCoroutine = StartCoroutine(FocusOnCollectionCoroutine(collection, transitionTime));
    }
    
    private IEnumerator FocusOnCollectionCoroutine(Collection collection, float transitionTime)
    {
        // Get collection position - for now we'll use this transform's position
        // In a real application, you'd get the actual collection's position
        Vector3 targetPosition = new Vector3(0, _collectionFocusHeight, -_collectionFocusDistance);
        Quaternion targetRotation = Quaternion.Euler(30f, 0f, 0f);
        
        Vector3 startPosition = _cameraTransform.position;
        Quaternion startRotation = _cameraTransform.rotation;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < transitionTime)
        {
            float t = elapsedTime / transitionTime;
            
            // Smooth interpolation
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            // Lerp position and rotation
            _cameraTransform.position = Vector3.Lerp(startPosition, targetPosition, smoothT);
            _cameraTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, smoothT);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we end at exactly the target
        _cameraTransform.position = targetPosition;
        _cameraTransform.rotation = targetRotation;
        
        _focusCoroutine = null;
    }
    
    private void HandleMovementInput()
    {
        // Implementation of HandleMovementInput method
    }
    
    private void HandleRotationInput()
    {
        // Implementation of HandleRotationInput method
    }
    
    private void HandleZoomInput()
    {
        // Implementation of HandleZoomInput method
    }
} 