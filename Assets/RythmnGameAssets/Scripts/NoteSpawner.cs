using System;
using UnityEngine;

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
    
    [SerializeField] private Vector3 spawnPoint = new Vector3(0f, 100f, 0f);

    [Header("Timing")]
    [SerializeField] private float noteSpeed = 5f;            // Units per second (upward)

    private void Update()
    {
        throw new NotImplementedException();
    }
}
