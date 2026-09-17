using UnityEngine;

public class AppleLilith : Apple
{
    [Header("Lilith Settings")]
    [SerializeField] private int _baseScore = 100;
    [SerializeField] private int _lilithGrowth = 1;
    [SerializeField] private float _preDeathSpeedMultiplier = 1.5f;
    [SerializeField] private int _postDeathMultiplier = 10;
    
    void Awake()
    {
        _appleType = AppleType.Lilith;
    }
    
    protected override void DoOnEated(SnakeController snake)
    {
        Debug.Log("[LILITH] Pomme de transgression mangée");
        
        if (snake.IsImmortal)
        {
            // POST-RENAISSANCE : Buffs figés (vitesse x2, multi x6)
            Debug.Log("[LILITH] Post-renaissance (immortelle) → Buffs figés");
            
            snake.BodyLength += _lilithGrowth;
            
            int finalScore = _baseScore * _postDeathMultiplier * snake.ScoreMultiplier;
            ServiceLocator.Get<ScoreManager>()?.AddScore(finalScore);
            
            Debug.Log($"[LILITH] Immortelle : Score {finalScore}");
        }
        else
        {
            // PRÉ-RENAISSANCE : Vitesse x1.5, score de base
            Debug.Log("[LILITH] Pré-renaissance");
            
            snake.ResetSpeed();
            snake.ResetScale();
            
            snake.ModifySpeed(_preDeathSpeedMultiplier);
            snake.BodyLength += _lilithGrowth;
            
            ServiceLocator.Get<ScoreManager>()?.AddScore(_baseScore);
            
            snake.CurrentPath = SnakeController.Path.Lilith;
            
            Debug.Log($"[LILITH] Pré-renaissance : Vitesse x{_preDeathSpeedMultiplier}, Score: {_baseScore}");
        }
    }
}