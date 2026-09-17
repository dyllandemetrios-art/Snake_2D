using UnityEngine;
using System.Collections;

public partial class FreezeFrame : MonoBehaviour
{
    public static FreezeFrame instance;

    private bool isFreezing = false;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

    }

    public void Freeze(float duration)
    {
        if (!isFreezing)
            StartCoroutine(FreezeCoroutine(duration));
    }

    private IEnumerator FreezeCoroutine(float duration)
    {
        isFreezing = true;
        float originalTimeScale = Time.timeScale;

        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration); // Realtime car le temps est fig�
        Time.timeScale = originalTimeScale;

        isFreezing = false;
    }
}
