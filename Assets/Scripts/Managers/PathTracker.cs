using UnityEngine;

/// <summary>
/// PathTracker - Suivi des choix du joueur et changement de couleur des murs
/// 
/// RESPONSABILITÉS :
/// - Compter les pommes mangées de chaque type (Adam/Eve/Lilith)
/// - Déclencher les narrations aux paliers clés (1, 3, 5, 7, 11 pommes)
/// - Changer la couleur de TOUS les murs selon la DERNIÈRE pomme mangée
/// - Tracker les statistiques de jeu (morts, temps, initiation Lilith)
/// - Spawner le boss à 5 pommes Lilith après renaissance
/// 
/// SYSTÈME DE COULEUR MURS :
/// - Adam → Bleu (couleur de la pomme Adam)
/// - Eve → Vert (couleur de la pomme Eve)
/// - Lilith → Rouge (couleur de la pomme Lilith)
/// 
/// La couleur change IMMÉDIATEMENT après chaque pomme mangée
/// Permet au joueur de savoir quel chemin il suit actuellement
/// 
/// PATTERN : Service (enregistré dans ServiceLocator)
/// </summary>
public class PathTracker : MonoBehaviour
{
    // ===========================
    // COMPTEURS DE POMMES
    // ===========================
    
    private int _adamCount = 0;
    private int _eveCount = 0;
    private int _lilithCount = 0;
    private int _lilithOutsideCount = 0;
    
    public int LilithOutsideCount => _lilithOutsideCount;
    public int LilithCount => _lilithCount;
    
    // ===========================
    // INDEX DE NARRATION
    // ===========================
    
    private int _adamNarrativeIndex = 1;
    private int _eveNarrativeIndex = 1;
    private int _lilithNarrativeIndex = 1;
    
    private readonly int[] _narrativeTriggers = { 1, 3, 5, 7, 11 };
    
    // ===========================
    // STATISTIQUES DE JEU
    // ===========================
    
    private bool _hasExperiencedLilithDeath = false;
    private int _totalDeaths = 0;
    private float _totalGameTime = 0f;
    private bool _freedomNarrativePlayed = false;
    
    // ===========================
    // PROPRIÉTÉS PUBLIQUES
    // ===========================
    
    public int AdamCount => _adamCount;
    public int EveCount => _eveCount;
    public int TotalApples => _adamCount + _eveCount + _lilithCount;
    public bool HasExperiencedLilithDeath => _hasExperiencedLilithDeath;
    public int TotalDeaths => _totalDeaths;
    public float TotalGameTime => _totalGameTime;
    
    // ===========================
    // BOSS
    // ===========================
    
    [Header("Boss")]
    [SerializeField] private GameObject _bossPrefab;
    
    // ===========================
    // COULEURS DES MURS
    // ===========================
    
    [Header("Wall Colors")]
    [SerializeField] private Color _adamColor = new Color(0.2f, 0.4f, 0.8f);
    [SerializeField] private Color _eveColor = new Color(0.3f, 0.7f, 0.3f);
    [SerializeField] private Color _lilithColor = new Color(0.8f, 0.2f, 0.2f);
    
    // ===========================
    // INITIALISATION
    // ===========================
    
    void Start()
    {
        GameObject[] walls = GameObject.FindGameObjectsWithTag("Obstacles");
        if (walls.Length == 0)
        {
            Debug.LogError("[PATH TRACKER] Aucun GameObject avec tag 'Obstacles' trouvé !");
        }
        else
        {
            Debug.Log($"[PATH TRACKER] {walls.Length} murs trouvés");
        }
    }
    
    // ===========================
    // MISE À JOUR DU TIMER
    // ===========================
    
    void Update()
    {
        _totalGameTime += Time.deltaTime;
    }
    
    // ===========================
    // ENREGISTREMENT DES CHOIX
    // ===========================
    
