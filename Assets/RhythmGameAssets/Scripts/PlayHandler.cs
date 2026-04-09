using System;
using System.Collections.Generic;
using MainMenu;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RhythmGameAssets.Scripts
{
    public class PlayHandler : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private HitPopEffect leftTarget;
        [SerializeField] private HitPopEffect upTarget;
        [SerializeField] private HitPopEffect downTarget;
        [SerializeField] private HitPopEffect rightTarget;
        
        [SerializeField] private Metronome metronome;
        [SerializeField] private NotePool notePool;

        [SerializeField] private float bpm = 121f;
        [SerializeField] private float delay = 0f; // milliseconds
        [SerializeField] private NoteDirection beatSpawnDirection = NoteDirection.Down;
        
        // [SerializeField] private ChartParser chartParser;
        // TODO: Saved settings not being used
        [SerializeField] private SavedSettings savedSettings;
        [SerializeField] private ScoreHandler _scoreHandler;
        
        private int _nextNoteIndex;
        Queue<int> activeNoteIds = new Queue<int>();
        private Chart _chart; // current chart
        
        private double DelaySeconds => delay / 1000.0;

        private void Start()
        {
            metronome.StartPlayback();
            // _song = chartParser.CurrentSong;
            var difficulty = savedSettings.Difficulty;
            var instrument = savedSettings.Instrument;
            _chart = savedSettings.SelectedSong.GetChart(difficulty, instrument);
            foreach (ChartNote note in _chart.Notes)
            {
                Debug.Log(note);
            }
        }

        private void Update()
        {
            var songTime = metronome.GetPlaybackTime() * 1000.0;

            while (songTime + (notePool.timeOnScreen * 1000.0) >= GetNoteTime(_nextNoteIndex))
            {
                activeNoteIds.Enqueue(_nextNoteIndex);
                notePool.SpawnNote(_nextNoteIndex, _chart.Notes[_nextNoteIndex++].Direction);
            }
            
            if (Keyboard.current.aKey.wasPressedThisFrame)
            {
                leftTarget.PlayPop();
            }

            if (Keyboard.current.wKey.wasPressedThisFrame)
            {
                upTarget.PlayPop();
            }

            if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                downTarget.PlayPop();
            }

            if (Keyboard.current.dKey.wasPressedThisFrame)
            {
                rightTarget.PlayPop();
            }

            if (activeNoteIds.Count == 0)
            {
                return;
            }
            
            // If input was hit and there is a note within 100 milliseconds
            // Check if that key was pressed. If not, wrong key was pressed so missed note
            // If key was pressed, log that hit with the delay

            if (songTime - 100.0 >= GetActiveNoteTime())
            {
                var id = activeNoteIds.Dequeue();
                notePool.Release(notePool.GetActiveNoteById(id));
                _scoreHandler.hitNotifText.text = "Miss!";
                _scoreHandler.hitNotif.SetActive(true);
                // TODO: disappear after a while
                _scoreHandler.NoteMissed();
            }
            
            
            if (Keyboard.current != null &&
                (Keyboard.current.aKey.wasPressedThisFrame ||
                 Keyboard.current.wKey.wasPressedThisFrame ||
                 Keyboard.current.sKey.wasPressedThisFrame ||
                 Keyboard.current.dKey.wasPressedThisFrame))
            {
                var timeToNextNote = GetActiveNoteTime();
                if (songTime + 150.0 >= timeToNextNote)
                {
                    var id = activeNoteIds.Dequeue();
                    var note = _chart.Notes[id];

                    if (KeyboardPressFromDirection(note.Direction))
                    {
                        _scoreHandler.NoteHitScoring(songTime - timeToNextNote);
                    }

                    notePool.Release(notePool.GetActiveNoteById(id));
                }
            }
            
        }

        private double GetNoteTime(int noteIndex)
        {
            return _chart.Notes[noteIndex].Position * 60000.0 / (_chart.BPM * 192);
        }
        
        private double GetActiveNoteTime()
        {
            return _chart.Notes[activeNoteIds.Peek()].Position * 60000.0 / (_chart.BPM * 192);
        }

        private bool KeyboardPressFromDirection(NoteDirection direction)
        {
            return direction switch
            {
                NoteDirection.Left => Keyboard.current.aKey.wasPressedThisFrame,
                NoteDirection.Up => Keyboard.current.wKey.wasPressedThisFrame,
                NoteDirection.Down => Keyboard.current.sKey.wasPressedThisFrame,
                NoteDirection.Right => Keyboard.current.dKey.wasPressedThisFrame,
                _ => false
            };
        }
    }
}
