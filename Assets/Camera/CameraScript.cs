using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CameraFollowScore2D : MonoBehaviour
{
    [SerializeField] private Transform player;

    [Header("Follow")]
    [SerializeField] private float yOffset = 2f;
    [SerializeField] private bool neverMoveDown = true;

    [Header("GameOver")]
    [SerializeField] private float gameOverBelowCamera = 6f;

    [Header("Scene")]
    [SerializeField] private string resultSceneName = "ResultScene";

    public float ScoreY { get; private set; }

    private float maxCameraY;
    private bool isGameOver;

    private void Awake()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    private void Start()
    {
        if (player == null)
        {
            Debug.LogError($"{nameof(CameraFollowScore2D)}: player が未設定です。Playerタグを付けるか、Inspectorで設定してください。");
            enabled = false;
            return;
        }

        maxCameraY = transform.position.y;
        ScoreY = maxCameraY;
    }

    private void LateUpdate()
    {
        if (isGameOver) return;

        float targetY = player.position.y + yOffset;

        float newY;
        if (neverMoveDown)
        {
            maxCameraY = Mathf.Max(maxCameraY, targetY);
            newY = maxCameraY;
        }
        else
        {
            newY = targetY;
            maxCameraY = newY;
        }

        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        ScoreY = maxCameraY;

        if (player.position.y < transform.position.y - gameOverBelowCamera)
        {
            StartCoroutine(GameOver());
        }
    }

    // 追加：外部から即GameOverさせる
    public void GameOverNow()
    {
        if (isGameOver) return;
        StartCoroutine(GameOver());
    }

    private IEnumerator GameOver()
    {
        if (isGameOver) yield break;
        isGameOver = true;
        PlayerPrefs.SetFloat("LAST_SCORE", ScoreY);
        PlayerPrefs.Save();
        var overlay = FindAnyObjectByType<Overlay>();
        if (overlay != null)
            yield return overlay.FadeOut();

        SceneManager.LoadScene(resultSceneName);
    }
}