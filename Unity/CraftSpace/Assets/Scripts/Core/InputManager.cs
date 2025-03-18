using UnityEngine;

namespace CraftSpace.Core
{
    public class InputManager : MonoBehaviour
    {
        [Header("Camera References")]
        [SerializeField] private Transform _cameraRig;
        [SerializeField] private Camera _camera;

        [Header("Pan Settings")]
        [SerializeField] private float _panSpeed = 10f;
        [SerializeField] private float _minX = -20f;
        [SerializeField] private float _maxX = 20f;
        [SerializeField] private float _minZ = -20f;
        [SerializeField] private float _maxZ = 20f;
        [SerializeField] private bool _invertDrag = false;

        [Header("Zoom Settings")]
        [SerializeField] private float _zoomSpeed = 1f;
        [SerializeField] private float _minZoom = 2f;
        [SerializeField] private float _maxZoom = 20f;
        [SerializeField] private float _keyboardZoomSpeed = 5f;

        [Header("Physics Settings")]
        [SerializeField] private float _baseVelocityThreshold = 0.1f; // Renamed to indicate this is the base value
        [SerializeField] private float _velocitySmoothingFactor = 0.8f;
        [SerializeField] private float _frictionFactor = 0.98f;
        [SerializeField] private float _bounceFactor = 0.8f;

        // State variables
        private bool _isDragging = false;
        private Vector3 _previousMousePosition;
        private Vector3 _cameraVelocity = Vector3.zero;
        private Vector3 _filteredVelocity = Vector3.zero;
        private bool _physicsEnabled = true;
        private float _lastDragTime;

        private void Start()
        {
            if (_camera == null) 
                _camera = Camera.main;

            if (_camera == null)
            {
                Debug.LogError("No camera assigned to InputManager and no main camera found!");
                enabled = false;
            }
        }

        private void Update()
        {
            // Only handle input capture in Update
            HandleInput();
        }

        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _isDragging = true;
                _previousMousePosition = Input.mousePosition;
                _lastDragTime = Time.realtimeSinceStartup;
                _physicsEnabled = false;
                _cameraVelocity = Vector3.zero;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _isDragging = false;
                if (_filteredVelocity.magnitude > GetScaledVelocityThreshold())
                {
                    _cameraVelocity = _filteredVelocity;
                    _physicsEnabled = true;
                }
            }
            
            // Handle active dragging AND velocity calculation in ONE place
            if (_isDragging)
            {
                float currentTime = Time.realtimeSinceStartup;
                float deltaTime = currentTime - _lastDragTime;
                
                if (deltaTime > 0.001f)
                {
                    // Get ACTUAL world positions
                    Vector3 oldWorldPos = GetMouseWorldPosition(_previousMousePosition);
                    Vector3 newWorldPos = GetMouseWorldPosition(Input.mousePosition);
                    Vector3 worldDelta = oldWorldPos - newWorldPos;  // Direction matches camera movement
                    
                    // Update tracking
                    _previousMousePosition = Input.mousePosition;
                    _lastDragTime = currentTime;
                    
                    // Calculate velocity from actual world movement
                    Vector3 instantVelocity = worldDelta / deltaTime;
                    _filteredVelocity = Vector3.Lerp(_filteredVelocity, instantVelocity, _velocitySmoothingFactor);
                    
                    // Move camera
                    Vector3 newPosition = _cameraRig.position + worldDelta;
                    newPosition.x = Mathf.Clamp(newPosition.x, _minX, _maxX);
                    newPosition.z = Mathf.Clamp(newPosition.z, _minZ, _maxZ);
                    _cameraRig.position = newPosition;
                }
            }

            HandleKeyboardPan();
            HandleZoom();
            
