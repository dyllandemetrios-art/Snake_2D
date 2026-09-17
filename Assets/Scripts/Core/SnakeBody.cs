using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SnakeBody - Gestion visuelle des segments du corps du serpent
/// 
/// RESPONSABILITÉS :
/// Créer et détruire les segments visuels
/// Faire suivre les segments derrière la tête avec espacement constant
/// Stocker l'historique des positions pour un mouvement fluide
/// 
/// PRINCIPE DE FONCTIONNEMENT (NOUVEAU SYSTÈME) :
/// 1. La tête se déplace et enregistre ses positions dans _positionHistory
/// 2. Les positions sont enregistrées seulement si la tête a bougé suffisamment (_minDistanceBetweenPoints)
/// 3. Chaque segment se positionne à une DISTANCE fixe du précédent (pas en frames)
/// 4. Le système calcule la position exacte en interpolant entre les points de l'historique
/// 
/// AVANTAGE SUR L'ANCIEN SYSTÈME :
/// - Espacement constant quelle que soit la vitesse du serpent
/// - Indépendant du framerate
/// - Alignement parfait sur la grille
/// 
/// EXEMPLE avec _segmentSpacing = 0.5 :
///   Segment 1 → 0.5 unités derrière la tête
///   Segment 2 → 1.0 unités derrière la tête
///   Segment 3 → 1.5 unités derrière la tête
/// 
/// PATTERN : Component (attaché au GameObject Snake)
/// </summary>
public class SnakeBody : MonoBehaviour
{
    // ===========================
    // CONFIGURATION
    // ===========================
    
    [Header("Segment Settings")]
    
    /// <summary>
    /// Prefab du segment de corps
    /// Doit contenir : SpriteRenderer, Collider2D (tag "SnakeBody", Is Trigger DÉCOCHÉ)
    /// Instancié à chaque fois que le serpent grandit
    /// </summary>
    [SerializeField] private Transform _segmentPrefab;
    
    /// <summary>
    /// Distance entre chaque segment en unités Unity
    /// Plus grand = segments plus espacés
    /// Plus petit = segments collés
    /// Valeur recommandée : 0.5 pour un espacement visuel agréable
    /// 
    /// IMPORTANT : Cette valeur est en DISTANCE RÉELLE, pas en frames
    /// Elle reste constante quelle que soit la vitesse du serpent
    /// </summary>
    [SerializeField] private float _segmentSpacing = 1f;
    
    [Header("Visual Settings")]
    
    /// <summary>
    /// Couleur des segments pour la visualisation debug (Gizmos)
    /// N'affecte PAS la couleur réelle (gérée par le SpriteRenderer)
    /// </summary>
    [SerializeField] private Color _segmentColor = Color.green;
    
    // ===========================
    // DONNÉES DU CORPS
    // ===========================
    
    /// <summary>
    /// Liste des segments visuels (GameObjects)
    /// Chaque Transform représente un segment du corps
    /// Index 0 = segment le plus proche de la tête
    /// </summary>
    private List<Transform> _segments = new List<Transform>();
    
    /// <summary>
    /// Historique des positions de la tête
    /// Index 0 = position actuelle
    /// Index 1 = position précédente
    /// 
    /// DIFFÉRENCE AVEC L'ANCIEN SYSTÈME :
    /// Les positions sont ajoutées seulement quand la tête bouge suffisamment
    /// Évite d'encombrer l'historique avec des positions quasi-identiques
    /// </summary>
    private List<Vector3> _positionHistory = new List<Vector3>();
    
    /// <summary>
    /// Distance minimum que la tête doit parcourir avant d'enregistrer une nouvelle position
    /// Optimise la taille de l'historique sans perdre en précision
    /// Valeur : 0.1 = enregistre une position tous les 0.1 unités parcourus
    /// </summary>
    private float _minDistanceBetweenPoints = 0.1f;
    
    // ===========================
    // PROPRIÉTÉS PUBLIQUES
    // ===========================
    
    /// <summary>
    /// Nombre de segments actuels (lecture seule)
    /// </summary>
    public int SegmentCount => _segments.Count;
    
    /// <summary>
    /// Accès direct à la liste des segments
    /// Utilisé par SnakeCollision pour vérifier les collisions
    /// </summary>
    public List<Transform> Segments => _segments;
    
    // ===========================
    // INITIALISATION
    // ===========================
    
    /// <summary>
    /// Vérification au démarrage que le prefab est assigné
    /// </summary>
    void Awake()
    {
        if (_segmentPrefab == null)
        {
            Debug.LogError("[SNAKE BODY] Segment Prefab non assigné !");
        }
    }
    
