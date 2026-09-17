using UnityEngine;

/// <summary>
/// SnakeCollision - Détection et gestion des collisions du serpent
/// 
/// RESPONSABILITÉS :
/// Détecter les collisions avec les murs (tag "Obstacles")
/// Détecter les collisions avec le corps (tag "SnakeBody")
/// Gérer les cas spéciaux Lilith (initiation, immortalité)
/// Informer GameManager de la mort du serpent
/// 
/// BOSS :
/// TOUTES les collisions avec le boss (tête ET segments) sont gérées dans BossCollision.cs
/// Ce script IGNORE complètement le boss
/// 
/// MURS (Obstacles) :
///   Adam/Eve → Mort immédiate
///   Lilith (première fois avec 6+ pommes) → Initiation
///   Lilith (immortelle) → Traversée (ignoré)
/// 
/// CORPS (SnakeBody) :
///   Tous → Mort (sauf Lilith immortelle)
///   Exception : Segment_1 toujours ignoré
/// 
/// SYSTÈME DE DÉTECTION :
/// OnTriggerEnter2D : Propre corps (SnakeBody) uniquement
/// OnCollisionEnter2D : Murs (Obstacles) uniquement
/// 
/// PATTERN : Component (attaché au GameObject Snake)
/// </summary>
public class SnakeCollision : MonoBehaviour
{
    // ===========================
    // RÉFÉRENCES
    // ===========================
    
    /// <summary>
    /// Référence au SnakeController du joueur
    /// Utilisé pour vérifier l'immortalité Lilith
    /// </summary>
    private SnakeController _snakeController;
    
    /// <summary>
    /// Flag pour éviter les double morts
    /// Empêche TriggerGameOver d'être appelé plusieurs fois
    /// </summary>
    private bool _isDead = false;

    // ===========================
    // INITIALISATION
    // ===========================
    
    /// <summary>
    /// Récupère le SnakeController au démarrage
    /// </summary>
    void Awake()
    {
        Debug.Log("[SNAKE COLLISION] Awake() - Initialisation");
        
        _snakeController = GetComponent<SnakeController>();
        
        if (_snakeController != null)
        {
            Debug.Log("[SNAKE COLLISION] SnakeController trouvé");
        }
        else
        {
            Debug.LogError("[SNAKE COLLISION] SnakeController introuvable !");
        }
    }
    
    // ===========================
    // COLLISION TRIGGER (Propre Corps uniquement)
    // ===========================
    
