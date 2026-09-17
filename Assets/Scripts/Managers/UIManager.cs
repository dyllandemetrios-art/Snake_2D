using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// UIManager - Gestion de l'interface utilisateur
/// 
/// RESPONSABILITÉS :
/// - Afficher/cacher les panneaux (gameplay, pause, narration)
/// - Mettre à jour le score et le timer
/// - Gérer les boutons (resume, restart, quit)
/// - Afficher le texte narratif et le bouton continuer
/// 
/// PANELS :
/// - Gameplay : Score, timer, high score
/// - Pause : Resume, restart, quit
/// - Narration : Texte narratif, bouton continuer
/// 
/// PATTERN : Service (enregistré dans ServiceLocator)
/// </summary>
public class UIManager : MonoBehaviour
{
    // ===========================
    // PANELS
    // ===========================
    
    [Header("Panels")]
    [SerializeField] private GameObject _gameplayPanel;
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private GameObject _narrativePanel;
    
    // ===========================
    // GAMEPLAY UI
    // ===========================
    
    [Header("Gameplay UI")]
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _highScoreText;
    [SerializeField] private TMP_Text _timerText;
    
    // ===========================
    // NARRATIVE UI
    // ===========================
    
    [Header("Narrative UI")]
    [SerializeField] private TMP_Text _narrativeText;
    [SerializeField] private Button _narrativeContinueButton;
    [SerializeField] private Image _narrativeBackground;
    
    // ===========================
    // PAUSE UI
    // ===========================
    
    [Header("Pause UI")]
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _pauseRestartButton;
    [SerializeField] private Button _pauseQuitButton;
    
    // ===========================
    // INITIALISATION
    // ===========================
    
    void Awake()
    {
        SetupButtons();
        HideAllPanels();
    }
    
    void Start()
    {
        if (ServiceLocator.Has<ScoreManager>())
        {
            ScoreManager scoreManager = ServiceLocator.Get<ScoreManager>();
            scoreManager.OnScoreChanged.AddListener(UpdateScore);
            scoreManager.OnHighScoreChanged.AddListener(UpdateHighScore);
            
            // Afficher le highscore au démarrage
            UpdateHighScore(scoreManager.HighScore);
        }
    }
    
    private void SetupButtons()
    {
        if (_narrativeContinueButton != null)
        {
            _narrativeContinueButton.onClick.AddListener(OnNarrativeContinue);
        }
        
        if (_resumeButton != null)
        {
            _resumeButton.onClick.AddListener(OnResumeClicked);
        }
        
        if (_pauseRestartButton != null)
        {
            _pauseRestartButton.onClick.AddListener(OnRestartClicked);
        }
        
        if (_pauseQuitButton != null)
        {
            _pauseQuitButton.onClick.AddListener(OnQuitClicked);
        }
    }
    
    // ===========================
    // GESTION DES PANELS
    // ===========================
    
    private void HideAllPanels()
    {
        if (_gameplayPanel != null) _gameplayPanel.SetActive(false);
        if (_pausePanel != null) _pausePanel.SetActive(false);
        if (_narrativePanel != null) _narrativePanel.SetActive(false);
    }
    
    public void ShowGameplay()
    {
        HideAllPanels();
        if (_gameplayPanel != null) _gameplayPanel.SetActive(true);
    }
    
    public void ShowPause()
    {
        if (_pausePanel != null) _pausePanel.SetActive(true);
    }
    
    public void HidePause()
    {
        if (_pausePanel != null) _pausePanel.SetActive(false);
    }
    
    public void ShowNarrative()
    {
        if (_narrativePanel != null) _narrativePanel.SetActive(true);
    }
    
    public void HideNarrative()
    {
        if (_narrativePanel != null) _narrativePanel.SetActive(false);
    }
    
    // ===========================
    // MISE À JOUR DU SCORE
    // ===========================
    
    /// <summary>
    /// Met à jour l'affichage du score
    /// Affiche également le compteur Lilith si le joueur est immortel
    /// </summary>
    public void UpdateScore(int score)
    {
        if (_scoreText != null)
        {
            SnakeController snake = FindFirstObjectByType<SnakeController>();
            if (snake != null && snake.IsImmortal)
            {
                PathTracker tracker = ServiceLocator.Get<PathTracker>();
                int lilithCount = tracker != null ? tracker.LilithOutsideCount : 0;
                _scoreText.text = $"Score: {score} | Lilith: {lilithCount}/11";
            }
            else
            {
                _scoreText.text = $"Score: {score}";
            }
        }
    }
    
    /// <summary>
    /// Met à jour l'affichage du highscore
    /// </summary>
    public void UpdateHighScore(int highScore)
    {
        if (_highScoreText != null)
        {
            _highScoreText.text = $"High Score: {highScore}";
            Debug.Log($"[UI] Highscore affiché: {highScore}");
        }
        else
        {
            Debug.LogWarning("[UI] _highScoreText est null dans l'Inspector !");
        }
    }
    
    // ===========================
    // MISE À JOUR DU TIMER
    // ===========================
    
    public void UpdateTimer(float gameTime)
    {
        if (_timerText != null)
        {
            int minutes = Mathf.FloorToInt(gameTime / 60f);
            int seconds = Mathf.FloorToInt(gameTime % 60f);
            _timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }
    
    // ===========================
    // NARRATIVE UI
    // ===========================
    
    public void SetNarrativeText(string text)
    {
        if (_narrativeText != null)
        {
            _narrativeText.text = text;
        }
    }
    
    public void SetNarrativeButtonVisible(bool visible)
    {
        if (_narrativeContinueButton != null)
        {
            _narrativeContinueButton.gameObject.SetActive(visible);
        }
    }
    
    // ===========================
    // BOUTONS
    // ===========================
    
    private void OnNarrativeContinue()
    {
        ServiceLocator.Get<AudioManager>()?.PlaySFX(AudioManager.SoundEffect.Click);
    }
    
    private void OnResumeClicked()
    {
        ServiceLocator.Get<AudioManager>()?.PlaySFX(AudioManager.SoundEffect.Click);
        
        if (ServiceLocator.Has<GameManager>())
        {
            ServiceLocator.Get<GameManager>().ResumeGame();
        }
    }
    
    private void OnRestartClicked()
    {
        ServiceLocator.Get<AudioManager>()?.PlaySFX(AudioManager.SoundEffect.Click);
        
        Time.timeScale = 1f;
        
        if (ServiceLocator.Has<GameManager>())
        {
            ServiceLocator.Get<GameManager>().RestartGame();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
    
    private void OnQuitClicked()
    {
        ServiceLocator.Get<AudioManager>()?.PlaySFX(AudioManager.SoundEffect.Click);
        
        Debug.Log("[UI] Quitter le jeu");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    public void ShowMessage(string message, float duration = 2f)
    {
        Debug.Log($"[UI MESSAGE] {message}");
    }
}