    /// <summary>
    /// Initialise l'historique avec la position de départ
    /// Un seul point suffit maintenant (ancien système en utilisait 200+)
    /// </summary>
    void Start()
    {
        _positionHistory.Add(transform.position);
        Debug.Log($"[SNAKE BODY] Initialisé");
    }
    
    // ===========================
    // MISE À JOUR DES SEGMENTS
    // ===========================
    
    /// <summary>
    /// Appelé chaque frame
    /// Met à jour la position de tous les segments
    /// 
    /// ÉTAPES :
    /// 1. Enregistrer la position actuelle SI la tête a bougé suffisamment
    /// 2. Calculer et appliquer la position de chaque segment
    /// 3. Nettoyer l'historique (supprimer les positions trop anciennes)
    /// </summary>
    void Update()
    {
        // Si le jeu est en pause, ne pas bouger les segments
        if (Time.timeScale == 0f) return;
        
        // 1. ENREGISTRER la position actuelle seulement si on a bougé suffisamment
        // Évite d'encombrer l'historique avec des micro-déplacements
        if (_positionHistory.Count == 0 || 
            Vector3.Distance(transform.position, _positionHistory[0]) > _minDistanceBetweenPoints)
        {
            _positionHistory.Insert(0, transform.position);
        }

        // 2. METTRE À JOUR chaque segment
        for (int i = 0; i < _segments.Count; i++)
        {
            // Distance cible : chaque segment est _segmentSpacing plus loin que le précédent
            // Segment 0 → 1 × 0.5 = 0.5 unités derrière
            // Segment 1 → 2 × 0.5 = 1.0 unités derrière
            float targetDistance = (i + 1) * _segmentSpacing;
            
            // Calculer la position exacte à cette distance dans l'historique
            Vector3 targetPos = GetPositionAtDistance(targetDistance);
            
            // Appliquer la position
            _segments[i].position = targetPos;
        }
        
        // 3. NETTOYER l'historique (optimisation mémoire)
        // Garder uniquement les positions nécessaires + un buffer de sécurité
        float maxDistance = (_segments.Count + 2) * _segmentSpacing + 5f;
        CleanHistory(maxDistance);
    }
    
    /// <summary>
    /// Calcule la position exacte à une distance donnée le long de l'historique
    /// 
    /// ALGORITHME :
    /// 1. Parcourir l'historique en cumulant les distances entre chaque point
    /// 2. Quand on dépasse la distance cible, interpoler entre les 2 derniers points
    /// 3. Retourner la position interpolée
    /// 
    /// EXEMPLE :
    /// Historique : [0,0] → [1,0] → [2,0] → [3,0]
    /// Distance cible : 1.5
    /// Résultat : Position entre [1,0] et [2,0] → [1.5, 0]
    /// </summary>
    /// <param name="distance">Distance cible en unités</param>
    /// <returns>Position interpolée à cette distance</returns>
    private Vector3 GetPositionAtDistance(float distance)
    {
        float currentDistance = 0f;
        
        // Parcourir l'historique
        for (int i = 0; i < _positionHistory.Count - 1; i++)
        {
            // Distance entre ce point et le suivant
            float segmentLength = Vector3.Distance(_positionHistory[i], _positionHistory[i + 1]);
            
            // Si on dépasse la distance cible dans ce segment
            if (currentDistance + segmentLength >= distance)
            {
                // Calculer la position exacte dans ce segment
                // t = progression entre 0 (début) et 1 (fin) du segment
                float t = (distance - currentDistance) / segmentLength;
                
                // Interpoler entre les 2 points
                return Vector3.Lerp(_positionHistory[i], _positionHistory[i + 1], t);
            }
            
            // Sinon, continuer à accumuler la distance
            currentDistance += segmentLength;
        }
        
        // Fallback : retourner la dernière position connue
        return _positionHistory.Count > 0 ? _positionHistory[_positionHistory.Count - 1] : transform.position;
    }
    
