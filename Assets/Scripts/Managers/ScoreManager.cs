using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ScoreManager - Gestion du score et du high score
/// 
/// RESPONSABILITÉS :
/// - Stocker le score actuel de la partie
/// - Ajouter des points (appelé par les pommes)
/// - Gérer le meilleur score (sauvegarde persistante)
/// - Notifier l'UI quand le score change (UnityEvent)
/// - Fournir un récapitulatif pour l'écran de fin
/// 
/// SAUVEGARDE :
/// Le high score est sauvegardé dans PlayerPrefs
/// Persiste entre les sessions (même après fermeture du jeu)
/// 
/// OPTIMISATION :
/// La sauvegarde sur disque n'est faite qu'UNE FOIS à la fin de la partie
/// Évite les écritures multiples pendant le jeu
/// 
/// PATTERN : Observer (UnityEvent pour notifier l'UI)
/// </summary>
public class ScoreManager : MonoBehaviour
{
    // ===========================
    // ÉVÉNEMENTS
    // ===========================
    
    /// <summary>
    /// Classe wrapper pour UnityEvent typé
    /// Permet de passer un paramètre int (le nouveau score)
    /// Nécessaire pour que Unity sérialise l'événement
    /// </summary>
    [System.Serializable]
    public class ScoreEvent : UnityEvent<int> { }
    
    /// <summary>
    /// Événement déclenché quand le score change
    /// 
    /// ABONNÉS :
    /// - UIManager.UpdateScore() (via Start dans UIManager)
    /// </summary>
    public ScoreEvent OnScoreChanged = new ScoreEvent();
    
    /// <summary>
    /// Événement déclenché quand le highscore change
    /// 
    /// ABONNÉS :
    /// - UIManager.UpdateHighScore() (via Start dans UIManager)
    /// </summary>
    public ScoreEvent OnHighScoreChanged = new ScoreEvent();
    
    // ===========================
    // ÉTAT DU SCORE
    // ===========================
    
    /// <summary>
    /// Score de la partie en cours
    /// Réinitialisé à 0 au début de chaque partie
    /// Incrémenté quand on mange des pommes
    /// </summary>
    private int _currentScore = 0;
    
    /// <summary>
    /// Meilleur score de toutes les parties
    /// Chargé depuis PlayerPrefs au démarrage
    /// Sauvegardé quand un nouveau record est établi
    /// Persiste entre les sessions
    /// </summary>
    private int _highScore = 0;
    
    /// <summary>
    /// Flag pour savoir si un nouveau record a été établi
    /// Utilisé pour optimiser la sauvegarde (1 seule fois en fin de partie)
    /// Évite d'écrire sur disque plusieurs fois pendant le jeu
    /// </summary>
    private bool _hasNewRecord = false;
    
    // ===========================
    // PROPRIÉTÉS PUBLIQUES
    // ===========================
    
    /// <summary>
    /// Score actuel (lecture seule)
    /// Utilisé par l'UI et les stats de fin
    /// </summary>
    public int CurrentScore => _currentScore;
    
    /// <summary>
    /// Meilleur score (lecture seule)
    /// Affiché dans l'UI et l'écran de fin
    /// </summary>
    public int HighScore => _highScore;
    
    // ===========================
    // INITIALISATION
    // ===========================
    
    void Start()
    {
        // Charger le meilleur score depuis PlayerPrefs
        _highScore = PlayerPrefs.GetInt("HighScore", 0);
        
        Debug.Log($"[SCORE] High Score chargé : {_highScore}");
        
        // Notifier l'UI du highscore au démarrage
        OnHighScoreChanged?.Invoke(_highScore);
    }
    
    // ===========================
    // AJOUTER DES POINTS
    // ===========================
    
    public void AddScore(int amount)
    {
        // Sécurité : ignorer les valeurs nulles ou négatives
        if (amount <= 0) return;
        
        // Ajouter les points
        _currentScore += amount;
        
        Debug.Log($"[SCORE] +{amount} points (Total: {_currentScore})");
        
        // Notifier tous les abonnés (UIManager)
        OnScoreChanged?.Invoke(_currentScore);
        
        // VÉRIFIER NOUVEAU RECORD
        if (_currentScore > _highScore)
        {
            // Nouveau record établi !
            _highScore = _currentScore;
            
            // Marquer qu'il y a un nouveau record
            _hasNewRecord = true;
            
            Debug.Log($"[SCORE] NOUVEAU RECORD : {_highScore} (sauvegarde différée)");
            
            // Notifier l'UI du nouveau highscore
            OnHighScoreChanged?.Invoke(_highScore);
        }
    }
    
    // ===========================
    // SAUVEGARDE
    // ===========================
    
    public void SaveHighScore()
    {
        // Sauvegarder seulement s'il y a un nouveau record
        if (_hasNewRecord)
        {
            PlayerPrefs.SetInt("HighScore", _highScore);
            PlayerPrefs.Save(); // Forcer l'écriture sur disque
            
            _hasNewRecord = false; // Reset du flag
            
            Debug.Log($"[SCORE] High Score sauvegardé : {_highScore}");
        }
    }
    
    // ===========================
    // RÉINITIALISATION
    // ===========================
    
    public void ResetScore()
    {
        // Sauvegarder avant de reset (au cas où)
        SaveHighScore();
        
        // Reset le score actuel
        _currentScore = 0;
        
        // Notifier l'UI
        OnScoreChanged?.Invoke(_currentScore);
        
        Debug.Log("[SCORE] Score réinitialisé");
    }
    
    // ===========================
    // STATS FINALES
    // ===========================
    
    public string GetScoreBreakdown()
    {
        return $"Score final : {_currentScore}\n" +
               $"Record : {_highScore}";
    }
    
    // ===========================
    // SÉCURITÉ (Sauvegarde à la fermeture)
    // ===========================
    
    void OnApplicationQuit()
    {
        SaveHighScore();
    }
}