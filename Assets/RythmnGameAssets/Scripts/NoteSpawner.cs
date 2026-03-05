using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class NewEmptyCSharpScript : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject leftPrefab;
    [SerializeField] private GameObject downPrefab;
    [SerializeField] private GameObject upPrefab;
    [SerializeField] private GameObject rightPrefab;

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

    private void Start()
    {
        _spawnPoint = Vector3.down * spawnDistance;
        _noteSpeed = spawnDistance / timeOnScreen;
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
        var position = leftTarget.transform.position;
        var noteObj = Instantiate(leftPrefab, position+_spawnPoint, Quaternion.identity);

        var note = noteObj.GetComponent<NoteBehavior>();
        note.Initialize(position, _noteSpeed);
    }
}
