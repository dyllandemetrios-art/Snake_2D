using UnityEngine;

/// <summary>
/// AudioManager - Gestion centralisée des sons et musiques
/// 
/// RESPONSABILITÉS :
/// - Jouer les musiques de fond (Menu, Gameplay, Lilith, Ending)
/// - Jouer les effets sonores (pommes, mort, renaissance, clic)
/// - Gérer les volumes (Master, Music, SFX)
/// - Contrôler la lecture (Play, Stop, Pause, Resume)
/// 
/// ARCHITECTURE :
/// Utilise 2 AudioSources :
///   - _musicSource : Musique de fond (loop activé)
///   - _sfxSource : Effets sonores (PlayOneShot)
/// 
/// VOLUME HIERARCHY :
/// Volume final = MasterVolume × MusicVolume
/// Volume SFX = SFXVolume (indépendant de Music)
/// 
/// PATTERN : Service (enregistré dans ServiceLocator)
/// </summary>
public class AudioManager : MonoBehaviour
{
    // ===========================
    // MUSIC TRACKS (AudioClips)
    // ===========================
    
    [Header("Music Tracks")]
    
    /// <summary>
    /// Musique du menu principal
    /// Joué au démarrage ou dans le menu
    /// </summary>
    [SerializeField] private AudioClip _menuMusic;
    
    /// <summary>
    /// Musique pendant le gameplay normal
    /// Joué pendant FirstChoice et Playing (avant Lilith)
    /// </summary>
    [SerializeField] private AudioClip _gameplayMusic;
    
    /// <summary>
    /// Musique après la renaissance Lilith
    /// Joué quand le serpent devient immortel
    /// Style suggéré : Sombre, intense, libératrice
    /// </summary>
    [SerializeField] private AudioClip _lilithMusic;
    
    /// <summary>
    /// Musique de l'écran de fin
    /// Joué pendant Ending
    /// </summary>
    [SerializeField] private AudioClip _endingMusic;
    
    // ===========================
    // SOUND EFFECTS (AudioClips)
    // ===========================
    
    [Header("SFX")]
    
    /// <summary>
    /// Son quand on mange une pomme (tous types)
    /// </summary>
    [SerializeField] private AudioClip _appleSound;
    
    /// <summary>
    /// Son de mort du serpent
    /// Joué lors d'un Game Over (collision mur/corps)
    /// </summary>
    [SerializeField] private AudioClip _deathSound;
    
    /// <summary>
    /// Son de renaissance Lilith
    /// Joué lors de l'initiation (première mort)
    /// Style suggéré : Mystique, triomphal, transformation
    /// </summary>
    [SerializeField] private AudioClip _rebirthSound;
    
    /// <summary>
    /// Son des clics de boutons
    /// Joué sur tous les boutons UI
    /// </summary>
    [SerializeField] private AudioClip _clickSound;
    
    // ===========================
    // VOLUME SETTINGS
    // ===========================
    
    [Header("Settings")]
    
    /// <summary>
    /// Volume global (affecte tout)
    /// Valeur entre 0 (muet) et 1 (max)
    /// Multiplie tous les autres volumes
    /// </summary>
    [SerializeField] private float _masterVolume = 0.5f;
    
    /// <summary>
    /// Volume de la musique de fond
    /// Valeur entre 0 et 1
    /// Par défaut à 0.7 (70%) pour ne pas couvrir les SFX
    /// </summary>
    [SerializeField] private float _musicVolume = 0.5f;
    
    /// <summary>
    /// Volume des effets sonores
    /// Valeur entre 0 et 1
    /// Indépendant de musicVolume
    /// </summary>
    [SerializeField] private float _sfxVolume = 0.5f;
    
    // ===========================
    // AUDIO SOURCES (Créées automatiquement)
    // ===========================
    
    /// <summary>
    /// AudioSource dédiée aux musiques de fond
    /// Configuration :
    ///   - loop = true (musique en boucle)
    ///   - playOnAwake = false (contrôle manuel)
    /// </summary>
    private AudioSource _musicSource;
    
    /// <summary>
    /// AudioSource dédiée aux effets sonores
    /// Configuration :
    ///   - loop = false (sons courts)
    ///   - playOnAwake = false
    /// 
    /// Utilise PlayOneShot() pour jouer plusieurs sons simultanément
    /// </summary>
    private AudioSource _sfxSource;
    
    // ===========================
    // INITIALISATION
    // ===========================
    
    /// <summary>
    /// Crée les AudioSources automatiquement
    /// Configure leurs propriétés par défaut
    /// </summary>
    void Awake()
    {
        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.loop = true;
        _musicSource.playOnAwake = false;
        _musicSource.volume = _masterVolume * _musicVolume;
        
        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.loop = false;
        _sfxSource.playOnAwake = false;
    }
    
