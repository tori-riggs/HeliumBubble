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
        [SerializeField] private int goodMargin = 100; // ±100 ms

        private float _pointsPerNote = 500f;

        [SerializeField] private Communicator communicator;

        [Header("UI")]
        public TextMeshProUGUI scoreText;
        public GameObject hitNotif;
        public TextMeshProUGUI hitNotifText;
        public SpriteRenderer background;
        public SpriteRenderer textBackground;

        private int _TotalNoteCount = 0;
        private float _CurrentHitScore = 0;

        private readonly int HIT_COUNT_INTERVAL = 5;
        private int DifficultyWindow => _pointsPerNote > 0 ? Mathf.CeilToInt(5000f / _pointsPerNote) : 12;
        private Queue<float> _recentNoteScores = new Queue<float>();
        private Difficulty _currentDifficulty = Difficulty.Easy;

        private bool interpColors = false;
        private long interpStart = -1;
        private long interpTime = 10000;

        // Background Colors
        private Color easyBgColor = Color.white;
        private Color mediumBgColor = new(255 / 255f, 255 / 255f, 0 / 255f);
        private Color expertBgColor = new(255 / 255f, 50 / 255f, 0 / 255f);

        // Text Background Colors (Placement, hit, score)
        private Color easyTextBgColor = new(65 / 255f, 60 / 255f, 122 / 255f);
        private Color mediumTextBgColor = new(161 / 255f, 150 / 255f, 29 / 255f);
        private Color expertTextBgColor = new(145 / 255f, 13 / 255f, 13 / 255f);

        // Hit notif colors
        private Color perfectColor = Color.cyan;
        private Color greatColor = Color.green;
        private Color goodColor = Color.yellow;
        private Color missColor = Color.red;
        
        // TODO input delay stuff???
        public int Score { get; private set; }

        public void SetChart(Chart chart)
        {
            _pointsPerNote = chart.PointsPerNote;
        }

        public void NoteHitScoring(double hitTiming)
        {
            // x ms early or x ms late
            double delay = Math.Abs(hitTiming);

            float noteAccuracy;
            if (delay <= perfectMargin)
            {
                hitNotifText.text = "Perfect!";
                hitNotifText.color = perfectColor;
                hitNotif.SetActive(true);
                Score += Mathf.RoundToInt(_pointsPerNote);
                _CurrentHitScore += 1f;
                noteAccuracy = 1.0f;
            } else if (delay <= greatMargin)
            {
                hitNotifText.text = "Great!";
                hitNotifText.color = greatColor;
                hitNotif.SetActive(true);
                Score += Mathf.RoundToInt(_pointsPerNote * 0.75f);
                _CurrentHitScore += 0.8f;
                noteAccuracy = 0.9f;
            } else if (delay <= goodMargin)
            {
                hitNotifText.text = "Good!";
                hitNotifText.color = goodColor;
                hitNotif.SetActive(true);
                Score += Mathf.RoundToInt(_pointsPerNote * 0.5f);
                _CurrentHitScore += 0.5f;
                noteAccuracy = 0.85f;
            } else
            {
                hitNotifText.text = "Miss!";
                hitNotifText.color = missColor;
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
            hitNotifText.color = missColor;
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
            var holdBonus = Mathf.RoundToInt(_pointsPerNote * heldFraction);
            Score += holdBonus;

            communicator.SendScorePacket(Score);
            scoreText.text = Score.ToString();
        }
        
        private void CheckForDifficultyAdjust(float noteScore)
        {
            _recentNoteScores.Enqueue(noteScore);
            if (_recentNoteScores.Count > DifficultyWindow)
                _recentNoteScores.Dequeue();

            if (_recentNoteScores.Count < DifficultyWindow) return;

            var sum = 0f;
            foreach (var s in _recentNoteScores) sum += s;
            var accuracy = sum / DifficultyWindow;

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

            interpColors = true;
            interpStart = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public void ResetCurrentDifficulty()
        {
            _currentDifficulty = SavedSettings.Instance.Difficulty.ToUpper() switch
            {
                "EASY" => Difficulty.Easy,
                "MEDIUM" => Difficulty.Medium,
                "EXPERT" => Difficulty.Expert,
                _ => Difficulty.Easy,
            };
        }

        public void ResetTexts()
        {
            _TotalNoteCount = 0;
            _CurrentHitScore = 0.0f;

            hitNotif.SetActive(false);
            hitNotifText.text = "";
            Score = 0;

            scoreText.text = "0";
        }

        private void AdjustColors()
        {
            Color bgColor = DifficultyToBgColor(this._currentDifficulty);
            Color textBgColor = DifficultyToTextBgColor(this._currentDifficulty);

            // These should always be changing at the same rate/time
            if (bgColor == background.color) return;

            Vector3 oldBgColor = new Vector3(background.color.r, background.color.g, background.color.b);
            Vector3 newBgColor = new Vector3(bgColor.r, bgColor.g, bgColor.b);

            Vector3 oldTextBgColor = new Vector3(textBackground.color.r, textBackground.color.g, textBackground.color.b);
            Vector3 newTextBgColor = new Vector3(textBgColor.r, textBgColor.g, textBgColor.b);

            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            float t = (float) (now - interpStart) / interpTime;

            if (t >= 1.0) interpColors = false;

            Vector3 interpBgColor = Vector3.Lerp(oldBgColor, newBgColor, t);
            background.color = new Color(interpBgColor.x, interpBgColor.y, interpBgColor.z);

            Vector3 interpTextBgColor = Vector3.Lerp(oldTextBgColor, newTextBgColor, t);
            textBackground.color = new Color(interpTextBgColor.x, interpTextBgColor.y, interpTextBgColor.z);
        }

        private Color DifficultyToBgColor(Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.Easy => easyBgColor,
                Difficulty.Medium => mediumBgColor,
                Difficulty.Expert => expertBgColor,
                _ => easyBgColor,
            };
        }

        private Color DifficultyToTextBgColor(Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.Easy => easyTextBgColor,
                Difficulty.Medium => mediumTextBgColor,
                Difficulty.Expert => expertTextBgColor,
                _ => easyTextBgColor,
            };
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
            //difficultyText.text = difficulty;
            if (!Enum.TryParse(difficulty, ignoreCase: true, out _currentDifficulty))
                _currentDifficulty = Difficulty.Easy;

            background.color = DifficultyToBgColor(this._currentDifficulty);
            textBackground.color = DifficultyToTextBgColor(this._currentDifficulty);
        }

        private void Update()
        {
            if (!interpColors) return;

            AdjustColors();
        }
    }
}