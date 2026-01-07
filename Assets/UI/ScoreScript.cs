using UnityEngine;
using UnityEngine.UI;

public class ScoreScript : MonoBehaviour
{
    [SerializeField] private CameraFollowScore2D cameraScore;
    [SerializeField] private Text scoreText; // Legacy UI Text

    [Header("Display")]
    [SerializeField] private float scoreOffset = 0f;
    [SerializeField] private string prefix = "Score: ";

    private void Awake()
    {
        if (scoreText == null)
            scoreText = GetComponent<Text>();

        if (cameraScore == null)
        {
            // Main Camera ‚É CameraFollowScore2D ‚ª•t‚¢‚Ä‚¢‚ê‚ÎŽ©“®Žæ“¾
            var mainCam = Camera.main;
            if (mainCam != null)
                cameraScore = mainCam.GetComponent<CameraFollowScore2D>();
        }
    }

    private void Update()
    {
        if (cameraScore == null || scoreText == null) return;

        float score = cameraScore.ScoreY + scoreOffset;
        if (score < 0) score = 0;

        scoreText.text = prefix + Mathf.FloorToInt(score).ToString();
    }
}