    /// <summary>
    /// Détecte la scène active et lance la musique correspondante
    /// MainMenu → Menu
    /// SampleScene → Gameplay
    /// GameOver → Ending
    /// </summary>
    void Start()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        if (sceneName == "MainMenu")
        {
            PlayMusic(MusicTrack.Menu);
        }
        else if (sceneName == "SampleScene")
        {
            PlayMusic(MusicTrack.Gameplay);
        }
        else if (sceneName == "GameOver")
        {
            PlayMusic(MusicTrack.Ending);
        }
    }
    
    // ===========================
    // GESTION DE LA MUSIQUE
    // ===========================
    
    /// <summary>
    /// Joue une musique de fond
    /// 
    /// COMPORTEMENT :
    /// - Si la musique demandée joue déjà, ne rien faire
    /// - Sinon, stopper l'actuelle et lancer la nouvelle
    /// 
    /// UTILISATION :
    /// audioManager.PlayMusic(AudioManager.MusicTrack.Lilith)
    /// </summary>
    /// <param name="track">Type de musique à jouer</param>
    public void PlayMusic(MusicTrack track)
    {
        AudioClip clip = track switch
        {
            MusicTrack.Menu => _menuMusic,
            MusicTrack.Gameplay => _gameplayMusic,
            MusicTrack.Lilith => _lilithMusic,
            MusicTrack.Ending => _endingMusic,
            _ => null
        };
        
        if (clip == null)
        {
            Debug.LogWarning($"[AUDIO] Musique {track} non assignée");
            return;
        }
        
        if (_musicSource.clip == clip && _musicSource.isPlaying) return;
        
        _musicSource.clip = clip;
        _musicSource.Play();
        
        Debug.Log($"[AUDIO] Musique {track} lancée");
    }
    
    /// <summary>
    /// Arrête complètement la musique
    /// Remet la position de lecture à 0
    /// Appelé lors de la mort du serpent
    /// </summary>
    public void StopMusic()
    {
        _musicSource.Stop();
    }
    
    /// <summary>
    /// Met en pause la musique
    /// La position de lecture est conservée
    /// Utilisé pendant la pause du jeu
    /// </summary>
    public void PauseMusic()
    {
        _musicSource.Pause();
    }
    
    /// <summary>
    /// Reprend la musique depuis où elle était en pause
    /// </summary>
    public void ResumeMusic()
    {
        _musicSource.UnPause();
    }
    
    // ===========================
    // GESTION DES SFX
    // ===========================
    
    /// <summary>
    /// Joue un effet sonore via l'enum
    /// 
    /// AVANTAGE DE PLAYONESHOT :
    /// Permet de jouer plusieurs sons simultanément
    /// Exemple : manger une pomme pendant qu'un autre son joue
    /// 
    /// UTILISATION :
    /// audioManager.PlaySFX(AudioManager.SoundEffect.Rebirth)
    /// </summary>
    /// <param name="sfx">Type d'effet sonore</param>
    public void PlaySFX(SoundEffect sfx)
    {
        AudioClip clip = sfx switch
        {
            SoundEffect.Apple => _appleSound,
            SoundEffect.Death => _deathSound,
            SoundEffect.Rebirth => _rebirthSound,
            SoundEffect.Click => _clickSound,
            _ => null
        };
        
        if (clip == null)
        {
            Debug.LogWarning($"[AUDIO] SFX {sfx} non assigné");
            return;
        }
        
        _sfxSource.PlayOneShot(clip, _sfxVolume);
    }
    
    // ===========================
    // CONTRÔLE DU VOLUME
    // ===========================
    
    /// <summary>
    /// Définit le volume global (0-1)
    /// Affecte TOUTES les musiques et SFX
    /// 
    /// UTILISATION :
    /// Dans un menu Options/Settings
    /// Slider relié à cette méthode
    /// </summary>
    /// <param name="volume">Volume global (0 = muet, 1 = max)</param>
    public void SetMasterVolume(float volume)
    {
        _masterVolume = Mathf.Clamp01(volume);
        _musicSource.volume = _masterVolume * _musicVolume;
    }
    
    /// <summary>
    /// Définit le volume de la musique (0-1)
    /// N'affecte PAS les SFX
    /// </summary>
    /// <param name="volume">Volume musique (0-1)</param>
    public void SetMusicVolume(float volume)
    {
        _musicVolume = Mathf.Clamp01(volume);
        _musicSource.volume = _masterVolume * _musicVolume;
    }
    
    /// <summary>
    /// Définit le volume des SFX (0-1)
    /// N'affecte PAS la musique
    /// </summary>
    /// <param name="volume">Volume SFX (0-1)</param>
    public void SetSFXVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);
    }
    
    // ===========================
    // ENUMS (Types de sons)
    // ===========================
    
    /// <summary>
    /// Types de musiques disponibles
    /// Menu : Menu principal
    /// Gameplay : Jeu normal (avant Lilith)
    /// Lilith : Après renaissance (immortalité)
    /// Ending : Écran de fin
    /// </summary>
    public enum MusicTrack
    {
        Menu,
        Gameplay,
        Lilith,
        Ending
    }
    
    /// <summary>
    /// Types d'effets sonores disponibles
    /// Apple : Manger une pomme
    /// Death : Mort définitive
    /// Rebirth : Renaissance Lilith (initiation)
    /// Click : Clic sur bouton UI
    /// </summary>
    public enum SoundEffect
    {
        Apple,
        Death,
        Rebirth,
        Click
    }
}