using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthBarUI : MonoBehaviour
{
    private static BossHealthBarUI instance;
    public static BossHealthBarUI Instance => instance;

    private GameObject bossBarRoot;
    private Image fillBar;
    private Image ghostBar;
    private Text nameText;
    private Text hpText;

    private EnemyHealth boundHealth;
    private float targetFill = 1f;
    private float ghostDelayTimer = 0f;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        BossAI.OnBossAwakened += ShowBossBar;
        BossAI.OnBossDefeated += HandleBossDefeated;
        BossAI.OnBossPhaseChanged += HandlePhaseChanged;

        BuildUI();
    }

    private void OnDestroy()
    {
        BossAI.OnBossAwakened -= ShowBossBar;
        BossAI.OnBossDefeated -= HandleBossDefeated;
        BossAI.OnBossPhaseChanged -= HandlePhaseChanged;
    }

    private void BuildUI()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // Container anchored to Top Center
        bossBarRoot = new GameObject("Boss_HealthBar_Panel");
        bossBarRoot.transform.SetParent(canvas.transform, false);

        var rootRect = bossBarRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 1f);
        rootRect.anchorMax = new Vector2(0.5f, 1f);
        rootRect.pivot = new Vector2(0.5f, 1f);
        rootRect.anchoredPosition = new Vector2(0f, -25f);
        rootRect.sizeDelta = new Vector2(520f, 68f);

        // Dark gothic background frame
        var bgObj = new GameObject("Boss_Bar_BG");
        bgObj.transform.SetParent(bossBarRoot.transform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.05f, 0.08f, 0.92f);

        // Border outline
        var outlineObj = new GameObject("Boss_Bar_Border");
        outlineObj.transform.SetParent(bgObj.transform, false);
        var outRect = outlineObj.AddComponent<RectTransform>();
        outRect.anchorMin = Vector2.zero;
        outRect.anchorMax = Vector2.one;
        outRect.sizeDelta = Vector2.zero;
        var outOutline = outlineObj.AddComponent<Outline>();
        outOutline.effectColor = new Color(0.70f, 0.15f, 0.25f, 0.85f);
        outOutline.effectDistance = new Vector2(2f, -2f);

        // Delayed Ghost Bar (Yellow/White)
        var ghostObj = new GameObject("Boss_Bar_Ghost");
        ghostObj.transform.SetParent(bossBarRoot.transform, false);
        var gRect = ghostObj.AddComponent<RectTransform>();
        gRect.anchorMin = new Vector2(0.02f, 0.12f);
        gRect.anchorMax = new Vector2(0.98f, 0.50f);
        gRect.sizeDelta = Vector2.zero;
        ghostBar = ghostObj.AddComponent<Image>();
        ghostBar.color = new Color(0.95f, 0.85f, 0.35f, 0.90f);
        ghostBar.type = Image.Type.Filled;
        ghostBar.fillMethod = Image.FillMethod.Horizontal;
        ghostBar.fillAmount = 1f;

        // Primary Crimson Fill Bar
        var fillObj = new GameObject("Boss_Bar_Fill");
        fillObj.transform.SetParent(bossBarRoot.transform, false);
        var fRect = fillObj.AddComponent<RectTransform>();
        fRect.anchorMin = new Vector2(0.02f, 0.12f);
        fRect.anchorMax = new Vector2(0.98f, 0.50f);
        fRect.sizeDelta = Vector2.zero;
        fillBar = fillObj.AddComponent<Image>();
        fillBar.color = new Color(0.85f, 0.12f, 0.20f, 1f);
        fillBar.type = Image.Type.Filled;
        fillBar.fillMethod = Image.FillMethod.Horizontal;
        fillBar.fillAmount = 1f;

        // Boss Title Text
        var textObj = new GameObject("Boss_Name_Text");
        textObj.transform.SetParent(bossBarRoot.transform, false);
        var tRect = textObj.AddComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0f, 0.52f);
        tRect.anchorMax = new Vector2(1f, 1f);
        tRect.sizeDelta = Vector2.zero;
        nameText = textObj.AddComponent<Text>();
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameText.fontSize = 17;
        nameText.alignment = TextAnchor.MiddleCenter;
        nameText.color = new Color(0.95f, 0.85f, 0.65f, 1f);
        nameText.text = "THE INFERNAL EXECUTIONER — WARDEN OF THE DAMNED";
        var tOutline = textObj.AddComponent<Outline>();
        tOutline.effectColor = Color.black;
        tOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // HP Numerical Text
        var hpNumObj = new GameObject("Boss_HP_Text");
        hpNumObj.transform.SetParent(bossBarRoot.transform, false);
        var hpNumRect = hpNumObj.AddComponent<RectTransform>();
        hpNumRect.anchorMin = new Vector2(0.02f, 0.12f);
        hpNumRect.anchorMax = new Vector2(0.98f, 0.50f);
        hpNumRect.sizeDelta = Vector2.zero;
        hpText = hpNumObj.AddComponent<Text>();
        hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hpText.fontSize = 13;
        hpText.alignment = TextAnchor.MiddleCenter;
        hpText.color = Color.white;
        hpText.text = "60 / 60";
        var hpOut = hpNumObj.AddComponent<Outline>();
        hpOut.effectColor = Color.black;
        hpOut.effectDistance = new Vector2(1f, -1f);

        bossBarRoot.SetActive(false);
    }

    public void ShowBossBar(BossAI boss)
    {
        if (boss == null) return;
        if (bossBarRoot == null) BuildUI();
        if (bossBarRoot == null) return;

        boundHealth = boss.Health;
        if (nameText != null) nameText.text = boss.BossName;

        bossBarRoot.SetActive(true);
        if (boundHealth != null)
        {
            boundHealth.OnDamaged += OnBossDamaged;
            UpdateDisplay(boundHealth.CurrentHealth, boundHealth.MaxHealth);
        }

        StartCoroutine(IntroFillRoutine());
    }

    private IEnumerator IntroFillRoutine()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 1.5f;
            if (fillBar != null) fillBar.fillAmount = t;
            if (ghostBar != null) ghostBar.fillAmount = t;
            yield return null;
        }
        if (fillBar != null) fillBar.fillAmount = 1f;
        if (ghostBar != null) ghostBar.fillAmount = 1f;
    }

    private void OnBossDamaged(int dmg)
    {
        if (boundHealth == null) return;
        UpdateDisplay(boundHealth.CurrentHealth, boundHealth.MaxHealth);
    }

    private void UpdateDisplay(int cur, int max)
    {
        targetFill = Mathf.Clamp01((float)cur / max);
        if (fillBar != null) fillBar.fillAmount = targetFill;
        if (hpText != null) hpText.text = string.Format("{0} / {1}", Mathf.Max(0, cur), max);
        ghostDelayTimer = 0.45f;
    }

    private void Update()
    {
        if (ghostDelayTimer > 0f)
        {
            ghostDelayTimer -= Time.deltaTime;
        }
        else if (ghostBar != null && ghostBar.fillAmount > targetFill)
        {
            ghostBar.fillAmount = Mathf.MoveTowards(ghostBar.fillAmount, targetFill, Time.deltaTime * 0.4f);
        }
    }

    private void HandlePhaseChanged(BossAI boss, int phase)
    {
        if (phase == 2)
        {
            if (nameText != null)
            {
                nameText.text = "<color=#FF4444>PHASE 2: BLOOD AWAKENING</color>";
                nameText.fontSize = 19;
            }
            if (fillBar != null)
            {
                fillBar.color = new Color(1f, 0.22f, 0.22f, 1f);
            }
            StartCoroutine(FlashPhaseChangeRoutine(5, new Color(1f, 0.22f, 0.22f, 1f)));
        }
        else if (phase == 3)
        {
            if (nameText != null)
            {
                nameText.text = "<color=#FF1111>FINAL JUDGMENT: WARDEN OF THE DAMNED</color>";
                nameText.fontSize = 20;
            }
            if (fillBar != null)
            {
                fillBar.color = new Color(1f, 0.05f, 0.05f, 1f);
            }
            StartCoroutine(FlashPhaseChangeRoutine(7, new Color(1f, 0.05f, 0.05f, 1f)));
        }
    }

    private IEnumerator FlashPhaseChangeRoutine(int flashes, Color targetColor)
    {
        if (fillBar == null) yield break;
        for (int i = 0; i < flashes; i++)
        {
            fillBar.color = Color.white;
            yield return new WaitForSeconds(0.07f);
            fillBar.color = targetColor;
            yield return new WaitForSeconds(0.07f);
        }
    }

    private void HandleBossDefeated()
    {
        if (nameText != null)
        {
            nameText.text = "<color=#888888>THE INFERNAL EXECUTIONER HAS FALLEN</color>";
        }
        StartCoroutine(FadeOutBossBar());
    }

    private IEnumerator FadeOutBossBar()
    {
        yield return new WaitForSeconds(3.5f);
        if (bossBarRoot != null)
        {
            bossBarRoot.SetActive(false);
        }
    }
}
