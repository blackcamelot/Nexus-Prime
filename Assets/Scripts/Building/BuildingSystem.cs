using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NexusPrime.Building
{
    public class BuildingSystem : MonoBehaviour
    {
        [Header("Building Settings")]
        public LayerMask groundLayer;
        public LayerMask obstacleLayer;
        public Material ghostValidMaterial;
        public Material ghostInvalidMaterial;
        
        [Header("Build Grid")]
        public float gridSize = 1f;
        public bool snapToGrid = true;
        
        [Header("References")]
        public BuildingFactory buildingFactory;
        public ResourceManager resourceManager;
        
        // Building state
        private bool isBuildingMode = false;
        private string selectedBuildingId;
        private GameObject ghostBuilding;
        private BuildingDefinition selectedDefinition;
        private List<Building> placedBuildings = new List<Building>();
        
        // Grid
        private Dictionary<Vector3Int, Building> buildingGrid = new Dictionary<Vector3Int, Building>();
        
        void Start()
        {
            if (buildingFactory == null)
            {
                buildingFactory = GetComponent<BuildingFactory>();
            }
            
            if (resourceManager == null)
            {
                resourceManager = FindObjectOfType<ResourceManager>();
            }
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.buildingSystem = this;
            }
        }
        
        void Update()
        {
            if (!isBuildingMode) return;
            
            UpdateGhostPosition();
            CheckBuildingPlacement();
            
            HandleBuildingInput();
        }
        
        public void EnterBuildingMode(string buildingId)
        {
            if (isBuildingMode) ExitBuildingMode();
            
            selectedBuildingId = buildingId;
            selectedDefinition = buildingFactory.GetBuildingDefinition(buildingId);
            
            if (selectedDefinition == null)
            {
                Debug.LogError($"Building definition not found: {buildingId}");
                return;
            }
            
            if (selectedDefinition.prefab == null)
            {
                Debug.LogError($"No prefab for building: {buildingId}");
                return;
            }
            
            // Create ghost building
            ghostBuilding = Instantiate(selectedDefinition.prefab);
            ghostBuilding.name = $"Ghost_{buildingId}";
            
            // Setup ghost components
            SetupGhostBuilding(ghostBuilding);
            
            isBuildingMode = true;
            
            Debug.Log($"Entered building mode for: {selectedDefinition.buildingName}");
        }
        
        public void ExitBuildingMode()
        {
            if (ghostBuilding != null)
            {
                Destroy(ghostBuilding);
                ghostBuilding = null;
            }
            
            isBuildingMode = false;
            selectedBuildingId = null;
            selectedDefinition = null;
            
            Debug.Log("Exited building mode");
        }
        
        private void SetupGhostBuilding(GameObject ghost)
        {
            // Disable all non-essential components
            Building buildingComponent = ghost.GetComponent<Building>();
            if (buildingComponent != null)
            {
                buildingComponent.enabled = false;
            }
            
            Collider collider = ghost.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
            
            // Set ghost material
            Renderer[] renderers = ghost.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.material = ghostValidMaterial;
                
                // Make transparent
                Color color = renderer.material.color;
                color.a = 0.5f;
                renderer.material.color = color;
            }
            
            // Disable particle systems
            ParticleSystem[] particles = ghost.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in particles)
            {
                ps.Stop();
            }
            
            // Disable lights
            Light[] lights = ghost.GetComponentsInChildren<Light>();
            foreach (Light light in lights)
            {
                light.enabled = false;
            }
        }
        
        private void UpdateGhostPosition()
        {
            if (ghostBuilding == null) return;
            
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
            {
                Vector3 position = hit.point;
                
                if (snapToGrid)
                {
                    position = SnapToGrid(position);
                }
                
                // Adjust for building size
                if (selectedDefinition != null)
                {
                    position.y += selectedDefinition.heightOffset;
                    
                    // Center building based on size
                    position.x -= (selectedDefinition.width * gridSize) / 2f;
                    position.z -= (selectedDefinition.depth * gridSize) / 2f;
                }
                
                ghostBuilding.transform.position = position;
            }
        }
        
        private void CheckBuildingPlacement()
        {
            if (ghostBuilding == null || selectedDefinition == null) return;
            
            bool isValid = true;
            
            // Check resources
            if (resourceManager != null)
            {
                if (!resourceManager.CheckResources(selectedDefinition.cost))
                {
                    isValid = false;
                }
            }
            
            // Check collisions
            Collider[] colliders = Physics.OverlapBox(
                ghostBuilding.transform.position + new Vector3(
                    selectedDefinition.width * gridSize / 2f,
                    selectedDefinition.height / 2f,
                    selectedDefinition.depth * gridSize / 2f
                ),
                new Vector3(
                    selectedDefinition.width * gridSize / 2f,
                    selectedDefinition.height / 2f,
                    selectedDefinition.depth * gridSize / 2f
                ),
                ghostBuilding.transform.rotation,
                obstacleLayer
            );
            
            foreach (Collider collider in colliders)
            {
                if (collider.gameObject != ghostBuilding && !collider.isTrigger)
                {
                    isValid = false;
                    break;
                }
            }
            
            // Check grid availability
            Vector3Int gridPos = WorldToGrid(ghostBuilding.transform.position);
            for (int x = 0; x < selectedDefinition.width; x++)
            {
                for (int z = 0; z < selectedDefinition.depth; z++)
                {
                    Vector3Int checkPos = gridPos + new Vector3Int(x, 0, z);
                    if (buildingGrid.ContainsKey(checkPos))
                    {
                        isValid = false;
                        break;
                    }
                }
                if (!isValid) break;
            }
            
            // Update ghost appearance
            Renderer[] renderers = ghostBuilding.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.material = isValid ? ghostValidMaterial : ghostInvalidMaterial;
                
                Color color = renderer.material.color;
                color.a = isValid ? 0.5f : 0.3f;
                renderer.material.color = color;
            }
        }
        
        private void HandleBuildingInput()
        {
            // Place building
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                if (ghostBuilding == null || selectedDefinition == null) return;
                
                // Final validation
                if (!ValidateBuildingPlacement()) return;
                
                // Spend resources
                if (resourceManager != null)
                {
                    if (!resourceManager.SpendResources(selectedDefinition.cost))
                    {
                        Debug.LogWarning("Not enough resources to build");
                        return;
                    }
                }
                
                // Place building
                PlaceBuilding();
            }
            
            // Rotate building
            if (Input.GetKeyDown(KeyCode.R))
            {
                RotateGhostBuilding();
            }
            
            // Cancel building
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                ExitBuildingMode();
            }
        }
        
        private bool ValidateBuildingPlacement()
        {
            if (ghostBuilding == null || selectedDefinition == null) return false;
            
            // Check all conditions
            if (resourceManager != null && !resourceManager.CheckResources(selectedDefinition.cost))
                return false;
            
            // Check collisions
            Collider[] colliders = Physics.OverlapBox(
                ghostBuilding.transform.position + new Vector3(
                    selectedDefinition.width * gridSize / 2f,
                    selectedDefinition.height / 2f,
                    selectedDefinition.depth * gridSize / 2f
                ),
                new Vector3(
                    selectedDefinition.width * gridSize / 2f,
                    selectedDefinition.height / 2f,
                    selectedDefinition.depth * gridSize / 2f
                ),
                ghostBuilding.transform.rotation,
                obstacleLayer
            );
            
            foreach (Collider collider in colliders)
            {
                if (collider.gameObject != ghostBuilding && !collider.isTrigger)
                {
                    return false;
                }
            }
            
            return true;
        }
        
        private void PlaceBuilding()
        {
            Vector3 position = ghostBuilding.transform.position;
            Quaternion rotation = ghostBuilding.transform.rotation;
            
            // Create real building
            GameObject buildingObject = buildingFactory.CreateBuilding(
                selectedBuildingId,
                position,
                rotation,
                "player" // TODO: Get actual faction
            );
            
            if (buildingObject == null)
            {
                Debug.LogError("Failed to create building");
                return;
            }
            
            Building buildingComponent = buildingObject.GetComponent<Building>();
            if (buildingComponent != null)
            {
                buildingComponent.OnConstructionComplete += OnBuildingComplete;
                placedBuildings.Add(buildingComponent);
                
                // Register with GameManager
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.RegisterBuilding(buildingComponent);
                }
                
                // Add to grid
                AddBuildingToGrid(buildingComponent);
            }
            
            // Cleanup ghost
            Destroy(ghostBuilding);
            ghostBuilding = null;
            
            // Exit building mode or stay for another placement
            if (!Input.GetKey(KeyCode.LeftShift))
            {
                ExitBuildingMode();
            }
            else
            {
                // Stay in building mode for another placement
                ghostBuilding = Instantiate(selectedDefinition.prefab);
                SetupGhostBuilding(ghostBuilding);
            }
            
            Debug.Log($"Building placed: {selectedDefinition.buildingName} at {position}");
        }
        
        private void RotateGhostBuilding()
        {
            if (ghostBuilding == null) return;
            
            ghostBuilding.transform.Rotate(0, 90, 0);
        }
        
        private Vector3 SnapToGrid(Vector3 position)
        {
            float x = Mathf.Round(position.x / gridSize) * gridSize;
            float z = Mathf.Round(position.z / gridSize) * gridSize;
            
            return new Vector3(x, position.y, z);
        }
        
        private Vector3Int WorldToGrid(Vector3 worldPosition)
        {
            int x = Mathf.RoundToInt(worldPosition.x / gridSize);
            int z = Mathf.RoundToInt(worldPosition.z / gridSize);
            
            return new Vector3Int(x, 0, z);
        }
        
        private void AddBuildingToGrid(Building building)
        {
            Vector3Int gridPos = WorldToGrid(building.transform.position);
            
            for (int x = 0; x < building.definition.width; x++)
            {
                for (int z = 0; z < building.definition.depth; z++)
                {
                    Vector3Int cellPos = gridPos + new Vector3Int(x, 0, z);
                    buildingGrid[cellPos] = building;
                }
            }
        }
        
        private void RemoveBuildingFromGrid(Building building)
        {
            Vector3Int gridPos = WorldToGrid(building.transform.position);
            
            for (int x = 0; x < building.definition.width; x++)
            {
                for (int z = 0; z < building.definition.depth; z++)
                {
                    Vector3Int cellPos = gridPos + new Vector3Int(x, 0, z);
                    buildingGrid.Remove(cellPos);
                }
            }
        }
        
        private void OnBuildingComplete(Building building)
        {
            Debug.Log($"Building complete: {building.definition.buildingName}");
            
            // Building is now fully functional
        }
        
        public bool CanPlaceBuilding(string buildingId, Vector3 position)
        {
            BuildingDefinition definition = buildingFactory.GetBuildingDefinition(buildingId);
            if (definition == null) return false;
            
            // Check grid
            Vector3Int gridPos = WorldToGrid(position);
            for (int x = 0; x < definition.width; x++)
            {
                for (int z = 0; z < definition.depth; z++)
                {
                    Vector3Int checkPos = gridPos + new Vector3Int(x, 0, z);
                    if (buildingGrid.ContainsKey(checkPos))
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        public List<Building> GetBuildingsInRadius(Vector3 center, float radius)
        {
            List<Building> buildingsInRadius = new List<Building>();
            
            foreach (Building building in placedBuildings)
            {
                if (building != null && Vector3.Distance(center, building.transform.position) <= radius)
                {
                    buildingsInRadius.Add(building);
                }
            }
            
            return buildingsInRadius;
        }
        
        public Building GetNearestBuilding(Vector3 position, string buildingType = "")
        {
            Building nearest = null;
            float nearestDistance = float.MaxValue;
            
            foreach (Building building in placedBuildings)
            {
                if (building == null || !building.IsOperational()) continue;
                
                if (!string.IsNullOrEmpty(buildingType) && 
                    building.definition != null && building.definition.buildingId != buildingType)
                    continue;
                
                float distance = Vector3.Distance(position, building.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = building;
                }
            }
            
            return nearest;
        }
        
        void OnDrawGizmos()
        {
            if (isBuildingMode && ghostBuilding != null && selectedDefinition != null)
            {
                // Draw building bounds
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(
                    ghostBuilding.transform.position + new Vector3(
                        selectedDefinition.width * gridSize / 2f,
                        selectedDefinition.height / 2f,
                        selectedDefinition.depth * gridSize / 2f
                    ),
                    new Vector3(
                        selectedDefinition.width * gridSize,
                        selectedDefinition.height,
                        selectedDefinition.depth * gridSize
                    )
                );
                
                // Draw grid cells
                Gizmos.color = Color.cyan;
                Vector3Int gridPos = WorldToGrid(ghostBuilding.transform.position);
                
                for (int x = 0; x < selectedDefinition.width; x++)
                {
                    for (int z = 0; z < selectedDefinition.depth; z++)
                    {
                        Vector3 worldPos = new Vector3(
                            (gridPos.x + x) * gridSize + gridSize / 2f,
                            ghostBuilding.transform.position.y,
                            (gridPos.z + z) * gridSize + gridSize / 2f
                        );
                        
                        Gizmos.DrawWireCube(worldPos, new Vector3(gridSize, 0.1f, gridSize));
                    }
                }
            }
        }
    }
}