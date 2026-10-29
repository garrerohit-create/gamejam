using System;
using System.Collections.Generic;
using UnityEngine;

public enum SoundID
{
    // Player Combat
    SwordSwingLight1,
    SwordSwingLight2,
    SwordCharge,
    SwordSwingHeavy,
    HeavyImpact,
    ParryClash,
    FleshHit,
    FleshHitHeavy,
    BloodSplatter,
    GauntletStrike,

    // Player Movement & Health
    DodgeWhoosh,
    RollSlide,
    Jump,
    LandThud,
    HealCast,
    KnightHurt,

    // Boss Malakor
    BossRoar,
    BossAxeSlash,
    BossLightningSlam,
    BossShockwaveLaunch,
    BossHurt,
    BossDeath,

    // Environment
    GateUnlock,
    VictoryFanfare
}

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<AudioManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("[AudioManager]");
                    _instance = go.AddComponent<AudioManager>();
                }
            }
            return _instance;
        }
    }

    [System.Serializable]
    public struct SoundMapping
    {
        public SoundID id;
        public AudioClip clip;
        [Range(0f, 1f)] public float defaultVolume;
    }

    [Header("Sound Mappings")]
    [SerializeField] private List<SoundMapping> soundMappings = new List<SoundMapping>();

    [Header("Audio Pool")]
    [SerializeField] private int poolSize = 16;

    private readonly Dictionary<SoundID, (AudioClip clip, float volume)> soundDict = new Dictionary<SoundID, (AudioClip, float)>();
    private readonly List<AudioSource> audioPool = new List<AudioSource>();
    private int poolIndex = 0;

    private static readonly Dictionary<SoundID, string> AssetPaths = new Dictionary<SoundID, string>
    {
        { SoundID.SwordSwingLight1, "Assets/Audio/Combat/Sword_Swing_Light1.wav" },
        { SoundID.SwordSwingLight2, "Assets/Audio/Combat/Sword_Swing_Light2.wav" },
        { SoundID.SwordCharge, "Assets/Audio/Combat/Sword_Charge.wav" },
        { SoundID.SwordSwingHeavy, "Assets/Audio/Combat/Sword_Swing_Heavy.wav" },
        { SoundID.HeavyImpact, "Assets/Audio/Combat/Heavy_Impact.wav" },
        { SoundID.ParryClash, "Assets/Audio/Combat/Parry_Clash.wav" },
        { SoundID.FleshHit, "Assets/Audio/Combat/Flesh_Hit.wav" },
        { SoundID.FleshHitHeavy, "Assets/Audio/Combat/Flesh_Hit_Heavy.wav" },
        { SoundID.BloodSplatter, "Assets/Audio/Combat/Blood_Splatter.wav" },
        { SoundID.GauntletStrike, "Assets/Audio/Combat/Gauntlet_Strike.wav" },

        { SoundID.DodgeWhoosh, "Assets/Audio/Movement/Dodge_Whoosh.wav" },
        { SoundID.RollSlide, "Assets/Audio/Movement/Roll_Slide.wav" },
        { SoundID.Jump, "Assets/Audio/Movement/Jump.wav" },
        { SoundID.LandThud, "Assets/Audio/Movement/Land_Thud.wav" },
        { SoundID.HealCast, "Assets/Audio/Movement/Heal_Cast.wav" },
        { SoundID.KnightHurt, "Assets/Audio/Movement/Knight_Hurt.wav" },

        { SoundID.BossRoar, "Assets/Audio/Boss/Boss_Roar.wav" },
        { SoundID.BossAxeSlash, "Assets/Audio/Boss/Boss_Axe_Slash.wav" },
        { SoundID.BossLightningSlam, "Assets/Audio/Boss/Boss_Lightning_Slam.wav" },
        { SoundID.BossShockwaveLaunch, "Assets/Audio/Boss/Boss_Shockwave_Launch.wav" },
        { SoundID.BossHurt, "Assets/Audio/Boss/Boss_Hurt.wav" },
        { SoundID.BossDeath, "Assets/Audio/Boss/Boss_Death.wav" },

        { SoundID.GateUnlock, "Assets/Audio/Environment/Gate_Unlock.wav" },
        { SoundID.VictoryFanfare, "Assets/Audio/Environment/Victory_Fanfare.wav" }
    };

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        InitializePool();
        BuildSoundDictionary();
    }

    private void InitializePool()
    {
        if (audioPool.Count > 0) return;

        for (int i = 0; i < poolSize; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f; // 2D flat stereo sound
            audioPool.Add(src);
        }
    }

    private void BuildSoundDictionary()
    {
        soundDict.Clear();

        // 1. Load from serialized mappings if provided
        foreach (var mapping in soundMappings)
        {
            if (mapping.clip != null && !soundDict.ContainsKey(mapping.id))
            {
                soundDict[mapping.id] = (mapping.clip, mapping.defaultVolume > 0f ? mapping.defaultVolume : 1f);
            }
        }

        // 2. Fallback in Editor: dynamically load from AssetPaths for any missing clips
#if UNITY_EDITOR
        foreach (var pair in AssetPaths)
        {
            if (!soundDict.ContainsKey(pair.Key))
            {
                AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(pair.Value);
                if (clip != null)
                {
                    soundDict[pair.Key] = (clip, 1f);
                }
            }
        }
#endif
    }

    public static void Play(SoundID sound, float volumeMultiplier = 1f, float pitchVariation = 0.05f)
    {
        if (Instance == null) return;
        Instance.PlayInternal(sound, volumeMultiplier, pitchVariation);
    }

    public void PlayInternal(SoundID sound, float volumeMultiplier = 1f, float pitchVariation = 0.05f)
    {
        if (soundDict.Count == 0)
        {
            BuildSoundDictionary();
        }

        if (!soundDict.TryGetValue(sound, out var data) || data.clip == null)
        {
#if UNITY_EDITOR
            if (AssetPaths.TryGetValue(sound, out string path))
            {
                AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                {
                    data = (clip, 1f);
                    soundDict[sound] = data;
                }
            }
#endif
            if (data.clip == null) return;
        }

        if (audioPool.Count == 0)
        {
            InitializePool();
        }

        AudioSource src = GetNextAudioSource();
        if (src == null) return;

        src.clip = data.clip;
        src.volume = Mathf.Clamp01(data.volume * volumeMultiplier);
        src.pitch = 1.0f + UnityEngine.Random.Range(-pitchVariation, pitchVariation);
        src.Play();
    }

    private AudioSource GetNextAudioSource()
    {
        if (audioPool.Count == 0) return null;

        // First find an idle source
        for (int i = 0; i < audioPool.Count; i++)
        {
            if (!audioPool[i].isPlaying)
            {
                return audioPool[i];
            }
        }

        // Otherwise round-robin
        poolIndex = (poolIndex + 1) % audioPool.Count;
        return audioPool[poolIndex];
    }
}

