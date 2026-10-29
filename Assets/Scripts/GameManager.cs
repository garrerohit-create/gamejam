using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance => instance;

    [Header("Respawn & Spawn")]
    [SerializeField] private Vector3 initialSpawnPos = new Vector3(-1.5f, 0.8f, 0f);

    private GameObject deathOverlayObj;
    private GameObject pauseOverlayObj;
    private bool isPaused = false;
    private bool isPlayerDead = false;

    private PlayerHealth playerHealth;
    private KnightMovement knightMovement;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        playerHealth = FindAnyObjectByType<PlayerHealth>();
        knightMovement = FindAnyObjectByType<KnightMovement>();

        if (playerHealth != null)
        {
            playerHealth.OnPlayerDied += HandlePlayerDeath;
        }

        BuildUI();
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDied -= HandlePlayerDeath;
        }
    }

    private void Update()
    {
        // 1. Respawn input on death
        if (isPlayerDead)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                RespawnPlayer();
            }
            return;
        }

        // 2. Pause toggle
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        // 3. Restart input in pause menu
        if (isPaused && Input.GetKeyDown(KeyCode.R))
        {
            TogglePause();
            RespawnPlayer();
        }
    }

    private void BuildUI()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // --- DEATH OVERLAY ---
        deathOverlayObj = new GameObject("Player_Death_Overlay");
        deathOverlayObj.transform.SetParent(canvas.transform, false);

        var dRect = deathOverlayObj.AddComponent<RectTransform>();
        dRect.anchorMin = Vector2.zero;
        dRect.anchorMax = Vector2.one;
        dRect.sizeDelta = Vector2.zero;

        var dImg = deathOverlayObj.AddComponent<Image>();
        dImg.color = new Color(0.05f, 0f, 0f, 0.88f);

        // Title
        var dTitle = new GameObject("Death_Title");
        dTitle.transform.SetParent(deathOverlayObj.transform, false);
        var dtRect = dTitle.AddComponent<RectTransform>();
        dtRect.anchorMin = new Vector2(0.5f, 0.60f);
        dtRect.anchorMax = new Vector2(0.5f, 0.60f);
        dtRect.sizeDelta = new Vector2(650f, 70f);
        var dtText = dTitle.AddComponent<Text>();
        dtText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        dtText.fontSize = 46;
        dtText.alignment = TextAnchor.MiddleCenter;
        dtText.color = new Color(0.95f, 0.15f, 0.15f, 1f);
        dtText.text = "YOU PERISHED";
        var dtOut = dTitle.AddComponent<Outline>();
        dtOut.effectColor = Color.black;
        dtOut.effectDistance = new Vector2(2f, -2f);

        // Subtitle prompt
        var dSub = new GameObject("Death_Prompt");
        dSub.transform.SetParent(deathOverlayObj.transform, false);
        var dsRect = dSub.AddComponent<RectTransform>();
        dsRect.anchorMin = new Vector2(0.5f, 0.44f);
        dsRect.anchorMax = new Vector2(0.5f, 0.44f);
        dsRect.sizeDelta = new Vector2(500f, 40f);
        var dsText = dSub.AddComponent<Text>();
        dsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        dsText.fontSize = 20;
        dsText.alignment = TextAnchor.MiddleCenter;
        dsText.color = new Color(0.90f, 0.85f, 0.70f, 1f);
        dsText.text = "Press [R] to Respawn at Checkpoint";

        deathOverlayObj.SetActive(false);

        // --- PAUSE OVERLAY ---
        pauseOverlayObj = new GameObject("Pause_Menu_Overlay");
        pauseOverlayObj.transform.SetParent(canvas.transform, false);

        var pRect = pauseOverlayObj.AddComponent<RectTransform>();
        pRect.anchorMin = Vector2.zero;
        pRect.anchorMax = Vector2.one;
        pRect.sizeDelta = Vector2.zero;

        var pImg = pauseOverlayObj.AddComponent<Image>();
        pImg.color = new Color(0.04f, 0.04f, 0.06f, 0.90f);

        // Title
        var pTitle = new GameObject("Pause_Title");
        pTitle.transform.SetParent(pauseOverlayObj.transform, false);
        var ptRect = pTitle.AddComponent<RectTransform>();
        ptRect.anchorMin = new Vector2(0.5f, 0.78f);
        ptRect.anchorMax = new Vector2(0.5f, 0.78f);
        ptRect.sizeDelta = new Vector2(400f, 60f);
        var ptText = pTitle.AddComponent<Text>();
        ptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ptText.fontSize = 38;
        ptText.alignment = TextAnchor.MiddleCenter;
        ptText.color = new Color(0.95f, 0.85f, 0.65f, 1f);
        ptText.text = "GAME PAUSED";

        // Controls Guide
        var pGuide = new GameObject("Pause_Controls_Guide");
        pGuide.transform.SetParent(pauseOverlayObj.transform, false);
        var pgRect = pGuide.AddComponent<RectTransform>();
        pgRect.anchorMin = new Vector2(0.5f, 0.48f);
        pgRect.anchorMax = new Vector2(0.5f, 0.48f);
        pgRect.sizeDelta = new Vector2(600f, 220f);
        var pgText = pGuide.AddComponent<Text>();
        pgText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        pgText.fontSize = 18;
        pgText.alignment = TextAnchor.MiddleCenter;
        pgText.lineSpacing = 1.3f;
        pgText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        pgText.text = "<b>CONTROLS</b>\n" +
                      "[A] / [D] or Arrows : Move\n" +
                      "[Space] : Jump (Variable Height)\n" +
                      "[Mouse 0] : Light Attack 2-Hit Combo\n" +
                      "[Mouse 1] : Heavy Attack (Hold to Charge)\n" +
                      "[Shift] : Dodge Dash | [Ctrl] : Combat Roll\n" +
                      "[E] : Parry incoming strike\n" +
                      "[Q] : Drink Healing Flask";

        // Footer buttons prompt
        var pFoot = new GameObject("Pause_Footer_Prompt");
        pFoot.transform.SetParent(pauseOverlayObj.transform, false);
        var pfRect = pFoot.AddComponent<RectTransform>();
        pfRect.anchorMin = new Vector2(0.5f, 0.22f);
        pfRect.anchorMax = new Vector2(0.5f, 0.22f);
        pfRect.sizeDelta = new Vector2(500f, 40f);
        var pfText = pFoot.AddComponent<Text>();
        pfText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        pfText.fontSize = 18;
        pfText.alignment = TextAnchor.MiddleCenter;
        pfText.color = new Color(0.95f, 0.80f, 0.35f, 1f);
        pfText.text = "[Escape] Resume Game  |  [R] Restart Checkpoint";

        pauseOverlayObj.SetActive(false);
    }

    private void HandlePlayerDeath()
    {
        isPlayerDead = true;

        if (knightMovement != null)
        {
            knightMovement.enabled = false;
            var rb = knightMovement.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        AudioManager.Play(SoundID.KnightHurt, 1.2f);
        CameraShake.Shake(0.5f, 0.4f);

        StartCoroutine(ShowDeathScreenRoutine());
    }

    private IEnumerator ShowDeathScreenRoutine()
    {
        yield return new WaitForSeconds(0.6f);
        if (deathOverlayObj != null)
        {
            deathOverlayObj.SetActive(true);
        }
    }

    public void RespawnPlayer()
    {
        isPlayerDead = false;

        if (deathOverlayObj != null)
            deathOverlayObj.SetActive(false);

        if (pauseOverlayObj != null)
            pauseOverlayObj.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;

        if (knightMovement == null)
            knightMovement = FindAnyObjectByType<KnightMovement>();

        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<PlayerHealth>();

        // Find active checkpoint or default start
        Vector3 spawnTarget = FallChasmTrigger.GetCurrentCheckpoint(initialSpawnPos);

        if (knightMovement != null)
        {
            knightMovement.enabled = true;
            knightMovement.transform.position = spawnTarget;
            var rb = knightMovement.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            knightMovement.ReturnToLocomotion();
        }

        if (playerHealth != null)
        {
            playerHealth.RespawnPlayer();
        }

        Debug.Log("Player Respawned cleanly at: " + spawnTarget);
    }

    public void TogglePause()
    {
        if (isPlayerDead) return;

        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;

        if (pauseOverlayObj != null)
        {
            pauseOverlayObj.SetActive(isPaused);
        }
    }
}

