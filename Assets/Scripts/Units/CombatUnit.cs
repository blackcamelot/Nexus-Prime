using UnityEngine;
using System.Collections;

namespace NexusPrime.Units
{
    [RequireComponent(typeof(UnitMovement))]
    [RequireComponent(typeof(UnitStats))]
    public class CombatUnit : MonoBehaviour
    {
        [Header("Combat Settings")]
        public string unitId;
        public string unitName;
        public string faction;
        
        [Header("Targeting")]
        public Transform currentTarget;
        public LayerMask targetLayer;
        public float targetSearchInterval = 0.5f;
        
        [Header("Combat Abilities")]
        public string primaryWeapon;
        public string[] abilities;
        public bool canAttackAir = false;
        public bool canAttackGround = true;
        
        [Header("Visual Effects")]
        public GameObject projectilePrefab;
        public Transform weaponMount;
        public ParticleSystem muzzleFlash;
        public AudioClip attackSound;
        
        // Components
        private UnitMovement movement;
        private UnitStats stats;
        private SelectableUnit selectable;
        
        // Combat state
        private bool isAttacking = false;
        private bool isInCombat = false;
        private float targetSearchTimer;
        private Coroutine attackCoroutine;
        
        // Events
        public delegate void CombatEventHandler(CombatUnit unit, Transform target, float damage);
        public event CombatEventHandler OnAttack;
        public event CombatEventHandler OnDamageTaken;
        public event CombatEventHandler OnTargetKilled;
        
        void Awake()
        {
            movement = GetComponent<UnitMovement>();
            stats = GetComponent<UnitStats>();
            selectable = GetComponent<SelectableUnit>();
            
            stats.Initialize();
        }
        
        void Start()
        {
            if (selectable != null)
            {
                selectable.OnSelected += OnUnitSelected;
                selectable.OnDeselected += OnUnitDeselected;
            }
        }
        
        void Update()
        {
            if (!stats.IsAlive()) return;
            
            stats.Update(Time.deltaTime);
            
            // Update target search
            targetSearchTimer += Time.deltaTime;
            if (targetSearchTimer >= targetSearchInterval)
            {
                targetSearchTimer = 0;
                FindTarget();
            }
            
            // Engage target if we have one
            if (currentTarget != null)
            {
                EngageTarget();
            }
        }
        
        public void SetTarget(Transform target)
        {
            if (target == currentTarget) return;
            
            currentTarget = target;
            isInCombat = true;
            
            if (selectable != null)
            {
                selectable.SetCombatState(true);
            }
            
            Debug.Log($"{unitName} acquired target: {target.name}");
        }
        
        public void ClearTarget()
        {
            currentTarget = null;
            isInCombat = false;
            
            if (selectable != null)
            {
                selectable.SetCombatState(false);
            }
            
            if (attackCoroutine != null)
            {
                StopCoroutine(attackCoroutine);
                attackCoroutine = null;
            }
            
            isAttacking = false;
        }
        
        private void FindTarget()
        {
            if (currentTarget != null) return;
            
            Collider[] potentialTargets = Physics.OverlapSphere(
                transform.position, 
                stats.sightRange, 
                targetLayer
            );
            
            float closestDistance = float.MaxValue;
            Transform closestTarget = null;
            
            foreach (Collider collider in potentialTargets)
            {
                // Check if target is valid
                CombatUnit targetUnit = collider.GetComponent<CombatUnit>();
                if (targetUnit == null || targetUnit.faction == faction || !targetUnit.stats.IsAlive())
                    continue;
                
                // Check if we can attack this target type
                bool isAirTarget = collider.gameObject.layer == LayerMask.NameToLayer("Air");
                if ((isAirTarget && !canAttackAir) || (!isAirTarget && !canAttackGround))
                    continue;
                
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = collider.transform;
                }
            }
            
            if (closestTarget != null)
            {
                SetTarget(closestTarget);
            }
        }
        
