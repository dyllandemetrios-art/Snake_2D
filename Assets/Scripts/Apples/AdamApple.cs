using UnityEngine;

public class AppleAdam : Apple
{
    [Header("Adam Settings")]
    [SerializeField] private int _baseScore = 100;
    [SerializeField] private float _speedMultiplier = 1.15f;
    [SerializeField] private int _scoreMultiplier = 2;
    [SerializeField] private int _adamGrowth = 1;
    
    void Awake()
    {
        _appleType = AppleType.Adam;
    }
    
    protected override void DoOnEated(SnakeController snake)
    {
        Debug.Log("[ADAM] Pomme de domination mangée");
        
        // Si Lilith immortelle, juste croissance + score
        if (snake.IsImmortal)
        {
            Debug.Log("[ADAM] Lilith immortelle → Buffs ignorés");
            snake.BodyLength += _adamGrowth;
            int finalScore = _baseScore * snake.ScoreMultiplier;
            ServiceLocator.Get<ScoreManager>()?.AddScore(finalScore);
            return;
        }

        snake.ResetSpeed();
        snake.ResetScale();
        
        snake.ModifySpeed(_speedMultiplier);
        snake.ScoreMultiplier = _scoreMultiplier;
        snake.BodyLength += _adamGrowth;
        
        int score = _baseScore * snake.ScoreMultiplier;
        ServiceLocator.Get<ScoreManager>()?.AddScore(score);
        
        snake.CurrentPath = SnakeController.Path.Adam;
        
        Debug.Log($"[ADAM] Vitesse: x{_speedMultiplier}, Multi: x{_scoreMultiplier}, Score: {score}");
    }
}