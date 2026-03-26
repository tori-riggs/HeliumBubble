using UnityEngine;
using System;

namespace RhythmGameAssets.Scripts
{
    public class Metronome : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;

        [Header("Playback")]
        [SerializeField] private double startDelaySeconds = 2.0;

        private double dspStartTime;
        private bool isPlayingScheduled = false;

        // this is in seconds
        // i.e. 0.02 = 20 ms
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
            audioSource.Stop();
            audioSource.Play();
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
        }

        public void AdjustPlaybackTime(bool isPlaying, float time)
        {
            if (isPlaying && !audioSource.IsPlaying)
            {
                ForceStartPlayback();
            }

            if (!isPlaying && audioSource.IsPlaying)
            {
                PausePlayback();
            }

            float currentTime = audioSource.time;
            float delay = Math.Abs(currentTime - time);

            // skip to server time if we are too far ahead/behind
            if (delay > SONG_DIFF_THRESHOLD)
            {
                audioSource.time = time;
            }
        }
    }
}