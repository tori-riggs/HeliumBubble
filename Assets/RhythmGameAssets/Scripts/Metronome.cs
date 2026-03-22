using UnityEngine;

namespace RhythmGameAssets.Scripts
{
    public class Metronome : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;

        [Header("Playback")]
        [SerializeField] private double startDelaySeconds = 0.1;

        private double dspStartTime;
        private bool isPlayingScheduled = false;

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
            if (!isPlayingScheduled)
                return 0.0;

            double currentTime = AudioSettings.dspTime - dspStartTime;
            return Mathf.Max(0f, (float)currentTime);
        }
    }
}