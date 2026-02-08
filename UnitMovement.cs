using UnityEngine;
using UnityEngine.AI;
using System.Collections;

namespace NexusPrime.Units
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class UnitMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float moveSpeed = 3.5f;
        public float rotationSpeed = 360f;
        public float acceleration = 8f;
        public float stoppingDistance = 0.5f;
        
        [Header("Formation Settings")]
        public int formationIndex = 0;
        public Vector3 formationOffset = Vector3.zero;
        
        [Header("Pathfinding")]
        public bool usePathfinding = true;
        public LayerMask groundLayer;
        
        [Header("Animation")]
        public Animator animator;
        public string moveSpeedParameter = "Speed";
        
        // Components
        private NavMeshAgent navAgent;
        private SelectableUnit selectableUnit;
        
        // Movement state
        private Vector3 targetPosition;
        private Vector3 lastPosition;
        private float currentSpeed;
        private bool isMoving = false;
        private Coroutine movementCoroutine;
        
        // Formation
        private Transform formationLeader;
        private Vector3 formationPosition;
        
        void Awake()
        {
            navAgent = GetComponent<NavMeshAgent>();
            selectableUnit = GetComponent<SelectableUnit>();
            
            SetupNavAgent();
        }
        
        void SetupNavAgent()
        {
            navAgent.speed = moveSpeed;
            navAgent.angularSpeed = rotationSpeed;
            navAgent.acceleration = acceleration;
            navAgent.stoppingDistance = stoppingDistance;
            navAgent.autoBraking = true;
            navAgent.autoRepath = true;
            navAgent.height = 2f;
            navAgent.radius = 0.5f;
        }
        
        void Start()
        {
            lastPosition = transform.position;
            
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }
        
        void Update()
        {
            UpdateSpeed();
            UpdateAnimation();
            
            if (formationLeader != null)
            {
                UpdateFormationPosition();
            }
            
            lastPosition = transform.position;
        }
        
        public void MoveTo(Vector3 position)
        {
            if (!usePathfinding)
            {
                // Simple movement without pathfinding
                targetPosition = position;
                if (movementCoroutine != null)
                    StopCoroutine(movementCoroutine);
                movementCoroutine = StartCoroutine(SimpleMoveTo(position));
            }
            else
            {
                // Use NavMesh pathfinding
                if (navAgent != null && navAgent.isActiveAndEnabled)
                {
                    navAgent.SetDestination(position);
                    isMoving = true;
                    targetPosition = position;
                    
                    // Show movement indicator
                    ShowMovementIndicator(position);
                }
            }
            
            if (selectableUnit != null)
            {
                selectableUnit.OnMovementStarted();
            }
        }
        
        public void MoveToWithFormation(Vector3 position, Transform leader, int index)
        {
            formationLeader = leader;
            formationIndex = index;
            
            // Calculate formation offset based on index
            float row = Mathf.Floor(index / 4f);
            float col = index % 4;
            formationOffset = new Vector3((col - 1.5f) * 2f, 0, row * -2f);
            
            MoveTo(position);
        }
        
        public void StopMovement()
        {
            if (navAgent != null && navAgent.isActiveAndEnabled)
            {
                navAgent.isStopped = true;
                navAgent.ResetPath();
            }
            
            if (movementCoroutine != null)
            {
                StopCoroutine(movementCoroutine);
                movementCoroutine = null;
            }
            
            isMoving = false;
            formationLeader = null;
        }
        
        public void SetSpeed(float speedMultiplier)
        {
            if (navAgent != null)
            {
                navAgent.speed = moveSpeed * speedMultiplier;
            }
        }
        
        public bool HasReachedDestination()
        {
            if (navAgent != null)
            {
                return !navAgent.pathPending && 
                       navAgent.remainingDistance <= navAgent.stoppingDistance && 
                       (!navAgent.hasPath || navAgent.velocity.sqrMagnitude == 0f);
            }
            
            return Vector3.Distance(transform.position, targetPosition) <= stoppingDistance;
        }
        
        public Vector3 GetDestination()
        {
            if (navAgent != null && navAgent.hasPath)
            {
                return navAgent.destination;
            }
            return targetPosition;
        }
        
        public float GetRemainingDistance()
        {
            if (navAgent != null && navAgent.hasPath)
            {
                return navAgent.remainingDistance;
            }
            return Vector3.Distance(transform.position, targetPosition);
        }
        
        private IEnumerator SimpleMoveTo(Vector3 position)
        {
            isMoving = true;
            targetPosition = position;
            
            while (Vector3.Distance(transform.position, position) > stoppingDistance)
            {
                Vector3 direction = (position - transform.position).normalized;
                Vector3 moveVector = direction * moveSpeed * Time.deltaTime;
                
                // Rotate towards target
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation, 
                        targetRotation, 
                        rotationSpeed * Time.deltaTime
                    );
                }
                
                // Move
                transform.position += moveVector;
                
                yield return null;
            }
            
            isMoving = false;
            movementCoroutine = null;
        }
        
        private void UpdateSpeed()
        {
            Vector3 velocity = (transform.position - lastPosition) / Time.deltaTime;
            currentSpeed = velocity.magnitude;
        }
        
        private void UpdateAnimation()
        {
            if (animator != null)
            {
                float normalizedSpeed = Mathf.Clamp01(currentSpeed / moveSpeed);
                animator.SetFloat(moveSpeedParameter, normalizedSpeed);
                
                // Set moving state
                animator.SetBool("IsMoving", isMoving && currentSpeed > 0.1f);
            }
        }
        
        private void UpdateFormationPosition()
        {
            if (formationLeader == null) return;
            
            // Calculate position relative to leader
            formationPosition = formationLeader.position + 
                               formationLeader.rotation * formationOffset;
            
            // If we're too far from formation, adjust position
            if (Vector3.Distance(transform.position, formationPosition) > 5f)
            {
                MoveTo(formationPosition);
            }
        }
        
        private void ShowMovementIndicator(Vector3 position)
        {
            // Create visual indicator at target position
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            indicator.transform.position = position + Vector3.up * 0.1f;
            indicator.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
            
            Renderer renderer = indicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.cyan;
                renderer.material.SetColor("_EmissionColor", Color.cyan * 0.5f);
            }
            
            Destroy(indicator, 1f);
        }
        
        public void TeleportTo(Vector3 position)
        {
            if (navAgent != null)
            {
                navAgent.Warp(position);
            }
            else
            {
                transform.position = position;
            }
        }
        
        public void SetAvoidancePriority(int priority)
        {
            if (navAgent != null)
            {
                navAgent.avoidancePriority = Mathf.Clamp(priority, 1, 99);
            }
        }
        
        void OnDrawGizmosSelected()
        {
            if (isMoving)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, targetPosition);
                Gizmos.DrawWireSphere(targetPosition, 0.5f);
            }
            
            if (formationLeader != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, formationPosition);
                Gizmos.DrawWireSphere(formationPosition, 0.3f);
            }
        }
    }
}