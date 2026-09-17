using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GameManager - Chef d'orchestre du jeu
/// 
/// RESPONSABILITÉS :
/// - Gérer les états du jeu (FirstChoice, Playing, Paused, etc.)
/// - Coordonner les autres managers (Spawner, Snake, Narrative, Score)
/// - Gérer le timer et la limite de temps (optionnelle)
/// - Gérer la pause (P ou ESC)
/// - Déclencher les endings selon le chemin dominant
/// - Gérer le restart du jeu (R)
/// 
/// ÉTATS DU JEU :
/// 1. FirstChoice : Les 3 premières pommes sont spawnées, attente du choix
/// 2. Playing : Jeu en cours
/// 3. Paused : Pause (P/ESC)
/// 4. LilithRebirth : Première mort Lilith (narration + renaissance)
/// 5. GameOver : Mort définitive
/// 6. Ending : Écran de fin avec narration
/// 
/// PATTERN : Singleton + State Machine
/// </summary>
public class GameManager : MonoBehaviour
{
    // ===========================
    // SINGLETON
    // ===========================
    
    /// <summary>
    /// Instance unique du GameManager
    /// Accessible depuis n'importe où via GameManager.Instance
    /// Utilisé par : SnakeCollision, AppleSpawner, UI
    /// </summary>
    public static GameManager Instance { get; private set; }
    
    // ===========================
    // ÉTAT DU JEU (State Machine)
    // ===========================
    
    /// <summary>
    /// Les 6 états possibles du jeu
    /// 
    /// FirstChoice : Attente du premier choix de pomme
    /// Playing : Jeu en cours normal
    /// Paused : Jeu en pause (P/ESC)
    /// LilithRebirth : Narration de renaissance Lilith
    /// GameOver : Mort définitive (pas utilisé comme état intermédiaire)
    /// Ending : Écran de fin avec narration et stats
    /// </summary>
    public enum GameState
    {
        FirstChoice,
        Playing,
        Paused,
        LilithRebirth,
        GameOver,
        Ending
    }
    
    /// <summary>
    /// État actuel du jeu
    /// Contrôle le comportement de nombreux systèmes
    /// </summary>
    private GameState _currentState = GameState.FirstChoice;
    
    // ===========================
    // RÉFÉRENCES AUX MANAGERS
    // ===========================
    
    [Header("Managers")]
    
    /// <summary>
    /// AppleSpawner pour spawner les pommes
    /// Utilisé pour : SpawnFirstChoice(), réinitialisation
    /// </summary>
    [SerializeField] private AppleSpawner _appleSpawner;
    
    /// <summary>
    /// SnakeController pour contrôler le serpent
    /// Utilisé pour : StopMovement(), ResumeMovement(), LilithRebirth()
    /// </summary>
    [SerializeField] private SnakeController _snake;
    
    /// <summary>
    /// NarrativeManager pour les textes Ink
    /// Utilisé pour : intro, première mort Lilith, endings
    /// </summary>
    [SerializeField] private NarrativeManager _narrativeManager;
    
    /// <summary>
    /// ScoreManager (optionnel, pas utilisé directement)
    /// Accessible via Service Locator si besoin
    /// </summary>
    [SerializeField] private ScoreManager _scoreManager;
    
    // ===========================
    // UI DIRECTE
    // ===========================
    
    [Header("UI Direct")]
    
    /// <summary>
    /// Panel de pause (GameObject à activer/désactiver)
    /// Contient les boutons Resume, Restart, Quit
    /// </summary>
    [SerializeField] private GameObject _pausePanel;
    
    // ===========================
    // CONFIGURATION
    // ===========================
    
    [Header("Settings")]
    
    /// <summary>
    /// Limite de temps du jeu en secondes
    /// Si _useTimeLimit = true, le jeu se termine automatiquement
    /// Exemple : 300f = 5 minutes
    /// </summary>
    [SerializeField] private float _gameTimeLimit = 300f;
    
    /// <summary>
    /// Active/désactive la limite de temps
    /// False = jeu infini (termine uniquement sur mort)
    /// True = jeu limité dans le temps
    /// </summary>
    [SerializeField] private bool _useTimeLimit = false;
    
