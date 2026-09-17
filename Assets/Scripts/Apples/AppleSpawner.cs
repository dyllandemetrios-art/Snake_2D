using System.Collections;
using UnityEngine;

/// <summary>
/// AppleSpawner - Gestion du spawn et du cycle de vie des pommes
/// 
/// RESPONSABILITÉS :
/// - Spawner TOUJOURS 3 pommes (Adam, Eve, Lilith) simultanément
/// - Gérer la durée de vie des pommes (10s normal, 5s mode Lilith)
/// - Adapter les positions selon le mode de jeu (grille ou map complète)
/// 
/// SYSTÈME DE SPAWN :
/// Les 3 pommes sont toujours présentes. Quand une est mangée, les 3 respawnent.
/// 
/// MODE NORMAL :
/// - Spawn dans la grille (via GridManager)
/// - Durée de vie : 10 secondes
/// 
/// MODE LILITH IMMORTELLE :
/// - Spawn partout sur la map (-35 à 35, -15 à 15)
/// - Durée de vie : 5 secondes
/// 
/// PATTERN : Singleton
/// </summary>
public class AppleSpawner : MonoBehaviour
{
    // ===========================
    // SINGLETON
    // ===========================
    
    /// <summary>
    /// Instance unique accessible globalement
    /// Utilisé par Apple.cs et BossSnake.cs
    /// </summary>
    public static AppleSpawner Instance { get; private set; }
    
    // ===========================
    // CONFIGURATION
    // ===========================
    
    [Header("References")]
    
    /// <summary>
    /// GridManager pour obtenir des positions dans la grille
    /// </summary>
    [SerializeField] private GridManager _grid;
    
    /// <summary>
    /// SnakeController pour vérifier l'état Lilith immortelle
    /// </summary>
    [SerializeField] private SnakeController _snake;
    
    [Header("Apple Prefabs")]
    
    /// <summary>
    /// Prefab de la pomme Adam (bleue)
    /// </summary>
    [SerializeField] private GameObject _adamApplePrefab;
    
    /// <summary>
    /// Prefab de la pomme Eve (verte)
    /// </summary>
    [SerializeField] private GameObject _eveApplePrefab;
    
    /// <summary>
    /// Prefab de la pomme Lilith (rouge)
    /// </summary>
    [SerializeField] private GameObject _lilithApplePrefab;
    
    [Header("Settings")]
    
    /// <summary>
    /// Délai avant respawn après qu'une pomme soit mangée
    /// </summary>
    [SerializeField] private float _spawnDelay = 0.1f;
    
    // ===========================
    // ÉTAT
    // ===========================
    
    /// <summary>
    /// Références aux 3 pommes actuellement actives
    /// </summary>
    private GameObject _currentAdamApple;
    private GameObject _currentEveApple;
    private GameObject _currentLilithApple;
    
    /// <summary>
    /// Coroutines gérant la durée de vie des pommes
    /// </summary>
    private Coroutine _adamLifetimeCoroutine;
    private Coroutine _eveLifetimeCoroutine;
    private Coroutine _lilithLifetimeCoroutine;
    
    // ===========================
    // INITIALISATION
    // ===========================
    
    /// <summary>
    /// Configure le Singleton
    /// </summary>
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    /// <summary>
    /// Valide les références Inspector
    /// </summary>
    void Start()
    {
        ValidateReferences();
    }
    
    /// <summary>
    /// Vérifie que toutes les références nécessaires sont assignées
    /// </summary>
    private void ValidateReferences()
    {
        if (_grid == null) Debug.LogError("[SPAWNER] GridManager manquant");
        if (_snake == null) Debug.LogError("[SPAWNER] SnakeController manquant");
        if (_adamApplePrefab == null) Debug.LogError("[SPAWNER] AdamApple prefab manquant");
        if (_eveApplePrefab == null) Debug.LogError("[SPAWNER] EveApple prefab manquant");
        if (_lilithApplePrefab == null) Debug.LogError("[SPAWNER] LilithApple prefab manquant");
    }
    
    // ===========================
    // API PUBLIQUE
    // ===========================
    
