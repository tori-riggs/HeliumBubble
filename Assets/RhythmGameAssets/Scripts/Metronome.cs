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

        [Header("UI")]
        public SpriteRenderer SongTimer;

        private double dspStartTime;
        private bool isPlayingScheduled = false;

        private bool IsPaused = false;

        private bool _NeedsSync = true;
        private int _NumSyncTries = 0;
        private readonly int MAX_SYNC_TRIES = 100;

        private float _songTime;
        private const float _songTimerXScale = 0.985f;
        private const float _songTimerYScale = 0.12f;

        public void Start()
        {
            audioSource.mute = true;
            _songTime = audioSource.clip.length;
        }

        public void Update()
        {
            float currentTime = GetPlaybackTime();
            float progress = Math.Min(currentTime / _songTime, 1.0f);
            float scaleX = progress * _songTimerXScale;
            SongTimer.transform.localScale = new Vector2(scaleX, _songTimerYScale);
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
        public float GetPlaybackTime()
        {
            if (IsPaused) return audioSource.time;

            double currentTime = AudioSettings.dspTime - dspStartTime;
            return Mathf.Max(0f, (float) currentTime);
        }

        public float GetMaxPlaytime()
        {
            return _songTime;
        }

        public void PausePlayback()
        {
            audioSource.Pause();
            IsPaused = true;
        }

        private void UnpausePlayback()
        {
            audioSource.UnPause();
            IsPaused = false;
            _NeedsSync = true;
        }

        public void ForceResync()
        {
            this._NumSyncTries = 0;
            this._NeedsSync = true;
        }

        public void AdjustPlaybackTime(bool isPlaying, float serverTime)
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

            if (!_NeedsSync && _NumSyncTries >= MAX_SYNC_TRIES)
            {
                return;
            }

            double clientTime = GetPlaybackTime();
            double delay = Math.Abs(clientTime - serverTime);

            _NeedsSync = delay > SONG_DIFF_THRESHOLD;

            // skip to server time if we are too far ahead/behind
            //Debug.Log("Calculated server time: " + serverTime);
            //Debug.Log("Client time: " + clientTime);
            //Debug.Log("Delay: " + delay);
            dspStartTime = AudioSettings.dspTime - serverTime;

            serverTime = Math.Max(0, serverTime);
            serverTime = Math.Min(serverTime, audioSource.clip.length - 0.001f);

            audioSource.time = serverTime;

            _NumSyncTries++;
        }
    }
}