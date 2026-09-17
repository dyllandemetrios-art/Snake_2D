using System.Collections;
using UnityEngine;

public class SlowMotion : MonoBehaviour
{

    public float slowTime = 0.2f;
    private bool isSlowed = false;

    [SerializeField] private float timeSlowed = 0f;

    public static SlowMotion instance;

    private bool once;


    private void Awake()
    {
        instance = this;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            StartSlowMotion(0.5f, 0f);
        }
        if (timeSlowed > 0 && isSlowed == false)
        {
            isSlowed = true;
            Time.timeScale = slowTime;
            Time.fixedDeltaTime = slowTime * Time.deltaTime;

        }
        else if(timeSlowed <= 0 && isSlowed == true)
        {
            isSlowed = false;
            Time.timeScale = 1;
            //Time.fixedDeltaTime = Time.timeScale;
        }

        
        if (isSlowed)
        {
            timeSlowed -= Time.deltaTime;
        }
        else
            timeSlowed = 0f;
    }
    
    public void StartSlowMotion(float duration, float WaitBeforeSM)
    {
        StartCoroutine(WaitBeforeSlowMotion(duration, WaitBeforeSM));
    }

    public IEnumerator WaitBeforeSlowMotion(float duration, float wait)
    {
        yield return new WaitForSeconds(wait);

        timeSlowed = duration;

    }
}
