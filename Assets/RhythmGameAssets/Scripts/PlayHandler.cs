using System;
using System.Collections.Generic;
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
        
        [SerializeField] private ChartParser chartParser;
        [SerializeField] private ScoreHandler _scoreHandler;
        
        private int _nextNoteIndex;
        Queue<int> activeNoteIds = new Queue<int>();

        // Hold-note tracking: direction → note ID currently being held
        private Dictionary<NoteDirection, int> _heldNoteIds = new Dictionary<NoteDirection, int>();
        // Hold-note tracking: direction → song time (ms) when the hold started
        private Dictionary<NoteDirection, double> _heldNoteStartSongTime = new Dictionary<NoteDirection, double>();

        private Chart _chart; // current chart
        
        private double DelaySeconds => delay / 1000.0;

        private void Start()
        {
            // _song = chartParser.CurrentSong;
            _chart = chartParser.CurrentChart;
            foreach (ChartNote note in _chart.Notes)
            {
                Debug.Log(note);
            }
        }

        private void Update()
        {
            var songTime = metronome.GetPlaybackTime() * 1000.0;

            while (_nextNoteIndex < _chart.Notes.Count &&
                   songTime + (notePool.timeOnScreen * 1000.0) >= GetNoteTime(_nextNoteIndex))
            {
                activeNoteIds.Enqueue(_nextNoteIndex);
                var chartNote = _chart.Notes[_nextNoteIndex];
                float holdDuration = (float)(GetNoteLengthMs(_nextNoteIndex) / 1000.0);
                notePool.SpawnNote(_nextNoteIndex, chartNote.Direction, holdDuration);
                _nextNoteIndex++;
            }

            // Update any notes currently being held
            UpdateHeldNotes(songTime);

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

                        if (note.Length > 0) // hold note: freeze head and begin tracking the tail
                        {
                            var noteBehavior = notePool.GetActiveNoteById(id);
                            noteBehavior.StartHold();
                            _heldNoteIds[note.Direction] = id;
                            _heldNoteStartSongTime[note.Direction] = songTime;
                            // Don't release from the pool yet; UpdateHeldNotes will do it
                        }
                        else
                        {
                            notePool.Release(notePool.GetActiveNoteById(id));
                        }
                    }
                    else
                    {
                        notePool.Release(notePool.GetActiveNoteById(id));
                    }
                }
            }

        }

        // Advances hold-note state each frame. Scores and releases notes when the hold ends.
        private void UpdateHeldNotes(double songTime)
        {
            var toRelease = new List<NoteDirection>();

            foreach (var kvp in _heldNoteIds)
            {
                NoteDirection dir = kvp.Key;
                int noteId = kvp.Value;

                var noteBehavior = notePool.GetActiveNoteById(noteId);
                if (noteBehavior == null) // already cleaned up somehow
                {
                    toRelease.Add(dir);
                    continue;
                }

                double holdElapsedMs = songTime - _heldNoteStartSongTime[dir];
                double holdDurationMs = GetNoteLengthMs(noteId);
                float progress = (float)Math.Min(holdElapsedMs / holdDurationMs, 1.0);

                noteBehavior.UpdateHoldVisual(progress);

                bool holdComplete = songTime >= GetNoteTime(noteId) + holdDurationMs;
                bool keyReleased = !KeyboardHeldFromDirection(dir);

                if (holdComplete || keyReleased)
                {
                    _scoreHandler.HoldNoteScoring(progress);
                    notePool.Release(noteBehavior);
                    toRelease.Add(dir);
                }
            }

            foreach (var dir in toRelease)
            {
                _heldNoteIds.Remove(dir);
                _heldNoteStartSongTime.Remove(dir);
            }
        }

        private double GetNoteTime(int noteIndex)
        {
            return _chart.Notes[noteIndex].Position * 60000.0 / (_chart.BPM * 192);
        }
        

        private double GetNoteLengthMs(int noteIndex)
        {
            return _chart.Notes[noteIndex].Length * 60000.0 / (_chart.BPM * 192);
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

        private bool KeyboardHeldFromDirection(NoteDirection direction)
        {
            return direction switch
            {
                NoteDirection.Left => Keyboard.current.aKey.isPressed,
                NoteDirection.Up => Keyboard.current.wKey.isPressed,
                NoteDirection.Down => Keyboard.current.sKey.isPressed,
                NoteDirection.Right => Keyboard.current.dKey.isPressed,
                _ => false
            };
        }
    }
}
