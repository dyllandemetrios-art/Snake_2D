using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// SnakeController - Cerveau et moteur du serpent
/// 
/// RESPONSABILITÉS :
/// Lire les inputs du joueur (flèches directionnelles)
/// Déplacer la tête du serpent sur la grille
/// Gérer la direction et empêcher les demi-tours
/// Stocker l'état du serpent (chemin philosophique, stats, buffs)
/// Gérer les transformations Lilith (immortalité, renaissance)
/// Appliquer les buffs des pommes (vitesse, scale, multiplicateur)
/// 
/// DÉLÉGATIONS :
/// Croissance visuelle du corps → SnakeBody
/// Détection des collisions → SnakeCollision
/// Gestion de la mort → GameManager
/// 
/// BUFFS POMMES (calculés depuis valeurs de base) :
/// Adam : Scale x2, Multiplicateur x2
/// Eve : Multiplicateur x4
/// Lilith : Vitesse x1.5 (x2 après rebirth)
/// 
/// PATTERN : Component (ce script contrôle la tête du serpent)
/// </summary>
public class SnakeController : MonoBehaviour
{
    // ===========================
    // CONFIGURATION (Inspector)
    // ===========================
    
    [Header("Movement Settings")]
    /// <summary>
    /// Vitesse de base du serpent (en unités par frame)
    /// Valeurs typiques : 0.1 à 0.3
    /// Plus petit = plus lent, plus grand = plus rapide
    /// </summary>
    [SerializeField] private float _baseSpeed = 0.2f;
    
    [Header("Grid Reference")]
    /// <summary>
    /// Référence au GridManager pour connaître la taille des cellules
    /// Utilisé pour aligner le mouvement sur la grille
    /// </summary>
    [SerializeField] private GridManager _grid;
    
    [Header("Body Reference")]
    /// <summary>
    /// Référence au SnakeBody qui gère les segments visuels
    /// Quand le serpent grandit, on appelle _snakeBody.Grow()
    /// </summary>
    [SerializeField] private SnakeBody _snakeBody;
    
    // ===========================
    // VALEURS DE BASE (pour calcul des buffs)
    // ===========================
    
    /// <summary>
    /// Scale de base du serpent
    /// Utilisé pour calculer le scale avec buff Adam (x2)
    /// </summary>
    private float _baseScale = 1f;
    
    /// <summary>
    /// Multiplicateur de score de base
    /// Utilisé pour calculer le multiplicateur avec buffs pommes
    /// </summary>
    private int _baseScoreMultiplier = 1;
    
    // ===========================
    // ÉTAT DU SERPENT
    // ===========================
    
    /// <summary>
    /// Vitesse actuelle (peut changer avec les buffs pommes)
    /// Calculée à partir de _baseSpeed × modificateur
    /// </summary>
    private float _currentSpeed;
    
    /// <summary>
    /// Nombre total de segments du corps
    /// Augmente quand on mange des pommes
    /// BodyLength = 1 → Juste la tête
    /// BodyLength = 5 → Tête + 4 segments
    /// </summary>
    private int _bodyLength = 1;
    
    /// <summary>
    /// Multiplicateur de score actuel (x1, x2, x4...)
    /// Adam : x2 (fixe)
    /// Eve : x4 (fixe)
    /// Lilith : x1 de base (ou hérité du chemin précédent)
    /// </summary>
    private int _scoreMultiplier = 1;
    
    // ===========================
    // Flags Lilith (capacités spéciales)
    // ===========================
    
    /// <summary>
    /// Immunité aux collisions (murs + corps)
    /// Activé APRÈS la première mort en chemin Lilith
    /// </summary>
    private bool _isImmortal = false;
    
    /// <summary>
    /// Peut sortir de la zone de jeu (zone entre Box et Edge Collider)
    /// Activé avec l'immortalité
    /// </summary>
    private bool _canExitGrid = false;
    