            if (_physicsEnabled)
            {
                ApplyPhysics();
            }
        }

        // Physics simulation for momentum and bouncing
        private void ApplyPhysics()
        {
            // Stop if velocity is very small
            if (_cameraVelocity.sqrMagnitude < 0.0001f)
            {
                _cameraVelocity = Vector3.zero;
                return;
            }
            
            // Apply friction
            _cameraVelocity *= _frictionFactor;
            
            // Calculate new position
            Vector3 newPosition = _cameraRig.position + _cameraVelocity * Time.deltaTime;
            
            // Check boundaries and bounce on X axis
            if (newPosition.x < _minX)
            {
                newPosition.x = _minX;
                _cameraVelocity.x = -_cameraVelocity.x * _bounceFactor;
            }
            else if (newPosition.x > _maxX)
            {
                newPosition.x = _maxX;
                _cameraVelocity.x = -_cameraVelocity.x * _bounceFactor;
            }
            
            // Check boundaries and bounce on Z axis
            if (newPosition.z < _minZ)
            {
                newPosition.z = _minZ;
                _cameraVelocity.z = -_cameraVelocity.z * _bounceFactor;
            }
            else if (newPosition.z > _maxZ)
            {
                newPosition.z = _maxZ;
                _cameraVelocity.z = -_cameraVelocity.z * _bounceFactor;
            }
            
            // Apply new position
            _cameraRig.position = newPosition;
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
                _physicsEnabled = false;
                _cameraVelocity = Vector3.zero;
                
                moveDirection.Normalize();
                float moveSpeed = _panSpeed * Time.deltaTime * _camera.orthographicSize * 0.5f;
                Vector3 newPosition = _cameraRig.position + moveDirection * moveSpeed;
                
                newPosition.x = Mathf.Clamp(newPosition.x, _minX, _maxX);
                newPosition.z = Mathf.Clamp(newPosition.z, _minZ, _maxZ);
                
                _cameraRig.position = newPosition;
            }
            else if (!_isDragging && !_physicsEnabled)
            {
                // Re-enable physics when keyboard controls are released
                _physicsEnabled = true;
            }
        }

        private void HandleZoom()
        {
            // Mouse wheel zoom
            float scrollAmount = Input.GetAxis("Mouse ScrollWheel");
            if (scrollAmount != 0)
            {
                ApplyZoomAroundCursor(-scrollAmount * _zoomSpeed * _camera.orthographicSize);
            }

            // Keyboard zoom
            float keyboardZoomInput = 0f;
            if (Input.GetKey(KeyCode.Comma)) keyboardZoomInput += 1f;
            if (Input.GetKey(KeyCode.Period)) keyboardZoomInput -= 1f;
            
            if (keyboardZoomInput != 0)
            {
                float zoomAmount = keyboardZoomInput * _keyboardZoomSpeed * Time.deltaTime;
                ApplyZoomAroundCursor(zoomAmount);
            }
        }

        // Helper method to apply zoom around cursor position
        private void ApplyZoomAroundCursor(float zoomAmount)
        {
            // Get world position at mouse before zoom
            Vector3 mouseWorldPosBefore = GetMouseWorldPosition();
            
            // Track velocity before zoom to scale it
            Vector3 velocityBeforeZoom = _cameraVelocity;
            float oldSize = _camera.orthographicSize;
            
            // Apply zoom
            float newSize = Mathf.Clamp(_camera.orthographicSize + zoomAmount, _minZoom, _maxZoom);
            _camera.orthographicSize = newSize;
            
            // Scale velocity to maintain world speed
            if (oldSize > 0 && _physicsEnabled)
            {
                float velocityScale = newSize / oldSize;
                _cameraVelocity = velocityBeforeZoom * velocityScale;
            }
            
            // Get world position at mouse after zoom
            Vector3 mouseWorldPosAfter = GetMouseWorldPosition();
            Vector3 adjustment = mouseWorldPosBefore - mouseWorldPosAfter;
            
            // Apply adjustment to camera position with strict boundary enforcement
            Vector3 newPosition = _cameraRig.position + adjustment;
            newPosition.x = Mathf.Clamp(newPosition.x, _minX, _maxX);
            newPosition.z = Mathf.Clamp(newPosition.z, _minZ, _maxZ);
            
            _cameraRig.position = newPosition;
            
            // One final boundary check to be certain we're within limits
            EnforceBoundaries();
        }

        // Utility method to ensure camera is always within boundaries
        private void EnforceBoundaries()
        {
            Vector3 position = _cameraRig.position;
            position.x = Mathf.Clamp(position.x, _minX, _maxX);
            position.z = Mathf.Clamp(position.z, _minZ, _maxZ);
            _cameraRig.position = position;
        }

        private Vector3 GetMouseWorldPosition()
        {
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            
            if (plane.Raycast(ray, out float enter))
            {
                return ray.GetPoint(enter);
            }
            
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = _camera.nearClipPlane;
            return _camera.ScreenToWorldPoint(mousePos);
        }

        private float GetScaledVelocityThreshold()
        {
            return _baseVelocityThreshold * (_camera.orthographicSize / _minZoom);
        }

        // Add overload to handle arbitrary screen positions
        private Vector3 GetMouseWorldPosition(Vector3 screenPos)
        {
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = _camera.ScreenPointToRay(screenPos);
            
            if (plane.Raycast(ray, out float enter))
            {
                return ray.GetPoint(enter);
            }
            
            screenPos.z = _camera.nearClipPlane;
            return _camera.ScreenToWorldPoint(screenPos);
        }
    }
} 