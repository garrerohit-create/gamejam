using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Health Bar UI Elements")]
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Image ghostFillImage;
    [SerializeField] private Text healthText;
    [SerializeField] private float ghostDrainSpeed = 1.8f;

    [Header("Heal Flasks UI Elements")]
    [SerializeField] private Text healsCounterText;
    [SerializeField] private List<Image> flaskPipImages = new List<Image>();
    [SerializeField] private Color flaskActiveColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color flaskDepletedColor = new Color(0.35f, 0.35f, 0.4f, 0.4f);

    private float targetFill = 1f;
    private float ghostFill = 1f;

    private void Awake()
    {
        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();
        }
    }

    private void Start()
    {
        if (playerHealth != null)
        {
            UpdateHealthDisplay(playerHealth.CurrentHealth, playerHealth.MaxHealth, immediate: true);
            UpdateHealsDisplay(playerHealth.CurrentHeals, playerHealth.MaxHeals);
        }
    }

    private void OnEnable()
    {
        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += HandleHealthChanged;
            playerHealth.OnHealsChanged += HandleHealsChanged;
            UpdateHealthDisplay(playerHealth.CurrentHealth, playerHealth.MaxHealth, immediate: true);
            UpdateHealsDisplay(playerHealth.CurrentHeals, playerHealth.MaxHeals);
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= HandleHealthChanged;
            playerHealth.OnHealsChanged -= HandleHealsChanged;
        }
    }

    private void Update()
    {
        // Smoothly drain ghost fill bar behind main health fill
        if (ghostFillImage != null && ghostFill > targetFill)
        {
            ghostFill = Mathf.MoveTowards(ghostFill, targetFill, Time.deltaTime * ghostDrainSpeed);
            ghostFillImage.fillAmount = ghostFill;
        }
        else if (ghostFillImage != null && ghostFill < targetFill)
        {
            ghostFill = targetFill;
            ghostFillImage.fillAmount = ghostFill;
        }
    }

    private void HandleHealthChanged(int current, int max)
    {
        UpdateHealthDisplay(current, max, immediate: false);
    }

    private void HandleHealsChanged(int currentHeals, int maxHeals)
    {
        UpdateHealsDisplay(currentHeals, maxHeals);
    }

    public void UpdateHealthDisplay(int current, int max, bool immediate = false)
    {
        targetFill = Mathf.Clamp01((float)current / Mathf.Max(1, max));

        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = targetFill;
        }

        if (immediate && ghostFillImage != null)
        {
            ghostFill = targetFill;
            ghostFillImage.fillAmount = targetFill;
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.Max(0, current)} / {max} HP";
        }
    }

    public void UpdateHealsDisplay(int currentHeals, int maxHeals)
    {
        if (healsCounterText != null)
        {
            healsCounterText.text = $"[Q] Heals: {currentHeals}/{maxHeals}";
        }

        for (int i = 0; i < flaskPipImages.Count; i++)
        {
            if (flaskPipImages[i] != null)
            {
                flaskPipImages[i].color = (i < currentHeals) ? flaskActiveColor : flaskDepletedColor;
            }
        }
    }

    public void SetFlaskPips(List<Image> pips)
    {
        flaskPipImages = pips;
    }
}

