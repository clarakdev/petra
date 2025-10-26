using System.Collections;
using UnityEngine;

public class PetSpawnHelper : MonoBehaviour
{
    [SerializeField] private float minMoveDistance = 0.3f;
    [SerializeField] private float checkInterval = 0.1f;

    private Vector3 spawnPosition;
    private Collider2D[] colliders;
    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        colliders = GetComponentsInChildren<Collider2D>();

        // Disable all colliders at spawn
        foreach (var col in colliders)
            col.enabled = false;

        spawnPosition = transform.position;

        // Start coroutine to re-enable colliders once pet has settled/moved
        StartCoroutine(EnableCollidersWhenMoved());
    }

    private IEnumerator EnableCollidersWhenMoved()
    {
        // Wait until pet moves away from spawn position slightly (or starts following)
        while (Vector3.Distance(transform.position, spawnPosition) < minMoveDistance)
            yield return new WaitForSeconds(checkInterval);

        // Enable colliders safely
        foreach (var col in colliders)
            col.enabled = true;
    }
}
