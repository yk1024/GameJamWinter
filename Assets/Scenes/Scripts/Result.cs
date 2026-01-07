using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class Result : MonoBehaviour
{
    [SerializeField]
    private string titleScene;

    private Overlay overlay;

    private void Start()
    {
        overlay = FindAnyObjectByType<Overlay>();
    }

    public void ToTitleScene()
    {
        StartCoroutine(Load());
    }

    private IEnumerator Load()
    {
        yield return overlay.FadeOut();
        SceneManager.LoadScene(titleScene);
    }
}
