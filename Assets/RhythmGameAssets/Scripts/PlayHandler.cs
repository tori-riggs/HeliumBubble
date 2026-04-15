using MainMenu;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

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

        public TextMeshProUGUI ControlSchemeText;

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

        private KeyControl _leftKeyBind;
        private KeyControl _rightKeyBind;
        private KeyControl _upKeyBind;
        private KeyControl _downKeyBind;

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

            UpdateKeybinds();
            
             metronome.StartPlayback();
        }

        private void Update()
        {

            if (Keyboard.current.ctrlKey.isPressed && Keyboard.current.slashKey.wasPressedThisFrame)
            {
                CycleControlScheme();
            }

            // Detecting a restart of song
            if (_lastMetronomeTime - 3 > metronome.GetPlaybackTime())
            {
                _nextNoteIndex = 0;
            }
            
            var targetDifficulty = _scoreHandler.GetCurrentDifficulty();
            if (_chart.Difficulty != targetDifficulty.ToUpper())
                SwapChart(targetDifficulty);
            
            if (this._leftKeyBind.wasPressedThisFrame) leftTarget.PlayPop();
            if (this._upKeyBind.wasPressedThisFrame) upTarget.PlayPop();
            if (this._downKeyBind.wasPressedThisFrame) downTarget.PlayPop();
            if (this._rightKeyBind.wasPressedThisFrame) rightTarget.PlayPop();
            
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
                (this._leftKeyBind.wasPressedThisFrame ||
                 this._rightKeyBind.wasPressedThisFrame ||
                 this._upKeyBind.wasPressedThisFrame ||
                 this._downKeyBind.wasPressedThisFrame))
            {
                // Save all input states 
                _inputStates[NoteDirection.Left]  = this._leftKeyBind.wasPressedThisFrame;
                _inputStates[NoteDirection.Up]    = this._upKeyBind.wasPressedThisFrame;
                _inputStates[NoteDirection.Down]  = this._downKeyBind.wasPressedThisFrame;
                _inputStates[NoteDirection.Right] = this._rightKeyBind.wasPressedThisFrame;

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
            return direction switch
            {
                NoteDirection.Left => this._leftKeyBind.wasPressedThisFrame,
                NoteDirection.Up => this._upKeyBind.wasPressedThisFrame,
                NoteDirection.Down => this._downKeyBind.wasPressedThisFrame,
                NoteDirection.Right => this._rightKeyBind.wasPressedThisFrame,
                _ => false
            };
        }

        private bool KeyboardHeldFromDirection(NoteDirection direction)
        {
            return direction switch
            {
                NoteDirection.Left => this._leftKeyBind.isPressed,
                NoteDirection.Up => this._upKeyBind.isPressed,
                NoteDirection.Down => this._downKeyBind.isPressed,
                NoteDirection.Right => this._rightKeyBind.isPressed,
                _ => false
            };
        }

        private void UpdateKeybinds()
        {
            switch (SavedSettings.Instance.CurrentControlScheme)
            {
                case ControlScheme.WASD:
                    _leftKeyBind = Keyboard.current.aKey;
                    _rightKeyBind = Keyboard.current.dKey;
                    _upKeyBind = Keyboard.current.wKey;
                    _downKeyBind = Keyboard.current.sKey;
                    break;
                case ControlScheme.DFJK:
                    _leftKeyBind = Keyboard.current.dKey;
                    _rightKeyBind = Keyboard.current.kKey;
                    _upKeyBind = Keyboard.current.fKey;
                    _downKeyBind = Keyboard.current.jKey;
                    break;
                case ControlScheme.ZXCV:
                    _leftKeyBind = Keyboard.current.zKey;
                    _rightKeyBind = Keyboard.current.vKey;
                    _upKeyBind = Keyboard.current.xKey;
                    _downKeyBind = Keyboard.current.cKey;
                    break;
            }

            ControlSchemeText.text = SavedSettings.Instance.CurrentControlScheme.ToString();
        }

        private void CycleControlScheme()
        {
            int enumIndex = (int)SavedSettings.Instance.CurrentControlScheme;

            enumIndex++;
            if (enumIndex > (int)ControlScheme.ZXCV)
            {
                enumIndex = 0;
            }

            ControlScheme newScheme = (ControlScheme)enumIndex;
            SavedSettings.Instance.CurrentControlScheme = newScheme;

            UpdateKeybinds();
        }
    }
}
