using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RhythmGameAssets.Scripts
{
    public class ScoreHandler : MonoBehaviour
    {
        // Timing windows
        [SerializeField] private int perfectMargin = 20; // ±20 ms
        [SerializeField] private int greatMargin = 50; // ±50 ms
        [SerializeField] private int goodMargin = 100; // ±20 ms
        [SerializeField] private int perfectScore = 500;
        [SerializeField] private int greatScore = 375;
        [SerializeField] private int goodScore = 250;
        [SerializeField] private int missScore = 0;

        [SerializeField] private Communicator communicator;

        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI difficultyText;
        public GameObject hitNotif;
        public TextMeshProUGUI hitNotifText;

        private int _TotalNoteCount = 0;
        private float _CurrentHitScore = 0;

        private readonly int HIT_COUNT_INTERVAL = 10;
        
        // TODO input delay stuff???
        public int Score { get; private set; }

        public void NoteHitScoring(double hitTiming)
        {
            // x ms early or x ms late
            double delay = Math.Abs(hitTiming);

            if (delay <= perfectMargin)
            {
                hitNotifText.text = "Perfect!";
                hitNotif.SetActive(true);
                Score += perfectScore;
                _CurrentHitScore += 1f;
            } else if (delay <= greatMargin)
            {
                hitNotifText.text = "Great!";
                hitNotif.SetActive(true);
                Score += greatScore;
                _CurrentHitScore += 0.9f;
            } else if (delay <= goodMargin)
            {
                hitNotifText.text = "Good!";
                hitNotif.SetActive(true);
                Score += goodScore;
                _CurrentHitScore += 0.85f;
            } else
            {
                hitNotifText.text = "Miss!";
                hitNotif.SetActive(true);
            }

            _TotalNoteCount++;

            CheckForInstrumentAdjust();

            communicator.SendScorePacket(Score);
            scoreText.text = Score.ToString();
        }

        public void NoteMissed()
        {
            hitNotifText.text = "Miss!";
            hitNotif.SetActive(true);
            
            _TotalNoteCount++;

            CheckForInstrumentAdjust();
        }

        // Called when a hold note tail ends (either completed or released early).
        // heldFraction: 0.0 (not held at all) → 1.0 (fully completed).
        // Does not increment note count; the head hit already counted this note.
        public void HoldNoteScoring(float heldFraction)
        {
            int holdBonus = Mathf.RoundToInt(goodScore * heldFraction);
            Score += holdBonus;

            communicator.SendScorePacket(Score);
            scoreText.text = Score.ToString();
        }

        private void CheckForInstrumentAdjust()
        {
            if (_TotalNoteCount % HIT_COUNT_INTERVAL == 0)
            {
                AdjustInstrumentSound();
            }
        }

        private void AdjustInstrumentSound()
        {
            float scorePercentage = _CurrentHitScore / HIT_COUNT_INTERVAL;
            communicator.SendSoundPacket(scorePercentage);

            _CurrentHitScore = 0f;
        }

        private void Start()
        {
            hitNotif.SetActive(false);
            Score = 0;
        }

        private void Update()
        {
            
        }
    }
}