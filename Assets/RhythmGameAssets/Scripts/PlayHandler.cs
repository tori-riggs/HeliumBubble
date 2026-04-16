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

        [SerializeField] private AudioSource noteHitSound;

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
        private double _lastMetronomeTime = 0;
        
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
            
            // metronome.StartPlayback();
        }

        private void Update()
        {
            // Detecting a restart of song
            if (_lastMetronomeTime - 3 > metronome.GetPlaybackTime())
            {
                SwapChart(_scoreHandler.GetCurrentDifficulty());
            }
            
            var targetDifficulty = _scoreHandler.GetCurrentDifficulty();
            if (_chart.Difficulty != targetDifficulty.ToUpper())
                SwapChart(targetDifficulty);
            
            if (KeyboardPressFromDirection(NoteDirection.Left)) leftTarget.PlayPop();
            if (KeyboardPressFromDirection(NoteDirection.Up)) upTarget.PlayPop();
            if (KeyboardPressFromDirection(NoteDirection.Down)) downTarget.PlayPop();
            if (KeyboardPressFromDirection(NoteDirection.Right)) rightTarget.PlayPop();
            
            if (_nextNoteIndex >= _chart.Notes.Count - 1) return;
            
            var songTime = metronome.GetPlaybackTime() * 1000.0;

            while (songTime + (notePool.timeOnScreen * 1000.0) >= GetNoteTime(_chart.Notes[_nextNoteIndex]))
            {
                var chartNote = _chart.Notes[_nextNoteIndex];
                activeNoteIds.Enqueue(chartNote);
                float holdDuration = (float)(GetNoteLengthMs(chartNote) / 1000.0);
                notePool.SpawnNote(chartNote, holdDuration);
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
                notePool.Release(notePool.GetActiveNote(note));
                // TODO: disappear after a while
                _scoreHandler.NoteMissed();
        
                if (activeNoteIds.Count == 0) return;
        
                nextNoteTime = GetActiveNoteTime();
            }
        
            if (Keyboard.current != null &&
                (KeyboardPressFromDirection(NoteDirection.Left)) ||
                 (KeyboardPressFromDirection(NoteDirection.Up)) ||
                  (KeyboardPressFromDirection(NoteDirection.Down)) ||
                   (KeyboardPressFromDirection(NoteDirection.Right)))
            {
                // Save all input states 
                _inputStates[NoteDirection.Left]  = KeyboardPressFromDirection(NoteDirection.Left);
                _inputStates[NoteDirection.Up]    = KeyboardPressFromDirection(NoteDirection.Up);
                _inputStates[NoteDirection.Down]  = KeyboardPressFromDirection(NoteDirection.Down);
                _inputStates[NoteDirection.Right] = KeyboardPressFromDirection(NoteDirection.Right);

                // Loop until either all inputs are handled or no more hittable notes
                double timeToNextNote = GetActiveNoteTime();
                while (songTime + 150.0 >= timeToNextNote && _inputStates.Values.Any(state => state))
                {
                    var note = activeNoteIds.Dequeue();

                    if (KeyboardPressFromDirection(note.Direction))
                    {
                        noteHitSound.Stop();
                        noteHitSound.time = 0.095f;
                        noteHitSound.PlayScheduled(AudioSettings.dspTime);

                        _inputStates[note.Direction] = false;

                        _scoreHandler.NoteHitScoring(songTime - timeToNextNote);

                        if (note.Length > 0) // hold note: freeze head and begin tracking the tail
                        {
                            var noteBehavior = notePool.GetActiveNote(note);
                            noteBehavior.StartHold();
                            _heldNoteIds[note.Direction] = note;
                            _heldNoteStartSongTime[note.Direction] = songTime;
                            // Don't release from the pool yet; UpdateHeldNotes will do it
                        }
                        else
                        {
                            notePool.Release(notePool.GetActiveNote(note));
                        }
                    }
                    else
                    {
                        notePool.Release(notePool.GetActiveNote(note));
                        _scoreHandler.NoteMissed();
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

                var noteBehavior = notePool.GetActiveNote(chartNote);
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

        private void SwapChart(string newDifficulty)
        {
            var instrument = SavedSettings.Instance.Instrument;
            _chart = SavedSettings.Instance.SelectedSong.GetChart(newDifficulty, instrument);

            // Seek to the first note that hasn't passed the spawn window yet
            var songTime = metronome.GetPlaybackTime() * 1000.0;
            _nextNoteIndex = 0;
            while (_nextNoteIndex < _chart.Notes.Count - 1 &&
                   GetNoteTime(_chart.Notes[_nextNoteIndex]) < songTime + notePool.timeOnScreen * 1000.0)
                _nextNoteIndex++;
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
            var k = Keyboard.current;
            
            // LEFT: A, Left Arrow, 1, Z
            var left = k.aKey.wasPressedThisFrame || 
                       k.leftArrowKey.wasPressedThisFrame || 
                       k.digit1Key.wasPressedThisFrame || 
                       k.zKey.wasPressedThisFrame;

            // UP: W, Up Arrow, 2, X
            var up = k.wKey.wasPressedThisFrame || 
                     k.upArrowKey.wasPressedThisFrame || 
                     k.digit2Key.wasPressedThisFrame || 
                     k.xKey.wasPressedThisFrame;

            // DOWN: S, Down Arrow, 3, N
            var down = k.sKey.wasPressedThisFrame || 
                       k.downArrowKey.wasPressedThisFrame || 
                       k.digit3Key.wasPressedThisFrame || 
                       k.nKey.wasPressedThisFrame;

            // RIGHT: D, Right Arrow, 4, M
            var right = k.dKey.wasPressedThisFrame || 
                        k.rightArrowKey.wasPressedThisFrame || 
                        k.digit4Key.wasPressedThisFrame || 
                        k.mKey.wasPressedThisFrame;
            
            return direction switch
            {
                NoteDirection.Left => left,
                NoteDirection.Up => up,
                NoteDirection.Down => down,
                NoteDirection.Right => right,
                _ => false
            };
        }

        private bool KeyboardHeldFromDirection(NoteDirection direction)
        {
            var k = Keyboard.current;
            
            // LEFT: A, Left Arrow, 1, Z
            var left = k.aKey.isPressed || 
                        k.leftArrowKey.isPressed || 
                        k.digit1Key.isPressed || 
                        k.zKey.isPressed;

            // UP: W, Up Arrow, 2, X
            var up = k.wKey.isPressed || 
                      k.upArrowKey.isPressed || 
                      k.digit2Key.isPressed || 
                      k.xKey.isPressed;

            // DOWN: S, Down Arrow, 3, N
            var down = k.sKey.isPressed || 
                        k.downArrowKey.isPressed || 
                        k.digit3Key.isPressed || 
                        k.nKey.isPressed;

            // RIGHT: D, Right Arrow, 4, M
            var right = k.dKey.isPressed || 
                         k.rightArrowKey.isPressed || 
                         k.digit4Key.isPressed || 
                         k.mKey.isPressed;
            
            return direction switch
            {
                NoteDirection.Left => left,
                NoteDirection.Up => up,
                NoteDirection.Down => down,
                NoteDirection.Right => right,
                _ => false
            };
        }
    }
}
