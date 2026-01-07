using TMPro;
using UnityEngine;

public class ResultScoreScript : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private string format = "SCORE : {0:0}";

    private void Start()
    {
        float score = PlayerPrefs.GetFloat("LAST_SCORE", 0f);

        if (scoreText != null)
            scoreText.text = string.Format(format, score);
    }
}