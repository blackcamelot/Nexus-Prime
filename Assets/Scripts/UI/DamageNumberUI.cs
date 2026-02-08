using UnityEngine;
using TMPro;

namespace NexusPrime.UI
{
    public class DamageNumberUI : MonoBehaviour
    {
        public TextMeshProUGUI textComponent;
        public float lifetime = 1f;
        public float floatSpeed = 1f;

        public void Initialize(float damage, bool isCritical = false)
        {
            if (textComponent != null)
            {
                textComponent.text = Mathf.RoundToInt(damage).ToString();
                textComponent.color = isCritical ? Color.red : Color.yellow;
            }
            Destroy(gameObject, lifetime);
        }

        void Update()
        {
            if (floatSpeed > 0)
                transform.position += Vector3.up * floatSpeed * Time.deltaTime;
        }
    }
}
