using UnityEngine;
using UnityEngine.UI;

namespace NexusPrime.Units
{
    public class HealthBar : MonoBehaviour
    {
        [Header("Bar References")]
        public Image healthFill;
        public Image shieldFill;
        public bool hideWhenFull = true;

        private UnitStats unitStats;

        public void Initialize(UnitStats stats)
        {
            unitStats = stats;
            if (healthFill != null) healthFill.fillAmount = 1f;
            if (shieldFill != null)
            {
                shieldFill.fillAmount = unitStats != null && unitStats.maxShield > 0 ? 1f : 0f;
                shieldFill.gameObject.SetActive(unitStats != null && unitStats.maxShield > 0);
            }
        }

        public void UpdateHealth(float percentage)
        {
            if (healthFill != null) healthFill.fillAmount = Mathf.Clamp01(percentage);
            if (hideWhenFull && healthFill != null)
                healthFill.transform.parent.gameObject.SetActive(percentage < 1f || (unitStats != null && unitStats.maxShield > 0));
        }

        public void UpdateShield(float percentage)
        {
            if (shieldFill != null)
            {
                shieldFill.fillAmount = Mathf.Clamp01(percentage);
                shieldFill.gameObject.SetActive(unitStats != null && unitStats.maxShield > 0);
            }
        }
    }
}
