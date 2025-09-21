using System.Collections;
using UnityEngine;

public class PlayerSpawnHelper : MonoBehaviour
{
    [SerializeField] private float minMoveDistance = 0.5f;
    [SerializeField] private float checkInterval = 0.1f;

    private Vector3 spawnPosition;
    private Collider2D[] colliders;

    private void Start()
    {
        colliders = GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders)
            col.enabled = false;

        spawnPosition = transform.position;
        StartCoroutine(EnableCollidersWhenMoved());
    }

    private IEnumerator EnableCollidersWhenMoved()
    {
        while (Vector3.Distance(transform.position, spawnPosition) < minMoveDistance)
            yield return new WaitForSeconds(checkInterval);

        foreach (var col in colliders)
            col.enabled = true;
    }
}