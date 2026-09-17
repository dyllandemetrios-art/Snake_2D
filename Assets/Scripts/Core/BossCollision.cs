using UnityEngine;

/// <summary>
/// BossCollision - Détection et gestion des collisions du boss
/// 
/// RESPONSABILITÉS :
/// Détecter les collisions avec le joueur (tête et segments)
/// Voler des segments au joueur quand le boss le touche
/// Gérer la mort du boss si joueur ≥ 11 pommes Lilith
/// Gérer le game over du joueur si < 11 pommes Lilith
/// Gérer le cooldown pour éviter le spam de vol
/// 
/// LOGIQUE DES COLLISIONS :
/// 
/// BOSS TOUCHE JOUEUR (tête ou segments) :
///   Joueur < 11 pommes → GAME OVER du joueur
///   Joueur ≥ 11 pommes :
///     - Boss touche tête joueur → Vole 1 segment (cooldown 1s)
///     - Boss touche segments joueur → Vole 1 segment (cooldown 1s)
/// 
/// JOUEUR TOUCHE BOSS :
///   Joueur < 11 pommes → GAME OVER du joueur
///   Joueur ≥ 11 pommes :
///     - Joueur touche TÊTE boss → Boss.Die() (VICTOIRE)
///     - Joueur touche SEGMENTS boss → Boss.LoseSegment() (géré dans SnakeCollision)
/// 
/// SYSTÈME DE DÉTECTION :
/// OnTriggerEnter2D : Segments du joueur (SnakeBody)
/// OnCollisionEnter2D : Tête du joueur (Player)
/// 
/// PATTERN : Component (attaché au GameObject BossSnake)
/// </summary>
public class BossCollision : MonoBehaviour
{
    // ===========================
    // RÉFÉRENCES
    // ===========================
    
    /// <summary>
    /// Référence au BossSnake (pour accéder à BodyLength)
    /// </summary>
    private BossSnake _bossSnake;
    
    /// <summary>
    /// Référence au SnakeController du joueur
    /// Utilisé pour voler ses segments
    /// </summary>
    private SnakeController _playerController;
    
    // ===========================
    // COOLDOWN VOL
    // ===========================
    
    /// <summary>
    /// Cooldown entre deux vols de segments (secondes)
    /// Empêche le spam de collision
    /// </summary>
    private float _stealCooldown = 0f;
    
    /// <summary>
    /// Durée du cooldown de vol
    /// </summary>
    [SerializeField] private float _stealCooldownDuration = 1f;
    
    // ===========================
    // INITIALISATION
    // ===========================
    
    /// <summary>
    /// Récupère les références au démarrage
    /// </summary>
    void Awake()
    {
        Debug.Log("[BOSS COLLISION] Awake() - Initialisation");
        
        // Récupérer le BossSnake sur le même GameObject
        _bossSnake = GetComponent<BossSnake>();
        
        if (_bossSnake != null)
        {
            Debug.Log("[BOSS COLLISION] BossSnake trouvé");
        }
        else
        {
            Debug.LogError("[BOSS COLLISION] BossSnake introuvable !");
        }
        
        // Trouver le joueur
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerController = player.GetComponent<SnakeController>();
            Debug.Log("[BOSS COLLISION] Joueur trouvé et référencé");
        }
        else
        {
            Debug.LogError("[BOSS COLLISION] Snake joueur introuvable !");
        }
    }
    
    // ===========================
    // UPDATE (Cooldown)
    // ===========================
    
    /// <summary>
    /// Décrémente le cooldown de vol chaque frame
    /// </summary>
    void Update()
    {
        if (_stealCooldown > 0f)
        {
            _stealCooldown -= Time.deltaTime;
        }
    }
    
    // ===========================
    // COLLISION TRIGGER (Segments Joueur)
    // ===========================
    
    /// <summary>
