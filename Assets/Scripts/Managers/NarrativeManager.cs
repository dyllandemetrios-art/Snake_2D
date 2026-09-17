using UnityEngine;
using Ink.Runtime;
using UnityEngine.UI;
using System.Collections;
using System;
using TMPro;

/// <summary>
/// NarrativeManager - Gestion du système de narration Ink
/// 
/// RESPONSABILITÉS :
/// - Charger et compiler le fichier Ink JSON
/// - Jouer des knots (sections narratives)
/// - Afficher le texte avec effet typing (lettre par lettre)
/// - Gérer le bouton Continue
/// - Mettre le jeu en pause pendant la narration (Time.timeScale = 0 + StopMovement)
/// - Appeler un callback à la fin d'un knot
/// 
/// SYSTÈME INK :
/// Ink est un langage de script narratif
/// Organisé en "knots" (sections) : === nom_knot ===
/// 
/// KNOTS UTILISÉS :
/// - start : Intro du jeu
/// - apple_adam/eve/lilith_X : Textes après avoir mangé ces pommes (X = 1 à 5)
/// - first_death : Renaissance Lilith
/// - lilith_freedom : Première pomme après renaissance
/// - lilith_vs_boss : Apparition du boss
/// - ending_adam/eve/lilith : Endings selon le chemin
/// - ending_lilith_victory : Victoire contre le boss
/// 
/// TYPING EFFECT :
/// Le texte s'affiche lettre par lettre (machine à écrire)
/// Vitesse contrôlée par _typingSpeed
/// 
/// PAUSE DU JEU :
/// Time.timeScale = 0 pendant la narration
/// GameObject.Find("Snake").StopMovement() pour arrêter explicitement le serpent
/// WaitForSecondsRealtime pour que le typing fonctionne malgré timeScale = 0
/// 
/// PATTERN : Service (enregistré dans ServiceLocator)
/// </summary>
public class NarrativeManager : MonoBehaviour
{
    [Header("Ink Story")]
    [SerializeField] private TextAsset _inkJSON;
    
    [Header("UI")]
    [SerializeField] private GameObject _narrativePanel;
    [SerializeField] private TMP_Text _dialogueText;
    [SerializeField] private Button _continueButton;
    [SerializeField] private float _typingSpeed = 0.03f;
    
    private Story _story;
    private Coroutine _typingCoroutine;
    private Action _onComplete;
    private bool _isPlaying = false;
    
    void Awake()
    {
        if (_inkJSON == null)
        {
            Debug.LogError("[NARRATIVE] InkJSON non assigné dans l'inspector !");
            return;
        }
    
        Debug.Log("[NARRATIVE] Ink JSON trouvé : " + _inkJSON.name);
    
        try
        {
            _story = new Story(_inkJSON.text);
            Debug.Log("[NARRATIVE] Story initialisée avec succès");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[NARRATIVE] Erreur compilation Ink : " + e.Message);
            return;
        }
    
        if (_narrativePanel != null)
        {
            _narrativePanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[NARRATIVE] Narrative Panel non assigné");
        }
    
        if (_continueButton != null)
        {
            _continueButton.onClick.AddListener(OnContinuePressed);
            Debug.Log("[NARRATIVE] Bouton Continue lié");
        }
        else
        {
            Debug.LogWarning("[NARRATIVE] Continue Button non assigné");
        }
    }
    
    /// <summary>
    /// Lance la lecture d'un knot Ink avec pause complète du jeu
    /// </summary>
    public void PlayKnot(string knotName, Action onComplete = null)
    {
        if (_story == null)
        {
            Debug.LogError($"[NARRATIVE] Story non initialisée ! Impossible de jouer '{knotName}'");
            onComplete?.Invoke();
            return;
        }
    
        Debug.Log($"[NARRATIVE] Lecture du knot : {knotName}");
    
        _onComplete = onComplete;
        _isPlaying = true;
    
        // PAUSE TOTALE
        Time.timeScale = 0f;
        
        // ARRÊTER LE SERPENT
        SnakeController snake = FindFirstObjectByType<SnakeController>();
        if (snake != null)
        {
            snake.StopMovement();
            Debug.Log("[NARRATIVE] Snake arrêté");
        }
        else
        {
            Debug.LogError("[NARRATIVE] Snake non trouvé !");
        }
    
        try
        {
            _story.ChoosePathString(knotName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NARRATIVE] Erreur : {e.Message}");
            Time.timeScale = 1f;
            snake?.ResumeMovement();
            onComplete?.Invoke();
            return;
        }
    
        ServiceLocator.Get<UIManager>()?.ShowNarrative();
        
        if (_continueButton != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(_continueButton.gameObject);
        }
    
        DisplayNextLine();
    }
    
    private void DisplayNextLine()
    {
        if (_story.canContinue)
        {
            string text = _story.Continue().Trim();
            
            if (string.IsNullOrEmpty(text))
            {
                DisplayNextLine();
                return;
            }
            
            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
            }
            
            _typingCoroutine = StartCoroutine(TypeText(text));
        }
        else
        {
            OnKnotComplete();
        }
    }
    
    private IEnumerator TypeText(string text)
    {
        string displayedText = "";
    
        foreach (char c in text)
        {
            displayedText += c;
            ServiceLocator.Get<UIManager>()?.SetNarrativeText(displayedText);
            yield return new WaitForSecondsRealtime(_typingSpeed);
        }
    }
    
    private void OnContinuePressed()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }
        
        DisplayNextLine();
    }
    
    /// <summary>
    /// Reprend le jeu après la narration
    /// </summary>
    private void OnKnotComplete()
    {
        Debug.Log("[NARRATIVE] Knot terminé");
    
        _isPlaying = false;
    
        // REPRENDRE LE JEU
        Time.timeScale = 1f;
        
        // REPRENDRE LE SERPENT
        SnakeController snake = FindFirstObjectByType<SnakeController>();
        if (snake != null)
        {
            snake.ResumeMovement();
            Debug.Log("[NARRATIVE] Snake relancé");
        }
    
        ServiceLocator.Get<UIManager>()?.HideNarrative();
    
        _onComplete?.Invoke();
        _onComplete = null;
    }
    
    public bool IsPlaying() => _isPlaying;
}