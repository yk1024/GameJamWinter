using System.Collections;
using UnityEngine;

public class GameState : MonoBehaviour
{
    public bool Playing { get; private set; } = true;

    private Result result;
    private Overlay overlay;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        result = FindAnyObjectByType<Result>(FindObjectsInactive.Include);
        overlay = FindAnyObjectByType<Overlay>();
        StartGameOver();
    }

    public void StartGameOver()
    {
        StartCoroutine(GameOver());
    }


    public IEnumerator GameOver()
    {
        Playing = false;
        result.gameObject.SetActive(true);
        yield return overlay.FadeIn();
    }
}
