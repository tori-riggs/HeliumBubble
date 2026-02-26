using UnityEngine;
using UnityEngine.UIElements;

public class NoteBehavior : MonoBehaviour
{
    [Header("Cleanup")]
    [SerializeField] private float destroyAfterPassingDistance = 1.5f;

    private Vector3 targetHitPoint;
    private float speed;
    private bool initialized = false;
    
    public void Initialize(Vector3 targetHitPoint, float moveSpeed)
    {
        this.targetHitPoint = targetHitPoint;
        speed = moveSpeed;
        initialized = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!initialized) return;
        
        transform.position += Vector3.up * (speed * Time.deltaTime);

        if (transform.position.y > targetHitPoint.y + destroyAfterPassingDistance)
        {
            Destroy(gameObject);
        }
    }
}
