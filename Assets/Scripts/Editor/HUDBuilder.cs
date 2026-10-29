using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class HUDBuilder
{
    private const string EFFECTS_PATH = "Assets/Art/Effects/";

    [MenuItem("Tools/Build Player Health & Flask HUD")]
    public static void BuildHUD()
    {
        var oldCanvas = GameObject.Find("Player_HUD_Canvas");
        if (oldCanvas != null) Object.DestroyImmediate(oldCanvas);

        var canvasObj = new GameObject("Player_HUD_Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Root HUD Panel
        var hudRoot = new GameObject("HUD_Panel");
        hudRoot.transform.SetParent(canvasObj.transform, false);
        var hudRect = hudRoot.AddComponent<RectTransform>();
        hudRect.anchorMin = new Vector2(0f, 1f);
        hudRect.anchorMax = new Vector2(0f, 1f);
        hudRect.pivot = new Vector2(0f, 1f);
        hudRect.anchoredPosition = new Vector2(36f, -32f);
        hudRect.sizeDelta = new Vector2(450f, 130f);

        var borderSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EFFECTS_PATH + "HealthBar_Border.png");
        var fillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EFFECTS_PATH + "HealthBar_Fill.png");
        var flaskSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EFFECTS_PATH + "Flask_Icon.png");

        // 1. Health Bar Border
        var borderObj = new GameObject("HealthBar_Border");
        borderObj.transform.SetParent(hudRoot.transform, false);
        var borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = new Vector2(0f, 1f);
        borderRect.anchorMax = new Vector2(0f, 1f);
        borderRect.pivot = new Vector2(0f, 1f);
        borderRect.anchoredPosition = new Vector2(0f, 0f);
        borderRect.sizeDelta = new Vector2(400f, 44f);
        var borderImg = borderObj.AddComponent<Image>();
        borderImg.sprite = borderSprite;

        // 2. Ghost Fill (Amber delayed bar)
        var ghostObj = new GameObject("HealthBar_Ghost");
        ghostObj.transform.SetParent(borderObj.transform, false);
        var ghostRect = ghostObj.AddComponent<RectTransform>();
        ghostRect.anchorMin = new Vector2(0f, 0.5f);
        ghostRect.anchorMax = new Vector2(0f, 0.5f);
        ghostRect.pivot = new Vector2(0f, 0.5f);
        ghostRect.anchoredPosition = new Vector2(8f, 0f);
        ghostRect.sizeDelta = new Vector2(384f, 28f);
        var ghostImg = ghostObj.AddComponent<Image>();
        ghostImg.color = new Color(0.95f, 0.5f, 0.1f, 0.85f);
        ghostImg.type = Image.Type.Filled;
        ghostImg.fillMethod = Image.FillMethod.Horizontal;
        ghostImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        ghostImg.fillAmount = 1f;

        // 3. Main Health Fill (Red gradient)
        var fillObj = new GameObject("HealthBar_Fill");
        fillObj.transform.SetParent(borderObj.transform, false);
        var fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0.5f);
        fillRect.anchorMax = new Vector2(0f, 0.5f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = new Vector2(8f, 0f);
        fillRect.sizeDelta = new Vector2(384f, 28f);
        var fillImg = fillObj.AddComponent<Image>();
        fillImg.sprite = fillSprite;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImg.fillAmount = 1f;

        // 4. Health Text
        var textObj = new GameObject("Health_Text");
        textObj.transform.SetParent(borderObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = Vector2.zero;
        var text = textObj.AddComponent<Text>();
        text.text = "10 / 10 HP";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 20;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        var shadow = textObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);

        // 5. Flask Container (Below health bar)
        var flaskContainer = new GameObject("Flask_Container");
        flaskContainer.transform.SetParent(hudRoot.transform, false);
        var flaskContainerRect = flaskContainer.AddComponent<RectTransform>();
        flaskContainerRect.anchorMin = new Vector2(0f, 1f);
        flaskContainerRect.anchorMax = new Vector2(0f, 1f);
        flaskContainerRect.pivot = new Vector2(0f, 1f);
        flaskContainerRect.anchoredPosition = new Vector2(4f, -50f);
        flaskContainerRect.sizeDelta = new Vector2(380f, 40f);

        // Big Flask Icon
        var iconObj = new GameObject("Main_Flask_Icon");
        iconObj.transform.SetParent(flaskContainer.transform, false);
        var iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(0f, 0f);
        iconRect.sizeDelta = new Vector2(34f, 34f);
        var iconImg = iconObj.AddComponent<Image>();
        iconImg.sprite = flaskSprite;

        // Flask Counter Text: "[Q] Heals: 4/4"
        var counterObj = new GameObject("Heals_Counter_Text");
        counterObj.transform.SetParent(flaskContainer.transform, false);
        var counterRect = counterObj.AddComponent<RectTransform>();
        counterRect.anchorMin = new Vector2(0f, 0.5f);
        counterRect.anchorMax = new Vector2(0f, 0.5f);
        counterRect.pivot = new Vector2(0f, 0.5f);
        counterRect.anchoredPosition = new Vector2(42f, 0f);
        counterRect.sizeDelta = new Vector2(160f, 32f);
        var counterText = counterObj.AddComponent<Text>();
        counterText.text = "[Q] Heals: 4/4";
        counterText.font = text.font;
        counterText.fontSize = 20;
        counterText.fontStyle = FontStyle.Bold;
        counterText.alignment = TextAnchor.MiddleLeft;
        counterText.color = new Color(0.95f, 0.9f, 0.8f);
        var counterShadow = counterObj.AddComponent<Shadow>();
        counterShadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
        counterShadow.effectDistance = new Vector2(1.5f, -1.5f);

        // 4 Flask Pips
        var pips = new List<Image>();
        for (int i = 0; i < 4; i++)
        {
            var pipObj = new GameObject("Flask_Pip_" + (i + 1));
            pipObj.transform.SetParent(flaskContainer.transform, false);
            var pipRect = pipObj.AddComponent<RectTransform>();
            pipRect.anchorMin = new Vector2(0f, 0.5f);
            pipRect.anchorMax = new Vector2(0f, 0.5f);
            pipRect.pivot = new Vector2(0f, 0.5f);
            pipRect.anchoredPosition = new Vector2(210f + (i * 30f), 0f);
            pipRect.sizeDelta = new Vector2(26f, 26f);
            var pipImg = pipObj.AddComponent<Image>();
            pipImg.sprite = flaskSprite;
            pipImg.color = Color.white;
            pips.Add(pipImg);
        }

        // 6. Connect PlayerHealthUI Component
        var healthUI = hudRoot.AddComponent<PlayerHealthUI>();
        var knight = GameObject.Find("Knight");
        var pHealth = knight != null ? knight.GetComponent<PlayerHealth>() : null;

        var fTarget = typeof(PlayerHealthUI).GetField("playerHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (fTarget != null) fTarget.SetValue(healthUI, pHealth);

        var fFill = typeof(PlayerHealthUI).GetField("healthFillImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (fFill != null) fFill.SetValue(healthUI, fillImg);

        var fGhost = typeof(PlayerHealthUI).GetField("ghostFillImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (fGhost != null) fGhost.SetValue(healthUI, ghostImg);

        var fText = typeof(PlayerHealthUI).GetField("healthText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (fText != null) fText.SetValue(healthUI, text);

        var fCounter = typeof(PlayerHealthUI).GetField("healsCounterText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (fCounter != null) fCounter.SetValue(healthUI, counterText);

        healthUI.SetFlaskPips(pips);

        EditorUtility.SetDirty(canvasObj);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("HUD Canvas built and wired successfully!");
    }
}

