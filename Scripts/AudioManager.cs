using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// AudioManager singleton that manages all audio in the game
///
/// Made by Jacky Yang, 
/// 
/// </summary>
public class AudioManager : MonoBehaviour {

    public enum Sound {
        Count
    }

    public enum Music {
        None,
        Count
    }

    /// <summary>
    /// From 0 (least important) to 255 (most important)
    /// </summary>
    public enum Priority
    {
        Music = 0,
        Low = 64,
        Default = 128,
        High = 192,
        Spam = 256
    }

    public enum Pitch
    {
        None,
        VeryLow,
        Low,
        Medium,
        High
    }

    /// <summary>
    /// Defined as a class with predefined static values to use as an enum of floats
    /// </summary>
    public class Pitches
    {
        public static float None = 0;
        public static float VeryLow = 0.05f;
        public static float Low = 0.15f;
        public static float Medium = 0.25f;
        public static float High = 0.5f;
    }

    /// <summary>
    /// Number of audiosources AKA sources
    /// </summary>
    [SerializeField]
    [Tooltip("Number of audiosources AKA sources")]
    int numSources = 16;

    /// <summary>
    /// Sound library, add more through the inspector window
    /// </summary>
    [SerializeField]
    [Tooltip("Track library, add more through the inspector window")]
    List<AudioClip> clips;

    /// <summary>
    /// Music library, add more through the inspector window
    /// </summary>
    [SerializeField]
    [Tooltip("Music library, these will loop by default, add more through the inspector window if necessary")]
    List<AudioClip> tracks;

    /// <summary>
    /// Music introduction library, add more through the inspector window if necessary
    /// </summary>
    [SerializeField]
    [Tooltip("Music introduction library, add more through the inspector window if necessary")]
    List<AudioClip> trackIntros;

    /// <summary>
    /// List of sources allocated to play looping sounds
    /// </summary>
    [SerializeField]
    [Tooltip("List of sources allocated to play looping sounds")]
    List<AudioSource> loopingSources;

    [SerializeField]
    [Tooltip("[DON'T TOUCH THIS], looping sound positions")]
    Transform[] sourcePositions;

    /// <summary>
    /// Limits the number of each sounds being played. If at 0 or no value, assume infinite
    /// </summary>
    [SerializeField]
    [Tooltip("Limits the number of each sounds being played. If at 0 or no value, assume infinite")]
    int[] exclusiveList;

    AudioSource[] sources;

    /// <summary>
    /// One sources dedicated to playing music
    /// </summary>
    AudioSource musicSource;

    [SerializeField]
    [Range(0, 1)]
    float soundVolume = 1;

    [SerializeField]
    [Range(0, 1)]
    float musicVolume = 1;

    [SerializeField]
    bool spatialSound = true;

    public static AudioManager Singleton;

    /// <summary>
    /// Only used if you have super special music with an intro portion that plays only once
    /// </summary>
    bool queueIntro;

    /// <summary>
    /// Current music that's playing
    /// </summary>
    [SerializeField]
    [Tooltip("Current music that's playing")]
    Music currentTrack;

    [SerializeField]
    AudioListener listener;

    [SerializeField]
    GameObject sourcePrefab;

    bool doneLoading;

    // Use this for initialization
    void Awake () {
        if (Singleton == null)
        {
            Singleton = this;
        }
        else
            Destroy(gameObject);

        //AudioManager is important, keep it between scenes
        DontDestroyOnLoad(gameObject);

        sources = new AudioSource[numSources];
        sourcePositions = new Transform[numSources];

        for (int i = 0; i < numSources; i++)
        {
            sources[i] = Instantiate(sourcePrefab, transform).GetComponent<AudioSource>();
        }

        //Subscribes itself to the sceneLoaded notifier
        SceneManager.sceneLoaded += OnSceneLoaded;

        //Get a reference to all our audiosources on startup
        sources = GetComponentsInChildren<AudioSource>();

        GameObject m = new GameObject("MusicSource");
        m.transform.parent = transform;
        m.AddComponent<AudioSource>();
        musicSource = m.GetComponent<AudioSource>();
        musicSource.priority = (int)Priority.Music;

        //Set sources properties based on current settings
        SetSoundVolume(soundVolume);
        SetMusicVolume(musicVolume);
        SetSpatialSound(spatialSound);

        // Find the listener if not manually set
        FindNewListener();

        PlayMusic(currentTrack);

        doneLoading = true;
    }

