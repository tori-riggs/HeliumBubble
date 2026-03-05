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
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SpawnNote();
        }
    }
    
    private void SpawnNote()
    {
        var note = _pool.SpawnNote(_tempCount++, NoteDirection.Left);
        
        var position = leftTarget.transform.position;
        note.transform.position = position + _spawnPoint;

        note.Initialize(position, _noteSpeed);
    }
}
