using UnityEngine;
using UnityEngine.InputSystem;

namespace RhythmGameAssets.Scripts
{
    public class PlayHandler : MonoBehaviour
    {
        [SerializeField] private Metronome metronome;
        [SerializeField] private NotePool notePool;

        [SerializeField] private float bpm = 121f;
        [SerializeField] private float delay = 0f; // milliseconds
        [SerializeField] private NoteDirection beatSpawnDirection = NoteDirection.Down;

        private int _tempCount;
        private int _nextBeatIndex;

        private double SecondsPerBeat => 60.0 / bpm;
        private double DelaySeconds => delay / 1000.0;

        private void Start()
        {
            _tempCount = 0;
            _nextBeatIndex = 0;
            metronome.StartPlayback();
        }

        private void Update()
        {
            double songTime = metronome.GetPlaybackTime();

            while (songTime >= GetBeatTime(_nextBeatIndex))
            {
                notePool.SpawnNote(_tempCount++, beatSpawnDirection);
                _nextBeatIndex++;
            }
        }

        private double GetBeatTime(int beatIndex)
        {
            return (beatIndex * SecondsPerBeat) + DelaySeconds;
        }
    }
}