    /// <summary>
    /// Spawne 3 pommes espacées (Adam, Eve, Lilith)
    /// 
    /// APPELÉ PAR :
    /// - GameManager.OnIntroComplete() (première fois)
    /// - OnAppleEaten() (après chaque pomme mangée)
    /// - AppleLifetimeCoroutine() (quand une pomme expire)
    /// 
    /// LOGIQUE :
    /// 1. Nettoyer les anciennes pommes
    /// 2. Déterminer le mode (normal ou Lilith immortelle)
    /// 3. Calculer 3 positions espacées
    /// 4. Instancier les 3 pommes
    /// 5. Ajouter le blink visuel
    /// 6. Lancer les coroutines de durée de vie
    /// </summary>
    public void SpawnThreeApples()
    {
        CleanupOldApples();

        bool isLilithMode = _snake != null && _snake.IsImmortal && _snake.CurrentPath == SnakeController.Path.Lilith;
        float lifetime = isLilithMode ? 5f : 10f;
        
        Vector2 adamPos = GetSpawnPosition(isLilithMode);
        Vector2 evePos = GetSpawnPosition(isLilithMode, adamPos, 2f);
        Vector2 lilithPos = GetSpawnPosition(isLilithMode, new Vector2[] { adamPos, evePos }, 2f);
        
        _currentAdamApple = Instantiate(_adamApplePrefab, adamPos, Quaternion.identity);
        _currentEveApple = Instantiate(_eveApplePrefab, evePos, Quaternion.identity);
        _currentLilithApple = Instantiate(_lilithApplePrefab, lilithPos, Quaternion.identity);
        
        _currentAdamApple.AddComponent<AppleBlink>().Initialize(lifetime);
        _currentEveApple.AddComponent<AppleBlink>().Initialize(lifetime);
        _currentLilithApple.AddComponent<AppleBlink>().Initialize(lifetime);
        
        _adamLifetimeCoroutine = StartCoroutine(AppleLifetimeCoroutine(_currentAdamApple, lifetime));
        _eveLifetimeCoroutine = StartCoroutine(AppleLifetimeCoroutine(_currentEveApple, lifetime));
        _lilithLifetimeCoroutine = StartCoroutine(AppleLifetimeCoroutine(_currentLilithApple, lifetime));
    }
    
    /// <summary>
    /// Appelé par Apple.Eated() quand une pomme est consommée
    /// 
    /// SÉQUENCE :
    /// 1. Notifier PathTracker (narration + changement couleur murs)
    /// 2. Programmer le respawn après délai (respecte pause)
    /// </summary>
    public void OnAppleEaten(Apple.AppleType eatenType)
    {
        ServiceLocator.Get<PathTracker>()?.RegisterAppleChoice(eatenType);
        StartCoroutine(SpawnThreeApplesDelayed(_spawnDelay));
    }
    
    // ===========================
    // LOGIQUE INTERNE
    // ===========================
    
    /// <summary>
    /// Nettoie les pommes et coroutines précédentes
    /// </summary>
    private void CleanupOldApples()
    {
        if (_adamLifetimeCoroutine != null) StopCoroutine(_adamLifetimeCoroutine);
        if (_eveLifetimeCoroutine != null) StopCoroutine(_eveLifetimeCoroutine);
        if (_lilithLifetimeCoroutine != null) StopCoroutine(_lilithLifetimeCoroutine);
        
        if (_currentAdamApple != null) Destroy(_currentAdamApple);
        if (_currentEveApple != null) Destroy(_currentEveApple);
        if (_currentLilithApple != null) Destroy(_currentLilithApple);
    }
    
    /// <summary>
    /// Retourne une position de spawn selon le mode
    /// MODE NORMAL : grille | MODE LILITH : map complète
    /// </summary>
    private Vector2 GetSpawnPosition(bool isLilithMode, Vector2? avoidPos = null, float minDistance = 0f)
    {
        Vector2 pos;
        int attempts = 0;
        
        do
        {
            pos = isLilithMode 
                ? new Vector2(Mathf.Round(Random.Range(-35f, 35f)), Mathf.Round(Random.Range(-15f, 15f)))
                : _grid.GetRandomCellPosition();
            
            attempts++;
        }
        while (avoidPos.HasValue && Vector2.Distance(pos, avoidPos.Value) < minDistance && attempts < 10);
        
        return pos;
    }
    
    /// <summary>
    /// Version avec plusieurs positions à éviter
    /// </summary>
    private Vector2 GetSpawnPosition(bool isLilithMode, Vector2[] avoidPositions, float minDistance)
    {
        Vector2 pos;
        int attempts = 0;
        bool valid;
        
        do
        {
            pos = isLilithMode 
                ? new Vector2(Mathf.Round(Random.Range(-35f, 35f)), Mathf.Round(Random.Range(-15f, 15f)))
                : _grid.GetRandomCellPosition();
            
            valid = true;
            
            foreach (Vector2 avoidPos in avoidPositions)
            {
                if (Vector2.Distance(pos, avoidPos) < minDistance)
                {
                    valid = false;
                    break;
                }
            }
            
            attempts++;
        }
        while (!valid && attempts < 10);
        
        return pos;
    }
    
    /// <summary>
    /// Attend le délai avant respawn (respecte pause)
    /// </summary>
    private IEnumerator SpawnThreeApplesDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnThreeApples();
    }
    
    /// <summary>
    /// Gère la durée de vie d'une pomme
    /// </summary>
    private IEnumerator AppleLifetimeCoroutine(GameObject apple, float lifetime)
    {
        float elapsed = 0f;
        
        while (elapsed < lifetime)
        {
            if (Time.timeScale > 0f)
            {
                elapsed += Time.deltaTime;
            }
            yield return null;
        }

        if (apple != null)
        {
            Destroy(apple);
            SpawnThreeApples();
        }
    }
}