using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Service Locator Pattern - Registre central des managers
/// AVEC Service Locator :
///     Les managers sont enregistrés UNE FOIS au démarrage
///     On y accède instantanément avec ServiceLocator.Get<ScoreManager>()
/// 
/// UTILISATION :
///     Bootstrap enregistre les managers : ServiceLocator.Register(scoreManager)
///     Les autres scripts y accèdent : ServiceLocator.Get<ScoreManager>()
///     Vérifier existence : ServiceLocator.Has<ScoreManager>()
/// 
/// PATTERN : Singleton + Dictionary
/// </summary>
public class ServiceLocator : MonoBehaviour
{
    // ===========================
    // SINGLETON
    // ===========================
    // Une seule instance dans tout le jeu
    // Survit au changement de scène (DontDestroyOnLoad)
    
    private static ServiceLocator _instance;
    
    /// <summary>
    /// Accès à l'instance unique du Service Locator
    /// Se crée automatiquement si n'existe pas encore
    /// </summary>
    public static ServiceLocator Instance
    {
        get
        {
            // Si pas encore créé, le créer automatiquement
            if (_instance == null)
            {
                GameObject go = new GameObject("[ServiceLocator]");
                _instance = go.AddComponent<ServiceLocator>();
                DontDestroyOnLoad(go); // Survit aux changements de scène
            }
            return _instance;
        }
    }
    
    // ===========================
    // REGISTRE DES SERVICES
    // ===========================
    // Dictionary = tableau clé-valeur ultra rapide
    // Clé = Type (ex: typeof(ScoreManager))
    // Valeur = Instance du manager (ex: l'objet scoreManager)
    
    private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
    
    // ===========================
    // INITIALISATION
    // ===========================
    /// <summary>
    /// S'assure qu'une seule instance existe
    /// Si une deuxième est créée, elle se détruit automatiquement
    /// </summary>
    void Awake()
    {
        // Si une instance existe déjà ET ce n'est pas moi
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject); // Je me détruis
            return;
        }
        
        // Sinon, je deviens l'instance unique
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        Debug.Log("[SERVICE LOCATOR] Initialisé");
    }
    
    // ===========================
    // ENREGISTRER UN SERVICE
    // ===========================
    /// <summary>
    /// Enregistre un manager dans le registre
    /// Appelé par Bootstrap au démarrage
    /// </summary>
    /// <typeparam name="T">Type du manager (ex: ScoreManager)</typeparam>
    /// <param name="service">Instance du manager à enregistrer</param>
    public static void Register<T>(T service) where T : class
    {
        // Récupérer le Type (ex: typeof(ScoreManager))
        Type type = typeof(T);
        
        // Si déjà enregistré, écraser (avec warning)
        if (Instance._services.ContainsKey(type))
        {
            Debug.LogWarning($"[SERVICE LOCATOR] Service {type.Name} déjà enregistré, écrasement");
            Instance._services[type] = service;
        }
        else
        {
            // Sinon, ajouter normalement
            Instance._services.Add(type, service);
            Debug.Log($"[SERVICE LOCATOR] Service {type.Name} enregistré");
        }
    }
    
    // ===========================
    // RÉCUPÉRER UN SERVICE
    // ===========================
    /// <summary>
    /// Récupère un manager depuis le registre
    /// Utilisé partout dans le code pour accéder aux managers
    /// </summary>
    /// <typeparam name="T">Type du manager recherché</typeparam>
    /// <returns>Le manager ou null si non trouvé</returns>
    public static T Get<T>() where T : class
    {
        Type type = typeof(T);
        
        // TryGetValue = cherche dans le Dictionary
        // Si trouvé, stocke dans 'service' et retourne true
        if (Instance._services.TryGetValue(type, out object service))
        {
            return service as T; // Cast vers le bon type
        }
        
        // Si non trouvé, erreur
        Debug.LogError($"[SERVICE LOCATOR] Service {type.Name} non trouvé !");
        return null;
    }
    
    // ===========================
    // VÉRIFIER EXISTENCE
    // ===========================
    /// <summary>
    /// Vérifie si un service existe sans le récupérer
    /// Utile pour éviter les erreurs null
    /// </summary>
    /// <typeparam name="T">Type du service à vérifier</typeparam>
    /// <returns>True si le service existe, False sinon</returns>
    public static bool Has<T>() where T : class
    {
        Type type = typeof(T);
        return Instance._services.ContainsKey(type);
    }
    
    // ===========================
    // DÉSENREGISTRER
    // ===========================
    /// <summary>
    /// Retire un service du registre
    /// Rarement utilisé, sauf changement de scène complexe
    /// </summary>
    public static void Unregister<T>() where T : class
    {
        Type type = typeof(T);
        
        if (Instance._services.ContainsKey(type))
        {
            Instance._services.Remove(type);
            Debug.Log($"[SERVICE LOCATOR] Service {type.Name} désenregistré");
        }
    }
    
    // ===========================
    // CLEAR
    // ===========================
    /// <summary>
    /// Efface TOUS les services
    /// Utilisé pour reset complet (ex: retour menu principal)
    /// </summary>
    public static void Clear()
    {
        Instance._services.Clear();
        Debug.Log("[SERVICE LOCATOR] Tous les services effacés");
    }
    
    // ===========================
    // DEBUG
    // ===========================
    /// <summary>
    /// Affiche la liste des services enregistrés
    /// Utile pour vérifier que Bootstrap a bien tout enregistré
    /// </summary>
    public static void LogRegisteredServices()
    {
        Debug.Log($"[SERVICE LOCATOR] Services enregistrés ({Instance._services.Count}) :");
        
        foreach (var kvp in Instance._services)
        {
            Debug.Log($"  - {kvp.Key.Name}");
        }
    }
}