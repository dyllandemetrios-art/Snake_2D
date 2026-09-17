using UnityEngine;

public class AppleEve : Apple
{
    [Header("Eve Settings")]
    [SerializeField] private int _baseScore = 100;
    [SerializeField] private int _scoreMultiplier = 4;
    [SerializeField] private int _eveGrowth = 2;
    
    void Awake()
    {
        _appleType = AppleType.Eve;
    }
    
    protected override void DoOnEated(SnakeController snake)
    {
        Debug.Log("[EVE] Pomme d'adaptation mangée");
        
        // Si Lilith immortelle, juste croissance + score
        if (snake.IsImmortal)
        {
            Debug.Log("[EVE] Lilith immortelle → Buffs ignorés");
            snake.BodyLength += _eveGrowth;
            int finalScore = _baseScore * snake.ScoreMultiplier;
            ServiceLocator.Get<ScoreManager>()?.AddScore(finalScore);
            return;
        }
        
        snake.ResetSpeed();
        snake.ResetScale();
        
        snake.ScoreMultiplier = _scoreMultiplier;
        snake.BodyLength += _eveGrowth;
        
        int score = _baseScore * snake.ScoreMultiplier;
        ServiceLocator.Get<ScoreManager>()?.AddScore(score);
        
        snake.CurrentPath = SnakeController.Path.Eve;
        
        Debug.Log($"[EVE] Multi: x{_scoreMultiplier}, Segments: +{_eveGrowth}, Score: {score}");
    }
}