    /// <summary>
    /// Le serpent peut-il bouger ?
    /// False pendant : pause, narration, game over
    /// </summary>
    private bool _canMove = true;
    
    // ===========================
    // MOUVEMENT
    // ===========================
    
    /// <summary>
    /// Direction actuelle du mouvement
    /// Appliquée dans FixedUpdate
    /// </summary>
    private Vector2 _currentDirection = Vector2.right;
    
    /// <summary>
    /// Direction demandée par le joueur (input)
    /// Stockée temporairement, appliquée au prochain FixedUpdate
    /// Évite les bugs d'input trop rapide
    /// </summary>
    private Vector2 _nextDirection = Vector2.right;
    
    /// <summary>
    /// Rigidbody2D pour le déplacement physique
    /// Utilisé avec MovePosition (pas de vélocité)
    /// </summary>
    private Rigidbody2D _rb;
    
    // ===========================
    // ENUM
    // ===========================
    
    /// <summary>
    /// Les trois chemins philosophiques du jeu
    /// Adam : Domination (scale x2, multiplicateur x2)
    /// Eve : Adaptation (multiplicateur x4)
    /// Lilith : Transgression (vitesse x1.5, renaissance, immortalité)
    /// </summary>
    public enum Path { Adam, Eve, Lilith }
    
    // ===========================
    // PROPRIÉTÉS
    // ===========================
    
    /// <summary>
    /// Chemin philosophique actuel
    /// Détermine le type de pomme dominant mangé
    /// </summary>
    public Path CurrentPath { get; set; } = Path.Adam;
    
    /// <summary>
    /// Nombre de segments du corps
    /// SETTER : Fait grandir le corps visuellement via SnakeBody
    /// EXEMPLE : BodyLength = 5 (était 3)
    ///   growth = 5 - 3 = 2
    ///   _snakeBody.Grow(2) → Ajoute 2 segments visuels
    /// </summary>
    public int BodyLength 
    { 
        get => _bodyLength; 
        set 
        {
            // Calculer combien de segments ajouter
            int growth = value - _bodyLength;
            _bodyLength = value;
        
            // Faire grandir le corps visuellement
            if (_snakeBody != null && growth > 0)
            {
                _snakeBody.Grow(growth);
            }
            else if (_snakeBody == null)
            {
                Debug.LogWarning("[SNAKE] SnakeBody non assigné dans l'inspector !");
            }
        } 
    }
    
    /// <summary>
    /// Multiplicateur de score (lecture/écriture)
    /// Les pommes peuvent le modifier directement
    /// </summary>
    public int ScoreMultiplier 
    { 
        get => _scoreMultiplier; 
        set => _scoreMultiplier = value; 
    }
    
    // Propriétés lecture seule (état Lilith et vitesse)
    public bool IsImmortal => _isImmortal;
    public bool CanExitGrid => _canExitGrid;
    public float CurrentSpeed => _currentSpeed;
    public Vector2 CurrentDirection => _currentDirection;
    
    // ===========================
    // INITIALISATION
    // ===========================
    
    /// <summary>
    /// Initialisation au démarrage
    /// Récupère les composants nécessaires
    /// Initialise les valeurs de base
    /// </summary>
    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _currentSpeed = _baseSpeed;
        _scoreMultiplier = _baseScoreMultiplier;
        
        // Auto-find SnakeBody si non assigné dans l'inspector
        if (_snakeBody == null)
        {
            _snakeBody = GetComponent<SnakeBody>();
        }
        
