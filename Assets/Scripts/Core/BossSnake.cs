using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BossSnake - Snake ennemi blanc qui combat le joueur
/// 
/// RESPONSABILITÉS :
/// - Se déplacer sur GRILLE (4 directions comme le joueur)
/// - Manger TOUTES les pommes (compétition)
/// - Gérer son corps (segments suiveurs)
/// - Mourir si le joueur (11+) mange sa tête
/// 
/// DÉLÉGATIONS :
/// - Collisions avec joueur → BossCollision
/// - Collisions avec pommes → Apple.EatedByBoss()
/// 
/// COMPORTEMENT IA :
/// PRIORITÉ 1 : Pommes Lilith (empêcher le joueur d'atteindre 11)
/// PRIORITÉ 2 : Joueur (foncer dessus pour voler des segments)
/// PRIORITÉ 3 : Mouvement aléatoire intelligent
/// 
/// SYSTÈME DE MOUVEMENT :
/// ALIGNÉ SUR GRILLE : 4 directions cardinales (haut/bas/gauche/droite)
/// Vitesse basée sur le nombre de segments (plus de segments = plus rapide)
/// 
/// VITESSE DYNAMIQUE :
/// Vitesse de base × (1 + segments × 0.05)
/// Exemple : 10 segments → vitesse × 1.5
/// 
/// PATTERN : Component (attaché au GameObject BossSnake)
/// </summary>
public class BossSnake : MonoBehaviour
{
    // ===========================
    // CONFIGURATION (Inspector)
    // ===========================
    
    [Header("Movement")]
    [SerializeField] private float _baseSpeed = 0.22f;
    [SerializeField] private float _speedPerSegment = 0.05f;
    [SerializeField] private float _directionChangeInterval = 0.4f;
    
    [Header("Visual Feedback")]
    public GameObject particleBoss;
    public int particleLenght;
    
    [Header("Body")]
    [SerializeField] private Transform _segmentPrefab;
    [SerializeField] private float _segmentSpacing = 1f;
    
    // ===========================
    // RÉFÉRENCES
    // ===========================
    
    private Transform _playerSnake;
    private List<Transform> _segments = new List<Transform>();
    
    // ===========================
    // ÉTAT MOUVEMENT
    // ===========================
    
    private Vector2 _currentDirection;
    private float _directionTimer;
    private bool _canMove = false;
    private float _currentSpeed;
    
    // ===========================
    // ÉTAT CORPS
    // ===========================
    
    private int _bodyLength = 0;
    private List<Vector3> _positionHistory = new List<Vector3>();
    
    public int BodyLength => _bodyLength;
    
    // ===========================
    // IA - CIBLAGE
    // ===========================
    
    private Transform _targetApple;
    
    // ===========================
    // INITIALISATION
    // ===========================
    
    void Start()
    {
        Debug.Log("[BOSS] Initialisation du boss");
        
        gameObject.tag = "BossSnake";
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerSnake = player.transform;
            Debug.Log("[BOSS] Joueur trouvé et référencé");
        }
        else
        {
            Debug.LogError("[BOSS] Snake joueur introuvable !");
        }

        _bodyLength = 0;
        _currentDirection = GetRandomCardinalDirection();
        Debug.Log($"[BOSS] Direction initiale: {_currentDirection}");
        
        UpdateSpeed();
        _directionTimer = _directionChangeInterval;
        _canMove = false;
    