    /// <summary>
    /// Enregistre le choix d'une pomme et change la couleur des murs
    /// </summary>
    public void RegisterAppleChoice(Apple.AppleType type)
    {
        switch (type)
        {
            case Apple.AppleType.Adam:
                _adamCount++;
                Debug.Log($"[PATH TRACKER] Adam mangée (Total: {_adamCount})");
                UpdateWallColors(Apple.AppleType.Adam);
                
                SnakeController snake = FindFirstObjectByType<SnakeController>();
                if (snake == null || !snake.IsImmortal)
                {
                    CheckNarrativeTrigger(Apple.AppleType.Adam, _adamCount, ref _adamNarrativeIndex);
                }
                break;
            
            case Apple.AppleType.Eve:
                _eveCount++;
                Debug.Log($"[PATH TRACKER] Ève mangée (Total: {_eveCount})");
                UpdateWallColors(Apple.AppleType.Eve);
                
                snake = FindFirstObjectByType<SnakeController>();
                if (snake == null || !snake.IsImmortal)
                {
                    CheckNarrativeTrigger(Apple.AppleType.Eve, _eveCount, ref _eveNarrativeIndex);
                }
                break;
            
            case Apple.AppleType.Lilith:
                _lilithCount++;
                Debug.Log($"[PATH TRACKER] Lilith mangée (Total: {_lilithCount})");
                UpdateWallColors(Apple.AppleType.Lilith);

                snake = FindFirstObjectByType<SnakeController>();
                if (snake != null && snake.IsImmortal)
                {
                    _lilithOutsideCount++;
        
                    Debug.Log($"[PATH TRACKER] Pommes Lilith dehors : {_lilithOutsideCount}/11");
                    
                    if (ServiceLocator.Has<UIManager>() && ServiceLocator.Has<ScoreManager>())
                    {
                        ServiceLocator.Get<UIManager>().UpdateScore(ServiceLocator.Get<ScoreManager>().CurrentScore);
                    }
        
                    if (_lilithOutsideCount == 5)
                    {
                        Debug.Log("[PATH TRACKER] 5 pommes atteintes ! SPAWN DU BOSS !");
                        SpawnBoss();
                    }
        
                    if (!_freedomNarrativePlayed)
                    {
                        _freedomNarrativePlayed = true;
                        Debug.Log("[PATH TRACKER] Déclenchement narration : lilith_freedom");
                        ServiceLocator.Get<NarrativeManager>()?.PlayKnot("lilith_freedom", null);
                    }

                    break;
                }

                // Si pas immortel, déclencher narration normale
                CheckNarrativeTrigger(Apple.AppleType.Lilith, _lilithCount, ref _lilithNarrativeIndex);
                break;
        }
        
        CheckClassicVictory();
    }
    
    /// <summary>
    /// Change la couleur de TOUS les murs selon le type de pomme mangée
    /// </summary>
    private void UpdateWallColors(Apple.AppleType type)
    {
        Color targetColor = type switch
        {
            Apple.AppleType.Adam => _adamColor,
            Apple.AppleType.Eve => _eveColor,
            Apple.AppleType.Lilith => _lilithColor,
            _ => Color.white
        };
        
        GameObject[] walls = GameObject.FindGameObjectsWithTag("Obstacles");
        foreach (GameObject wall in walls)
        {
            SpriteRenderer sr = wall.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = targetColor;
            }
        }
        
