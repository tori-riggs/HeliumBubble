using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RhythmGameAssets.Scripts
{
    // THIS WHOLE CLASS IS AI GENERATED, JUST FOR PLAYTESTING PURPOSES
    
    public class HitPopEffect : MonoBehaviour
    {
        [SerializeField] private float popScale = 1.2f;
        [SerializeField] private float popDuration = 0.08f;
        [SerializeField] private Color flashColor = Color.white;

        private Vector3 _baseScale;
        private SpriteRenderer _spriteRenderer;
        private Graphic _graphic;
        private Color _baseColor;
        private Coroutine _runningCoroutine;

        private void Awake()
        {
            _baseScale = transform.localScale;

            _spriteRenderer = GetComponent<SpriteRenderer>();
            _graphic = GetComponent<Graphic>();

            if (_spriteRenderer != null)
                _baseColor = _spriteRenderer.color;
            else if (_graphic != null)
                _baseColor = _graphic.color;
            else
                _baseColor = Color.white;
        }

        public void PlayPop()
        {
            if (_runningCoroutine != null)
                StopCoroutine(_runningCoroutine);

            _runningCoroutine = StartCoroutine(PopRoutine());
        }

        private IEnumerator PopRoutine()
        {
            Vector3 startScale = _baseScale * popScale;

            transform.localScale = startScale;
            SetVisualColor(flashColor);

            float elapsed = 0f;

            while (elapsed < popDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / popDuration;

                transform.localScale = Vector3.Lerp(startScale, _baseScale, t);
                SetVisualColor(Color.Lerp(flashColor, _baseColor, t));

                yield return null;
            }

            transform.localScale = _baseScale;
            SetVisualColor(_baseColor);
            _runningCoroutine = null;
        }

        private void SetVisualColor(Color color)
        {
            if (_spriteRenderer != null)
                _spriteRenderer.color = color;
            else if (_graphic != null)
                _graphic.color = color;
        }
    }
}