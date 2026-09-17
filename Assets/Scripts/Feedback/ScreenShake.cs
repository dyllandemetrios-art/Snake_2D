using Unity.VisualScripting;
using UnityEngine;

public class ScreenShake : MonoBehaviour
{
    public float shakeAmount = 0.05f;

    private Vector3 initialPosition;
    private float shakeTimer;

    public static ScreenShake instance;

    void Start()
    {
        instance = this;
        initialPosition = transform.position;
    }

    void Update()
    {

        if (shakeTimer > 0)
        {
            transform.position = initialPosition + (Vector3)Random.insideUnitSphere * shakeAmount;
            shakeTimer -= Time.deltaTime;
        }
        
        else
        {
            transform.position = initialPosition;
        }
    }

    public void Shake(float _shakeDuration, float _shakeAmount)
    {
        shakeTimer = _shakeDuration;
        shakeAmount = _shakeAmount;
    }

}