        Debug.Log($"[PATH TRACKER] {walls.Length} murs changés en {type}");
    }

    // ===========================
    // SPAWN DU BOSS
    // ===========================
    
    private void SpawnBoss()
    {
        if (_bossPrefab == null)
        {
            Debug.LogError("[PATH TRACKER] Boss Prefab non assigné !");
            return;
        }

        SnakeController player = FindFirstObjectByType<SnakeController>();
        Vector3 spawnPos = player != null ? -player.transform.position : new Vector3(20f, 0f, 0f);

        GameObject boss = Instantiate(_bossPrefab, spawnPos, Quaternion.identity);
        boss.name = "BossSnake";

        Debug.Log($"[PATH TRACKER] Boss spawné à {spawnPos}");

        StartCoroutine(PlayBossNarrativeDelayed());
    }

    private System.Collections.IEnumerator PlayBossNarrativeDelayed()
    {
        yield return null;
    
        ServiceLocator.Get<NarrativeManager>()?.PlayKnot("lilith_vs_boss", OnBossNarrativeComplete);
    }
    
    private void OnBossNarrativeComplete()
    {
        BossSnake boss = FindFirstObjectByType<BossSnake>();
        if (boss != null)
        {
            boss.Release();
            Debug.Log("[PATHTRACKER] Boss libéré après narration, le combat commence !");
        }
    }
    
    // ===========================
    // VÉRIFICATION VICTOIRE CLASSIQUE
    // ===========================
    
    private void CheckClassicVictory()
    {
        SnakeController snake = FindFirstObjectByType<SnakeController>();
        if (snake == null) return;
        
        const int gridWidth = 51;
        const int gridHeight = 21;
        int totalGridCells = gridWidth * gridHeight;
        
        int victoryThreshold = (int)(totalGridCells * 0.8f);
        
        if (snake.BodyLength >= victoryThreshold)
        {
            Debug.Log($"[PATH TRACKER] VICTOIRE CLASSIQUE - Grille remplie !");
            GameManager.Instance?.TriggerEnding();
        }
    }
    
    // ===========================
    // DÉCLENCHEMENT DES NARRATIONS
    // ===========================
    
    private void CheckNarrativeTrigger(Apple.AppleType type, int count, ref int narrativeIndex)
    {
        if (System.Array.IndexOf(_narrativeTriggers, count) != -1)
        {
            string knotName = type switch
            {
                Apple.AppleType.Adam => $"apple_adam_{narrativeIndex}",
                Apple.AppleType.Eve => $"apple_eve_{narrativeIndex}",
                Apple.AppleType.Lilith => $"apple_lilith_{narrativeIndex}",
                _ => ""
            };
            
            Debug.Log($"[PATH TRACKER] Déclenchement narration : {knotName}");
            
            ServiceLocator.Get<NarrativeManager>()?.PlayKnot(knotName, null);
            
            narrativeIndex++;
        }
    }
    
    // ===========================
    // ENREGISTREMENT DES MORTS
    // ===========================
    
    public void RegisterDeath(bool isLilithInitiation)
    {
        _totalDeaths++;
        
        if (isLilithInitiation)
        {
            _hasExperiencedLilithDeath = true;
            Debug.Log("[PATH TRACKER] Initiation Lilith enregistrée");
        }
        
        Debug.Log($"[PATH TRACKER] Mort #{_totalDeaths}");
    }
    
    // ===========================
    // DÉTERMINATION DU CHEMIN DOMINANT
    // ===========================
    
    public Apple.AppleType GetDominantPath()
    {
        if (_hasExperiencedLilithDeath && _lilithCount >= 3)
        {
            Debug.Log("[PATH TRACKER] Chemin dominant : LILITH (initiation accomplie)");
            return Apple.AppleType.Lilith;
        }
        
        if (_adamCount > _eveCount && _adamCount > _lilithCount)
        {
            Debug.Log("[PATH TRACKER] Chemin dominant : ADAM");
            return Apple.AppleType.Adam;
        }
        else if (_eveCount > _adamCount && _eveCount > _lilithCount)
        {
            Debug.Log("[PATH TRACKER] Chemin dominant : ÈVE");
            return Apple.AppleType.Eve;
        }
        else if (_lilithCount > _adamCount && _lilithCount > _eveCount)
        {
            Debug.Log("[PATH TRACKER] Chemin dominant : LILITH");
            return Apple.AppleType.Lilith;
        }
        else
        {
            Debug.Log("[PATH TRACKER] Égalité, défaut : ÈVE");
            return Apple.AppleType.Eve;
        }
    }
    
    // ===========================
    // GÉNÉRATION DES STATISTIQUES
    // ===========================
    
    public string GetStatsBreakdown()
    {
        int total = TotalApples;
        
        float adamPercent = total > 0 ? (_adamCount / (float)total) * 100f : 0f;
        float evePercent = total > 0 ? (_eveCount / (float)total) * 100f : 0f;
        float lilithPercent = total > 0 ? (_lilithCount / (float)total) * 100f : 0f;
        
        string breakdown = $"=== STATISTIQUES ===\n\n";
        breakdown += $"Pommes mangées : {total}\n\n";
        breakdown += $"[ADAM] : {_adamCount} ({adamPercent:F1}%)\n";
        breakdown += $"[EVE] : {_eveCount} ({evePercent:F1}%)\n";
        breakdown += $"[LILITH] : {_lilithCount} ({lilithPercent:F1}%)\n\n";
        breakdown += $"Initiation Lilith : {(_hasExperiencedLilithDeath ? "OUI" : "NON")}\n";
        breakdown += $"Temps de jeu : {FormatTime(_totalGameTime)}\n";
        
        return breakdown;
    }
    
    private string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes:00}:{secs:00}";
    }
    
    // ===========================
    // RÉINITIALISATION
    // ===========================
    
    public void ResetStats()
    {
        _adamCount = 0;
        _eveCount = 0;
        _lilithCount = 0;
        _lilithOutsideCount = 0;
        _adamNarrativeIndex = 1;
        _eveNarrativeIndex = 1;
        _lilithNarrativeIndex = 1;
        _hasExperiencedLilithDeath = false;
        _totalDeaths = 0;
        _totalGameTime = 0f;
        _freedomNarrativePlayed = false;
        
        Debug.Log("[PATH TRACKER] Stats réinitialisées");
    }
}