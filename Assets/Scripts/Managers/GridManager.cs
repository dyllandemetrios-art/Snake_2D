using UnityEngine;

/// <summary>
/// GridManager - Gestion de la zone de jeu et des positions sur grille
/// 
/// RESPONSABILITÉS :
/// Définir la zone jouable (à partir d'un Collider2D)
/// Générer des positions aléatoires alignées sur la grille (pour spawn des pommes)
/// Aligner n'importe quelle position sur la grille (snap)
/// Vérifier si une position est dans la zone de jeu
/// 
/// UTILISATION :
/// AppleSpawner l'utilise pour spawner les pommes : GridManager.Instance.GetRandomCellPosition()
/// Peut être utilisé pour vérifier si le Snake sort de la zone
/// 
/// PATTERN : Singleton
/// </summary>
public class GridManager : MonoBehaviour
{
    // ===========================
    // SINGLETON
    // ===========================
    
    /// <summary>
    /// Instance unique du GridManager
    /// Accessible depuis n'importe où via GridManager.Instance
    /// </summary>
    public static GridManager Instance { get; private set; }
    
    // ===========================
    // CONFIGURATION
    // ===========================
    
    [Header("Grid Settings")]
    
    /// <summary>
    /// Collider2D qui définit la zone de jeu (Tilemap Collider 2D)
    /// Utilisé pour calculer automatiquement les limites de spawn
    /// Glisse ton GameObject Tilemap ici dans l'Inspector
    /// </summary>
    [SerializeField] private Collider2D _playAreaCollider;
    
    /// <summary>
    /// Taille d'une cellule de la grille (en unités Unity)
    /// EXEMPLE : 1f = chaque case fait 1x1 unité
    /// Les pommes et le snake se déplacent de cellule en cellule
    /// </summary>
    [SerializeField] private float _cellSize = 1f;
    
    /// <summary>
    /// Distance de sécurité par rapport aux murs (en unités Unity)
    /// Empêche les pommes de spawner collées aux murs
    /// EXEMPLE : 1f = les pommes spawn à minimum 1 unité des bords
    /// </summary>
    [SerializeField] private float _margin = 1f;
    
    // ===========================
    // PROPRIÉTÉS PUBLIQUES
    // ===========================
    
    /// <summary>
    /// Taille d'une cellule (lecture seule)
    /// Utilisé par d'autres scripts qui ont besoin de connaître la taille des cases
    /// </summary>
    public float CellSize => _cellSize;
    
    /// <summary>
    /// Limites de la zone de jeu (position et taille)
    /// Retourne les bounds du collider ou un bounds vide si non assigné
    /// </summary>
    public Bounds PlayArea => _playAreaCollider != null ? _playAreaCollider.bounds : new Bounds();
    
    // ===========================
    // INITIALISATION
    // ===========================
    
    /// <summary>
    /// Configure le Singleton et vérifie les références
    /// </summary>
    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Vérification
        if (_playAreaCollider == null)
        {
            Debug.LogError("[GRID] PlayAreaCollider non assigné ! Glisse ton Tilemap dans l'Inspector.");
        }
    }
    
    // ===========================
    // GÉNÉRATION DE POSITION ALÉATOIRE
    // ===========================
    
    /// <summary>
    /// Génère une position aléatoire ALIGNÉE sur la grille
    /// Utilisée par AppleSpawner pour placer les pommes
    /// 
    /// FONCTIONNEMENT :
    /// 1. Récupère les bounds du Tilemap Collider
    /// 2. Calcule les limites avec marge de sécurité
    /// 3. Génère une position aléatoire dans ces limites
    /// 4. Aligne (snap) cette position sur la grille
    /// 
    /// EXEMPLE :
    /// Tilemap de -10 à +10 en X, margin = 1
    /// → minX = -9, maxX = 9
    /// → Random.Range(-9, 9) donne 3.7
    /// → Mathf.Round(3.7) = 4
    /// → Position finale = (4, y)
    /// </summary>
    /// <returns>Position aléatoire alignée sur la grille</returns>
    public Vector2 GetRandomCellPosition()
    {
        // Sécurité : si pas de collider, retourner (0,0)
        if (_playAreaCollider == null)
        {
            Debug.LogError("[GRID] PlayAreaCollider non assigné, retour position par défaut");
            return Vector2.zero;
        }

        // Récupérer les limites du collider
        Bounds bounds = _playAreaCollider.bounds;

        // Calculer les limites utilisables (zone - marge)
        // EXEMPLE : Si bounds.min.x = -10 et margin = 1, alors minX = -9
        float minX = bounds.min.x + _margin;
        float maxX = bounds.max.x - _margin;
        float minY = bounds.min.y + _margin;
        float maxY = bounds.max.y - _margin;

        // Vérification : la zone est-elle assez grande ?
        // Si minX >= maxX, la marge est trop grande pour la zone
        if (minX >= maxX || minY >= maxY)
        {
            Debug.LogError($"[GRID] Zone trop petite ou marge trop grande ! Bounds: X[{bounds.min.x}, {bounds.max.x}] Y[{bounds.min.y}, {bounds.max.y}], Margin: {_margin}");
            return Vector2.zero;
        }

        // Générer une position aléatoire dans les limites
        // EXEMPLE : Random.Range(-9, 9) peut donner 3.7
        float x = Random.Range(minX, maxX);
        float y = Random.Range(minY, maxY);

        // SNAP sur la grille : arrondir à la cellule la plus proche
        // EXEMPLE : x = 3.7 avec cellSize = 1
        //   3.7 / 1 = 3.7
        //   Round(3.7) = 4
        //   4 * 1 = 4
        //   Résultat : x = 4 (aligné sur la grille)
        x = Mathf.Round(x / _cellSize) * _cellSize;
        y = Mathf.Round(y / _cellSize) * _cellSize;

        return new Vector2(x, y);
    }
    
    // ===========================
    // UTILITAIRES
    // ===========================
    
    /// <summary>
    /// Vérifie si une position est à l'intérieur de la zone de jeu
    /// Utilisé pour détecter si le snake sort de la zone
    /// </summary>
    /// <param name="position">Position à vérifier</param>
    /// <returns>True si dans la zone, False sinon</returns>
    public bool IsInsideGrid(Vector2 position)
    {
        if (_playAreaCollider == null) return false;
        
        Bounds bounds = _playAreaCollider.bounds;
        return bounds.Contains(position);
    }
    
    /// <summary>
    /// Aligne n'importe quelle position sur la grille
    /// 
    /// EXEMPLE : position = (3.7, 5.2) avec cellSize = 1
    ///   x : Round(3.7 / 1) * 1 = 4
    ///   y : Round(5.2 / 1) * 1 = 5
    ///   Résultat : (4, 5)
    /// </summary>
    /// <param name="position">Position quelconque</param>
    /// <returns>Position alignée sur la grille</returns>
    public Vector2 SnapToGrid(Vector2 position)
    {
        float x = Mathf.Round(position.x / _cellSize) * _cellSize;
        float y = Mathf.Round(position.y / _cellSize) * _cellSize;
        return new Vector2(x, y);
    }
}