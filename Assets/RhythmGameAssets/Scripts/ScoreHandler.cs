using System;
using TMPro;
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
        
        // TODO input delay stuff???
        public int Score { get; private set; }

        public void NoteHitScoring(double hitTiming)
        {
            // x ms early or x ms late
            double delay = Math.Abs(hitTiming);
            if (delay <= perfectMargin)
            {
                Debug.Log("Perfect!");
                hitNotifText.text = "Perfect!";
                hitNotif.SetActive(true);
                Score += perfectScore;
            } else if (delay <= greatMargin)
            {
                hitNotifText.text = "Great!";
                hitNotif.SetActive(true);
                Debug.Log("Great!");
                Score += greatScore;
            } else if (delay <= goodMargin)
            {
                hitNotifText.text = "Good!";
                hitNotif.SetActive(true);
                Debug.Log("Good!");
                Score += goodScore;
            } else
            {
                hitNotifText.text = "Miss!";
                hitNotif.SetActive(true);
                Debug.Log("Miss!");
            }
            communicator.SendScorePacket(Score);
            scoreText.text = Score.ToString();
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