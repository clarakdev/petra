using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D)), RequireComponent(typeof(SpriteRenderer))]
public class PetFollower : MonoBehaviour
public class PetFollower : MonoBehaviourPun, IPunObservable
{
    [SerializeField] public float moveSpeed = 5f;
    public float followDistance = 1.5f;
    Rigidbody2D rb; Transform target; Vector2 moveDir; SpriteRenderer sr;

    [Header("Directional Sprites")]
    [SerializeField] private Sprite frontSprite, backSprite, leftSprite, rightSprite;

    void Awake(){ rb = GetComponent<Rigidbody2D>(); sr = GetComponent<SpriteRenderer>(); }
    void Start(){ TryBindTarget(); }
    void Update()
    {
        if (!target) TryBindTarget();
        if (!target) return;

        Vector3 off = target.position - transform.position;
        float d = off.magnitude;
        if (d > followDistance){ moveDir = ((Vector2)off).normalized; UpdateFacing(moveDir); }
        else moveDir = Vector2.zero;
    }

    void FixedUpdate(){ if (target) rb.linearVelocity = moveDir * moveSpeed; }

    void UpdateFacing(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) sr.sprite = (dir.x < 0) ? leftSprite : rightSprite;
        else sr.sprite = (dir.y > 0) ? backSprite : frontSprite;
    // Update is called once per frame

    void Update()
    {
        if (!PhotonNetwork.IsConnected || photonView.IsMine)
        {
            if (target)
            {
                Vector3 offset = target.position - transform.position;
                float distance = offset.magnitude;

                if (distance > followDistance)
                {
                    moveDirection = offset.normalized;
                    UpdateSpriteDirection(moveDirection);
                }
                else
                {
                    moveDirection = Vector2.zero;
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (!PhotonNetwork.IsConnected || photonView.IsMine)
        {
            if (target)
            {
                rb.linearVelocity = moveDirection * moveSpeed;
            }
        }
    }

    public void SetTarget(Transform t){ target = t; }

    public void TryBindTarget()
    {
        if (target) return;
        var tagged = GameObject.FindWithTag("Player"); if (tagged){ target = tagged.transform; return; }
        var pc = Object.FindObjectOfType<PlayerController>(); if (pc){ target = pc.transform; return; }
        var byName = GameObject.Find("Player"); if (byName) target = byName.transform;
    }

    void OnDisable(){ if (rb) rb.linearVelocity = Vector2.zero; }
    void OnDrawGizmosSelected(){ Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, followDistance); }
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(rb.position);
            stream.SendNext(rb.linearVelocity);
            stream.SendNext(spriteRenderer.sprite.name); // Send sprite direction by name
        }
        else
        {
            rb.position = (Vector2)stream.ReceiveNext();
            rb.linearVelocity = (Vector2)stream.ReceiveNext();
            string spriteName = (string)stream.ReceiveNext();

            // Set sprite based on name
            if (spriteName == leftSprite.name)
            {
                spriteRenderer.sprite = leftSprite;
            }
            else if (spriteName == rightSprite.name)
            {
                spriteRenderer.sprite = rightSprite;
            }
            else if (spriteName == backSprite.name)
            {
                spriteRenderer.sprite = backSprite;
            }
            else
            {
                spriteRenderer.sprite = frontSprite;
            }
        }
    }
}