/// Détecte les collisions TRIGGER
/// 
/// UTILISÉ POUR :
/// - Segments du joueur (SnakeBody) : Is Trigger = true
/// 
/// LOGIQUE :
/// Boss touche segments joueur → TOUJOURS voler 1 segment (cooldown 1s)
/// (La condition des 11 pommes s'applique uniquement quand le JOUEUR touche le BOSS)
/// </summary>
private void OnTriggerEnter2D(Collider2D other)
{
    Debug.Log($"[BOSS COLLISION] OnTriggerEnter2D avec {other.name} (tag: {other.tag})");
    
    // COLLISION AVEC SEGMENTS DU JOUEUR
    if (other.CompareTag("SnakeBody"))
    {
        Debug.Log("[BOSS COLLISION] Collision avec segment du joueur");
        
        // Cooldown pour éviter le spam
        if (_stealCooldown > 0f)
        {
            Debug.Log($"[BOSS COLLISION] Cooldown actif ({_stealCooldown:F2}s), vol ignoré");
            return;
        }
    
        // Vérifier que le joueur a des segments à voler
        if (_playerController != null && _playerController.BodyLength > 1)
        {
            _playerController.BodyLength--;
            _bossSnake.Grow();
            _stealCooldown = _stealCooldownDuration;
            Debug.Log("[BOSS COLLISION] Segment volé au joueur !");
        }
        return;
    }
}

/// <summary>
/// Détecte les collisions PHYSIQUES
/// 
/// UTILISÉ POUR :
/// - Tête du joueur (Player) : Is Trigger = false
/// 
/// LOGIQUE :
/// Boss touche tête joueur :
///   - Joueur < 11 pommes → GAME OVER
///   - Joueur ≥ 11 pommes → Boss meurt (VICTOIRE)
/// </summary>
private void OnCollisionEnter2D(Collision2D collision)
{
    Debug.Log($"[BOSS COLLISION] OnCollisionEnter2D avec {collision.gameObject.name} (tag: {collision.gameObject.tag})");
    
    // TÊTE DU JOUEUR
    if (collision.gameObject.CompareTag("Player"))
    {
        Debug.Log("[BOSS COLLISION] Collision avec tête du joueur");
        
        // VÉRIFIER LES POMMES LILITH
        PathTracker pathTracker = ServiceLocator.Get<PathTracker>();
        int pommes = pathTracker != null ? pathTracker.LilithOutsideCount : 0;
        
        Debug.Log($"[BOSS COLLISION] Pommes Lilith dehors: {pommes}/11");
        
        // CAS 1 : JOUEUR A ≥ 11 POMMES → BOSS MEURT
        if (pommes >= 11)
        {
            Debug.Log("[BOSS COLLISION] JOUEUR DÉVORE LE BOSS !");
            _bossSnake.Die();
            return;
        }
        
        // CAS 2 : JOUEUR A < 11 POMMES → GAME OVER
        Debug.Log("[BOSS COLLISION] Pas assez de pommes → GAME OVER du joueur");
        TriggerPlayerGameOver();
        return;
    }
}
    
    // ===========================
    // GAME OVER DU JOUEUR
    // ===========================
    
    /// <summary>
    /// Déclenche le game over du joueur
    /// 
    /// SÉQUENCE :
    /// 1. Arrêter le mouvement du joueur
    /// 2. Arrêter la musique
    /// 3. Jouer le son Death
    /// 4. Informer GameManager
    /// </summary>
    private void TriggerPlayerGameOver()
    {
        Debug.Log("[BOSS COLLISION] TriggerPlayerGameOver() appelée");
        
        if (_playerController != null)
        {
            _playerController.StopMovement();
        }
        
        ServiceLocator.Get<AudioManager>()?.StopMusic();
        ServiceLocator.Get<AudioManager>()?.PlaySFX(AudioManager.SoundEffect.Death);
        
        if (GameManager.Instance != null)
        {
            Debug.Log("[BOSS COLLISION] Appel GameManager.OnSnakeDeath(false)");
            GameManager.Instance.OnSnakeDeath(isLilithInitiation: false);
        }
        else
        {
            Debug.LogError("[BOSS COLLISION] GameManager.Instance est null !");
        }
    }
}