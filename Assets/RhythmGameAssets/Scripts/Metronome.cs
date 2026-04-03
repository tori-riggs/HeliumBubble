using UnityEngine;
using System;
using TMPro;

namespace RhythmGameAssets.Scripts
{
    public class Metronome : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;

        [Header("Playback")]
        [SerializeField] private double startDelaySeconds = 2.0;

        [SerializeField] private TextMeshProUGUI songTimer;

        private double dspStartTime;
        private double dspPauseStartTime;
        private double totalPausedTime = 0.0;
        private bool isPlayingScheduled = false;

        private bool IsPaused = false;

        private bool _NeedsSync = true;
        private int _NumSyncTries = 0;
        private readonly int MAX_SYNC_TRIES = 20;

        public void Update()
        {
            double songTime = GetPlaybackTime();
            songTimer.text = songTime.ToString("0.000");
        }

        // this is in seconds
        // i.e. 0.01 = 10 ms
        private readonly float SONG_DIFF_THRESHOLD = 0.01f;

        // This part will probably only be for single-player
        /// <summary>
        /// Starts playback of the song
        /// </summary>
        public void StartPlayback()
        {
            dspStartTime = AudioSettings.dspTime + startDelaySeconds;
            audioSource.Stop();
            audioSource.PlayScheduled(dspStartTime);

            isPlayingScheduled = true;
        }

        /// <summary>
        /// Returns the playback time in seconds since the start
        /// </summary>
        public double GetPlaybackTime()
        {
            if (!isPlayingScheduled) return 0.0;
            if (IsPaused) return audioSource.time;

            double currentTime = AudioSettings.dspTime - dspStartTime - totalPausedTime;
            return Mathf.Max(0f, (float) currentTime);
        }

        public void PausePlayback()
        {
            dspPauseStartTime = AudioSettings.dspTime;

            audioSource.Pause();
            IsPaused = true;
        }

        private void UnpausePlayback()
        {
            double timePaused = AudioSettings.dspTime - dspPauseStartTime;
            totalPausedTime += timePaused;

            audioSource.UnPause();
            IsPaused = false;
            _NeedsSync = true;
        }

        public void AdjustPlaybackTime(bool isPlaying, float clientTime, float serverTime)
        {
            if (isPlaying && !audioSource.isPlaying)
            {
                if (IsPaused)
                {
                    UnpausePlayback();
                } 
                else
                {
                    StartPlayback();
                }
            }

            if (!isPlaying && audioSource.isPlaying)
            {
                PausePlayback();
            }

            float delay = Math.Abs(clientTime - serverTime);

            if (!_NeedsSync && _NumSyncTries >= MAX_SYNC_TRIES)
            {
                return;
            }

            _NeedsSync = delay > SONG_DIFF_THRESHOLD;

            // skip to server time if we are too far ahead/behind
            Debug.Log("Calculated server time: " + serverTime);
            Debug.Log("Client time: " + clientTime);
            Debug.Log("Delay: " + delay);
            audioSource.time = serverTime;

            _NumSyncTries++;
        }
    }
}