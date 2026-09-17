using UnityEngine;

/// <summary>
/// Bootstrap - Point d'entrée du jeu
/// 
/// RESPONSABILITÉS :
/// Trouve tous les managers dans la scène (ou utilise ceux assignés dans l'Inspector)
/// Les enregistre dans le Service Locator pour qu'ils soient accessibles partout
/// S'exécute en PREMIER (avant tous les autres scripts) Edit > Project Settings > Service Execution Order : -50
/// </summary>
public class Bootstrap : MonoBehaviour
{
    // ===========================
    // RÉFÉRENCES AUX MANAGERS
    // ===========================
    // Assigner un manager dans l'Inspector, il sera utilisé
    // Sinon, Bootstrap le cherchera automatiquement dans la scène
    
    [Header("Managers (Auto-trouvés si non assignés)")]
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private ScoreManager _scoreManager;
    [SerializeField] private NarrativeManager _narrativeManager;
    [SerializeField] private AudioManager _audioManager;
    [SerializeField] private PathTracker _pathTracker;
    [SerializeField] private UIManager _uiManager;
    [SerializeField] private AppleSpawner _appleSpawner;
    
    // ===========================
    // INITIALISATION
    // ===========================
    // Awake() s'exécute AVANT tous les Start()
    // On initialise le Service Locator
    void Awake()
    {
        Debug.Log("=== BOOTSTRAP DÉMARRÉ ===");
        
        // 1. Trouver les managers manquants
        FindMissingManagers();
        
        // 2. Les enregistrer dans le Service Locator
        RegisterServices();
        
        // 3. Vérifier que tout est bien enregistré
        ServiceLocator.LogRegisteredServices();
        
        Debug.Log("=== BOOTSTRAP TERMINÉ ===");
    }
    
    // ===========================
    // RECHERCHE AUTOMATIQUE
    // ===========================
    /// <summary>
    /// Cherche automatiquement les managers non assignés dans la scène
    /// </summary>
    private void FindMissingManagers()
    {
        // Pour chaque manager, si null → chercher dans la scène
        if (_pathTracker == null) _pathTracker = FindFirstObjectByType<PathTracker>();
        if (_scoreManager == null) _scoreManager = FindFirstObjectByType<ScoreManager>();
        if (_narrativeManager == null) _narrativeManager = FindFirstObjectByType<NarrativeManager>();
        if (_audioManager == null) _audioManager = FindFirstObjectByType<AudioManager>();
        if (_uiManager == null) _uiManager = FindFirstObjectByType<UIManager>();
        if (_gameManager == null) _gameManager = FindFirstObjectByType<GameManager>();
        if (_appleSpawner == null) _appleSpawner = FindFirstObjectByType<AppleSpawner>();
    }
    
    // ===========================
    // ENREGISTREMENT DES SERVICES
    // ===========================
    /// <summary>
    /// Enregistre tous les managers dans le Service Locator
    /// ORDRE IMPORTANT : UIManager en premier, car d'autres en dépendent
    /// </summary>
    private void RegisterServices()
    {
        // UIManager EN PREMIER (d'autres managers l'utilisent)
        RegisterManager(_uiManager, "UIManager");
        
        // Managers de données
        RegisterManager(_pathTracker, "PathTracker");
        RegisterManager(_scoreManager, "ScoreManager");
        RegisterManager(_appleSpawner, "AppleSpawner");
        
        // Managers narratifs et audio
        RegisterManager(_narrativeManager, "NarrativeManager");
        RegisterManager(_audioManager, "AudioManager");
        
        // GameManager EN DERNIER (orchestre tous les autres)
        RegisterManager(_gameManager, "GameManager");
    }
    
    /// <summary>
    /// Enregistre un manager dans le Service Locator avec gestion d'erreur
    /// </summary>
    /// <typeparam name="T">Type du manager</typeparam>
    /// <param name="manager">Instance du manager</param>
    /// <param name="managerName">Nom pour les logs</param>
    private void RegisterManager<T>(T manager, string managerName) where T : class
    {
        if (manager != null)
        {
            ServiceLocator.Register(manager);
            Debug.Log($"[BOOTSTRAP] {managerName} enregistré");
        }
        else
        {
            Debug.LogError($"[BOOTSTRAP] {managerName} est NULL !");
        }
    }
}