using System;
using UnityEngine;


/// <summary>
/// Global audio manager (Singleton).
/// Responsible for:
/// - Playing background music and sound effects
/// - Persisting volume settings using PlayerPrefs
/// - Synchronizing audio state across scenes
/// </summary>
public class SoundManager : MonoBehaviour
{
    // Singleton instance to ensure only one SoundManager exists across scenes
    private static SoundManager instance;
    public static SoundManager Instance {  get { return instance; } }

    // AudioSource for one-shot sound effects (UI clicks, pickups, events)
    public AudioSource soundEffect;
    // AudioSource dedicated to background music
    public AudioSource soundMusic;

    public SoundType[] sounds;

    // Stores current volume values (0–1 range)
    // These act as the single source of truth for UI sliders
    public float musicVolume = 1.0f;
    public float effectVolume = 1.0f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Load saved volume values and immediately apply them to AudioSources
    // Ensures audio settings persist across sessions
    void Start()
    {
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", musicVolume);
        effectVolume = PlayerPrefs.GetFloat("EffectVolume", effectVolume);

        SetMusicVolume(musicVolume);
        SetEffectVolume(effectVolume);

        PlayMusic(global::Sounds.Music);
    }

    // Sets and persists music volume
    // Volume is clamped to avoid invalid values
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        soundMusic.volume = musicVolume;
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.Save();
    }

    // Sets and persists sound effect volume
    // Volume is clamped to avoid invalid values
    public void SetEffectVolume(float volume)
    {
        effectVolume = Mathf.Clamp01(volume);
        soundEffect.volume = effectVolume;
        PlayerPrefs.SetFloat("EffectVolume", effectVolume);
        PlayerPrefs.Save();
    }

    // Returns current music volume for UI synchronization
    public float GetMusicVolume()
    {
        return musicVolume;
    }

    // Returns current effect volume for UI synchronization
    public float GetEffectVolume()
    {
        return effectVolume;
    }

    // Plays looping background music
    // Called once on startup
    public void PlayMusic(Sounds sound)
    {
        AudioClip clip = GetSoundClip(sound);
        if (clip != null)
        {
            soundMusic.clip = clip;
            soundMusic.loop = true;
            soundMusic.Play();
        }
        else
        {
            Debug.Log("Clip not found for sound type: " +  sound);
        }
    }


    // Plays one-shot sound effects without interrupting others
    public void Play(Sounds sound)
    {
        AudioClip clip = GetSoundClip(sound);
        if (clip != null)
        {
            soundEffect.PlayOneShot(clip);
        }
        else
        {
            Debug.Log("Clip not found for sound type:" + sound);
        }
    }


    private AudioClip GetSoundClip(Sounds sound)
    {
        SoundType item = Array.Find(sounds, i => i.soundType ==  sound);
        if (item != null)
            return item.soundClip;
        return null;
    }
}


[Serializable]
public class SoundType
{
    public Sounds soundType;
    public AudioClip soundClip;
}

public enum Sounds
{
    ButtonClick, 
    Music,
    PlayerDeath,
    FoodPickUP,
    PoisonPickUP,
    ShieldPickUP,
    ScoreBoosterPickUP,
    SpeedBoosterPickUP,
    ShieldBlock,
    Win
}
