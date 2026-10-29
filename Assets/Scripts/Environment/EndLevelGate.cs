using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EndLevelGate : MonoBehaviour
{
    [Header("Visual Components")]
    [SerializeField] private GameObject sealedBarrierObj;
    [SerializeField] private GameObject activePortalObj;
    [SerializeField] private Collider2D solidBlockerCollider;
    [SerializeField] private Collider2D triggerPassageCollider;

    [Header("Gate State")]
    private bool isUnlocked = false;
    private bool hasPassedThrough = false;

    private void Awake()
    {
        BossAI.OnBossDefeated += UnlockGate;

        // Initial state: sealed
        if (sealedBarrierObj != null) sealedBarrierObj.SetActive(true);
        if (activePortalObj != null) activePortalObj.SetActive(false);
        if (solidBlockerCollider != null) solidBlockerCollider.enabled = true;
        if (triggerPassageCollider != null) triggerPassageCollider.enabled = false;
    }

    private void OnDestroy()
    {
        BossAI.OnBossDefeated -= UnlockGate;
    }

    public void UnlockGate()
    {
        if (isUnlocked) return;
        isUnlocked = true;

        if (solidBlockerCollider != null) solidBlockerCollider.enabled = false;
        StartCoroutine(OpenGateSequence());
    }

    private IEnumerator OpenGateSequence()
    {
        yield return new WaitForSeconds(1.5f);

        // Flash & screen shake
        AudioManager.Play(SoundID.GateUnlock, 1.0f);
        CameraShake.Shake(0.45f, 0.40f);

        if (sealedBarrierObj != null) sealedBarrierObj.SetActive(false);
        if (solidBlockerCollider != null) solidBlockerCollider.enabled = false;

        if (activePortalObj != null)
        {
            activePortalObj.SetActive(true);
            activePortalObj.transform.localScale = Vector3.zero;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 2.0f;
                activePortalObj.transform.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, t);
                yield return null;
            }
        }

        if (triggerPassageCollider != null) triggerPassageCollider.enabled = true;

        Debug.Log("END LEVEL GATE UNLOCKED! Passage is now open!");
        ShowGateBanner("GATEWAY UNLOCKED - STEP THROUGH TO ASCEND");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isUnlocked || hasPassedThrough) return;

        var km = collision.GetComponent<KnightMovement>();
        if (km == null) km = collision.GetComponentInParent<KnightMovement>();

        if (km != null)
        {
            hasPassedThrough = true;
            StartCoroutine(PassThroughGateSequence(km));
        }
    }

    private IEnumerator PassThroughGateSequence(KnightMovement knight)
    {
        Debug.Log("PLAYER PASSING THROUGH THE GATE!");

        // 1. Lock player combat/actions
        knight.enabled = false;
        var rb = knight.GetComponent<Rigidbody2D>();
        var anim = knight.GetComponent<Animator>();
        var sr = knight.GetComponent<SpriteRenderer>();

        if (anim != null) anim.SetBool("IsRunning", true);

        // 2. Walk knight into the center of the portal
        float targetX = transform.position.x;
        float elapsed = 0f;
        while (elapsed < 2.0f)
        {
            elapsed += Time.deltaTime;
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(2.5f, rb.linearVelocity.y);
            }

            // Fade knight into the light
            if (sr != null)
            {
                float a = Mathf.Lerp(1f, 0f, elapsed / 2.0f);
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, a);
            }
            yield return null;
        }

        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (anim != null) anim.SetBool("IsRunning", false);

        // 3. Trigger cinematic Victory UI
        ShowVictoryScreen();
    }

    private void ShowGateBanner(string msg)
    {
        var hud = FindAnyObjectByType<Canvas>();
        if (hud == null) return;

        var bannerObj = new GameObject("Gate_Prompt_Banner");
        bannerObj.transform.SetParent(hud.transform, false);

        var rect = bannerObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.75f);
        rect.anchorMax = new Vector2(0.5f, 0.75f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(600f, 40f);

        var txt = bannerObj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 20;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = new Color(1f, 0.85f, 0.4f, 1f);
        txt.text = msg;

        var outline = bannerObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);

        Destroy(bannerObj, 4.5f);
    }

    private void ShowVictoryScreen()
    {
        AudioManager.Play(SoundID.VictoryFanfare, 1.0f);
        var hud = FindAnyObjectByType<Canvas>();
        if (hud == null) return;

        var victoryPanel = new GameObject("Victory_Canvas_Panel");
        victoryPanel.transform.SetParent(hud.transform, false);

        var rect = victoryPanel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        var bg = victoryPanel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);

        // Title
        var titleObj = new GameObject("Victory_Title");
        titleObj.transform.SetParent(victoryPanel.transform, false);
        var tRect = titleObj.AddComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0.5f, 0.65f);
        tRect.anchorMax = new Vector2(0.5f, 0.65f);
        tRect.sizeDelta = new Vector2(700f, 80f);
        var titleText = titleObj.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 42;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(0.95f, 0.82f, 0.35f, 1f);
        titleText.text = "VICTORY ACHIEVED";
        var out1 = titleObj.AddComponent<Outline>();
        out1.effectColor = new Color(0.5f, 0.05f, 0.1f, 1f);
        out1.effectDistance = new Vector2(3f, -3f);

        // Subtitle
        var subObj = new GameObject("Victory_Subtitle");
        subObj.transform.SetParent(victoryPanel.transform, false);
        var sRect = subObj.AddComponent<RectTransform>();
        sRect.anchorMin = new Vector2(0.5f, 0.50f);
        sRect.anchorMax = new Vector2(0.5f, 0.50f);
        sRect.sizeDelta = new Vector2(700f, 50f);
        var subText = subObj.AddComponent<Text>();
        subText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        subText.fontSize = 22;
        subText.alignment = TextAnchor.MiddleCenter;
        subText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        subText.text = "The Abyssal Dreadlord has fallen.\nYou have passed beyond the Gates of the Underworld.";
        var out2 = subObj.AddComponent<Outline>();
        out2.effectColor = Color.black;
        out2.effectDistance = new Vector2(1.5f, -1.5f);
    }
}
