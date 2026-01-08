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
        ScoreY = maxCameraY * 100;

        if (player.position.y < transform.position.y - gameOverBelowCamera)
        {
            var player = FindAnyObjectByType<PlayerAutoJump2D>();
            if (player != null) player.StopPlayerMovement();
            GameOverNow();
        }
    }

    public void GameOverNow()
    {
        if (isGameOver) return;
        StartCoroutine(GameOverCoroutine());
    }

    private IEnumerator GameOverCoroutine()
    {
        isGameOver = true;

        // スコア保存（ResultSceneで読む用）
        PlayerPrefs.SetFloat("LAST_SCORE", ScoreY);
        PlayerPrefs.Save();

        // Overlayは無くても進む。見つからなくても例外にしない。
        Overlay overlay = null;
#if UNITY_2023_1_OR_NEWER
        overlay = FindFirstObjectByType<Overlay>();
#else
        overlay = FindObjectOfType<Overlay>();
#endif
        if (overlay != null)
        {
            // フェードが途中で失敗しても遷移するように保険
            IEnumerator fadeEnum = null;
            try { fadeEnum = overlay.FadeOut(); }
            catch (System.Exception e) { Debug.LogWarning($"FadeOut start failed: {e}"); }

            if (fadeEnum != null)
            {
                bool finished = false;
                while (!finished)
                {
                    object current = null;
                    try
                    {
                        finished = !fadeEnum.MoveNext();
                        if (!finished) current = fadeEnum.Current;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"FadeOut failed: {e}");
                        break;
                    }
                    yield return current;
                }
            }
        }

        // Build SettingsにResultSceneが入ってないとWebGLでは特に分かりにくいのでログ
        if (!Application.CanStreamedLevelBeLoaded(resultSceneName))
        {
            Debug.LogError($"ResultScene '{resultSceneName}' がBuild Settingsに含まれていません。File > Build Settings に追加してください。");
            yield break;
        }

        SceneManager.LoadScene(resultSceneName);
    }
}