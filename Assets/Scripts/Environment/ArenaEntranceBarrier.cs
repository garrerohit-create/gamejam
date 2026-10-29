using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ArenaEntranceBarrier : MonoBehaviour
{
    private static ArenaEntranceBarrier instance;
    public static ArenaEntranceBarrier Instance => instance;

    [SerializeField] private SpriteRenderer barrierVisual;
    [SerializeField] private BoxCollider2D barrierCollider;
    private bool isSealed = false;

    private void Awake()
    {
        instance = this;
        if (barrierCollider == null) barrierCollider = GetComponent<BoxCollider2D>();
        if (barrierVisual == null) barrierVisual = GetComponent<SpriteRenderer>();

        // Default to inactive until boss encounter begins
        SetBarrierActive(false);

        BossAI.OnBossAwakened += HandleBossAwakened;
        BossAI.OnBossDefeated += HandleBossDefeated;
    }

    private void OnDestroy()
    {
        BossAI.OnBossAwakened -= HandleBossAwakened;
        BossAI.OnBossDefeated -= HandleBossDefeated;
    }

    private void HandleBossAwakened(BossAI boss)
    {
        SealBarrier();
    }

    private void HandleBossDefeated()
    {
        UnsealBarrier();
    }

    public void SealBarrier()
    {
        if (isSealed) return;
        isSealed = true;
        SetBarrierActive(true);
        StartCoroutine(SealSequence());
    }

    public void UnsealBarrier()
    {
        if (!isSealed) return;
        isSealed = false;
        StartCoroutine(UnsealSequence());
    }

    private void SetBarrierActive(bool active)
    {
        if (barrierCollider != null) barrierCollider.enabled = active;
        if (barrierVisual != null) barrierVisual.enabled = active;
    }

    private IEnumerator SealSequence()
    {
        if (barrierVisual != null)
        {
            barrierVisual.enabled = true;
            Color c = barrierVisual.color;
            for (float t = 0; t < 1f; t += Time.deltaTime * 3f)
            {
                c.a = Mathf.Lerp(0f, 0.85f, t);
                barrierVisual.color = c;
                yield return null;
            }
            c.a = 0.85f;
            barrierVisual.color = c;
        }

        if (barrierCollider != null) barrierCollider.enabled = true;
        AudioManager.Play(SoundID.GateUnlock, 0.85f);
        CameraShake.Shake(0.3f, 0.25f);
    }

    private IEnumerator UnsealSequence()
    {
        yield return new WaitForSeconds(1.0f);
        if (barrierVisual != null)
        {
            Color c = barrierVisual.color;
            for (float t = 0; t < 1f; t += Time.deltaTime * 1.5f)
            {
                c.a = Mathf.Lerp(0.85f, 0f, t);
                barrierVisual.color = c;
                yield return null;
            }
            barrierVisual.enabled = false;
        }

        if (barrierCollider != null) barrierCollider.enabled = false;
    }
}