    /// <summary>
    /// Détecte les collisions TRIGGER
    /// 
    /// UTILISÉ POUR :
    /// - Propre corps (SnakeBody) : Is Trigger = true
    /// 
    /// IGNORE :
    /// - Boss et segments boss (géré dans BossCollision.cs)
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isDead) return;
        
        Debug.Log($"[SNAKE COLLISION] OnTriggerEnter2D avec {other.name} (tag: {other.tag})");
        
        // IGNORER COMPLÈTEMENT LE BOSS
        if (other.CompareTag("BossSnake") || other.CompareTag("BossBody"))
        {
            Debug.Log("[SNAKE COLLISION] Boss détecté → IGNORÉ (géré dans BossCollision)");
            return;
        }
    
        // COLLISION AVEC SON PROPRE CORPS
        if (other.CompareTag("SnakeBody"))
        {
            Debug.Log($"[SNAKE COLLISION] Collision avec propre corps: {other.name}");
            
            // Ignorer le segment juste derrière la tête
            if (other.name.Contains("Segment_1"))
            {
                Debug.Log("[SNAKE COLLISION] Segment_1 détecté → Ignoré");
                return;
            }
            
            HandleBodyCollision();
            return;
        }
    }
    
    // ===========================
    // COLLISION PHYSIQUE (Murs uniquement)
    // ===========================
    
    /// <summary>
    /// Détecte les collisions PHYSIQUES
    /// 
    /// UTILISÉ POUR :
    /// - Murs (Obstacles)
    /// 
    /// IGNORE :
    /// - Boss (géré dans BossCollision.cs)
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_isDead) return;
    
        Debug.Log($"[SNAKE COLLISION] OnCollisionEnter2D avec {collision.gameObject.name} (tag: {collision.gameObject.tag})");
    
        // IGNORER COMPLÈTEMENT LE BOSS
        if (collision.gameObject.CompareTag("BossSnake"))
        {
            Debug.Log("[SNAKE COLLISION] Boss détecté → IGNORÉ (géré dans BossCollision)");
            return;
        }
    
        // COLLISION AVEC LES MURS
        if (collision.gameObject.CompareTag("Obstacles"))
        {
            Debug.Log("[SNAKE COLLISION] Collision avec un mur");
            HandleObstacleCollision();
            return;
        }
    }
    
    // ===========================
    // GESTION COLLISION MURS
    // ===========================
    
    /// <summary>
    /// Gère la collision avec un mur
    /// 
    /// LOGIQUE :
    /// 1. Lilith pré-initiation (6+ pommes) → Initiation
    /// 2. Lilith immortelle → Ignore
    /// 3. Adam/Eve ou Lilith <6 pommes → Mort
    /// </summary>
    private void HandleObstacleCollision()
    {
        Debug.Log($"[SNAKE COLLISION] Chemin actuel: {_snakeController.CurrentPath}");
        Debug.Log($"[SNAKE COLLISION] Est immortel: {_snakeController.IsImmortal}");
        
        // CAS 1 : Initiation Lilith
        if (_snakeController.CurrentPath == SnakeController.Path.Lilith 
            && !_snakeController.IsImmortal)
        {
            Debug.Log("[SNAKE COLLISION] Chemin Lilith + Pas immortel");
            
            PathTracker pathTracker = ServiceLocator.Get<PathTracker>();
            if (pathTracker != null && pathTracker.LilithCount >= 6)
            {
                Debug.Log($"[SNAKE COLLISION] {pathTracker.LilithCount} pommes Lilith → INITIATION");
                TriggerLilithInitiation();
                return;
            }
        
            // Lilith avec moins de 6 pommes → Mort normale
            Debug.Log("[SNAKE COLLISION] Pas assez de pommes Lilith → MORT");
            TriggerGameOver("Collision avec un mur (Lilith insuffisant)");
            return;
        }

        // CAS 2 : Lilith immortelle → Ignore les murs
        if (_snakeController.CurrentPath == SnakeController.Path.Lilith 
            && _snakeController.IsImmortal)
        {
            Debug.Log("[SNAKE COLLISION] Lilith immortelle → Collision mur IGNORÉE");
            return;
        }

        // CAS 3 : Adam/Eve → Mort
        Debug.Log("[SNAKE COLLISION] Adam/Eve → MORT");
        TriggerGameOver("Collision avec un mur");
    }
    
    /// <summary>
    /// Gère la collision avec son propre corps
    /// 
    /// LOGIQUE :
    /// - Lilith immortelle → Ignore
    /// - Sinon → Mort
    /// </summary>
    private void HandleBodyCollision()
    {
        Debug.Log($"[SNAKE COLLISION] Gestion collision corps - Chemin: {_snakeController.CurrentPath}, Immortel: {_snakeController.IsImmortal}");
        
        // Lilith immortelle traverse son propre corps
        if (_snakeController.CurrentPath == SnakeController.Path.Lilith 
            && _snakeController.IsImmortal)
        {
            Debug.Log("[SNAKE COLLISION] Lilith immortelle → Collision corps IGNORÉE");
            return;
        }
    
        Debug.Log("[SNAKE COLLISION] Pas d'immortalité → MORT");
        TriggerGameOver("Collision avec le corps");
    }
    
    // ===========================
    // INITIATION LILITH
    // ===========================
    
    /// <summary>
    /// Déclenche l'initiation Lilith (première mort)
    /// 
    /// SÉQUENCE :
    /// 1. Arrêter le mouvement du serpent
    /// 2. Arrêter la musique
    /// 3. Jouer les sons Death + Rebirth
    /// 4. Informer GameManager (isLilithInitiation = true)
    /// 5. GameManager déclenche la narration "first_death"
    /// 6. Après la narration → Renaissance (immortalité)
    /// </summary>
    private void TriggerLilithInitiation()
    {
        if (_isDead) return;
        _isDead = true;
        
        Debug.Log("[SNAKE COLLISION] INITIATION LILITH déclenchée");
        
        _snakeController.StopMovement();
        
        ServiceLocator.Get<AudioManager>()?.StopMusic();
        ServiceLocator.Get<AudioManager>()?.PlaySFX(AudioManager.SoundEffect.Death);
        ServiceLocator.Get<AudioManager>()?.PlaySFX(AudioManager.SoundEffect.Rebirth);
    
        if (GameManager.Instance != null)
        {
            Debug.Log("[SNAKE COLLISION] Appel GameManager.OnSnakeDeath(true)");
            GameManager.Instance.OnSnakeDeath(isLilithInitiation: true);
        }
        else
        {
            Debug.LogError("[SNAKE COLLISION] GameManager.Instance est null !");
        }
    }
    
    // ===========================
    // GAME OVER
    // ===========================
    
    /// <summary>
    /// Déclenche le Game Over
    /// 
    /// SÉQUENCE :
    /// 1. Arrêter le mouvement du serpent
    /// 2. Arrêter la musique
    /// 3. Jouer le son Death
    /// 4. Informer GameManager (isLilithInitiation = false)
    /// 5. GameManager déclenche l'ending selon le chemin dominant
    /// </summary>
    /// <param name="reason">Raison de la mort (pour les logs)</param>
    private void TriggerGameOver(string reason)
    {
        if (_isDead) return;
        _isDead = true;
        
        Debug.Log($"[SNAKE COLLISION] GAME OVER - Raison: {reason}");
    
        _snakeController.StopMovement();
        
        ServiceLocator.Get<AudioManager>()?.StopMusic();
        ServiceLocator.Get<AudioManager>()?.PlaySFX(AudioManager.SoundEffect.Death);
    
        if (GameManager.Instance != null)
        {
            Debug.Log("[SNAKE COLLISION] Appel GameManager.OnSnakeDeath(false)");
            GameManager.Instance.OnSnakeDeath(isLilithInitiation: false);
        }
        else
        {
            Debug.LogError("[SNAKE COLLISION] GameManager.Instance est null !");
        }
    }
}