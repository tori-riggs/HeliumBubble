using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class NoteSpawner : MonoBehaviour
{
    [Header("Pool")]
    [SerializeField] private GameObject poolPrefab;

    [Header("Target")]
    [SerializeField] private GameObject leftTarget;
    [SerializeField] private GameObject downTarget;
    [SerializeField] private GameObject upTarget;
    [SerializeField] private GameObject rightTarget;

    [SerializeField] private float spawnDistance;
    private Vector3 _spawnPoint;

    [Header("Timing")]
    [SerializeField] private float timeOnScreen = 1f;   // Seconds for Note on Screen
    private float _noteSpeed;

    private NotePool _pool;

    private int _tempCount;

    private void Start()
    {
        _tempCount = 0;
        
        _spawnPoint = Vector3.down * spawnDistance;
        _noteSpeed = spawnDistance / timeOnScreen;

        _pool = Instantiate(poolPrefab).GetComponent<NotePool>();
    }

    private void Update()
    {
        // manual test spawn (Space)
        if (Keyboard.current != null && Keyboard.current.aKey.wasPressedThisFrame)
        {
            SpawnNote(NoteDirection.Left);
        }
        if (Keyboard.current != null && Keyboard.current.wKey.wasPressedThisFrame)
        {
            SpawnNote(NoteDirection.Up);
        }
        if (Keyboard.current != null && Keyboard.current.sKey.wasPressedThisFrame)
        {
            SpawnNote(NoteDirection.Down);
        }
        if (Keyboard.current != null && Keyboard.current.dKey.wasPressedThisFrame)
        {
            SpawnNote(NoteDirection.Right);
        }
    }
    
    private void SpawnNote(NoteDirection direction)
    {
        var note = _pool.SpawnNote(_tempCount++, direction);
        
        var position = GetTarget(direction).transform.position;
        note.transform.position = position + _spawnPoint;

        note.Initialize(position, _noteSpeed);
    }
    
    // Returns Prefab by direction
    private GameObject GetTarget(NoteDirection dir)
    {
        return dir switch
        {
            NoteDirection.Left => leftTarget,
            NoteDirection.Up => upTarget,
            NoteDirection.Down => downTarget,
            NoteDirection.Right => rightTarget,
            _ => null
        };
    }
}
