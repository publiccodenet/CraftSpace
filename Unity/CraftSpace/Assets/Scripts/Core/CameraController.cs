using UnityEngine;
using System.Collections;

namespace CraftSpace.Core  // Add this namespace to match other core scripts
{
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
        
        // Internal state
        private Vector3 _moveVelocity = Vector3.zero;
        private float _rotationVelocity = 0f;
        private float _zoomVelocity = 0f;
        private float _currentZoomLevel;
        
        private Transform _cameraTransform;
        
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
} 