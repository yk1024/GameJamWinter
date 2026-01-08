using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MovingPlatform2D : MonoBehaviour
{
    [Header("Move")]
    [Tooltip("左右に動く距離（中心からの片側距離）")]
    [SerializeField] private float moveRange = 2f;

    [Tooltip("移動速度（units/sec）")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Timing")]
    [Tooltip("移動方向を反転する間隔（秒）。一定間隔で左右に折り返します")]
    [SerializeField] private float flipInterval = 1.5f;

    [Tooltip("開始時に右へ動くならON（OFFなら左から）")]
    [SerializeField] private bool startMoveRight = true;

    [Header("Optional (Physics)")]
    [Tooltip("Rigidbody2Dが付いている場合、MovePositionで動かす（安定）。無い場合はTransformで動かす")]
    [SerializeField] private bool useRigidbodyIfExists = true;

    private Vector3 startPos;
    private float timer;
    private int dir; // +1: right, -1: left
    private Rigidbody2D rb;

    private void Awake()
    {
        startPos = transform.position;
        dir = startMoveRight ? 1 : -1;

        rb = GetComponent<Rigidbody2D>();
        if (rb != null && useRigidbodyIfExists)
        {
            // 動く床は Kinematic 推奨（Dynamicだと押し合いが起きやすい）
            if (rb.bodyType == RigidbodyType2D.Dynamic)
                rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    private void FixedUpdate()
    {
        // 一定間隔で反転
        timer += Time.fixedDeltaTime;
        if (timer >= flipInterval)
        {
            timer = 0f;
            dir *= -1;
        }

        // 次の位置へ
        Vector3 pos = transform.position;
        pos.x += dir * moveSpeed * Time.fixedDeltaTime;

        // 範囲外に出ないようにクランプ（中心±moveRange）
        float minX = startPos.x - moveRange;
        float maxX = startPos.x + moveRange;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);

        if (rb != null && useRigidbodyIfExists)
        {
            rb.MovePosition(pos);
        }
        else
        {
            transform.position = pos;
        }
    }

    // シーン上で動く範囲を可視化
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Vector3 center = Application.isPlaying ? startPos : transform.position;
        Vector3 left = new Vector3(center.x - moveRange, center.y, center.z);
        Vector3 right = new Vector3(center.x + moveRange, center.y, center.z);

        Gizmos.DrawLine(left, right);
        Gizmos.DrawSphere(left, 0.05f);
        Gizmos.DrawSphere(right, 0.05f);
    }
}