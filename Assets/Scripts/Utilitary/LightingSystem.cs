using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// LightingSystem - Gestion de l'éclairage dynamique
/// 
/// RESPONSABILITÉS :
/// - Modifier la lumière globale existante (sombre → claire)
/// - Attacher une lumière à la tête du snake
/// - Basculer en mode lumineux après Lilith
/// 
/// PATTERN : Singleton
/// </summary>
public class LightingSystem : MonoBehaviour
{
    public static LightingSystem Instance { get; private set; }
    
    [Header("Snake Light")]
    [SerializeField] private float _snakeLightRadius = 15f;
    [SerializeField] private float _snakeLightIntensity = 3f;
    [SerializeField] private Color _snakeLightColor = Color.white;
    
    [Header("Ambiance")]
    [SerializeField] private Color _darkAmbientColor = new Color(0.05f, 0.05f, 0.05f);
    [SerializeField] private float _darkAmbientIntensity = 0.3f;
    [SerializeField] private Color _brightAmbientColor = new Color(0.8f, 0.1f, 0.1f);
    [SerializeField] private float _brightAmbientIntensity = 1f;
    
    private Light2D _globalLight;
    private Light2D _snakeLight;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    void Start()
    {
        // MÉTHODE 1 : Par nom
        GameObject globalLightObj = GameObject.Find("Global Light 2D");
    
        if (globalLightObj != null)
        {
            _globalLight = globalLightObj.GetComponent<Light2D>();
            Debug.Log("[LIGHTING] Trouvé par nom");
        }
    
        // MÉTHODE 2 : Chercher tous les Light2D
        if (_globalLight == null)
        {
            Light2D[] allLights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
            Debug.Log($"[LIGHTING] Nombre de Light2D trouvées : {allLights.Length}");
        
            foreach (Light2D light in allLights)
            {
                Debug.Log($"[LIGHTING] Light trouvée : {light.name}, Type : {light.lightType}");
            
                if (light.lightType == Light2D.LightType.Global)
                {
                    _globalLight = light;
                    Debug.Log("[LIGHTING] Trouvé par type");
                    break;
                }
            }
        }
    
        if (_globalLight != null)
        {
            _globalLight.color = _darkAmbientColor;
            _globalLight.intensity = _darkAmbientIntensity;
            Debug.Log($"[LIGHTING] Lumière globale '{_globalLight.name}' configurée");
        }
        else
        {
            Debug.LogError("[LIGHTING] AUCUNE LIGHT2D TROUVÉE DU TOUT !");
        }
    
        SetupSnakeLight();
    }
    
    private void SetupSnakeLight()
    {
        GameObject snakeHead = GameObject.Find("Snake");
        if (snakeHead == null)
        {
            Debug.LogError("[LIGHTING] Snake non trouvé !");
            return;
        }
        
        GameObject lightObj = new GameObject("Snake_Head_Light");
        lightObj.transform.SetParent(snakeHead.transform);
        lightObj.transform.localPosition = Vector3.zero;
        
        _snakeLight = lightObj.AddComponent<Light2D>();
        _snakeLight.lightType = Light2D.LightType.Point;
        _snakeLight.color = _snakeLightColor;
        _snakeLight.intensity = _snakeLightIntensity;
        _snakeLight.pointLightOuterRadius = _snakeLightRadius;
        
        Debug.Log("[LIGHTING] Lumière du snake créée");
    }
    
    public void SwitchToBrightMode()
    {
        if (_globalLight != null)
        {
            _globalLight.color = _brightAmbientColor;
            _globalLight.intensity = _brightAmbientIntensity;
            Camera.main.backgroundColor = new Color(0.2f, 0.0f, 0.0f);
        }
        
        if (_snakeLight != null)
        {
            _snakeLight.enabled = false;
        }
        
        Debug.Log("[LIGHTING] Mode lumineux activé");
    }
}