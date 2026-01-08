using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class Overlay : MonoBehaviour
{
    [Header("Auto Fade")]
    [SerializeField] private bool fadeInOnStart = true;
    [SerializeField] private bool dontDestroyOnLoad = false;

    [Header("Fade Settings")]
    [SerializeField] private float fadeSpeed = 1f; // 1秒でalphaを1動かす（Time.deltaTime * fadeSpeed）

    private Image image;
    private Coroutine running;

    private void Awake()
    {
        image = GetComponent<Image>();

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (fadeInOnStart)
            Run(FadeIn());
    }

    private void OnDisable()
    {
        StopRunning();
    }

    private void OnDestroy()
    {
        StopRunning();
    }

    private void StopRunning()
    {
        if (running != null)
        {
            StopCoroutine(running);
            running = null;
        }
    }

    private void Run(IEnumerator routine)
    {
        StopRunning();
        running = StartCoroutine(routine);
    }

    public IEnumerator FadeIn()
    {
        if (image == null) yield break;

        image.enabled = true;

        Color color = image.color;
        color.a = 1f;
        image.color = color;

        while (true)
        {
            if (image == null) yield break; // 破棄/シーン遷移保険

            color = image.color;
            color.a -= Time.deltaTime * fadeSpeed;
            if (color.a <= 0f)
            {
                color.a = 0f;
                image.color = color;
                break;
            }

            image.color = color;
            yield return null;
        }

        // 完全に透明になったら非表示（クリックも通したいならRaycast TargetもOFF推奨）
        if (image != null)
            image.enabled = false;

        running = null;
    }

    public IEnumerator FadeOut()
    {
        if (image == null) yield break;

        image.enabled = true;

        Color color = image.color;
        color.a = 0f;
        image.color = color;

        while (true)
        {
            if (image == null) yield break; // 破棄/シーン遷移保険

            color = image.color;
            color.a += Time.deltaTime * fadeSpeed;
            if (color.a >= 1f)
            {
                color.a = 1f;
                image.color = color;
                break;
            }

            image.color = color;
            yield return null;
        }

        running = null;
    }
}