    /// <summary>
    /// Nettoie l'historique en supprimant les positions trop anciennes
    /// 
    /// LOGIQUE :
    /// 1. Calculer combien de distance on doit garder (basé sur le nombre de segments)
    /// 2. Parcourir l'historique et compter jusqu'à atteindre cette distance
    /// 3. Supprimer tout ce qui est au-delà
    /// 
    /// OPTIMISATION :
    /// Évite que l'historique grandisse infiniment et ralentisse le jeu
    /// </summary>
    /// <param name="maxDistance">Distance maximum à garder dans l'historique</param>
    private void CleanHistory(float maxDistance)
    {
        float currentDistance = 0f;
        int keepCount = 1; // Garder au moins la position actuelle
        
        // Calculer combien de points garder
        for (int i = 0; i < _positionHistory.Count - 1; i++)
        {
            currentDistance += Vector3.Distance(_positionHistory[i], _positionHistory[i + 1]);
            keepCount++;
            
            // Dès qu'on a assez de distance, arrêter
            if (currentDistance > maxDistance)
            {
                break;
            }
        }
        
        // Supprimer les positions en trop
        if (_positionHistory.Count > keepCount)
        {
            _positionHistory.RemoveRange(keepCount, _positionHistory.Count - keepCount);
        }
    }
    
    // ===========================
    // CROISSANCE DU CORPS
    // ===========================
    
    // REMPLACE la méthode Grow() dans SnakeBody.cs

    /// <summary>
    /// Ajoute des segments au corps
    /// Appelée par SnakeController quand BodyLength augmente
    /// 
    /// LOGIQUE DE SPAWN :
    /// - Si des segments existent déjà → Spawner DERRIÈRE le dernier segment
    /// - Sinon → Calculer la position à _segmentSpacing derrière la tête
    /// - HÉRITE DU SCALE de la tête pour garder la cohérence visuelle
    /// 
    /// FIX : Spawner à (_segments.Count + 1) × _segmentSpacing pour éviter collision immédiate
    /// </summary>
    /// <param name="amount">Nombre de segments à ajouter</param>
    public void Grow(int amount = 1)
    {
        // Récupérer le scale actuel de la tête pour l'appliquer aux nouveaux segments
        float currentScale = transform.localScale.x;
    
        for (int i = 0; i < amount; i++)
        {
            Vector3 spawnPosition;
    
            // Calculer la distance pour ce nouveau segment
            float spawnDistance = (_segments.Count + 1) * _segmentSpacing;
            spawnPosition = GetPositionAtDistance(spawnDistance);
    
            // Créer le segment
            Transform newSegment = Instantiate(_segmentPrefab, spawnPosition, Quaternion.identity, null);
            newSegment.name = $"Segment_{_segments.Count + 1}";
        
            // APPLIQUER LE SCALE DE LA TÊTE
            newSegment.localScale = new Vector3(currentScale, currentScale, 1f);
    
            // DÉSACTIVER le collider pendant 0.5 secondes
            Collider2D col = newSegment.GetComponent<Collider2D>();
            if (col != null)
            {
                StartCoroutine(EnableColliderDelayed(col, 0.5f));
            }
    
            _segments.Add(newSegment);
        }

        Debug.Log($"[SNAKE BODY] +{amount} segment(s) (Total: {_segments.Count}, Scale: {currentScale})");
    }

    /// <summary>
    /// Réactive le collider après un délai
    /// Évite les collisions pendant le spawn
    /// </summary>
    private System.Collections.IEnumerator EnableColliderDelayed(Collider2D collider, float delay)
    {
        collider.enabled = false;
        yield return new WaitForSeconds(delay);
        if (collider != null)
        {
            collider.enabled = true;
        }
    }

    /// <summary>
    /// Applique un scale à tous les segments existants
    /// Appelé par SnakeController.ApplyScale()
    /// 
    /// LOGIQUE :
    /// Parcourt tous les segments et modifie leur localScale
    /// Les nouveaux segments créés après hériteront du prefab (scale 1)
    /// donc il faudra aussi les scaler au moment du Grow()
    /// </summary>
    /// <param name="scale">Scale à appliquer (ex: 1.5)</param>
    public void ApplyScaleToSegments(float scale)
    {
        foreach (Transform segment in _segments)
        {
            if (segment != null)
            {
                segment.localScale = new Vector3(scale, scale, 1f);
            }
        }
    
        Debug.Log($"[SNAKE BODY] Scale appliqué à {_segments.Count} segments : {scale}");
    }
    
    // ===========================
    // VISUALISATION DEBUG
    // ===========================
    
    /// <summary>
    /// Dessine les segments en mode Scene (pas en Play)
    /// Affiche des cercles verts autour de chaque segment
    /// Utile pour vérifier visuellement le corps
    /// </summary>
    void OnDrawGizmos()
    {
        if (_segments == null || _segments.Count == 0) return;
        
        Gizmos.color = _segmentColor;
        
        foreach (Transform segment in _segments)
        {
            if (segment != null)
            {
                Gizmos.DrawWireSphere(segment.position, 0.4f);
            }
        }
    }
}