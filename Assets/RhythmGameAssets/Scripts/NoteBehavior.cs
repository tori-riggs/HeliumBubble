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
        [SerializeField] private SpriteRenderer tail;

        [Header("Bubble")]
        [SerializeField] private SpriteRenderer bubbleRenderer;
        [SerializeField] private Sprite[] bubbleSprites;

        private Vector3 _targetHitPoint;
        private float _speed;
    
        private float _tailHeight;      // world-unit height of a full tail
        private bool _isHeld = false;

        public ChartNote Note { get; private set; }
        public NoteDirection Direction { get; private set; }
        public bool IsHoldNote => HoldDurationSeconds > 0f;

        private float HoldDurationSeconds { get; set; }
        private NotePool _pool;

        private SpriteRenderer _selfSprite;
        private Color _tailColor;
        private Color _tailGrey;

        public void Start()
        {
            _selfSprite = GetComponent<SpriteRenderer>();
            _tailColor = tail.color;
            _tailGrey = _tailColor / 2.0f;
        }

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

            bubbleRenderer.sprite = bubbleSprites[Random.Range(0, bubbleSprites.Length)];

            if (tail != null)
            {
                _tailHeight = moveSpeed * holdDurationSeconds;
                tail.gameObject.SetActive(holdDurationSeconds > 0f);
                if (holdDurationSeconds > 0f)
                {
                    // Scale the tail to represent the full hold duration.
                    // Assumes a top-pivot child so scaling extends downward.
                    tail.transform.localScale = new Vector3(tail.transform.localScale.x, _tailHeight, tail.transform.localScale.z);
                }
            }
        }

        // Called by PlayHandler when the player presses the correct key on time.
        // Snaps the note head to the target and freezes its movement.
        public void StartHold()
        {
            _isHeld = true;
            bubbleRenderer.enabled = false;
            transform.position = _targetHitPoint;
        }

        public void KeyReleasedEarly()
        {
            _isHeld = false;
            GreyOut();
        }

        public void GreyOut()
        {
            _selfSprite.color = Color.grey;
            tail.color = _tailGrey;
            bubbleRenderer.color = Color.grey;
        }

        public void ResetColors()
        {
            _selfSprite.color = Color.white;
            tail.color = _tailColor;
            bubbleRenderer.color = Color.white;
        }

        // Called every frame while the note is being held.
        // progress: 0.0 (just started) → 1.0 (hold complete).
        public void UpdateHoldVisual(float progress)
        {
            if (tail == null) return;

            float remaining = _tailHeight * (1f - Mathf.Clamp01(progress));
            tail.transform.localScale = new Vector3(tail.transform.localScale.x, Mathf.Max(remaining, 0f), tail.transform.localScale.z);
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
            bubbleRenderer.enabled = true;

            if (tail != null) tail.gameObject.SetActive(false);

            ResetColors();
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
