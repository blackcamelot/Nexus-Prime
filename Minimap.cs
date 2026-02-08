using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace NexusPrime.UI
{
    public class Minimap : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Minimap Components")]
        public RawImage minimapImage;
        public RectTransform minimapRect;
        public RectTransform viewportIndicator;
        public RectTransform playerIndicator;
        public GameObject markerPrefab;
        public Transform markersContainer;
        
        [Header("Minimap Settings")]
        public float minimapSize = 200f;
        public float worldSize = 200f;
        public float updateInterval = 0.1f;
        public bool rotateWithCamera = false;
        public bool showViewport = true;
        public bool showPlayer = true;
        
        [Header("Zoom Settings")]
        public float minZoom = 0.5f;
        public float maxZoom = 2f;
        public float zoomSpeed = 0.1f;
        public float currentZoom = 1f;
        
        [Header("Colors")]
        public Color backgroundColor = new Color(0.1f, 0.1f, 0.2f, 0.8f);
        public Color terrainColor = new Color(0.3f, 0.6f, 0.3f, 1f);
        public Color viewportColor = new Color(1f, 1f, 1f, 0.3f);
        public Color playerColor = Color.cyan;
        public Color enemyColor = Color.red;
        public Color resourceColor = Color.yellow;
        public Color objectiveColor = Color.magenta;
        
        // References
        private Camera mainCamera;
        private Camera minimapCamera;
        private RenderTexture minimapRenderTexture;
        private Texture2D staticMinimapTexture;
        
        // State
        private Dictionary<Vector3, MinimapMarker> markers = new Dictionary<Vector3, MinimapMarker>();
        private float updateTimer;
        private bool isDragging = false;
        private Vector2 dragStartPos;
        private bool useStaticTexture = false;
        
        // Events
        public delegate void MinimapClickHandler(Vector3 worldPosition);
        public event MinimapClickHandler OnMinimapClicked;
        
        void Start()
        {
            Initialize();
        }
        
        void Initialize()
        {
            // Get main camera
            mainCamera = Camera.main;
            
            // Create minimap camera if not using static texture
            if (!useStaticTexture)
            {
                CreateMinimapCamera();
            }
            
            // Setup minimap image
            if (minimapImage != null)
            {
                // Set background color
                minimapImage.color = backgroundColor;
                
                if (useStaticTexture && staticMinimapTexture != null)
                {
                    minimapImage.texture = staticMinimapTexture;
                }
            }
            
            // Setup minimap rect
            if (minimapRect == null)
            {
                minimapRect = GetComponent<RectTransform>();
            }
            
            // Setup viewport indicator
            if (viewportIndicator != null)
            {
                viewportIndicator.gameObject.SetActive(showViewport);
                Image viewportImage = viewportIndicator.GetComponent<Image>();
                if (viewportImage != null)
                {
                    viewportImage.color = viewportColor;
                }
            }
            
            // Setup player indicator
            if (playerIndicator != null)
            {
                playerIndicator.gameObject.SetActive(showPlayer);
                Image playerImage = playerIndicator.GetComponent<Image>();
                if (playerImage != null)
                {
                    playerImage.color = playerColor;
                }
            }
            
            // Create markers container if needed
            if (markersContainer == null)
            {
                GameObject container = new GameObject("MinimapMarkers");
                container.transform.SetParent(transform);
                markersContainer = container.transform;
            }
            
            Debug.Log("Minimap Initialized");
        }
        
        void CreateMinimapCamera()
        {
            // Create camera GameObject
            GameObject cameraObj = new GameObject("MinimapCamera");
            cameraObj.transform.position = new Vector3(0, 50, 0); // High above the map
            cameraObj.transform.rotation = Quaternion.Euler(90, 0, 0); // Look straight down
            
            // Add camera component
            minimapCamera = cameraObj.AddComponent<Camera>();
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = worldSize / 2f;
            minimapCamera.cullingMask = LayerMask.GetMask("Terrain", "Buildings", "Units");
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor = backgroundColor;
            minimapCamera.enabled = false; // We'll render manually
            
            // Create render texture
            int textureSize = Mathf.RoundToInt(minimapSize * currentZoom);
            minimapRenderTexture = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32);
            minimapRenderTexture.Create();
            
            // Assign to camera
            minimapCamera.targetTexture = minimapRenderTexture;
            
            // Assign to minimap image
            if (minimapImage != null)
            {
                minimapImage.texture = minimapRenderTexture;
            }
        }
        
        void Update()
        {
            updateTimer += Time.deltaTime;
            
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0;
                UpdateMinimap();
            }
            
            UpdateViewportIndicator();
            UpdatePlayerIndicator();
            UpdateMarkers();
            
            // Handle zoom input
            HandleZoomInput();
        }
        
        void UpdateMinimap()
        {
            if (useStaticTexture) return;
            
            // Render minimap
            if (minimapCamera != null)
            {
                // Update camera position to follow player or center of map
                Vector3 cameraPosition = GetMinimapCameraPosition();
                minimapCamera.transform.position = cameraPosition;
                
                // Update orthographic size based on zoom
                minimapCamera.orthographicSize = (worldSize / 2f) / currentZoom;
                
                // Render
                minimapCamera.Render();
            }
        }
        
        Vector3 GetMinimapCameraPosition()
        {
            // Center on player if available, otherwise use world center
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Vector3 playerPos = player.transform.position;
                return new Vector3(playerPos.x, 50, playerPos.z);
            }
            
            // Or center on all units average position
            List<SelectableUnit> playerUnits = GetPlayerUnits();
            if (playerUnits.Count > 0)
            {
                Vector3 averagePos = Vector3.zero;
                foreach (SelectableUnit unit in playerUnits)
                {
                    averagePos += unit.transform.position;
                }
                averagePos /= playerUnits.Count;
                return new Vector3(averagePos.x, 50, averagePos.z);
            }
            
            return new Vector3(0, 50, 0);
        }
        
        void UpdateViewportIndicator()
        {
            if (viewportIndicator == null || !showViewport) return;
            if (mainCamera == null) return;
            
            // Calculate viewport bounds in world space
            Camera cam = mainCamera;
            float camHeight = 2f * cam.orthographicSize;
            float camWidth = camHeight * cam.aspect;
            
            Vector3 camCenter = cam.transform.position;
            Vector3 camForward = cam.transform.forward;
            Vector3 camRight = cam.transform.right;
            
            // Calculate corners of viewport
            Vector3[] corners = new Vector3[4];
            corners[0] = camCenter + (camForward * cam.nearClipPlane) + (camRight * -camWidth / 2f); // Bottom-left
            corners[1] = camCenter + (camForward * cam.nearClipPlane) + (camRight * camWidth / 2f); // Bottom-right
            corners[2] = camCenter + (camForward * cam.farClipPlane) + (camRight * -camWidth / 2f); // Top-left
            corners[3] = camCenter + (camForward * cam.farClipPlane) + (camRight * camWidth / 2f); // Top-right
            
            // Convert to minimap coordinates
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            
            foreach (Vector3 corner in corners)
            {
                Vector2 minimapPos = WorldToMinimapPosition(corner);
                min.x = Mathf.Min(min.x, minimapPos.x);
                min.y = Mathf.Min(min.y, minimapPos.y);
                max.x = Mathf.Max(max.x, minimapPos.x);
                max.y = Mathf.Max(max.y, minimapPos.y);
            }
            
            // Update viewport indicator
            Vector2 size = max - min;
            Vector2 center = (min + max) / 2f;
            
            viewportIndicator.anchoredPosition = center;
            viewportIndicator.sizeDelta = size;
            
            // Rotate with camera if enabled
            if (rotateWithCamera)
            {
                float angle = cam.transform.eulerAngles.y;
                viewportIndicator.rotation = Quaternion.Euler(0, 0, -angle);
            }
            else
            {
                viewportIndicator.rotation = Quaternion.identity;
            }
        }
        
        void UpdatePlayerIndicator()
        {
            if (playerIndicator == null || !showPlayer) return;
            
            // Find player unit
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Vector2 minimapPos = WorldToMinimapPosition(player.transform.position);
                playerIndicator.anchoredPosition = minimapPos;
                
                // Rotate with player if enabled
                if (rotateWithCamera)
                {
                    float angle = player.transform.eulerAngles.y;
                    playerIndicator.rotation = Quaternion.Euler(0, 0, -angle);
                }
                else
                {
                    playerIndicator.rotation = Quaternion.identity;
                }
            }
        }
        
        void UpdateMarkers()
        {
            // Update existing markers
            foreach (var marker in markers.Values)
            {
                if (marker != null && marker.gameObject != null)
                {
                    UpdateMarkerPosition(marker);
                }
            }
        }
        
        void HandleZoomInput()
        {
            // Zoom with scroll wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0 && IsPointerOverMinimap())
            {
                currentZoom = Mathf.Clamp(currentZoom + scroll * zoomSpeed, minZoom, maxZoom);
                
                // Update render texture size
                if (minimapRenderTexture != null)
                {
                    int newSize = Mathf.RoundToInt(minimapSize * currentZoom);
                    if (minimapRenderTexture.width != newSize)
                    {
                        minimapRenderTexture.Release();
                        minimapRenderTexture.width = newSize;
                        minimapRenderTexture.height = newSize;
                        minimapRenderTexture.Create();
                    }
                }
                
                // Update camera size
                if (minimapCamera != null)
                {
                    minimapCamera.orthographicSize = (worldSize / 2f) / currentZoom;
                }
            }
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsPointerOverMinimap()) return;
            
            isDragging = true;
            dragStartPos = eventData.position;
            
            // If not dragging viewport, handle as click
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                Vector2 localPos;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    minimapRect, eventData.position, eventData.pressEventCamera, out localPos))
                {
                    Vector3 worldPos = MinimapToWorldPosition(localPos);
                    OnMinimapClicked?.Invoke(worldPos);
                    
                    // Move camera to clicked position
                    MoveCameraToPosition(worldPos);
                }
            }
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;
            
            // Calculate drag delta
            Vector2 dragDelta = eventData.position - dragStartPos;
            
            if (eventData.button == PointerEventData.InputButton.Right || 
                eventData.button == PointerEventData.InputButton.Middle)
            {
                // Drag to move camera
                Vector2 localPos;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    minimapRect, eventData.position, eventData.pressEventCamera, out localPos))
                {
                    Vector3 worldPos = MinimapToWorldPosition(localPos);
                    MoveCameraToPosition(worldPos);
                }
            }
            
            dragStartPos = eventData.position;
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            isDragging = false;
        }
        
        Vector2 WorldToMinimapPosition(Vector3 worldPosition)
        {
            if (minimapRect == null) return Vector2.zero;
            
            // Convert world position to normalized minimap coordinates
            Vector3 cameraPos = minimapCamera != null ? minimapCamera.transform.position : Vector3.zero;
            float cameraSize = minimapCamera != null ? minimapCamera.orthographicSize : worldSize / 2f;
            
            // Calculate normalized position (-1 to 1)
            float normX = (worldPosition.x - cameraPos.x) / cameraSize;
            float normZ = (worldPosition.z - cameraPos.z) / cameraSize;
            
            // Convert to minimap pixel coordinates
            float pixelX = normX * (minimapRect.rect.width / 2f);
            float pixelY = normZ * (minimapRect.rect.height / 2f);
            
            return new Vector2(pixelX, pixelY);
        }
        
        Vector3 MinimapToWorldPosition(Vector2 minimapPosition)
        {
            if (minimapCamera == null) return Vector3.zero;
            
            // Convert from minimap pixel coordinates to normalized
            float normX = minimapPosition.x / (minimapRect.rect.width / 2f);
            float normY = minimapPosition.y / (minimapRect.rect.height / 2f);
            
            // Convert to world position
            Vector3 cameraPos = minimapCamera.transform.position;
            float cameraSize = minimapCamera.orthographicSize;
            
            float worldX = cameraPos.x + (normX * cameraSize);
            float worldZ = cameraPos.z + (normY * cameraSize);
            
            // Raycast to find ground height
            Ray ray = new Ray(new Vector3(worldX, 100, worldZ), Vector3.down);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 200f, LayerMask.GetMask("Terrain")))
            {
                return hit.point;
            }
            
            return new Vector3(worldX, 0, worldZ);
        }
        
        void MoveCameraToPosition(Vector3 worldPosition)
        {
            if (mainCamera == null) return;
            
            // Move camera to position while maintaining height
            Vector3 camPos = mainCamera.transform.position;
            camPos.x = worldPosition.x;
            camPos.z = worldPosition.z;
            mainCamera.transform.position = camPos;
        }
        
        public void AddMarker(Vector3 worldPosition, MinimapMarkerType type, Color color, string label = "")
        {
            // Check if marker already exists at this position
            Vector3 key = new Vector3(
                Mathf.Round(worldPosition.x * 10) / 10,
                Mathf.Round(worldPosition.y * 10) / 10,
                Mathf.Round(worldPosition.z * 10) / 10
            );
            
            if (markers.ContainsKey(key))
            {
                // Update existing marker
                UpdateMarker(markers[key], type, color, label);
                return;
            }
            
            // Create new marker
            if (markerPrefab == null) return;
            
            GameObject markerObj = Instantiate(markerPrefab, markersContainer);
            markerObj.name = $"MinimapMarker_{type}_{key}";
            
            MinimapMarker marker = markerObj.GetComponent<MinimapMarker>();
            if (marker == null)
            {
                marker = markerObj.AddComponent<MinimapMarker>();
            }
            
            marker.Initialize(type, color, label);
            markers[key] = marker;
            
            UpdateMarkerPosition(marker);
        }
        
        public void RemoveMarker(Vector3 worldPosition)
        {
            Vector3 key = new Vector3(
                Mathf.Round(worldPosition.x * 10) / 10,
                Mathf.Round(worldPosition.y * 10) / 10,
                Mathf.Round(worldPosition.z * 10) / 10
            );
            
            if (markers.ContainsKey(key))
            {
                Destroy(markers[key].gameObject);
                markers.Remove(key);
            }
        }
        
        public void ClearMarkers(MinimapMarkerType type = MinimapMarkerType.None)
        {
            List<Vector3> toRemove = new List<Vector3>();
            
            foreach (var kvp in markers)
            {
                if (type == MinimapMarkerType.None || kvp.Value.markerType == type)
                {
                    Destroy(kvp.Value.gameObject);
                    toRemove.Add(kvp.Key);
                }
            }
            
            foreach (Vector3 key in toRemove)
            {
                markers.Remove(key);
            }
        }
        
        void UpdateMarker(MinimapMarker marker, MinimapMarkerType type, Color color, string label)
        {
            marker.SetType(type);
            marker.SetColor(color);
            marker.SetLabel(label);
        }
        
        void UpdateMarkerPosition(MinimapMarker marker)
        {
            // Marker positions are set when added
            // This would update dynamic markers that move
        }
        
        public void UpdateMinimapTexture(Texture2D texture)
        {
            useStaticTexture = true;
            staticMinimapTexture = texture;
            
            if (minimapImage != null)
            {
                minimapImage.texture = texture;
            }
            
            // Disable minimap camera if using static texture
            if (minimapCamera != null)
            {
                minimapCamera.enabled = false;
            }
        }
        
        public void ToggleViewport(bool show)
        {
            showViewport = show;
            if (viewportIndicator != null)
            {
                viewportIndicator.gameObject.SetActive(show);
            }
        }
        
        public void TogglePlayerIndicator(bool show)
        {
            showPlayer = show;
            if (playerIndicator != null)
            {
                playerIndicator.gameObject.SetActive(show);
            }
        }
        
        public void SetZoom(float zoomLevel)
        {
            currentZoom = Mathf.Clamp(zoomLevel, minZoom, maxZoom);
        }
        
        public void ResetZoom()
        {
            currentZoom = 1f;
        }
        
        bool IsPointerOverMinimap()
        {
            if (EventSystem.current == null) return false;
            
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;
            
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            
            foreach (RaycastResult result in results)
            {
                if (result.gameObject == gameObject)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        List<SelectableUnit> GetPlayerUnits()
        {
            List<SelectableUnit> playerUnits = new List<SelectableUnit>();
            List<SelectableUnit> allUnits = GameManager.Instance.GetAllUnits();
            
            foreach (SelectableUnit unit in allUnits)
            {
                CombatUnit combat = unit.GetComponent<CombatUnit>();
                if (combat != null && combat.faction == "player")
                {
                    playerUnits.Add(unit);
                }
            }
            
            return playerUnits;
        }
        
        public void Flash()
        {
            // Flash effect to draw attention to minimap
            StartCoroutine(FlashCoroutine());
        }
        
        System.Collections.IEnumerator FlashCoroutine()
        {
            Image background = GetComponent<Image>();
            Color originalColor = backgroundColor;
            Color flashColor = Color.yellow;
            
            for (int i = 0; i < 3; i++)
            {
                background.color = flashColor;
                yield return new WaitForSeconds(0.1f);
                background.color = originalColor;
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        void OnDestroy()
        {
            // Clean up render texture
            if (minimapRenderTexture != null)
            {
                minimapRenderTexture.Release();
                Destroy(minimapRenderTexture);
            }
            
            // Destroy minimap camera
            if (minimapCamera != null)
            {
                Destroy(minimapCamera.gameObject);
            }
            
            // Clear markers
            ClearMarkers();
        }
    }
    
    public class MinimapMarker : MonoBehaviour
    {
        [Header("Marker Components")]
        public Image markerImage;
        public TextMeshProUGUI labelText;
        public GameObject pulseEffect;
        
        [Header("Marker Settings")]
        public MinimapMarkerType markerType;
        public Color markerColor = Color.white;
        public string markerLabel = "";
        public float pulseInterval = 2f;
        
        // State
        private bool isPulsing = false;
        private Coroutine pulseCoroutine;
        
        public void Initialize(MinimapMarkerType type, Color color, string label = "")
        {
            markerType = type;
            markerColor = color;
            markerLabel = label;
            
            // Setup appearance based on type
            SetupAppearance();
            
            // Set label
            if (labelText != null)
            {
                labelText.text = label;
                labelText.gameObject.SetActive(!string.IsNullOrEmpty(label));
            }
            
            // Set color
            if (markerImage != null)
            {
                markerImage.color = color;
            }
        }
        
        void SetupAppearance()
        {
            // Different appearance for different marker types
            if (markerImage != null)
            {
                switch (markerType)
                {
                    case MinimapMarkerType.Player:
                        // Triangle for player
                        break;
                    case MinimapMarkerType.Enemy:
                        // Skull or crosshair for enemy
                        break;
                    case MinimapMarkerType.Resource:
                        // Diamond for resources
                        break;
                    case MinimapMarkerType.Objective:
                        // Star for objectives
                        break;
                    case MinimapMarkerType.Waypoint:
                        // Flag for waypoints
                        break;
                    case MinimapMarkerType.Building:
                        // Square for buildings
                        break;
                }
            }
        }
        
        public void SetType(MinimapMarkerType type)
        {
            markerType = type;
            SetupAppearance();
        }
        
        public void SetColor(Color color)
        {
            markerColor = color;
            if (markerImage != null)
            {
                markerImage.color = color;
            }
        }
        
        public void SetLabel(string label)
        {
            markerLabel = label;
            if (labelText != null)
            {
                labelText.text = label;
                labelText.gameObject.SetActive(!string.IsNullOrEmpty(label));
            }
        }
        
        public void StartPulse()
        {
            if (isPulsing) return;
            
            isPulsing = true;
            pulseCoroutine = StartCoroutine(PulseCoroutine());
        }
        
        public void StopPulse()
        {
            if (!isPulsing) return;
            
            isPulsing = false;
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }
            
            // Reset scale
            transform.localScale = Vector3.one;
        }
        
        System.Collections.IEnumerator PulseCoroutine()
        {
            while (isPulsing)
            {
                // Scale up
                float timer = 0;
                while (timer < pulseInterval / 2)
                {
                    timer += Time.deltaTime;
                    float t = timer / (pulseInterval / 2);
                    transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.5f, t);
                    yield return null;
                }
                
                // Scale down
                timer = 0;
                while (timer < pulseInterval / 2)
                {
                    timer += Time.deltaTime;
                    float t = timer / (pulseInterval / 2);
                    transform.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.one, t);
                    yield return null;
                }
            }
        }
        
        public void Show()
        {
            gameObject.SetActive(true);
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
        }
        
        void OnDestroy()
        {
            StopPulse();
        }
    }
}