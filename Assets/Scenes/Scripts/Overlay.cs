using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Overlay : MonoBehaviour
{
    private Image image;
    private const int TIME = 3;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        image = GetComponent<Image>();
        StartCoroutine(FadeIn());
    }

    // Update is called once per frame
    void Update()
    {

    }

    public IEnumerator FadeIn()
    {
        Color color = image.color;
        color.a = 1;
        image.color = color;

        while (true)
        {
            color.a -= Time.deltaTime;
            color.a = Mathf.Max(color.a, 0);
            image.color = color;

            if (color.a == 0)
            {
                break;
            }

            yield return null;
        }

        image.enabled = false;
    }

    public IEnumerator FadeOut()
    {
        image.enabled = true;

        Color color = image.color;
        color.a = 0;
        image.color = color;

        while (true)
        {
            color.a += Time.deltaTime;
            color.a = Mathf.Min(color.a, 1);
            image.color = color;

            if (color.a == 1)
            {
                break;
            }

            yield return null;
        }
    }
}
