using System.Collections.Generic;
using UnityEngine;

public enum NoteDirection
{
    Left,
    Up,
    Down,
    Right
}

public class NotePool : MonoBehaviour
{
    [Header("Note Prefabs")]
    [SerializeField] private NoteBehavior leftPrefab;
    [SerializeField] private NoteBehavior upPrefab;
    [SerializeField] private NoteBehavior downPrefab;
    [SerializeField] private NoteBehavior rightPrefab;

    [Header("Pool Settings")]
    [SerializeField] private int initialBufferSizePerDirection = 5;
    [SerializeField] private Transform poolContainer;

    // One buffer per direction
    private readonly Dictionary<NoteDirection, Stack<NoteBehavior>> _buffers = new();

    // Active notes by NoteId (your "pointer lookup")
    private readonly Dictionary<int, NoteBehavior> _activeById = new();

    private void Awake()
    {
        _buffers[NoteDirection.Left]  = new Stack<NoteBehavior>(initialBufferSizePerDirection);
        _buffers[NoteDirection.Up]    = new Stack<NoteBehavior>(initialBufferSizePerDirection);
        _buffers[NoteDirection.Down]  = new Stack<NoteBehavior>(initialBufferSizePerDirection);
        _buffers[NoteDirection.Right] = new Stack<NoteBehavior>(initialBufferSizePerDirection);

        // Prewarm each direction
        Prewarm(NoteDirection.Left, initialBufferSizePerDirection);
        Prewarm(NoteDirection.Up, initialBufferSizePerDirection);
        Prewarm(NoteDirection.Down, initialBufferSizePerDirection);
        Prewarm(NoteDirection.Right, initialBufferSizePerDirection);
    }

    private void Prewarm(NoteDirection dir, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var note = CreateNewNote(dir);
            ReturnToBuffer(note, dir);
        }
    }

    // Creates a new note object; Starts deactivated 
    private NoteBehavior CreateNewNote(NoteDirection dir)
    {
        var prefab = GetPrefab(dir);

        var note = Instantiate(prefab, poolContainer);
        note.gameObject.SetActive(false);
        return note;
    }

    // Returns Prefab by direction
    private NoteBehavior GetPrefab(NoteDirection dir)
    {
        return dir switch
        {
            NoteDirection.Left => leftPrefab,
            NoteDirection.Up => upPrefab,
            NoteDirection.Down => downPrefab,
            NoteDirection.Right => rightPrefab,
            _ => null
        };
    }

    /// <summary>
    /// Spawns a note with a given NoteId + direction.
    /// </summary>
    public NoteBehavior SpawnNote(int noteId, NoteDirection dir)
    {
        if (_activeById.TryGetValue(noteId, out var existing))
        {
            Debug.LogWarning($"SpawnNote called with NoteId {noteId}, but that ID is already active. Returning existing note.");
            return existing;
        }

        var note = GetFromBufferOrGrow(dir);

        note.gameObject.SetActive(true);
        note.Pool_Initialize(this, noteId, dir);

        _activeById[noteId] = note;
        return note;
    }

    /// <summary>
    /// Returns a reference to the ACTIVE note with that ID, else null.
    /// </summary>
    public NoteBehavior GetActiveNoteById(int noteId)
    {
        _activeById.TryGetValue(noteId, out var note);
        return note;
    }

    /// <summary>
    /// Despawn and release the note from the scene
    /// </summary>
    public void Release(NoteBehavior note)
    {
        var id = note.NoteId;
        var dir = note.Direction;

        if (id != -1 && _activeById.TryGetValue(id, out var tracked) && tracked == note)
            _activeById.Remove(id);

        note.ResetForPool();
        ReturnToBuffer(note, dir);
    }

    // Returns note to the buffer, adds note pack to pool
    private void ReturnToBuffer(NoteBehavior note, NoteDirection dir)
    {
        note.gameObject.SetActive(false);

        if (poolContainer != null)
            note.transform.SetParent(poolContainer, worldPositionStays: false);

        _buffers[dir].Push(note);
    }

    // Gets a note from the pool or grows to get a note
    private NoteBehavior GetFromBufferOrGrow(NoteDirection dir)
    {
        var buffer = _buffers[dir];

        return buffer.Count > 0 ? buffer.Pop() : CreateNewNote(dir);
    }
}