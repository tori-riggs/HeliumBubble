using UnityEngine;

public class NoteBehavior : MonoBehaviour
{
    [Header("Cleanup")]
    [SerializeField] private float destroyAfterPassingDistance = 1.5f;

    private Vector3 targetHitPoint;
    private float speed;
    private bool initialized = false;
    
    public int NoteId { get; private set; } = -1;
    public NoteDirection Direction { get; private set; }

    private NotePool _pool;

    // Called by NotePool right after the note is taken from the pool.
    public void Pool_Initialize(NotePool pool, int noteId, NoteDirection direction)
    {
        _pool = pool;
        NoteId = noteId;
        Direction = direction;
    }
    
    public void Initialize(Vector3 targetHitPoint, float moveSpeed)
    {
        this.targetHitPoint = targetHitPoint;
        speed = moveSpeed;
        initialized = true;
    }

    // Despawns note
    public void Despawn()
    {
        _pool.Release(this);
    }

    // Pool uses this to clear state before returning to buffer
    public void ResetForPool()
    {
        NoteId = -1;
    }
    
    // Update is called once per frame
    private void Update()
    {
        if (!gameObject.activeSelf) return;
        
        transform.position += Vector3.up * (speed * Time.deltaTime);

        if (transform.position.y > targetHitPoint.y + destroyAfterPassingDistance)
        {
            Despawn();
        }
    }
}
