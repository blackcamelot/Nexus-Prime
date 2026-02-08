using UnityEngine;

namespace NexusPrime.Building
{
    public class BuildingGhost : MonoBehaviour
    {
        [Header("Visual Settings")]
        public Material validMaterial;
        public Material invalidMaterial;
        public Color validColor = new Color(0, 1, 0, 0.5f);
        public Color invalidColor = new Color(1, 0, 0, 0.5f);
        
        [Header("Components")]
        public Building buildingComponent;
        
        private Renderer[] renderers;
        private bool isValid = true;
        private BuildingDefinition definition;
        
        void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
            buildingComponent = GetComponent<Building>();
            
            if (buildingComponent != null)
            {
                definition = buildingComponent.definition;
            }
            
            // Disable all non-essential components
            DisableComponents();
        }
        
        void Start()
        {
            // Set initial material
            UpdateVisuals();
        }
        
        void Update()
        {
            // Rotate to face camera (for selection indicators)
            FaceCamera();
        }
        
        public void Initialize(BuildingDefinition buildingDefinition)
        {
            definition = buildingDefinition;
            UpdateVisuals();
        }
        
        public void SetValid(bool valid)
        {
            if (isValid != valid)
            {
                isValid = valid;
                UpdateVisuals();
            }
        }
        
        public bool IsValid()
        {
            return isValid;
        }
        
        private void DisableComponents()
        {
            // Disable building functionality
            if (buildingComponent != null)
            {
                buildingComponent.enabled = false;
            }
            
            // Disable colliders or set to trigger
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider collider in colliders)
            {
                collider.isTrigger = true;
            }
            
            // Disable particle systems
            ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in particles)
            {
                ps.Stop();
            }
            
            // Disable lights
            Light[] lights = GetComponentsInChildren<Light>();
            foreach (Light light in lights)
            {
                light.enabled = false;
            }
            
            // Disable audio
            AudioSource[] audioSources = GetComponentsInChildren<AudioSource>();
            foreach (AudioSource audio in audioSources)
            {
                audio.enabled = false;
            }
        }
        
        private void UpdateVisuals()
        {
            if (renderers == null) return;
            
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    if (isValid)
                    {
                        if (validMaterial != null)
                        {
                            renderer.material = validMaterial;
                        }
                        else
                        {
                            // Create transparent material
                            Material mat = new Material(Shader.Find("Standard"));
                            mat.color = validColor;
                            mat.SetFloat("_Mode", 3); // Transparent mode
                            renderer.material = mat;
                        }
                    }
                    else
                    {
                        if (invalidMaterial != null)
                        {
                            renderer.material = invalidMaterial;
                        }
                        else
                        {
                            // Create transparent material
                            Material mat = new Material(Shader.Find("Standard"));
                            mat.color = invalidColor;
                            mat.SetFloat("_Mode", 3); // Transparent mode
                            renderer.material = mat;
                        }
                    }
                    
                    // Enable emission for better visibility
                    renderer.material.EnableKeyword("_EMISSION");
                    renderer.material.SetColor("_EmissionColor", isValid ? Color.green * 0.2f : Color.red * 0.2f);
                }
            }
        }
        
        private void FaceCamera()
        {
            // Make selection indicators face camera
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Transform selectionIndicator = transform.Find("SelectionIndicator");
                if (selectionIndicator != null)
                {
                    selectionIndicator.LookAt(mainCamera.transform);
                    selectionIndicator.Rotate(0, 180, 0);
                }
            }
        }
        
        public void ShowGridOverlay(bool show)
        {
            // Show/hide grid overlay for placement
            Transform gridOverlay = transform.Find("GridOverlay");
            if (gridOverlay != null)
            {
                gridOverlay.gameObject.SetActive(show);
            }
        }
        
        public void UpdateGridOverlay(Vector3 gridPosition)
        {
            // Update grid overlay position
            Transform gridOverlay = transform.Find("GridOverlay");
            if (gridOverlay != null && definition != null)
            {
                // Position grid overlay based on building size
                gridOverlay.localScale = new Vector3(
                    definition.width,
                    0.1f,
                    definition.depth
                );
                
                gridOverlay.localPosition = new Vector3(
                    definition.width / 2f,
                    -0.5f,
                    definition.depth / 2f
                );
            }
        }
        
        public void PulseEffect()
        {
            // Play pulse effect when placement is valid
            if (isValid)
            {
                StartCoroutine(PulseRoutine());
            }
        }
        
        private System.Collections.IEnumerator PulseRoutine()
        {
            float duration = 0.5f;
            float timer = 0f;
            
            Color startColor = isValid ? validColor : invalidColor;
            Color pulseColor = new Color(startColor.r, startColor.g, startColor.b, 0.8f);
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = Mathf.PingPong(timer * 2, 1f);
                
                foreach (Renderer renderer in renderers)
                {
                    if (renderer != null)
                    {
                        Color currentColor = Color.Lerp(startColor, pulseColor, t);
                        renderer.material.color = currentColor;
                    }
                }
                
                yield return null;
            }
            
            // Restore original color
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.material.color = startColor;
                }
            }
        }
        
        void OnDrawGizmos()
        {
            if (definition != null)
            {
                // Draw ghost bounds
                Gizmos.color = isValid ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.3f);
                Gizmos.DrawWireCube(
                    transform.position + new Vector3(
                        definition.width / 2f,
                        definition.height / 2f,
                        definition.depth / 2f
                    ),
                    new Vector3(
                        definition.width,
                        definition.height,
                        definition.depth
                    )
                );
                
                // Draw grid cells
                if (definition.width > 1 || definition.depth > 1)
                {
                    Gizmos.color = new Color(1, 1, 1, 0.2f);
                    float gridSize = 1f; // Assuming 1 unit grid
                    
                    for (int x = 0; x < definition.width; x++)
                    {
                        for (int z = 0; z < definition.depth; z++)
                        {
                            Vector3 cellCenter = transform.position + new Vector3(
                                x * gridSize + gridSize / 2f,
                                0.1f,
                                z * gridSize + gridSize / 2f
                            );
                            
                            Gizmos.DrawWireCube(cellCenter, new Vector3(gridSize, 0.1f, gridSize));
                        }
                    }
                }
            }
        }
    }
}