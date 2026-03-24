using System;
using UnityEngine;

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
        // TODO input delay stuff???
        public int Score { get; private set; }

        public void NoteHitScoring(double noteTime, double hitTiming)
        {
            // x ms early or x ms late
            double delay = Math.Abs(hitTiming - noteTime);
            if (delay <= perfectMargin)
            {
                Debug.Log("Perfect!");
                Score += perfectScore;
            } else if (delay <= greatMargin)
            {
                Debug.Log("Great!");
                Score += greatScore;
            } else if (delay <= goodMargin)
            {
                Debug.Log("Good!");
                Score += goodScore;
            }
            Debug.Log("Miss!");
        }

        private void Start()
        {
            Score = 0;
        }

        private void Update()
        {
            
        }
    }
}