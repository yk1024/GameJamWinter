using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class Title : MonoBehaviour
{
    [SerializeField]
    private string GameScene;

    private Overlay overlay;
    private static AudioSource audioSource;

    private void Start()
    {
        overlay = FindAnyObjectByType<Overlay>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            DontDestroyOnLoad(audioSource);
        }
        else
        {
            Destroy(audioSource);
        }
    }

    public void ToGameScene()
    {
        StartCoroutine(Load());
    }

    private IEnumerator Load()
    {
        yield return overlay.FadeOut();
        SceneManager.LoadScene(GameScene);
    }
}
