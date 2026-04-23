using UnityEngine;

namespace RhythmGameAssets.Scripts
{
    public class NoteBehavior : MonoBehaviour
    {
        [Header("Cleanup")]
        [SerializeField] private float destroyAfterPassingDistance = 1.5f;

        [Header("Hold Note")]
        // Assign a child Transform in hold-note prefabs (top pivot, extending downward).
        // Leave null on normal note prefabs.
        [SerializeField] private Transform tail;

        [Header("Bubble")]
        [SerializeField] private SpriteRenderer bubbleRenderer;
        [SerializeField] private Sprite[] bubbleSprites;

        private Vector3 _targetHitPoint;
        private float _speed;
        private bool _initialized = false;
    
        private float _tailHeight;      // world-unit height of a full tail
        private bool _isHeld = false;

        public ChartNote Note { get; private set; }
        public NoteDirection Direction { get; private set; }
        public float HoldDurationSeconds { get; private set; }
        public bool IsHoldNote => HoldDurationSeconds > 0f;

        private NotePool _pool;

        // Called by NotePool right after the note is taken from the pool.
        public void Pool_Initialize(NotePool pool, ChartNote note)
        {
            _pool = pool;
            Note = note;
            Direction = note.Direction;
        }

        public void Initialize(Vector3 targetHitPoint, float moveSpeed, float holdDurationSeconds = 0f)
        {
            _targetHitPoint = targetHitPoint;
            _speed = moveSpeed;
            HoldDurationSeconds = holdDurationSeconds;
            _isHeld = false;
            _initialized = true;

            bubbleRenderer.sprite = bubbleSprites[Random.Range(0, bubbleSprites.Length)];

            if (tail != null)
            {
                _tailHeight = moveSpeed * holdDurationSeconds;
                tail.gameObject.SetActive(holdDurationSeconds > 0f);
                if (holdDurationSeconds > 0f)
                {
                    // Scale the tail to represent the full hold duration.
                    // Assumes a top-pivot child so scaling extends downward.
                    tail.localScale = new Vector3(tail.localScale.x, _tailHeight, tail.localScale.z);
                }
            }
        }

        // Called by PlayHandler when the player presses the correct key on time.
        // Snaps the note head to the target and freezes its movement.
        public void StartHold()
        {
            _isHeld = true;
            transform.position = _targetHitPoint;
        }

        // Called every frame while the note is being held.
        // progress: 0.0 (just started) → 1.0 (hold complete).
        public void UpdateHoldVisual(float progress)
        {
            if (tail == null) return;

            float remaining = _tailHeight * (1f - Mathf.Clamp01(progress));
            tail.localScale = new Vector3(tail.localScale.x, Mathf.Max(remaining, 0f), tail.localScale.z);
        }

        // Despawns note
        public void Despawn()
        {
            _pool.Release(this);
        }

        // Pool uses this to clear state before returning to buffer
        public void ResetForPool()
        {
            Note = null;
            _isHeld = false;
            HoldDurationSeconds = 0f;
            if (tail != null) tail.gameObject.SetActive(false);
        }
    
        // Update is called once per frame
        private void Update()
        {
            if (!gameObject.activeSelf) return;
            if (_isHeld) return; // held notes stay frozen at the target

            transform.position += Vector3.up * (_speed * Time.deltaTime);
        }
    }
}
