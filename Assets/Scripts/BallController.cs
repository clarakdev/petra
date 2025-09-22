using UnityEngine;

public class BallController : MonoBehaviour
{
    public enum Owner { Free, PlayerHand, Pet, Flying }

    [Header("Sorting")]
    public int defaultOrder = 0;
    public int carriedOrder = 10;

    [Header("References")]
    public Rigidbody2D rb;
    public Collider2D col;
    public SpriteRenderer sr;

    [Header("Debug")]
    public Owner currentOwner = Owner.PlayerHand;

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!col) col = GetComponent<Collider2D>();
        if (!sr) sr = GetComponent<SpriteRenderer>();
        if (sr) defaultOrder = sr.sortingOrder;
    }

    public void AttachToHand(Transform hand)
    {
        currentOwner = Owner.PlayerHand;
        rb.isKinematic = true;
        rb.linearVelocity = Vector2.zero;
        if (col) col.enabled = false;

        transform.SetParent(hand);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        if (sr) sr.sortingOrder = defaultOrder;
    }

    public void AttachToMouth(Transform mouth)
    {
        currentOwner = Owner.Pet;
        rb.isKinematic = true;
        rb.linearVelocity = Vector2.zero;
        if (col) col.enabled = false;

        transform.SetParent(mouth);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        if (sr) sr.sortingOrder = carriedOrder;
    }

    public void Release(Vector3 worldPos)
    {
        currentOwner = Owner.Free;
        transform.SetParent(null);
        transform.position = worldPos;

        if (rb) rb.isKinematic = false;
        if (col) col.enabled = true;
        if (sr) sr.sortingOrder = defaultOrder;
    }

    public void SetFlying()
    {
        currentOwner = Owner.Flying;
        rb.isKinematic = true;
        if (col) col.enabled = true;
        if (sr) sr.sortingOrder = defaultOrder;
        transform.SetParent(null);
    }
}