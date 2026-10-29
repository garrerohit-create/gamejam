using System.Collections;
using UnityEngine;

public class BleedWoundEffect : MonoBehaviour
{
    [Header("Bleed Visuals")]
    [SerializeField] private Sprite bloodSlashSprite;
    [SerializeField] private Sprite bloodDecalSprite;
    [SerializeField] private Sprite bloodDropletSprite;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.22f, 0.22f, 1f);
    [SerializeField] private float bleedDuration = 1.6f;
    [SerializeField] private float dripInterval = 0.22f;

    private SpriteRenderer spriteRenderer;
    private Color originalColor = Color.white;
    private Coroutine flashRoutine;
    private Coroutine bleedRoutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        LoadDefaultSprites();
    }

    private void LoadDefaultSprites()
    {
#if UNITY_EDITOR
        if (bloodSlashSprite == null)
            bloodSlashSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/BloodSlashArc.png");
        if (bloodDecalSprite == null)
            bloodDecalSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/BloodSplatDecal.png");
        if (bloodDropletSprite == null)
            bloodDropletSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/BloodDroplet.png");
#endif
    }

    public void TriggerBleed(Vector3 impactPoint, Vector2 hitDirection, bool isHeavy = false)
    {
        LoadDefaultSprites();

        AudioManager.Play(SoundID.BloodSplatter, isHeavy ? 1.0f : 0.8f);

        // 1. Animated Blood Slash Arc
        SpawnAnimatedBloodSlash(impactPoint, hitDirection, isHeavy);

        // 2. Visceral Trauma Flash on character
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(HitFlashRoutine(isHeavy ? 0.16f : 0.10f));

        // 3. Ongoing Bleeding Drips from wound
        if (bleedRoutine != null) StopCoroutine(bleedRoutine);
        bleedRoutine = StartCoroutine(BleedDripsRoutine(impactPoint, isHeavy ? bleedDuration * 1.3f : bleedDuration));
    }

    private void SpawnAnimatedBloodSlash(Vector3 position, Vector2 hitDirection, bool isHeavy)
    {
        var slashObj = new GameObject("BloodSlash_Anim");
        slashObj.transform.position = position;

        float angle = Mathf.Atan2(hitDirection.y, hitDirection.x) * Mathf.Rad2Deg;
        slashObj.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        var sr = slashObj.AddComponent<SpriteRenderer>();
        if (bloodSlashSprite != null) sr.sprite = bloodSlashSprite;
        sr.color = new Color(0.95f, 0.05f, 0.05f, 1f);
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 12;

        float initialScale = isHeavy ? 0.6f : 0.4f;
        float targetScale = isHeavy ? 1.6f : 1.1f;
        float animDuration = isHeavy ? 0.28f : 0.22f;

        StartCoroutine(AnimateSlash(slashObj, sr, initialScale, targetScale, animDuration));
    }

    private IEnumerator AnimateSlash(GameObject obj, SpriteRenderer sr, float startScale, float endScale, float duration)
    {
        float elapsed = 0f;
        obj.transform.localScale = Vector3.one * startScale;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            // Ease out expansion
            float scale = Mathf.Lerp(startScale, endScale, Mathf.Sin(t * Mathf.PI * 0.5f));
            obj.transform.localScale = Vector3.one * scale;

            // Fade out alpha
            if (sr != null)
            {
                float alpha = Mathf.Lerp(1f, 0f, t * t);
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }

    private IEnumerator HitFlashRoutine(float duration)
    {
        if (spriteRenderer == null) yield break;

        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(duration);
        spriteRenderer.color = originalColor;
        flashRoutine = null;
    }

    private IEnumerator BleedDripsRoutine(Vector3 woundPos, float totalDuration)
    {
        float elapsed = 0f;
        while (elapsed < totalDuration)
        {
            SpawnBloodDrip(transform.position + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(0.8f, 1.4f), 0f));
            elapsed += dripInterval;
            yield return new WaitForSeconds(dripInterval);
        }
        bleedRoutine = null;
    }

    private void SpawnBloodDrip(Vector3 startPos)
    {
        var drip = new GameObject("Blood_Drip");
        drip.transform.position = startPos;

        var sr = drip.AddComponent<SpriteRenderer>();
        if (bloodDropletSprite != null) sr.sprite = bloodDropletSprite;
        sr.color = new Color(0.65f, 0.02f, 0.02f, 0.9f);
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 9;
        drip.transform.localScale = Vector3.one * Random.Range(0.12f, 0.18f);

        StartCoroutine(DripFallRoutine(drip));
    }

    private IEnumerator DripFallRoutine(GameObject drip)
    {
        float velocityY = -0.5f;
        float gravity = 9.8f;
        float groundY = transform.position.y - 0.1f; // approximate foot level
        float elapsed = 0f;

        while (elapsed < 0.6f && drip != null && drip.transform.position.y > groundY)
        {
            velocityY -= gravity * Time.deltaTime;
            drip.transform.position += new Vector3(0f, velocityY * Time.deltaTime, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (drip != null)
        {
            Vector3 splatPos = drip.transform.position;
            if (Application.isPlaying) Destroy(drip);
            else DestroyImmediate(drip);

            SpawnGroundDecal(splatPos);
        }
    }

    private void SpawnGroundDecal(Vector3 pos)
    {
        var decal = new GameObject("Blood_GroundDecal");
        decal.transform.position = pos;
        decal.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        var sr = decal.AddComponent<SpriteRenderer>();
        if (bloodDecalSprite != null) sr.sprite = bloodDecalSprite;
        sr.color = new Color(0.55f, 0.02f, 0.02f, 0.85f);
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 1; // On top of ground platform, behind characters
        decal.transform.localScale = Vector3.one * Random.Range(0.25f, 0.40f);

        StartCoroutine(DecalFadeRoutine(decal, sr));
    }

    private IEnumerator DecalFadeRoutine(GameObject decal, SpriteRenderer sr)
    {
        yield return new WaitForSeconds(3.5f);
        float elapsed = 0f;
        float fadeTime = 1.5f;
        Color c = sr.color;

        while (elapsed < fadeTime && decal != null && sr != null)
        {
            float alpha = Mathf.Lerp(c.a, 0f, elapsed / fadeTime);
            sr.color = new Color(c.r, c.g, c.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (decal != null)
        {
            if (Application.isPlaying) Destroy(decal);
            else DestroyImmediate(decal);
        }
    }
}

