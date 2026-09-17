using UnityEngine;

/// <summary>
/// Apple - Classe de base pour toutes les pommes
/// 
/// RESPONSABILITÉS :
/// Détecter la collision avec le serpent JOUEUR
/// Détecter la collision avec le BOSS
/// Jouer le feedback visuel/audio
/// Informer le spawner qu'une pomme a été mangée
/// 
/// HÉRITAGE :
/// Les 3 types de pommes héritent de cette classe :
/// AppleAdam : Vitesse +15%, multiplicateur x2
/// AppleEve : Croissance x2, multiplicateur x4
/// AppleLilith : Vitesse x1.5 (x2 après rebirth)
/// 
/// PATTERN : Template Method
/// DoOnEated() est virtuel et redéfini dans les enfants
/// </summary>
public class Apple : MonoBehaviour
{
    [Header("Visual Feedback")]
    /// <summary>
    /// Prefab de particules spawné quand la pomme est mangée
    /// </summary>
    public GameObject particleApple;
    public int particleLenght;
    
    // ===========================
    // TYPE DE POMME
    // ===========================
    
    public enum AppleType { Adam, Eve, Lilith }
    
    /// <summary>
    /// Type de cette pomme (défini dans les enfants)
    /// </summary>
    [SerializeField] protected AppleType _appleType = AppleType.Adam;
    
    public AppleType Type => _appleType;
    
    // ===========================
    // DÉTECTION COLLISION
    // ===========================
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // JOUEUR
        SnakeController snake = other.GetComponent<SnakeController>();
        if (snake != null)
        {
            Eated(snake);
            return;
        }
        
        // BOSS
        BossSnake boss = other.GetComponent<BossSnake>();
        if (boss != null)
        {
            EatedByBoss(boss);
            return;
        }
    }
    
    // ===========================
    // MÉTHODE JOUEUR
    // ===========================
    
    public virtual void Eated(SnakeController snake)
    {
        DoOnEated(snake);
        PlayFeedback();
        AppleSpawner.Instance?.OnAppleEaten(_appleType);
        Destroy(gameObject);
    }
    
    // ===========================
    // MÉTHODE BOSS
    // ===========================
    
    protected virtual void EatedByBoss(BossSnake boss)
    {
        Debug.Log($"[APPLE] {_appleType} mangée par le boss");
        boss.Grow();
        Destroy(gameObject);
    }
    
    // ===========================
    // EFFETS (redéfinis dans enfants)
    // ===========================
    
    /// <summary>
    /// Méthode abstraite redéfinie dans chaque enfant
    /// Applique les effets spécifiques de chaque pomme
    /// </summary>
    protected virtual void DoOnEated(SnakeController snake)
    {
        Debug.LogWarning("[APPLE] DoOnEated() appelée sur la classe de base !");
    }
    
    // ===========================
    // FEEDBACK
    // ===========================
    
    protected virtual void PlayFeedback()
    {
        FreezeFrame.instance.Freeze(0.05f);
        ScreenShake.instance.Shake(0.15f, 0.2f);

        for (int i = 0; i < particleLenght; i++)
        {
            GameObject particule = Instantiate(particleApple, transform.position, Quaternion.identity);
            particule.GetComponent<SpriteRenderer>().color = GetComponent<SpriteRenderer>().color;
        }
        
        ServiceLocator.Get<AudioManager>()?.PlaySFX(AudioManager.SoundEffect.Apple);
    }
}