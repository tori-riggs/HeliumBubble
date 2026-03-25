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
            _TotalNoteCount++;

            CheckForInstrumentAdjust();
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