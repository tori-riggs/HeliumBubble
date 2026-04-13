using System;
using System.Collections.Generic;
using MainMenu;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RhythmGameAssets.Scripts
{
    public class ScoreHandler : MonoBehaviour
    {
        private enum Difficulty { Easy, Medium, Expert }

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
        private const int DIFFICULTY_WINDOW = 12;
        private Queue<float> _recentNoteScores = new Queue<float>();
        private Difficulty _currentDifficulty = Difficulty.Easy;
        
        // TODO input delay stuff???
        public int Score { get; private set; }

        public void NoteHitScoring(double hitTiming)
        {
            // x ms early or x ms late
            double delay = Math.Abs(hitTiming);

            float noteAccuracy;
            if (delay <= perfectMargin)
            {
                hitNotifText.text = "Perfect!";
                hitNotif.SetActive(true);
                Score += perfectScore;
                _CurrentHitScore += 1f;
                noteAccuracy = 1.0f;
            } else if (delay <= greatMargin)
            {
                hitNotifText.text = "Great!";
                hitNotif.SetActive(true);
                Score += greatScore;
                _CurrentHitScore += 0.9f;
                noteAccuracy = 0.9f;
            } else if (delay <= goodMargin)
            {
                hitNotifText.text = "Good!";
                hitNotif.SetActive(true);
                Score += goodScore;
                _CurrentHitScore += 0.85f;
                noteAccuracy = 0.85f;
            } else
            {
                hitNotifText.text = "Miss!";
                hitNotif.SetActive(true);
                noteAccuracy = 0.0f;
            }

            _TotalNoteCount++;

            CheckForDifficultyAdjust(noteAccuracy);
            CheckForInstrumentAdjust();

            communicator.SendScorePacket(Score);
            scoreText.text = Score.ToString();
        }

        public void NoteMissed()
        {
            hitNotifText.text = "Miss!";
            hitNotif.SetActive(true);
            
            _TotalNoteCount++;

            CheckForDifficultyAdjust(0.0f);
            CheckForInstrumentAdjust();
        }

        // Called when a hold note tail ends (either completed or released early).
        // heldFraction: 0.0 (not held at all) → 1.0 (fully completed).
        // Does not increment note count; the head hit already counted this note.
        public void HoldNoteScoring(float heldFraction)
        {
            var holdBonus = Mathf.RoundToInt(goodScore * heldFraction);
            Score += holdBonus;

            communicator.SendScorePacket(Score);
            scoreText.text = Score.ToString();
        }
        
        private void CheckForDifficultyAdjust(float noteScore)
        {
            _recentNoteScores.Enqueue(noteScore);
            if (_recentNoteScores.Count > DIFFICULTY_WINDOW)
                _recentNoteScores.Dequeue();

            if (_recentNoteScores.Count < DIFFICULTY_WINDOW) return;

            var sum = 0f;
            foreach (var s in _recentNoteScores) sum += s;
            var accuracy = sum / DIFFICULTY_WINDOW;

            switch (accuracy)
            {
                case > 0.8f when _currentDifficulty < Difficulty.Expert:
                    _currentDifficulty++;
                    _recentNoteScores.Clear();
                    break;
                case < 0.5f when _currentDifficulty > Difficulty.Easy:
                    _currentDifficulty--;
                    _recentNoteScores.Clear();
                    break;
            }

            difficultyText.text = GetCurrentDifficulty();
        }

        public string GetCurrentDifficulty()
        {
            return _currentDifficulty.ToString();
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
                        
            var difficulty = SavedSettings.Instance.Difficulty;
            difficultyText.text = difficulty;
            if (!Enum.TryParse(difficulty, ignoreCase: true, out _currentDifficulty))
                _currentDifficulty = Difficulty.Easy;
        }

        private void Update()
        {
            
        }
    }
}