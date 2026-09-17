using UnityEngine;

/// <summary>
/// AppleBlink - Effet visuel de clignotement avant disparition
/// 
/// RESPONSABILITÉS :
/// - Faire clignoter la pomme dans ses dernières secondes de vie
/// - Accélérer progressivement le clignotement (lent → rapide)
/// - Alerter visuellement le joueur que la pomme va disparaître
/// 
/// COMPORTEMENT :
/// - Pendant les 2 dernières secondes, la pomme clignote
/// - Le clignotement commence lent (0.5s) et accélère jusqu'à rapide (0.1s)
/// - Cela crée un effet d'urgence progressif
/// 
/// UTILISATION :
/// Ajouté dynamiquement par AppleSpawner via AddComponent
/// Initialisé avec la durée de vie de la pomme (5s ou 10s selon contexte)
/// 
/// PATTERN : Component (attaché aux pommes au runtime)
/// </summary>
public class AppleBlink : MonoBehaviour
{
    // ===========================
    // CONFIGURATION
    // ===========================
    
    /// <summary>
    /// Temps avant disparition où le clignotement commence
    /// Défaut : 2 secondes avant la fin
    /// Exemple : Si lifetime = 10s, clignote à partir de 8s
    /// </summary>
    [SerializeField] private float _blinkStartTime = 2f;
    
    /// <summary>
    /// Vitesse initiale du clignotement en secondes
    /// Défaut : 0.5s (lent, visible/invisible toutes les demi-secondes)
    /// </summary>
    [SerializeField] private float _initialBlinkSpeed = 0.5f;
    
    /// <summary>
    /// Vitesse finale du clignotement en secondes
    /// Défaut : 0.1s (rapide, visible/invisible 10 fois par seconde)
    /// Crée un effet d'urgence
    /// </summary>
    [SerializeField] private float _finalBlinkSpeed = 0.1f;
    
    // ===========================
    // ÉTAT DU CLIGNOTEMENT
    // ===========================
    
    /// <summary>
    /// Référence au SpriteRenderer de la pomme
    /// Utilisé pour activer/désactiver la visibilité
    /// </summary>
    private SpriteRenderer _renderer;
    
    /// <summary>
    /// Temps écoulé depuis le spawn de la pomme
    /// Incrémenté dans Update()
    /// </summary>
    private float _timeAlive = 0f;
    
    /// <summary>
    /// Durée de vie totale de la pomme
    /// Défini par Initialize() (5s ou 10s selon contexte)
    /// </summary>
    private float _lifetime;
    
    /// <summary>
    /// État actuel de visibilité de la pomme
    /// True = visible, False = invisible
    /// Alternance pendant le clignotement
    /// </summary>
    private bool _isVisible = true;
    
    /// <summary>
    /// Prochain moment où le clignotement doit changer d'état
    /// Comparé avec Time.time pour savoir quand basculer
    /// </summary>
    private float _nextBlinkTime;
    
    /// <summary>
    /// Vitesse actuelle du clignotement
    /// Interpole entre _initialBlinkSpeed et _finalBlinkSpeed
    /// </summary>
    private float _currentBlinkSpeed;
    
    // ===========================
    // INITIALISATION
    // ===========================
    
    /// <summary>
    /// Récupère le SpriteRenderer de la pomme
    /// </summary>
    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }
    
    /// <summary>
    /// Initialise le système de clignotement
    /// Appelé par AppleSpawner après avoir instancié la pomme
    /// 
    /// DURÉE DE VIE :
    /// - Gameplay normal : 10 secondes
    /// - Lilith immortelle : 5 secondes (spawn plus rapide)
    /// </summary>
    /// <param name="lifetime">Durée de vie totale de la pomme en secondes</param>
    public void Initialize(float lifetime)
    {
        _lifetime = lifetime;
        _currentBlinkSpeed = _initialBlinkSpeed;
        _nextBlinkTime = (_lifetime - _blinkStartTime);
    }
    
    // ===========================
    // MISE À JOUR DU CLIGNOTEMENT
    // ===========================
    
    /// <summary>
    /// Gère le clignotement progressif de la pomme
    /// 
    /// LOGIQUE :
    /// 1. Incrémenter le temps écoulé
    /// 2. Calculer le temps restant avant disparition
    /// 3. Si dans les 2 dernières secondes :
    ///    a. Calculer la progression du clignotement (0 → 1)
    ///    b. Interpoler la vitesse (lent → rapide)
    ///    c. Alterner visible/invisible selon la vitesse
    /// 
    /// EFFET VISUEL :
    /// - 0-8s : Pomme normale (visible, stable)
    /// - 8-10s : Clignotement de plus en plus rapide
    ///   - 8.0s : Clignote lentement (0.5s)
    ///   - 9.0s : Clignote moyennement (0.3s)
    ///   - 9.9s : Clignote très vite (0.1s)
    /// </summary>
    void Update()
    {
        _timeAlive += Time.deltaTime;
        
        // Calculer le temps restant
        float timeRemaining = _lifetime - _timeAlive;
        
        // Si dans la période de clignotement
        if (timeRemaining <= _blinkStartTime && timeRemaining > 0)
        {
            // Calculer la progression (0 = début du clignotement, 1 = fin de vie)
            float blinkProgress = 1f - (timeRemaining / _blinkStartTime);
            
            // Interpoler la vitesse du clignotement
            // Plus on approche de la fin, plus ça clignote vite
            _currentBlinkSpeed = Mathf.Lerp(_initialBlinkSpeed, _finalBlinkSpeed, blinkProgress);
            
            // Alterner la visibilité selon la vitesse
            if (Time.time >= _nextBlinkTime)
            {
                _isVisible = !_isVisible;
                _renderer.enabled = _isVisible;
                _nextBlinkTime = Time.time + _currentBlinkSpeed;
            }
        }
    }
}