using System;
using System.Collections.Generic;
using System.Linq;
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
        // [SerializeField] private SavedSettings savedSettings;
        [SerializeField] private ScoreHandler _scoreHandler;
        
        private int _nextNoteIndex;
        Queue<ChartNote> activeNoteIds = new Queue<ChartNote>();

        // Hold-note tracking: direction → ChartNote currently being held
        private Dictionary<NoteDirection, ChartNote> _heldNoteIds = new Dictionary<NoteDirection, ChartNote>();
        // Hold-note tracking: direction → song time (ms) when the hold started
        private Dictionary<NoteDirection, double> _heldNoteStartSongTime = new Dictionary<NoteDirection, double>();
        
        private Dictionary<NoteDirection, bool> _inputStates = new Dictionary<NoteDirection, bool>
        {
            { NoteDirection.Left, false },
            { NoteDirection.Up, false },
            { NoteDirection.Down, false },
            { NoteDirection.Right, false }
        };

        private Chart _chart; // current chart
        
        private double DelaySeconds => delay / 1000.0;

        private void Start()
        {
            // _song = chartParser.CurrentSong;
            var difficulty = SavedSettings.Instance.Difficulty;
            var instrument = SavedSettings.Instance.Instrument;
            _chart = SavedSettings.Instance.SelectedSong.GetChart(difficulty, instrument);
            foreach (ChartNote note in _chart.Notes)
            {
                Debug.Log(note);
            }
            
            metronome.StartPlayback();
        }

        private void Update()
        {
            if (Keyboard.current.aKey.wasPressedThisFrame) leftTarget.PlayPop();
            if (Keyboard.current.wKey.wasPressedThisFrame) upTarget.PlayPop();
            if (Keyboard.current.sKey.wasPressedThisFrame) downTarget.PlayPop();
            if (Keyboard.current.dKey.wasPressedThisFrame) rightTarget.PlayPop();
            
            if (_nextNoteIndex >= _chart.Notes.Count - 1) return;
            
            var songTime = metronome.GetPlaybackTime() * 1000.0;

            while (songTime + (notePool.timeOnScreen * 1000.0) >= GetNoteTime(_chart.Notes[_nextNoteIndex]))
            {
                var chartNote = _chart.Notes[_nextNoteIndex];
                activeNoteIds.Enqueue(chartNote);
                float holdDuration = (float)(GetNoteLengthMs(chartNote) / 1000.0);
                notePool.SpawnNote(chartNote.ID, chartNote.Direction, holdDuration);
                _nextNoteIndex++;
            }

            // Update any notes currently being held
            UpdateHeldNotes(songTime);

            if (activeNoteIds.Count == 0)
            {
                return;
            }
            
            // If input was hit and there is a note within 100 milliseconds
            // Check if that key was pressed. If not, wrong key was pressed so missed note
            // If key was pressed, log that hit with the delay

            if (activeNoteIds.Count == 0) return;
            double nextNoteTime = GetActiveNoteTime();
            while (songTime - 100.0 >= nextNoteTime)
            {
                var note = activeNoteIds.Dequeue();
                notePool.Release(notePool.GetActiveNoteById(note.ID));
                _scoreHandler.hitNotifText.text = "Miss!";
                _scoreHandler.hitNotif.SetActive(true);
                // TODO: disappear after a while
                _scoreHandler.NoteMissed();
        
                if (activeNoteIds.Count == 0) return;
        
                nextNoteTime = GetActiveNoteTime();
            }
        
            if (Keyboard.current != null &&
                (Keyboard.current.aKey.wasPressedThisFrame ||
                 Keyboard.current.wKey.wasPressedThisFrame ||
                 Keyboard.current.sKey.wasPressedThisFrame ||
                 Keyboard.current.dKey.wasPressedThisFrame))
            {
                // Save all input states 
                _inputStates[NoteDirection.Left]  = Keyboard.current.aKey.wasPressedThisFrame;
                _inputStates[NoteDirection.Up]    = Keyboard.current.wKey.wasPressedThisFrame;
                _inputStates[NoteDirection.Down]  = Keyboard.current.sKey.wasPressedThisFrame;
                _inputStates[NoteDirection.Right] = Keyboard.current.dKey.wasPressedThisFrame;

                // Loop until either all inputs are handled or no more hittable notes
                double timeToNextNote = GetActiveNoteTime();
                while (songTime + 150.0 >= timeToNextNote && _inputStates.Values.Any(state => state))
                {
                    var note = activeNoteIds.Dequeue();

                    if (KeyboardPressFromDirection(note.Direction))
                    {
                        _inputStates[note.Direction] = false;

                        _scoreHandler.NoteHitScoring(songTime - timeToNextNote);

                        if (note.Length > 0) // hold note: freeze head and begin tracking the tail
                        {
                            var noteBehavior = notePool.GetActiveNoteById(note.ID);
                            noteBehavior.StartHold();
                            _heldNoteIds[note.Direction] = note;
                            _heldNoteStartSongTime[note.Direction] = songTime;
                            // Don't release from the pool yet; UpdateHeldNotes will do it
                        }
                        else
                        {
                            notePool.Release(notePool.GetActiveNoteById(note.ID));
                        }
                    }
                    else
                    {
                        notePool.Release(notePool.GetActiveNoteById(note.ID));
                    }
                    
                    if (activeNoteIds.Count == 0) return;

                    timeToNextNote = GetActiveNoteTime();
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
                ChartNote chartNote = kvp.Value;

                var noteBehavior = notePool.GetActiveNoteById(chartNote.ID);
                if (noteBehavior == null) // already cleaned up somehow
                {
                    toRelease.Add(dir);
                    continue;
                }

                double holdElapsedMs = songTime - _heldNoteStartSongTime[dir];
                double holdDurationMs = GetNoteLengthMs(chartNote);
                float progress = (float)Math.Min(holdElapsedMs / holdDurationMs, 1.0);

                noteBehavior.UpdateHoldVisual(progress);

                bool holdComplete = songTime >= GetNoteTime(chartNote) + holdDurationMs;
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

        private double GetNoteTime(ChartNote note)
        {
            return note.Position * 60000.0 / (_chart.BPM * 192);
        }

        private double GetNoteLengthMs(ChartNote note)
        {
            return note.Length * 60000.0 / (_chart.BPM * 192);
        }

        private double GetActiveNoteTime()
        {
            return activeNoteIds.Peek().Position * 60000.0 / (_chart.BPM * 192);
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
