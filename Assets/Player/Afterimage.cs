using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Afterimage2D : MonoBehaviour
{
    [SerializeField] private float lifeTime = 0.25f;
    [SerializeField] private bool fadeOut = true;

    private SpriteRenderer sr;
    private float t;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    // Player側から見た目をコピーして呼ぶ
    public void InitFrom(SpriteRenderer source)
    {
        if (source == null) return;

        sr.sprite = source.sprite;
        sr.flipX = source.flipX;
        sr.flipY = source.flipY;
        sr.sortingLayerID = source.sortingLayerID;
        sr.sortingOrder = source.sortingOrder - 1; // 本体より後ろにしたい場合
        sr.color = source.color;
        transform.localScale = source.transform.lossyScale; // 親子構造でも同じ見た目に寄せる
    }

    private void Update()
    {
        t += Time.deltaTime;

        if (fadeOut)
        {
            float a = Mathf.Clamp01(1f - (t / lifeTime));
            var c = sr.color;
            sr.color = new Color(c.r, c.g, c.b, a);
        }

        if (t >= lifeTime)
            Destroy(gameObject);
    }
}