        Debug.Log("[BOSS] Initialisé (FIGÉ jusqu'à la fin de la narration)");
    }
    
    // ===========================
    // UPDATE
    // ===========================
    
    void Update()
    {
        if (!_canMove)
        {
            if (Time.frameCount % 60 == 0) Debug.Log("[BOSS] Toujours figé, attend Release()");
            return;
        }
        
        _directionTimer -= Time.deltaTime;
        
        if (_directionTimer <= 0f)
        {
            Debug.Log("[BOSS] Recalcul de direction IA");
            DecideDirection();
            _directionTimer = _directionChangeInterval;
        }
        
        UpdatePositionHistory();
        UpdateSegments();
    }
    
    // ===========================
    // FIXED UPDATE (Mouvement)
    // ===========================
    
    void FixedUpdate()
    {
        if (Time.timeScale == 0f || !_canMove) return;
    
        transform.position += (Vector3)(_currentDirection * _currentSpeed);
    }
    
    // ===========================
    // CONTRÔLE DU MOUVEMENT
    // ===========================
    
    public void Release()
    {
        _canMove = true;
        Debug.Log("[BOSS] Libéré ! Le boss peut maintenant bouger.");
    }
    
    // ===========================
    // GESTION VITESSE
    // ===========================
    
    private void UpdateSpeed()
    {
        _currentSpeed = _baseSpeed * (1f + _bodyLength * _speedPerSegment);
        Debug.Log($"[BOSS] Vitesse mise à jour : {_currentSpeed:F3} ({_bodyLength} segments)");
    }
    
    // ===========================
    // IA - DÉCISION DE DIRECTION
    // ===========================
    
    private void DecideDirection()
    {
        Debug.Log("[BOSS IA] Décision de direction");
        
        FindNearestLilithApple();
        
        if (_targetApple != null)
        {
            Debug.Log("[BOSS IA] Cible pomme Lilith trouvée");
            Vector2 toApple = GetCardinalDirectionToTarget(_targetApple.position);
            
            if (IsSafeDirection(toApple))
            {
                _currentDirection = toApple;
                Debug.Log($"[BOSS IA] Direction vers pomme: {_currentDirection}");
                return;
            }
        }
        
        if (_playerSnake != null)
        {
            Debug.Log("[BOSS IA] Cible joueur");
            Vector2 toPlayer = GetCardinalDirectionToTarget(_playerSnake.position);
            
            if (IsSafeDirection(toPlayer))
            {
                _currentDirection = toPlayer;
                Debug.Log($"[BOSS IA] Direction vers joueur: {_currentDirection}");
                return;
            }
        }
    
        if (!IsSafeDirection(_currentDirection) || Random.value < 0.2f)
        {
            Debug.Log("[BOSS IA] Mouvement aléatoire");
            _currentDirection = GetRandomSafeCardinalDirection();
        }
    }
    
    private Vector2 GetCardinalDirectionToTarget(Vector3 targetPos)
    {
        Vector2 diff = targetPos - transform.position;
        
        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
        {
            return diff.x > 0 ? Vector2.right : Vector2.left;
        }
        else
        {
            return diff.y > 0 ? Vector2.up : Vector2.down;
        }
    }
    
    private bool IsSafeDirection(Vector2 direction)
    {
        if (direction == -_currentDirection)
        {
            return false;
        }
        
        Vector3 nextPos = transform.position + (Vector3)direction * 1f;
        
        foreach (Transform segment in _segments)
        {
            if (segment != null && Vector3.Distance(segment.position, nextPos) < 0.4f)
            {
                return false;
            }
        }
        
        return true;
    }
    
    private Vector2 GetRandomSafeCardinalDirection()
    {
        List<Vector2> possibleDirections = new List<Vector2>
        {
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right
        };
        
        for (int i = 0; i < possibleDirections.Count; i++)
        {
            Vector2 temp = possibleDirections[i];
            int randomIndex = Random.Range(i, possibleDirections.Count);
            possibleDirections[i] = possibleDirections[randomIndex];
            possibleDirections[randomIndex] = temp;
        }
        
        foreach (Vector2 dir in possibleDirections)
        {
            if (IsSafeDirection(dir))
            {
                return dir;
            }
        }
        
        return _currentDirection;
    }
    
    private Vector2 GetRandomCardinalDirection()
    {
        int choice = Random.Range(0, 4);
        return choice switch
        {
            0 => Vector2.up,
            1 => Vector2.down,
            2 => Vector2.left,
            _ => Vector2.right
        };
    }
    
    // ===========================
    // DÉTECTION DES CIBLES
    // ===========================
    
    private void FindNearestLilithApple()
    {
        GameObject[] apples = GameObject.FindGameObjectsWithTag("Apple");
        
        float nearestDist = Mathf.Infinity;
        _targetApple = null;
    
        foreach (GameObject apple in apples)
        {
            Apple appleScript = apple.GetComponent<Apple>();
            if (appleScript != null && appleScript.Type == Apple.AppleType.Lilith)
            {
                float dist = Vector2.Distance(transform.position, apple.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    _targetApple = apple.transform;
                }
            }
        }
        
        if (_targetApple != null)
        {
            Debug.Log($"[BOSS] Pomme Lilith ciblée à distance {nearestDist:F2}");
        }
    }
    
    // ===========================
    // GESTION DU CORPS
    // ===========================
    
    private void UpdatePositionHistory()
    {
        if (_positionHistory.Count == 0 || Vector3.Distance(transform.position, _positionHistory[0]) > 0.05f)
        {
            _positionHistory.Insert(0, transform.position);
        }
        
        float maxDistance = (_segments.Count + 2) * _segmentSpacing + 5f;
        CleanHistory(maxDistance);
    }
    
    private void UpdateSegments()
    {
        for (int i = 0; i < _segments.Count; i++)
        {
            if (_segments[i] == null) continue;
            
            float targetDistance = (i + 1) * _segmentSpacing;
            Vector3 targetPos = GetPositionAtDistance(targetDistance);
            _segments[i].position = targetPos;
        }
    }
    
    private Vector3 GetPositionAtDistance(float distance)
    {
        float currentDistance = 0f;
        
        for (int i = 0; i < _positionHistory.Count - 1; i++)
        {
            float segmentLength = Vector3.Distance(_positionHistory[i], _positionHistory[i + 1]);
            
            if (currentDistance + segmentLength >= distance)
            {
                float t = (distance - currentDistance) / segmentLength;
                return Vector3.Lerp(_positionHistory[i], _positionHistory[i + 1], t);
            }
            
            currentDistance += segmentLength;
        }
        
        return _positionHistory.Count > 0 ? _positionHistory[_positionHistory.Count - 1] : transform.position;
    }
    
    private void CleanHistory(float maxDistance)
    {
        float currentDistance = 0f;
        int keepCount = 1;
        
        for (int i = 0; i < _positionHistory.Count - 1; i++)
        {
            currentDistance += Vector3.Distance(_positionHistory[i], _positionHistory[i + 1]);
            keepCount++;
            
            if (currentDistance > maxDistance)
            {
                break;
            }
        }
        
        if (_positionHistory.Count > keepCount)
        {
            _positionHistory.RemoveRange(keepCount, _positionHistory.Count - keepCount);
        }
    }
    
    public void Grow()
    {
        Debug.Log("[BOSS] Grow() appelée");
        
        if (_segmentPrefab == null)
        {
            Debug.LogError("[BOSS] Segment Prefab non assigné dans l'Inspector !");
            return;
        }
        
        Vector3 spawnPos = _segments.Count > 0 ? _segments[_segments.Count - 1].position : transform.position;
        
        Transform newSegment = Instantiate(_segmentPrefab, spawnPos, Quaternion.identity);
        
        newSegment.tag = "BossBody";
        newSegment.name = $"BossSegment_{_segments.Count + 1}";
        
        Collider2D col = newSegment.GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
            StartCoroutine(EnableColliderDelayed(col, 0.5f));
        }
        
        _segments.Add(newSegment);
        _bodyLength++;
        
        UpdateSpeed();
        
        Debug.Log($"[BOSS] Croissance → {_bodyLength} segments");
    }
    
    public void LoseSegment()
    {
        Debug.Log("[BOSS] LoseSegment() appelée");
        
        if (_segments.Count == 0)
        {
            Debug.LogWarning("[BOSS] Aucun segment à perdre");
            return;
        }
        
        Transform lastSegment = _segments[_segments.Count - 1];
        _segments.RemoveAt(_segments.Count - 1);
        Destroy(lastSegment.gameObject);
        
        _bodyLength--;
        
        UpdateSpeed();
        
        Debug.Log($"[BOSS] Segment perdu → {_bodyLength} segments restants");
    }
    
    private IEnumerator EnableColliderDelayed(Collider2D collider, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (collider != null)
        {
            collider.enabled = true;
        }
    }
    
    // ===========================
    // MORT DU BOSS
    // ===========================

    public void Die()
    {
        // Détruire tous les segments avec particules
        foreach (Transform segment in _segments)
        {
            if (segment != null)
            {
                for (int i = 0; i < particleLenght; i++)
                {
                    GameObject particule = Instantiate(particleBoss, segment.position, Quaternion.identity);
                    particule.GetComponent<SpriteRenderer>().color = segment.GetComponent<SpriteRenderer>().color;
                }
                Destroy(segment.gameObject);
            }
        }
        
        for (int i = 0; i < particleLenght * 2; i++)
        {
            GameObject particule = Instantiate(particleBoss, transform.position, Quaternion.identity);
            particule.GetComponent<SpriteRenderer>().color = GetComponent<SpriteRenderer>().color;;
        }
    
        // Freeze/Shake
        FreezeFrame.instance.Freeze(0.1f);
        ScreenShake.instance.Shake(0.5f, 0.5f);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnBossDestroyed();
        }

        Destroy(gameObject);
    }
}