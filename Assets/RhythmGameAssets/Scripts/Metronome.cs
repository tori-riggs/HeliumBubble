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
        private bool isPlayingScheduled = false;

        private bool IsPaused = false;

        private bool NeedsSync = true;

        public void Update()
        {
            float songTime = audioSource.time;
            songTimer.text = Math.Floor(songTime).ToString();
        }

        // this is in seconds
        // i.e. 0.03 = 30 ms
        private readonly float SONG_DIFF_THRESHOLD = 0.02f;

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

        private void ForceStartPlayback()
        {
            audioSource.Play();
        }
        
        private void UnpausePlayback()
        {
            audioSource.UnPause();
            IsPaused = false;
            NeedsSync = true;
        }

        /// <summary>
        /// Returns the playback time in seconds since the start
        /// </summary>
        public double GetPlaybackTime()
        {
            //if (!isPlayingScheduled)
            //    return 0.0;

            //double currentTime = AudioSettings.dspTime - dspStartTime;
            //return Mathf.Max(0f, (float)currentTime);
            return audioSource.time;
        }

        public void PausePlayback()
        {
            audioSource.Pause();
            IsPaused = true;
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
                    ForceStartPlayback();
                }
            }

            if (!isPlaying && audioSource.isPlaying)
            {
                PausePlayback();
            }

            float delay = Math.Abs(clientTime - serverTime);

            if (!NeedsSync)
            {
                return;
            }

            if (delay < SONG_DIFF_THRESHOLD)
            {
                NeedsSync = false;
            }

            // skip to server time if we are too far ahead/behind
            Debug.Log("Calculated server time: " + serverTime);
            Debug.Log("Client time: " + clientTime);
            Debug.Log("Delay: " + delay);
            audioSource.time = serverTime;
        }
    }
}