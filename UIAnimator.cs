using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace NexusPrime.UI
{
    public class UIAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        public float hoverScale = 1.1f;
        public float hoverDuration = 0.2f;
        public float clickScale = 0.95f;
        public float clickDuration = 0.1f;
        public float pulseInterval = 2f;
        
        [Header("Color Settings")]
        public Color normalColor = Color.white;
        public Color hoverColor = new Color(0.8f, 0.8f, 1f);
        public Color pressedColor = new Color(0.6f, 0.6f, 1f);
        public Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        
        [Header("Effect Prefabs")]
        public GameObject clickEffectPrefab;
        public GameObject hoverEffectPrefab;
        
        // Components
        private RectTransform rectTransform;
        private Image image;
        private Button button;
        private Vector3 originalScale;
        private Color originalColor;
        
        // Animation state
        private bool isHovering = false;
        private bool isPulsing = false;
        private Sequence pulseSequence;
        
        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            image = GetComponent<Image>();
            button = GetComponent<Button>();
            
            if (rectTransform != null)
            {
                originalScale = rectTransform.localScale;
            }
            
            if (image != null)
            {
                originalColor = image.color;
            }
        }
        
        void Start()
        {
            // Setup button events if available
            if (button != null)
            {
                // Create event triggers if they don't exist
                SetupEventTriggers();
            }
        }
        
        void SetupEventTriggers()
        {
            // Remove existing EventTriggers to avoid duplicates
            EventTrigger[] existingTriggers = GetComponents<EventTrigger>();
            foreach (var trigger in existingTriggers)
            {
                Destroy(trigger);
            }
            
            // Add new EventTrigger
            EventTrigger eventTrigger = gameObject.AddComponent<EventTrigger>();
            
            // Pointer Enter
            EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
            pointerEnter.eventID = EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((data) => OnPointerEnter());
            eventTrigger.triggers.Add(pointerEnter);
            
            // Pointer Exit
            EventTrigger.Entry pointerExit = new EventTrigger.Entry();
            pointerExit.eventID = EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((data) => OnPointerExit());
            eventTrigger.triggers.Add(pointerExit);
            
            // Pointer Down
            EventTrigger.Entry pointerDown = new EventTrigger.Entry();
            pointerDown.eventID = EventTriggerType.PointerDown;
            pointerDown.callback.AddListener((data) => OnPointerDown());
            eventTrigger.triggers.Add(pointerDown);
            
            // Pointer Up
            EventTrigger.Entry pointerUp = new EventTrigger.Entry();
            pointerUp.eventID = EventTriggerType.PointerUp;
            pointerUp.callback.AddListener((data) => OnPointerUp());
            eventTrigger.triggers.Add(pointerUp);
            
            // Pointer Click
            EventTrigger.Entry pointerClick = new EventTrigger.Entry();
            pointerClick.eventID = EventTriggerType.PointerClick;
            pointerClick.callback.AddListener((data) => OnPointerClick());
            eventTrigger.triggers.Add(pointerClick);
        }
        
        public void OnPointerEnter()
        {
            if (!IsInteractable()) return;
            
            isHovering = true;
            
            // Scale animation
            if (rectTransform != null)
            {
                rectTransform.DOScale(originalScale * hoverScale, hoverDuration)
                    .SetEase(Ease.OutBack);
            }
            
            // Color animation
            if (image != null)
            {
                image.DOColor(hoverColor, hoverDuration);
            }
            
            // Play hover sound
            PlayHoverSound();
            
            // Show hover effect
            if (hoverEffectPrefab != null)
            {
                GameObject effect = Instantiate(hoverEffectPrefab, transform);
                effect.transform.localPosition = Vector3.zero;
                Destroy(effect, 1f);
            }
        }
        
        public void OnPointerExit()
        {
            if (!IsInteractable()) return;
            
            isHovering = false;
            
            // Scale animation
            if (rectTransform != null)
            {
                rectTransform.DOScale(originalScale, hoverDuration)
                    .SetEase(Ease.OutBack);
            }
            
            // Color animation
            if (image != null)
            {
                image.DOColor(originalColor, hoverDuration);
            }
        }
        
        public void OnPointerDown()
        {
            if (!IsInteractable()) return;
            
            // Scale animation
            if (rectTransform != null)
            {
                rectTransform.DOScale(originalScale * clickScale, clickDuration)
                    .SetEase(Ease.OutQuad);
            }
            
            // Color animation
            if (image != null)
            {
                image.DOColor(pressedColor, clickDuration);
            }
        }
        
        public void OnPointerUp()
        {
            if (!IsInteractable()) return;
            
            // Scale animation
            if (rectTransform != null)
            {
                if (isHovering)
                {
                    rectTransform.DOScale(originalScale * hoverScale, clickDuration)
                        .SetEase(Ease.OutBack);
                }
                else
                {
                    rectTransform.DOScale(originalScale, clickDuration)
                        .SetEase(Ease.OutBack);
                }
            }
            
            // Color animation
            if (image != null)
            {
                if (isHovering)
                {
                    image.DOColor(hoverColor, clickDuration);
                }
                else
                {
                    image.DOColor(originalColor, clickDuration);
                }
            }
        }
        
        public void OnPointerClick()
        {
            if (!IsInteractable()) return;
            
            // Play click sound
            PlayClickSound();
            
            // Show click effect
            if (clickEffectPrefab != null)
            {
                GameObject effect = Instantiate(clickEffectPrefab, transform);
                effect.transform.localPosition = Vector3.zero;
                Destroy(effect, 1f);
            }
            
            // Shake animation
            if (rectTransform != null)
            {
                rectTransform.DOShakePosition(0.1f, 5f, 10, 90, false, true);
            }
        }
        
        public void StartPulseAnimation()
        {
            if (isPulsing) return;
            
            isPulsing = true;
            
            pulseSequence = DOTween.Sequence();
            pulseSequence.Append(rectTransform.DOScale(originalScale * 1.05f, pulseInterval / 2f).SetEase(Ease.InOutSine));
            pulseSequence.Append(rectTransform.DOScale(originalScale, pulseInterval / 2f).SetEase(Ease.InOutSine));
            pulseSequence.SetLoops(-1);
            pulseSequence.Play();
        }
        
        public void StopPulseAnimation()
        {
            if (!isPulsing) return;
            
            isPulsing = false;
            
            if (pulseSequence != null)
            {
                pulseSequence.Kill();
                pulseSequence = null;
            }
            
            // Return to original scale
            if (rectTransform != null)
            {
                rectTransform.DOScale(originalScale, 0.2f);
            }
        }
        
        public void PlaySuccessAnimation()
        {
            Sequence successSequence = DOTween.Sequence();
            
            // Scale up
            successSequence.Append(rectTransform.DOScale(originalScale * 1.2f, 0.2f));
            
            // Rotate slightly
            successSequence.Join(rectTransform.DORotate(new Vector3(0, 0, 10), 0.2f));
            
            // Scale back with rotation
            successSequence.Append(rectTransform.DOScale(originalScale, 0.3f));
            successSequence.Join(rectTransform.DORotate(Vector3.zero, 0.3f));
            
            // Color flash
            if (image != null)
            {
                successSequence.Join(image.DOColor(Color.green, 0.1f));
                successSequence.Append(image.DOColor(originalColor, 0.2f));
            }
        }
        
        public void PlayErrorAnimation()
        {
            Sequence errorSequence = DOTween.Sequence();
            
            // Shake
            errorSequence.Append(rectTransform.DOShakePosition(0.5f, 10f, 10, 90, false, true));
            
            // Color flash
            if (image != null)
            {
                errorSequence.Join(image.DOColor(Color.red, 0.1f));
                errorSequence.Append(image.DOColor(originalColor, 0.2f));
            }
        }
        
        public void PlayWarningAnimation()
        {
            Sequence warningSequence = DOTween.Sequence();
            
            // Pulse in yellow
            warningSequence.Append(rectTransform.DOScale(originalScale * 1.1f, 0.2f));
            if (image != null)
            {
                warningSequence.Join(image.DOColor(Color.yellow, 0.2f));
            }
            
            warningSequence.Append(rectTransform.DOScale(originalScale, 0.2f));
            if (image != null)
            {
                warningSequence.Join(image.DOColor(originalColor, 0.2f));
            }
            
            warningSequence.SetLoops(2);
        }
        
        public void SlideInFromLeft(float duration = 0.3f)
        {
            // Store original position
            Vector2 originalPos = rectTransform.anchoredPosition;
            Vector2 startPos = new Vector2(-Screen.width, originalPos.y);
            
            // Set start position
            rectTransform.anchoredPosition = startPos;
            
            // Animate to original position
            rectTransform.DOAnchorPos(originalPos, duration)
                .SetEase(Ease.OutBack);
        }
        
        public void SlideOutToRight(float duration = 0.3f)
        {
            Vector2 endPos = new Vector2(Screen.width, rectTransform.anchoredPosition.y);
            
            rectTransform.DOAnchorPos(endPos, duration)
                .SetEase(Ease.InBack)
                .OnComplete(() => gameObject.SetActive(false));
        }
        
        public void FadeIn(float duration = 0.3f)
        {
            if (image != null)
            {
                image.color = new Color(image.color.r, image.color.g, image.color.b, 0);
                image.DOFade(1, duration);
            }
            
            // Also fade child text/images
            Graphic[] graphics = GetComponentsInChildren<Graphic>();
            foreach (Graphic graphic in graphics)
            {
                if (graphic != image)
                {
                    graphic.color = new Color(graphic.color.r, graphic.color.g, graphic.color.b, 0);
                    graphic.DOFade(1, duration);
                }
            }
        }
        
        public void FadeOut(float duration = 0.3f)
        {
            if (image != null)
            {
                image.DOFade(0, duration)
                    .OnComplete(() => gameObject.SetActive(false));
            }
            
            Graphic[] graphics = GetComponentsInChildren<Graphic>();
            foreach (Graphic graphic in graphics)
            {
                if (graphic != image)
                {
                    graphic.DOFade(0, duration);
                }
            }
        }
        
        public void Bounce(float strength = 0.2f, float duration = 0.5f)
        {
            Sequence bounceSequence = DOTween.Sequence();
            
            bounceSequence.Append(rectTransform.DOScale(originalScale * (1 + strength), duration * 0.3f));
            bounceSequence.Append(rectTransform.DOScale(originalScale * (1 - strength * 0.5f), duration * 0.2f));
            bounceSequence.Append(rectTransform.DOScale(originalScale * (1 + strength * 0.2f), duration * 0.2f));
            bounceSequence.Append(rectTransform.DOScale(originalScale, duration * 0.3f));
        }
        
        public void Spin(float duration = 1f, int rotations = 1)
        {
            rectTransform.DORotate(new Vector3(0, 0, 360 * rotations), duration, RotateMode.FastBeyond360)
                .SetEase(Ease.InOutSine)
                .OnComplete(() => rectTransform.rotation = Quaternion.identity);
        }
        
        public void SetInteractable(bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
            
            // Update color
            if (image != null)
            {
                if (interactable)
                {
                    image.color = originalColor;
                }
                else
                {
                    image.color = disabledColor;
                }
            }
            
            // Stop animations if disabled
            if (!interactable)
            {
                StopPulseAnimation();
            }
        }
        
        bool IsInteractable()
        {
            if (button != null)
            {
                return button.interactable;
            }
            return true;
        }
        
        void PlayHoverSound()
        {
            // Play UI hover sound
            AudioManager.Instance?.PlayUISound("UI_Hover");
        }
        
        void PlayClickSound()
        {
            // Play UI click sound
            AudioManager.Instance?.PlayUISound("UI_Click");
        }
        
        void OnDestroy()
        {
            // Clean up DOTween sequences
            if (pulseSequence != null)
            {
                pulseSequence.Kill();
            }
        }
        
        // Utility method for quick animations
        public static void AnimateResourceGain(RectTransform target, int amount, ResourceType type)
        {
            if (UIManager.Instance == null) return;
            
            // Create floating text
            Vector3 screenPos = target.position;
            Color color = GetResourceColor(type);
            
            UIManager.Instance.CreateFloatingText(screenPos, $"+{amount}", color);
            
            // Bounce the resource icon
            UIAnimator animator = target.GetComponent<UIAnimator>();
            if (animator != null)
            {
                animator.Bounce();
            }
        }
        
        static Color GetResourceColor(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Credits: return Color.yellow;
                case ResourceType.Energy: return new Color(1, 0.5f, 0); // Orange
                case ResourceType.Nanites: return Color.green;
                case ResourceType.Data: return Color.cyan;
                case ResourceType.Influence: return Color.magenta;
                default: return Color.white;
            }
        }
    }
}