using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameOverManager : MonoBehaviour
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _statsText;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _menuButton;
    
    void Start()
    {
        if (_restartButton != null)
            _restartButton.onClick.AddListener(OnRestartClicked);
        
        if (_menuButton != null)
            _menuButton.onClick.AddListener(OnMenuClicked);
        
        DisplayEnding();
    }
    
    private void DisplayEnding()
    {
        string dominantPath = PlayerPrefs.GetString("EndingPath", "Eve");
        string stats = PlayerPrefs.GetString("EndingStats", "");
        
        string title = dominantPath switch
        {
            "Adam" => "ADAM PATH",
            "Eve" => "EVE PATH",
            "Lilith" => "LILITH PATH",
            _ => "FIN"
        };
        
        if (_titleText != null)
            _titleText.text = title;
        
        if (_statsText != null)
            _statsText.text = stats;
    }
    
    private void OnRestartClicked()
    {
        SceneManager.LoadScene(1);
    }
    
    private void OnMenuClicked()
    {
        SceneManager.LoadScene(0);
    }
}