    // ===========================
    // TIMER
    // ===========================
    
    /// <summary>
    /// Temps de jeu écoulé en secondes
    /// Incrémenté dans Update() si état = Playing ou FirstChoice
    /// Réinitialisé au restart
    /// </summary>
    private float _gameTime = 0f;
    
    // ===========================
    // PROPRIÉTÉS PUBLIQUES
    // ===========================
    
    /// <summary>
    /// État actuel du jeu (lecture seule)
    /// Utilisé par d'autres scripts pour adapter leur comportement
    /// </summary>
    public GameState CurrentState => _currentState;
    
    /// <summary>
    /// Temps de jeu écoulé (lecture seule)
    /// Affiché dans l'UI via UIManager.UpdateTimer()
    /// </summary>
    public float GameTime => _gameTime;
    
    // ===========================
    // INITIALISATION
    // ===========================
    
    /// <summary>
    /// Configure le Singleton
    /// S'assure qu'une seule instance existe
    /// Exécuté AVANT Start()
    /// </summary>
    void Awake()
    {
        Debug.Log("[GM] Awake() - Initialisation du GameManager");
        
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GM] Instance duplicate détectée, destruction");
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        Debug.Log("[GM] Singleton GameManager créé");
    }
    
    /// <summary>
    /// Démarre le jeu
    /// Vérifie les références et lance la séquence d'intro
    /// </summary>
    void Start()
    {
        Debug.Log("[GM] Start() - Démarrage du jeu");
        ValidateReferences();
        StartGame();
    }
    
    /// <summary>
    /// Boucle principale du jeu
    /// Gère le timer et les inputs
    /// </summary>
    void Update()
    {
        if (_currentState == GameState.Playing || _currentState == GameState.FirstChoice)
        {
            _gameTime += Time.deltaTime;
            ServiceLocator.Get<UIManager>()?.UpdateTimer(_gameTime);
            
            if (_useTimeLimit && _gameTime >= _gameTimeLimit)
            {
                Debug.Log($"[GM] Limite de temps atteinte ({_gameTimeLimit}s)");
                TriggerEnding();
            }
        }

        HandleInputs();
    }
    
    /// <summary>
    /// Vérifie que toutes les références requises sont assignées
    /// Affiche des erreurs claires si quelque chose manque
    /// </summary>
    private void ValidateReferences()
    {
        Debug.Log("[GM] Validation des références...");
        
        if (_appleSpawner == null) 
            Debug.LogError("[GM] AppleSpawner non assigné !");
        else
            Debug.Log("[GM] AppleSpawner OK");
        
        if (_snake == null) 
            Debug.LogError("[GM] SnakeController non assigné !");
        else
            Debug.Log("[GM] SnakeController OK");
        
        if (_pausePanel == null) 
            Debug.LogError("[GM] PausePanel non assigné !");
        else
            Debug.Log("[GM] PausePanel OK");
    }
    
    // ===========================
    // GESTION DES INPUTS
    // ===========================
    
    /// <summary>
    /// Gère les inputs globaux du jeu
    /// 
    /// TOUCHES :
    /// - P ou ESC : Pause/Resume
    /// - R : Restart
    /// 
    /// LOGIQUE PAUSE :
    /// - On peut mettre en pause depuis FirstChoice ou Playing
    /// - On ne peut PAS mettre en pause pendant narration/ending
    /// </summary>
    private void HandleInputs()
    {
        if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log($"[GM] Input Pause détecté, état actuel: {_currentState}");
            
            if (_currentState == GameState.Playing || _currentState == GameState.FirstChoice)
            {
                TogglePause(true);
            }
            else if (_currentState == GameState.Paused)
            {
                TogglePause(false);
            }
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("[GM] Input Restart (R) détecté");
            RestartGame();
        }
    }
    
    /// <summary>
    /// Active ou désactive la pause
    /// 
    /// EFFETS DE LA PAUSE :
    /// - Time.timeScale = 0 → Fige le temps Unity
    /// - Arrête le mouvement du serpent
    /// - Met la musique en pause
    /// - Affiche le panel de pause
    /// 
    /// EFFETS DU RESUME :
    /// - Time.timeScale = 1 → Reprend le temps
    /// - Reprend le mouvement du serpent
    /// - Reprend la musique
    /// - Cache le panel de pause
    /// </summary>
    /// <param name="pause">True = mettre en pause, False = reprendre</param>
    private void TogglePause(bool pause)
    {
        if (pause)
        {
            Debug.Log("[GM] MISE EN PAUSE");
        
            _currentState = GameState.Paused;
            Time.timeScale = 0f;
            _snake?.StopMovement();
        
            // PAUSE LA MUSIQUE
            ServiceLocator.Get<AudioManager>()?.PauseMusic();
            Debug.Log("[GM] Musique mise en pause");
        
            if (_pausePanel != null)
            {
                _pausePanel.SetActive(true);
            }
        }
        else
        {
            Debug.Log("[GM] REPRISE DU JEU");
        
            _currentState = GameState.Playing;
            Time.timeScale = 1f;
            _snake?.ResumeMovement();
        
            // REPREND LA MUSIQUE
            ServiceLocator.Get<AudioManager>()?.ResumeMusic();
            Debug.Log("[GM] Musique reprise");
        
            if (_pausePanel != null)
            {
                _pausePanel.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Méthode publique pour mettre en pause
    /// Appelée par les boutons UI ou d'autres scripts
    /// </summary>
    public void PauseGame()
    {
        Debug.Log("[GM] PauseGame() appelée");
        
        if (_currentState == GameState.Playing || _currentState == GameState.FirstChoice)
        {
            TogglePause(true);
        }
    }
    
    /// <summary>
    /// Méthode publique pour reprendre
    /// Appelée par le bouton Resume dans le panel de pause
    /// </summary>
    public void ResumeGame()
    {
        Debug.Log("[GM] ResumeGame() appelée");
        
        if (_currentState == GameState.Paused)
        {
            TogglePause(false);
        }
    }
    
    // ===========================
    // DÉMARRAGE DU JEU
    // ===========================
    
    /// <summary>
    /// Lance la séquence de démarrage du jeu
    /// 
    /// SÉQUENCE :
    /// 1. Afficher l'UI de gameplay
    /// 2. Jouer la narration d'intro (knot "start")
    /// 3. Callback OnIntroComplete() → Spawn des 3 pommes
    /// </summary>
    private void StartGame()
    {
        Debug.Log("[GM] Démarrage du jeu");
        
        ServiceLocator.Get<UIManager>()?.ShowGameplay();
        
        if (_narrativeManager != null)
        {
            Debug.Log("[GM] Lancement de la narration d'intro");
            _narrativeManager.PlayKnot("start", OnIntroComplete);
        }
        else
        {
            Debug.LogWarning("[GM] Pas de NarrativeManager, skip intro");
            OnIntroComplete();
        }
    }
    
    /// <summary>
    /// Appelé après la narration d'intro
    /// Spawne les 3 premières pommes (choix philosophique)
    /// </summary>
    private void OnIntroComplete()
    {
        Debug.Log("[GM] Intro terminée, spawn des 3 pommes");
        
        if (_appleSpawner != null)
        {
            _appleSpawner.SpawnThreeApples();
        }
        
        ChangeState(GameState.FirstChoice);
    }
    
    // ===========================
    // GESTION DES ÉTATS
    // ===========================
    
    /// <summary>
    /// Change l'état du jeu et applique les effets associés
    /// 
    /// EFFETS PAR ÉTAT :
    /// - Playing : Time.timeScale = 1, serpent bouge
    /// - Paused : Time.timeScale = 0
    /// - LilithRebirth : Time.timeScale = 0, serpent arrêté (narration)
    /// - GameOver/Ending : Time.timeScale = 0, serpent arrêté
    /// </summary>
    /// <param name="newState">Nouvel état</param>
    public void ChangeState(GameState newState)
    {
        Debug.Log($"[GM] Changement d'état : {_currentState} → {newState}");
        
        _currentState = newState;
        
        switch (newState)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                _snake?.ResumeMovement();
                Debug.Log("[GM] État PLAYING activé");
                break;
            
            case GameState.Paused:
                Time.timeScale = 0f;
                Debug.Log("[GM] État PAUSED activé");
                break;
            
            case GameState.LilithRebirth:
                Time.timeScale = 0f;
                _snake?.StopMovement();
                Debug.Log("[GM] État LILITH REBIRTH activé");
                break;
            
            case GameState.GameOver:
            case GameState.Ending:
                Time.timeScale = 0f;
                _snake?.StopMovement();
                Debug.Log($"[GM] État {newState} activé");
                break;
        }
    }
    
    // ===========================
    // ÉVÉNEMENTS DU JEU
    // ===========================
    
    /// <summary>
    /// Appelé par AppleSpawner quand la première pomme est mangée
    /// Transition FirstChoice → Playing
    /// </summary>
    public void OnFirstAppleEaten()
    {
        Debug.Log("[GM] OnFirstAppleEaten() - Première pomme mangée");
        
        if (_currentState == GameState.FirstChoice)
        {
            Debug.Log("[GM] Première pomme mangée → Playing");
            _snake?.ResumeMovement();
            ChangeState(GameState.Playing);
        }
    }
    
    /// <summary>
    /// Appelé par SnakeCollision quand le serpent meurt
    /// 
    /// DEUX CAS :
    /// 1. isLilithInitiation = true → Première mort Lilith (renaissance)
    /// 2. isLilithInitiation = false → Mort définitive (ending après 1.5s pour laisser le son se jouer)
    /// </summary>
    /// <param name="isLilithInitiation">True si première mort Lilith</param>
    public void OnSnakeDeath(bool isLilithInitiation)
    {
        Debug.Log($"[GM] OnSnakeDeath() appelée, isLilithInitiation: {isLilithInitiation}");
        
        if (isLilithInitiation)
        {
            Debug.Log("[GM] Initiation Lilith détectée");
            ChangeState(GameState.LilithRebirth);
            
            PathTracker pathTracker = ServiceLocator.Get<PathTracker>();
            pathTracker?.RegisterDeath(isLilithInitiation: true);
            
            _narrativeManager?.PlayKnot("first_death", OnLilithRebirthComplete);
        }
        else
        {
            Debug.Log("[GM] Mort définitive");
            ChangeState(GameState.GameOver);
            
            StartCoroutine(DelayedEnding());
        }
    }
    
    /// <summary>
    /// Coroutine pour attendre 1.5 secondes avant de passer à l'écran de fin
    /// Permet au son de mort de se jouer complètement
    /// </summary>
    private System.Collections.IEnumerator DelayedEnding()
    {
        Debug.Log("[GM] Attente de 1.5s avant l'ending...");
        yield return new WaitForSecondsRealtime(1.5f);
        Debug.Log("[GM] Délai écoulé, déclenchement de l'ending");
        TriggerEnding();
    }
    
    /// <summary>
    /// Appelé après la narration de renaissance Lilith (knot "first_death")
    /// Active l'immortalité et reprend le jeu avec la musique Lilith
    /// 
    /// SÉQUENCE :
    /// 1. Appeler snake.LilithRebirth() → Immortalité activée
    /// 2. Lancer musique Lilith
    /// 3. Changer état → Playing
    /// 4. Le serpent peut maintenant traverser murs et corps
    /// </summary>
    public void OnLilithRebirthComplete()
    {
        Debug.Log("[GM] OnLilithRebirthComplete() - Renaissance Lilith");
    
        FindFirstObjectByType<SnakeController>()?.LilithRebirth();
        ServiceLocator.Get<AudioManager>()?.PlayMusic(AudioManager.MusicTrack.Lilith);
        
        ChangeState(GameState.Playing);
    }
    
    /// <summary>
    /// Appelé par le Boss quand il meurt
    /// Lance une coroutine de délai avant la victoire
    /// La coroutine tourne sur le GameManager, pas sur le boss qui est détruit
    /// </summary>
    public void OnBossDestroyed()
    {
        Debug.Log("[GM] OnBossDestroyed() - Boss détruit, attente avant victoire...");
        StartCoroutine(VictoryDelay());
    }

    /// <summary>
    /// Attend 2 secondes puis déclenche l'écran de victoire
    /// Utilise WaitForSecondsRealtime car TimeScale pourrait être à 0
    /// </summary>
    private System.Collections.IEnumerator VictoryDelay()
    {
        yield return new WaitForSecondsRealtime(2f);
        Debug.Log("[GM] Délai écoulé → TriggerVictory()");
        TriggerVictory();
    }
    
    // ===========================
    // FIN DU JEU
    // ===========================
    
    /// <summary>
    /// Déclenche la fin du jeu
    /// 
    /// SÉQUENCE :
    /// 1. Changer état → Ending
    /// 2. Déterminer le chemin dominant (via PathTracker)
    /// 3. Sauvegarder les données dans PlayerPrefs
    /// 4. Charger la scène GameOver (index 2)
    /// 
    /// ENDINGS POSSIBLES :
    /// - ending_adam : Chemin de la domination
    /// - ending_eve : Chemin de l'adaptation
    /// - ending_lilith : Chemin de la transgression
    /// </summary>
    public void TriggerEnding()
    {
        Debug.Log("[GM] TriggerEnding() - Déclenchement de la fin du jeu");
    
        ChangeState(GameState.Ending);
    
        Apple.AppleType dominantPath = Apple.AppleType.Eve;
    
        if (ServiceLocator.Has<PathTracker>())
        {
            dominantPath = ServiceLocator.Get<PathTracker>().GetDominantPath();
            Debug.Log($"[GM] Chemin dominant: {dominantPath}");
        }
    
        string pathName = dominantPath.ToString();
        PlayerPrefs.SetString("EndingPath", pathName);
        Debug.Log($"[GM] EndingPath sauvegardé: {pathName}");
    
        if (ServiceLocator.Has<PathTracker>())
        {
            string stats = ServiceLocator.Get<PathTracker>().GetStatsBreakdown();
            PlayerPrefs.SetString("EndingStats", stats);
            Debug.Log($"[GM] Stats sauvegardées: {stats}");
        }
    
        PlayerPrefs.Save();
        Debug.Log("[GM] PlayerPrefs sauvegardés");
    
        Debug.Log("[GM] Chargement de la scène GameOver (index 2)");
        SceneManager.LoadScene(2);
    }
    
    /// <summary>
    /// Déclenche la VICTOIRE (boss tué)
    /// Différent de TriggerEnding() qui est pour la défaite/temps écoulé
    /// 
    /// SÉQUENCE :
    /// 1. Changer état → Ending
    /// 2. Sauvegarder "Victory" au lieu du chemin dominant
    /// 3. Charger la scène GameOver avec message "Well Done"
    /// </summary>
    public void TriggerVictory()
    {
        Debug.Log("[GM] TriggerVictory() - BOSS VAINCU !");

        ChangeState(GameState.Ending);

        // Sauvegarder "Victory" au lieu du chemin dominant
        PlayerPrefs.SetString("EndingPath", "Victory");
        Debug.Log("[GM] EndingPath sauvegardé: Victory");

        if (ServiceLocator.Has<PathTracker>())
        {
            string stats = ServiceLocator.Get<PathTracker>().GetStatsBreakdown();
            PlayerPrefs.SetString("EndingStats", stats);
            Debug.Log($"[GM] Stats sauvegardées: {stats}");
        }

        PlayerPrefs.Save();
        Debug.Log("[GM] PlayerPrefs sauvegardés");

        Debug.Log("[GM] Chargement de la scène GameOver (index 2)");
        SceneManager.LoadScene(2);
    }
    
    // ===========================
    // RESTART
    // ===========================
    
    /// <summary>
    /// Redémarre le jeu en rechargeant la scène
    /// 
    /// EFFETS :
    /// - Détruit tous les GameObjects de la scène actuelle
    /// - Recharge la scène depuis zéro
    /// - Tous les managers sont réinitialisés
    /// - Le temps est remis à 1 (au cas où timeScale était à 0)
    /// 
    /// ALTERNATIVE POSSIBLE :
    /// Au lieu de recharger la scène, appeler des méthodes Reset()
    /// sur chaque manager (plus rapide mais plus complexe)
    /// </summary>
    public void RestartGame()
    {
        Debug.Log("[GM] Redémarrage du jeu");
        
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}