        private void EngageTarget()
        {
            if (!stats.IsAlive() || currentTarget == null) return;
            
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
            
            if (distanceToTarget <= stats.attackRange)
            {
                // In range, stop moving and attack
                movement.StopMovement();
                
                // Rotate towards target
                Vector3 direction = (currentTarget.position - transform.position).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, 
                    targetRotation, 
                    stats.rotationSpeed * Time.deltaTime
                );
                
                // Attack if cooldown is ready
                if (stats.CanAttack())
                {
                    StartAttack();
                }
            }
            else
            {
                // Move towards target
                movement.MoveTo(currentTarget.position);
            }
        }
        
        private void StartAttack()
        {
            if (isAttacking) return;
            
            stats.StartAttackCooldown();
            
            if (attackCoroutine != null)
                StopCoroutine(attackCoroutine);
            
            attackCoroutine = StartCoroutine(AttackSequence());
        }
        
        private IEnumerator AttackSequence()
        {
            isAttacking = true;
            
            // Attack animation
            if (selectable != null)
            {
                selectable.SetAttackState(true);
            }
            
            // Visual and audio effects
            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }
            
            if (attackSound != null)
            {
                AudioSource.PlayClipAtPoint(attackSound, transform.position);
            }
            
            // Create projectile if applicable
            if (projectilePrefab != null && weaponMount != null)
            {
                GameObject projectile = Instantiate(
                    projectilePrefab, 
                    weaponMount.position, 
                    weaponMount.rotation
                );
                
                Projectile proj = projectile.GetComponent<Projectile>();
                if (proj != null)
                {
                    proj.Initialize(currentTarget, stats.damage, faction);
                }
            }
            else
            {
                // Instant hit
                DealDamage(currentTarget, stats.damage);
            }
            
            // Wait for attack animation
            yield return new WaitForSeconds(0.3f);
            
            // Reset attack state
            if (selectable != null)
            {
                selectable.SetAttackState(false);
            }
            
            isAttacking = false;
            attackCoroutine = null;
            
            // Fire attack event
            OnAttack?.Invoke(this, currentTarget, stats.damage);
        }
        
        private void DealDamage(Transform target, float damage)
        {
            if (target == null) return;
            
            CombatUnit targetUnit = target.GetComponent<CombatUnit>();
            if (targetUnit == null) return;
            
            targetUnit.TakeDamage(damage, stats.penetration);
            
            // Check if target died
            if (!targetUnit.stats.IsAlive())
            {
                OnTargetKilled?.Invoke(this, target, damage);
                ClearTarget();
            }
        }
        
        public void TakeDamage(float damage, float armorPenetration = 0f)
        {
            if (!stats.IsAlive()) return;
            
            float previousHealth = stats.currentHealth;
            stats.TakeDamage(damage, armorPenetration);
            
            // Fire damage event
            OnDamageTaken?.Invoke(this, currentTarget, damage);
            
            // Check if we died
            if (!stats.IsAlive())
            {
                Die();
            }
            else if (selectable != null)
            {
                // Show damage effect
                selectable.ShowDamageEffect(damage);
            }
        }
        
        public void Heal(float amount)
        {
            if (!stats.IsAlive()) return;
            
            stats.Heal(amount);
            
            if (selectable != null)
            {
                selectable.ShowHealEffect(amount);
            }
        }
        
        private void Die()
        {
            Debug.Log($"{unitName} has been destroyed");
            
            // Stop all actions
            ClearTarget();
            movement.StopMovement();
            
            // Death animation
            if (selectable != null)
            {
                selectable.PlayDeathAnimation();
            }
            
            // Fire death event
            OnDamageTaken?.Invoke(this, currentTarget, stats.currentHealth);
            
            // Destroy after delay
            Destroy(gameObject, 2f);
        }
        
        private void OnUnitSelected()
        {
            // Highlight when selected
            // Could add selection ring or other effects
        }
        
        private void OnUnitDeselected()
        {
            // Remove selection effects
        }
        
        public bool UseAbility(int abilityIndex, Transform target = null)
        {
            if (abilityIndex < 0 || abilityIndex >= abilities.Length) return false;
            if (!stats.IsAlive()) return false;
            
            // Check if ability is available
            // Implement ability logic here
            
            return true;
        }
        
        public void UpgradeWeapon(float damageMultiplier)
        {
            stats.UpgradeDamage(damageMultiplier);
        }
        
        public void UpgradeArmor(float armorMultiplier)
        {
            stats.UpgradeArmor(armorMultiplier);
        }
        
        void OnDrawGizmosSelected()
        {
            if (stats != null)
            {
                // Draw attack range
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, stats.attackRange);
                
                // Draw sight range
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, stats.sightRange);
                
                // Draw line to target
                if (currentTarget != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(transform.position, currentTarget.position);
                }
            }
        }
    }
}