        Debug.Log($"[SNAKE] Initialisé - BaseSpeed: {_baseSpeed}, BaseScale: {_baseScale}, BaseMultiplier: {_baseScoreMultiplier}");
    }
    
    // ===========================
    // UPDATE (Input seulement)
    // ===========================
    
    /// <summary>
    /// Appelé chaque frame
    /// Utilisé UNIQUEMENT pour lire les inputs
    /// Le mouvement se fait dans FixedUpdate (physique)
    /// </summary>
    private void Update()
    {
        HandleInput();
    }
    
    // ===========================
    // FIXED UPDATE (Mouvement physique)
    // ===========================
    
    /// <summary>
    /// Appelé à intervalle fixe (physique)
    /// Déplace le serpent dans la direction actuelle
    /// 
    /// POURQUOI FixedUpdate ?
    /// Update : Variable (30-120 fps)
    /// FixedUpdate : Fixe (50 fps par défaut)
    /// Garantit un mouvement constant et prévisible
    /// 
    /// PAUSE :
    /// Time.timeScale = 0 arrête FixedUpdate mais peut prendre 1-2 frames
    /// On check aussi _canMove pour double sécurité
    /// </summary>
    void FixedUpdate()
    {
        // VÉRIFIER SI EN PAUSE (Time.timeScale = 0)
        if (Time.timeScale == 0f) return;
        
        // Si en pause/narration, ne pas bouger
        if (!_canMove) return;
        
        // Appliquer la direction demandée
        // Évite les bugs si le joueur spam les touches
        _currentDirection = _nextDirection;
        
        // Déplacer le Rigidbody2D
        // MovePosition = téléportation (pas de vélocité)
        _rb.MovePosition(_rb.position + _currentDirection * _currentSpeed);
    }
    
    // ===========================
    // GESTION DES INPUTS
    // ===========================
    
    /// <summary>
    /// Lit les inputs du joueur (flèches directionnelles)
    /// Empêche les demi-tours (droite→gauche impossible)
    /// 
    /// LOGIQUE ANTI-DEMI-TOUR :
    /// Si direction actuelle = droite, on ne peut PAS aller à gauche
    /// Sinon le serpent se mangerait lui-même instantanément
    /// </summary>
    private void HandleInput()
    {
        // Droite (sauf si on va déjà à gauche)
        if (Input.GetKeyDown(KeyCode.RightArrow) && _currentDirection != Vector2.left)
        {
            _nextDirection = Vector2.right;
        }
        // Gauche (sauf si on va déjà à droite)
        else if (Input.GetKeyDown(KeyCode.LeftArrow) && _currentDirection != Vector2.right)
        {
            _nextDirection = Vector2.left;
        }
        // Haut (sauf si on va déjà en bas)
        else if (Input.GetKeyDown(KeyCode.UpArrow) && _currentDirection != Vector2.down)
        {
            _nextDirection = Vector2.up;
        }
        // Bas (sauf si on va déjà en haut)
        else if (Input.GetKeyDown(KeyCode.DownArrow) && _currentDirection != Vector2.up)
        {
            _nextDirection = Vector2.down;
        }
    }
    
    // ===========================
    // MODIFICATEURS (appelés par les pommes)
    // ===========================
    
    /// <summary>
    /// Modifie la vitesse avec un multiplicateur DEPUIS LA BASE
    /// 
    /// FORMULE : vitesse actuelle = vitesse de base × modificateur
    /// 
    /// EXEMPLES :
    /// Lilith pré-rebirth : ModifySpeed(1.5) → vitesse = 0.2 × 1.5 = 0.3
    /// Lilith post-rebirth : ModifySpeed(2.0) → vitesse = 0.2 × 2.0 = 0.4
    /// Adam/Eve : ModifySpeed(1.0) → vitesse = 0.2 × 1.0 = 0.2 (base)
    /// 
    /// APPELÉ PAR : AppleLilith.DoOnEated()
    /// </summary>
    /// <param name="speedModifier">Multiplicateur depuis la base (1.0 = normal, 1.5 = +50%, 2.0 = doublé)</param>
    public void ModifySpeed(float speedModifier)
    {
        _currentSpeed = _baseSpeed * speedModifier;
        Debug.Log($"[SNAKE] Vitesse modifiée : {_currentSpeed:F3} (base: {_baseSpeed} × {speedModifier})");
    }
    
    // REMPLACE la méthode ApplyScale existante dans SnakeController.cs

    /// <summary>
    /// Applique un scale DEPUIS LA BASE sur la TÊTE + TOUS LES SEGMENTS
    /// 
    /// FORMULE : scale actuel = scale de base × multiplicateur
    /// 
    /// EXEMPLE :
    /// Adam : ApplyScale(1.5) → scale = 1.0 × 1.5 = 1.5
    /// Eve/Lilith : ApplyScale(1.0) → scale = 1.0 × 1.0 = 1.0 (base)
    /// 
    /// APPELÉ PAR : AppleAdam.DoOnEated()
    /// </summary>
    /// <param name="scaleMultiplier">Multiplicateur depuis la base (1.0 = normal, 1.5 = +50%)</param>
    public void ApplyScale(float scaleMultiplier)
    {
        float newScale = _baseScale * scaleMultiplier;
    
        // Scaler la TÊTE
        transform.localScale = new Vector3(newScale, newScale, 1f);
    
        // Scaler TOUS LES SEGMENTS
        if (_snakeBody != null)
        {
            _snakeBody.ApplyScaleToSegments(newScale);
        }
    
        Debug.Log($"[SNAKE] Scale modifié : {newScale} (base: {_baseScale} × {scaleMultiplier})");
    }
    
    /// <summary>
    /// Réinitialise la vitesse à sa valeur de base
    /// Utilisé si on a besoin de reset complètement (pas utilisé actuellement)
    /// </summary>
    public void ResetSpeed()
    {
        _currentSpeed = _baseSpeed;
        Debug.Log($"[SNAKE] Vitesse réinitialisée : {_currentSpeed}");
    }
    
    /// <summary>
    /// Réinitialise le scale à sa valeur de base
    /// Utilisé si on a besoin de reset complètement (pas utilisé actuellement)
    /// </summary>
    public void ResetScale()
    {
        transform.localScale = new Vector3(_baseScale, _baseScale, 1f);
        Debug.Log($"[SNAKE] Scale réinitialisé : {_baseScale}");
    }
    
    // ===========================
    // RENAISSANCE LILITH (Twist narratif)
    // ===========================
    
    /// <summary>
    /// Active l'immortalité Lilith après la narration de renaissance
    /// 
    /// EFFETS :
    /// - Immortalité activée (traverse murs et corps)
    /// - Multiplicateur de score × 6 (double celui d'Adam)
    /// - Peut sortir de la grille
    /// - VITESSE DOUBLÉE (x2 depuis base)
    /// - ÉCHANGE les Tilemaps
    /// - ÉCLAIRE TOUTE LA MAP
    /// 
    /// APPELÉ PAR : GameManager.OnLilithRebirthComplete()
    /// </summary>
    public void LilithRebirth()
    {
        _isImmortal = true;
        _canExitGrid = true;
        _scoreMultiplier = 6;
        _canMove = true;
        
        CurrentPath = Path.Lilith;
    
        // DOUBLER LA VITESSE (depuis base)
        ModifySpeed(2f);
    
        // Désactiver le Tilemap de base
        GameObject tilemap = GameObject.Find("Tilemap");
        if (tilemap != null)
        {
            tilemap.SetActive(false);
        }
    
        // Activer la lumière lilith mode
        LightingSystem.Instance?.SwitchToBrightMode();
    
        Debug.Log("[LILITH] Tilemap caché");
        Debug.Log($"[LILITH] Renaissance accomplie. Immortalité activée. Vitesse x2 ({_currentSpeed})");
    }
    
    // ===========================
    // MÉTHODES DE CONTRÔLE
    // ===========================
    
    /// <summary>
    /// Arrête le mouvement du serpent
    /// Utilisé pendant : pause, narration, game over
    /// </summary>
    public void StopMovement()
    {
        _canMove = false;
    }
    
    /// <summary>
    /// Reprend le mouvement du serpent
    /// Utilisé après : pause, narration
    /// </summary>
    public void ResumeMovement()
    {
        _canMove = true;
    }
}