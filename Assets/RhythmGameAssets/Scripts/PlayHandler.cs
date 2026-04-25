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

        private int _nextNoteIndex = 0;
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

        private Dictionary<NoteBehavior, double> _notesToDespawn = new();

        private Chart _chart; // current chart
        
        private double DelaySeconds => delay / 1000.0;

        // This is a scuffed way of fixing logic,
        // so ideally find a better way instead of
        // using this lol
        private const int MAX_CHECKS = 100;

        private bool _noMoreNotes = false;
        private bool _isAFK = false;
        private const long AFK_TIME = 20000;

        private int _maximumPressWindow;

        private long _lastKeyPress;

        private void Start()
        {
            LoadChart();
            _lastKeyPress = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _maximumPressWindow = _scoreHandler.GetMaximumMargin();

            //foreach (ChartNote note in _chart.Notes)
            //{
            //    Debug.Log(note);
            //}

            // metronome.StartPlayback();
        }

        private void Update()
        {
            // Detecting a restart of song
            if (_noMoreNotes && metronome.GetPlaybackTime() <= 0.1)
            {
                ResetAll();
            }
            
            var targetDifficulty = _scoreHandler.GetCurrentDifficulty();
            if (_chart.Difficulty != targetDifficulty.ToUpper())
                SwapChart(targetDifficulty);

            //long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (KeyboardPressFromDirection(NoteDirection.Left))
            {
                leftTarget.PlayPop();
                //_lastKeyPress = now;
            }
            if (KeyboardPressFromDirection(NoteDirection.Up))
            {
                upTarget.PlayPop();
                //_lastKeyPress = now;
            }
            if (KeyboardPressFromDirection(NoteDirection.Down))
            {
                downTarget.PlayPop();
                //_lastKeyPress = now;
            }
            if (KeyboardPressFromDirection(NoteDirection.Right))
            {
                rightTarget.PlayPop();
                //_lastKeyPress = now;
            }
            // if (Input.GetKeyDown(KeyCode.Escape))

            //bool wasAFK = _isAFK;
            //_isAFK = now - _lastKeyPress > AFK_TIME;

            //bool needsReset = wasAFK != _isAFK;

            bool needsReset = Keyboard.current.backspaceKey.wasPressedThisFrame;
            if (needsReset)
            {
                _scoreHandler.SetCurrentDifficulty("EASY");
                _scoreHandler.ResetTexts();

                SwapChart("EASY");
            }

            bool spawnNext = _nextNoteIndex < _chart.Notes.Count;
            
            var songTime = metronome.GetPlaybackTime() * 1000.0;

            while (spawnNext && songTime + (notePool.timeOnScreen * 1000.0) >= GetNoteTime(_chart.Notes[_nextNoteIndex]))
            {
                var chartNote = _chart.Notes[_nextNoteIndex];
                activeNoteIds.Enqueue(chartNote);
                float holdDuration = (float)(GetNoteLengthMs(chartNote) / 1000.0);
                notePool.SpawnNote(chartNote, holdDuration);
                _nextNoteIndex++;

                spawnNext = _nextNoteIndex < _chart.Notes.Count;
            }

            if (!spawnNext) _noMoreNotes = true;

            // Update any notes currently being held
            UpdateHeldNotes(songTime);

            // If input was hit and there is a note within 100 milliseconds
            // Check if that key was pressed. If not, wrong key was pressed so missed note
            // If key was pressed, log that hit with the delay

            if (activeNoteIds.Count == 0) return;

            double nextNoteTime = GetActiveNoteTime();
            // Once we are outside the maximum window, grey the notes
            // and mark for despawn when needed
            while (songTime - _maximumPressWindow >= nextNoteTime)
            {
                ChartNote note = activeNoteIds.Dequeue();
                NoteBehavior behavior = notePool.GetActiveNote(note);

                double releaseTime = GetNoteTime(note) + GetNoteLengthMs(note) + 200;
                _notesToDespawn.Add(behavior, releaseTime);
                behavior.GreyOut();

                if (!_isAFK) _scoreHandler.NoteMissed();
        
                if (activeNoteIds.Count == 0) break;
        
                nextNoteTime = GetActiveNoteTime();
            }

            // Since hold times can be diff, queue is not guaranteed
            // to be in order, so we'll just look at all of them :/
            List<NoteBehavior> behaviorsToRemove = new();
            foreach ((NoteBehavior behavior, double releaseTime) in _notesToDespawn)
            {
                if (songTime - 100.0 < releaseTime) continue;

                behavior.Despawn();
                behaviorsToRemove.Add(behavior);
            }

            // Remove from the tracker
            foreach (NoteBehavior behavior in behaviorsToRemove)
            {
                _notesToDespawn.Remove(behavior);
            }

            if (activeNoteIds.Count == 0) return;

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
                int checks = 0;

                while (songTime + 150.0 >= timeToNextNote && _inputStates.Values.Any(state => state) && checks < MAX_CHECKS)
                {
                    ChartNote note = activeNoteIds.Dequeue();

                    if (KeyboardPressFromDirection(note.Direction))
                    {
                        _inputStates[note.Direction] = false;

                        double hitTiming = songTime - timeToNextNote;
                        _scoreHandler.NoteHitScoring(hitTiming);

                        if (Math.Abs(hitTiming) <= _maximumPressWindow)
                        {
                            noteHitSound.PlayScheduled(AudioSettings.dspTime);
                        }

                        if (note.Length > 0) // hold note: freeze head and begin tracking the tail
                        {
                            var noteBehavior = notePool.GetActiveNote(note);
                            if (Math.Abs(hitTiming) <= _maximumPressWindow)
                            {
                                noteBehavior.StartHold();
                                _heldNoteIds[note.Direction] = note;
                                _heldNoteStartSongTime[note.Direction] = songTime;
                                // Don't release from the pool yet; UpdateHeldNotes will do it
                            }
                            else
                            {
                                // Pressed too early
                                noteBehavior.GreyOut();
                                double releaseTime = GetNoteTime(note) + GetNoteLengthMs(note) + 200;
                                _notesToDespawn.Add(noteBehavior, releaseTime);
                            }
                        }
                        else
                        {
                            notePool.Release(notePool.GetActiveNote(note));
                        }
                    }
                    else
                    {
                        // Add the note back into the queue if we got a different keypress.
                        // This fixes only one hold note activating if the left-most one
                        // is pressed AFTER one to the right
                        activeNoteIds.Enqueue(note);
                    }

                    if (activeNoteIds.Count == 0) return;

                    timeToNextNote = GetActiveNoteTime();

                    checks++;
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
                    toRelease.Add(dir);
                    _scoreHandler.HoldNoteScoring(progress);
                }

                // grace period for when to count a "good" release
                // TODO: Add scoring based on release time?
                if (holdComplete || (keyReleased && holdDurationMs - holdElapsedMs <= 110))
                {
                    noteBehavior.Despawn();
                }

                if (keyReleased && holdDurationMs - holdElapsedMs > 110)
                {
                    noteBehavior.KeyReleasedEarly();
                }
            }

            foreach (var dir in toRelease)
            {
                _heldNoteIds.Remove(dir);
                _heldNoteStartSongTime.Remove(dir);
            }
        }

        private void LoadChart()
        {
            var difficulty = SavedSettings.Instance.Difficulty;
            var instrument = SavedSettings.Instance.Instrument;

            _chart = SavedSettings.Instance.SelectedSong.GetChart(difficulty, instrument);
            _scoreHandler.SetChart(_chart);
        }

        private void ResetAll()
        {
            _noMoreNotes = false;
            _isAFK = false;
            _lastKeyPress = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            metronome.ForceResync();

            LoadChart();
            _scoreHandler.ResetCurrentDifficulty();
            _scoreHandler.ResetTexts();

            _nextNoteIndex = 0;
            activeNoteIds = new Queue<ChartNote>();

            _heldNoteIds = new Dictionary<NoteDirection, ChartNote>();
            _heldNoteStartSongTime = new Dictionary<NoteDirection, double>();

            _inputStates = new Dictionary<NoteDirection, bool>
            {
                { NoteDirection.Left, false },
                { NoteDirection.Up, false },
                { NoteDirection.Down, false },
                { NoteDirection.Right, false }
            };
        }

        private void SwapChart(string newDifficulty)
        {
            var instrument = SavedSettings.Instance.Instrument;
            _chart = SavedSettings.Instance.SelectedSong.GetChart(newDifficulty, instrument);
            _scoreHandler.SetChart(_chart);

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
