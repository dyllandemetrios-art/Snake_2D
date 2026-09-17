using UnityEngine;

public class ExplosionForce : MonoBehaviour
{
    public float explosionForce = 50f;

    public float desagregationSpeed;
    
    private void Start()
    {
        GetComponent<Rigidbody2D>().AddForce(new Vector3(Random.Range(-1f, 1f), Random.Range(2f, 3f), 0) * explosionForce, ForceMode2D.Impulse);
    }
    
    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, Time.deltaTime * desagregationSpeed);

        if(transform.localScale.magnitude < 0.05f)
        {
            Destroy(gameObject);
        }
    }
}
