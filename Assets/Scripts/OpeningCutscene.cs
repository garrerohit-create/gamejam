using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

// Runs before the first scene's Start methods, in Editor and standalone builds.
public sealed class OpeningCutscene : MonoBehaviour
{
    private readonly List<GameObject> suspended = new List<GameObject>();
    private VideoPlayer video;
    private RenderTexture frame;
    private float startedAt;
    private bool finished;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (SceneManager.GetActiveScene().name != "SampleScene") return;
        new GameObject("Opening Cutscene").AddComponent<OpeningCutscene>();
    }

    private void Start()
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root != gameObject && root.activeSelf)
            {
                suspended.Add(root);
                root.SetActive(false);
            }
        }
        var camera = gameObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.cullingMask = 0;
        gameObject.AddComponent<AudioListener>();
        var canvasObject = new GameObject("Video Canvas", typeof(Canvas));
        canvasObject.transform.SetParent(transform);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        var picture = new GameObject("Picture", typeof(RawImage), typeof(AspectRatioFitter));
        picture.transform.SetParent(canvasObject.transform, false);
        var ratio = picture.GetComponent<AspectRatioFitter>();
        ratio.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        ratio.aspectRatio = 960f / 544f;
        frame = new RenderTexture(960, 544, 0);
        frame.Create();
        picture.GetComponent<RawImage>().texture = frame;
        video = gameObject.AddComponent<VideoPlayer>();
        video.playOnAwake = false;
        video.source = VideoSource.Url;
        video.url = Path.Combine(Application.streamingAssetsPath, "OpeningCutscene.mp4");
        video.renderMode = VideoRenderMode.RenderTexture;
        video.targetTexture = frame;
        video.audioOutputMode = VideoAudioOutputMode.None;
        video.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        video.isLooping = false;
        video.waitForFirstFrame = true;
        video.loopPointReached += OnFinished;
        video.errorReceived += OnError;
        video.prepareCompleted += OnPrepared;
        startedAt = Time.realtimeSinceStartup;
        video.Prepare();
    }

    private void OnPrepared(VideoPlayer source) { source.Play(); }
    private void OnFinished(VideoPlayer source) { Finish(); }
    private void OnError(VideoPlayer source, string message) { Debug.LogWarning("Opening video: " + message); Finish(); }
    private void Update()
    {
        if (!finished && video != null && video.isPrepared && !video.isPlaying) video.Play();
        if (Input.GetKeyDown(KeyCode.Escape) || Time.realtimeSinceStartup - startedAt > 35f) Finish();
    }
    private void Finish()
    {
        if (finished) return;
        finished = true;
        if (video != null) video.Stop();
        var listener = GetComponent<AudioListener>();
        if (listener != null) listener.enabled = false;
        foreach (var root in suspended) if (root != null) root.SetActive(true);
        Destroy(gameObject);
    }
    private void OnDestroy()
    {
        if (frame != null) { frame.Release(); Destroy(frame); }
    }
}