    void FindNewListener()
    {
        if (listener == null)
        {
            listener = Camera.main.GetComponent<AudioListener>();
            if (listener == null) // In the case that there still isn't an AudioListener
            {
                listener = gameObject.AddComponent<AudioListener>();
                print("AudioManager Warning: Scene is missing an AudioListener! Mark the listener with the \"Main Camera\" tag!");
            }
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindNewListener();
        StopSoundLoopAll(true);
    }

    /// <summary>
    /// Set whether or not sounds are 2D or 3D (spatial)
    /// </summary>
    /// <param name="b">Enable spatial sound if true</param>
    public void SetSpatialSound(bool b)
    {
        spatialSound = b;
        float val = (b) ? 1 : 0;
        foreach (AudioSource s in sources)
        {
            s.spatialBlend = val;
        }
    }

    /// <summary>
    /// Swaps the current music track with the new music track
    /// </summary>
    /// <param name="m">Index of the music</param>
    /// <param name="hasIntro">Does the clip have an intro portion that plays only once?</param>
    public void PlayMusic(Music m, bool hasIntro = false)
    {
        if (m == Music.None) return;
        currentTrack = m;
        m--; //Offset for the code to follow
        if (hasIntro)
        {
            musicSource.clip = trackIntros[(int)m];
            musicSource.loop = false;
        }
        else
        {
            musicSource.clip = tracks[(int)m];
            musicSource.loop = true;
        }
        queueIntro = hasIntro;

        musicSource.Play();
    }

    /// <summary>
    /// Stop whatever is playing in musicSource
    /// </summary>
    public void StopMusic()
    {
        musicSource.Stop();
        currentTrack = Music.None;
    }

    private void Update()
    {
        if (queueIntro) //If we're playing the intro to a track
        {
            if (!musicSource.isPlaying) //Returns true the moment the intro ends
            {
                //Swap to the main loop
                musicSource.clip = tracks[(int)currentTrack];
                musicSource.loop = true;
                musicSource.Play();
                queueIntro = false;
            }
        }

        if (spatialSound) // Only do this part if we have 3D sound enabled
        {
            for (int i = 0; i < numSources; i++) // Search every sources
            {
                if (sourcePositions[i] != null) // If there's a designated location
                {
                    sources[i].transform.position = sourcePositions[i].transform.position;
                }
                if (!sources[i].isPlaying) // However if it's not playing a sound
                {
                    sourcePositions[i] = null; // Erase the designated transform so we don't check again
                }
            }
        }
    }

    /// <summary>
    /// Equivalent of PlayOneShot
    /// </summary>
    /// <param name="s"></param>
    /// <param name="trans">The transform of the sound's source</param>
    /// <param name="p">The priority of the sound</param>
    /// <param name="pitchShift">If not None, randomizes the pitch of the sound, use AudioManager.Pitches for presets</param>
    /// <param name="delay">Amount of seconds to wait before playing the sound</param>
    public void PlaySoundOnce(Sound s, Transform trans = null, Priority p = Priority.Default, float pitchShift = 0, float delay = 0)
    {
        AudioSource a = GetAvailableSource();

        if (trans != null)
        {
            sourcePositions[Array.IndexOf(sources, a)] = trans;
            if (spatialSound)
            {
                a.spatialBlend = 1;
            }
        }
        else
        {
            a.transform.position = listener.transform.position;
            if (spatialSound)
            {
                a.spatialBlend = 0;
            }
        }
        
        //This is the base unchanged pitch
        if (pitchShift > Pitches.None)
        {
            a.pitch = 1 + UnityEngine.Random.Range(-pitchShift, pitchShift);
        }
        else
        {
            a.pitch = 1;
        }

        a.clip = clips[(int)s];
        a.priority = (int)p;
        a.PlayDelayed(delay);
    }

    /// <summary>
    /// Equivalent of PlayOneShot
    /// </summary>
    /// <param name="s"></param>
    /// <param name="trans">The transform of the sound's source</param>
    /// <param name="p">The priority of the sound</param>
    /// <param name="pitchShift">If not None, randomizes the pitch of the sound, use AudioManager.Pitches for presets</param>
    /// <param name="delay">Amount of seconds to wait before playing the sound</param>
    public void PlaySoundOnce(AudioClip s, Transform trans = null, Priority p = Priority.Default, float pitchShift = 0, float delay = 0)
    {
        AudioSource a = GetAvailableSource();

        if (trans != null)
        {
            sourcePositions[Array.IndexOf(sources, a)] = trans;
            if (spatialSound)
            {
                a.spatialBlend = 1;
            }
        }
        else
        {
            a.transform.position = listener.transform.position;
            if (spatialSound)
            {
                a.spatialBlend = 0;
            }
        }

        //This is the base unchanged pitch
        if (pitchShift > Pitches.None)
        {
            a.pitch = 1 + UnityEngine.Random.Range(-pitchShift, pitchShift);
        }
        else
        {
            a.pitch = 1;
        }

        a.clip = s;
        a.priority = (int)p;
        a.PlayDelayed(delay);
    }


    /// <summary>
    /// Play a sound and loop it forever
    /// </summary>
    /// <param name="s"></param>
    /// <param name="trans">The transform of the sound's source</param>
    /// <param name="p">The priority of the sound</param>
    public void PlaySoundLoop(Sound s, Transform trans = null, Priority p = Priority.Default)
    {
        AudioSource a = GetAvailableSource();
        loopingSources.Add(a);
        if (trans != null)
        {
            sourcePositions[Array.IndexOf(sources, a)] = trans;
        }
        else
        {
            sourcePositions[Array.IndexOf(sources, a)] = listener.transform;
        }
        loopingSources[loopingSources.Count - 1].priority = (int)p;
        loopingSources[loopingSources.Count - 1].clip = clips[(int)s];
        loopingSources[loopingSources.Count - 1].pitch = 1;
        loopingSources[loopingSources.Count - 1].Play();
        loopingSources[loopingSources.Count - 1].loop = true;
    }

    /// <summary>
    /// Play a sound and loop it forever
    /// </summary>
    /// <param name="s"></param>
    /// <param name="trans">The transform of the sound's source</param>
    /// <param name="p">The priority of the sound</param>
    public void PlaySoundLoop(AudioClip s, Transform trans = null, Priority p = Priority.Default)
    {
        AudioSource a = GetAvailableSource();
        loopingSources.Add(a);
        if (trans != null)
        {
            sourcePositions[Array.IndexOf(sources, a)] = trans;
        }
        else
        {
            sourcePositions[Array.IndexOf(sources, a)] = listener.transform;
        }
        loopingSources[loopingSources.Count - 1].priority = (int)p;
        loopingSources[loopingSources.Count - 1].clip = s;
        loopingSources[loopingSources.Count - 1].pitch = 1;
        loopingSources[loopingSources.Count - 1].Play();
        loopingSources[loopingSources.Count - 1].loop = true;
    }

    /// <summary>
    /// Stops any sound playing through PlaySoundOnce() immediately 
    /// </summary>
    /// <param name="s">The sound to be stopped</param>
    /// <param name="t">For sources, helps with duplicate soundss</param>
    public void StopSound(Sound s, Transform t = null)
    {
        for (int i = 0; i < numSources; i++)
        {
            if (sources[i].clip == clips[(int)s])
            {
                if (t != null)
                {
                    if (sourcePositions[i] != t) continue;
                }
                sources[i].Stop();
                return;
            }
        }
    }

    /// <summary>
    /// Stops a looping sound
    /// </summary>
    /// <param name="s"></param>
    /// <param name="stopInstantly">Stops sound instantly if true</param>
    public void StopSoundLoop(Sound s, bool stopInstantly = false, Transform t = null)
    {
        for (int i = 0; i < loopingSources.Count; i++)
        {
            if (loopingSources[i].clip == clips[(int)s])
            {
                for (int j = 0; j < sources.Length; j++) { // Thanks Connor Smiley 
                    if (sources[j] == loopingSources[i])
                    {
                        if (t != sources[j].transform)
                            continue;
                    }
                }
                if (stopInstantly) loopingSources[i].Stop();
                loopingSources[i].loop = false;
                loopingSources.RemoveAt(i);
                sourcePositions[i] = null;
                return;
            }
        }
        //Debug.LogError("AudioManager Error: Did not find specified loop to stop!");
    }

    /// <summary>
    /// Stops a looping sound
    /// </summary>
    /// <param name="s"></param>
    /// <param name="stopInstantly">Stops sound instantly if true</param>
    public void StopSoundLoop(AudioClip s, bool stopInstantly = false, Transform t = null)
    {
        for (int i = 0; i < loopingSources.Count; i++)
        {
            if (loopingSources[i].clip == s)
            {
                for (int j = 0; j < sources.Length; j++)
                { // Thanks Connor Smiley 
                    if (sources[j] == loopingSources[i])
                    {
                        if (t != sources[j].transform)
                            continue;
                    }
                }
                if (stopInstantly) loopingSources[i].Stop();
                loopingSources[i].loop = false;
                loopingSources.RemoveAt(i);
                sourcePositions[i] = null;
                return;
            }
        }
        //Debug.LogError("AudioManager Error: Did not find specified loop to stop!");
    }

    /// <summary>
    /// Stops all looping sounds
    /// </summary>
    /// <param name="stopPlaying">
    /// Stops sounds instantly if true, lets them finish if false
    /// </param>
    public void StopSoundLoopAll(bool stopPlaying = false)
    {
        foreach (AudioSource a in loopingSources)
        {
            if (stopPlaying) a.Stop();
            a.loop = false;
            loopingSources.Remove(a);
        }
        if (sourcePositions != null)
            sourcePositions = new Transform[numSources];
    }

    /// <summary>
    /// Set's the volume of sounds and applies changes instantly across all sources
    /// </summary>
    /// <param name="v"></param>
    public void SetSoundVolume(float v)
    {
        soundVolume = v;
        ApplySoundVolume();
    }

    void ApplySoundVolume()
    {
        foreach (AudioSource s in sources)
        {
            s.volume = soundVolume;
        }
    }
    
    public void SetSoundVolume(UnityEngine.UI.Slider v)
    {
        soundVolume = v.value;
        foreach (AudioSource s in sources)
        {
            s.volume = soundVolume;
        }
    }
    
    public void SetMusicVolume(UnityEngine.UI.Slider v)
    {
        musicVolume = v.value;
        musicSource.volume = musicVolume;
    }

    /// <summary>
    /// A Monobehaviour function called when the script is loaded or a value is changed in the inspector (Called in the editor only).
    /// </summary>
    private void OnValidate()
    {
        if (!doneLoading) return;
        //Updates volume
        ApplySoundVolume();
        SetSpatialSound(spatialSound);
        musicSource.volume = musicVolume;
    }

    /// <summary>
    /// Set's the volume of the music
    /// </summary>
    /// <param name="v"></param>
    public void SetMusicVolume(float v)
    {
        musicVolume = v;
        musicSource.volume = musicVolume;
    }

    /// <summary>
    /// Called externally to update slider values
    /// </summary>
    /// <param name="s"></param>
    public void UpdateSoundSlider(UnityEngine.UI.Slider s)
    {
        s.value = soundVolume;
    }

    /// <summary>
    /// Called externally to update slider values
    /// </summary>
    /// <param name="s"></param>
    public void UpdateMusicSlider(UnityEngine.UI.Slider s)
    {
        s.value = musicVolume;
    }

    /// <summary>
    /// Returns null if all sources are used
    /// </summary>
    /// <returns></returns>
    AudioSource GetAvailableSource()
    {
        foreach(AudioSource a in sources)
        {
            if (!a.isPlaying && !loopingSources.Contains(a))
            {
                return a;
            }
        }
        Debug.LogError("AudioManager Error: Ran out of Audio Sources!");
        return null;
    }

    public static AudioManager GetInstance()
    {
        return Singleton;
    }

    public float GetSoundVolume()
    {
        return soundVolume;
    }

    public float GetMusicVolume()
    {
        return musicVolume;
    }

    /// <summary>
    /// Returns true if a sound is currently being played
    /// </summary>
    /// <param name="s">The sound in question</param>
    /// <param name="trans">Specify is the sound is playing from that transform</param>
    /// <returns></returns>
    public bool IsSoundPlaying(Sound s, Transform trans = null)
    {
        for (int i = 0; i < numSources; i++) // Loop through all sources
        {
            if (sources[i].clip == clips[(int)s] && sources[i].isPlaying) // If this source is playing the clip
            {
                if (trans != null)
                {
                    if (trans != sourcePositions[i]) // Continue if this isn't the specified source position
                    {
                        continue;
                    }
                }
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns true if a sound is currently being played by a looping sources, more efficient for looping sounds than IsSoundPlaying
    /// </summary>
    /// <param name="s">The sound in question</param>
    /// <returns>True or false you dingus</returns>
    public bool IsSoundLooping(Sound s) {
        foreach (AudioSource c in loopingSources)
        {
            if (c.clip == clips[(int)s])
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Converts a pitch enum to float value
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public static float UsePitch(Pitch p)
    {
        switch (p)
        {
            case Pitch.None:
                return Pitches.None;
            case Pitch.VeryLow:
                return Pitches.VeryLow;
            case Pitch.Low:
                return Pitches.Low;
            case Pitch.Medium:
                return Pitches.Medium;
            case Pitch.High:
                return Pitches.High;
        }
        return 0;
    }
}
