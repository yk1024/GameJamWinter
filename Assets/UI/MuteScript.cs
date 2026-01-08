using UnityEngine;
using UnityEngine.UI;

public class AudioMuteToggleButton : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("未設定なら同じGameObjectのButtonを探します")]
    [SerializeField] private Button button;

    [Tooltip("このボタンに表示するImage（未設定なら同じGameObjectのImageを探します）")]
    [SerializeField] private Image targetImage;

    [Header("Sprites")]
    [Tooltip("ミュートOFF（音あり）のときに表示する画像")]
    [SerializeField] private Sprite unmutedSprite;

    [Tooltip("ミュートON（音なし）のときに表示する画像")]
    [SerializeField] private Sprite mutedSprite;

    [Header("Prefs")]
    [Tooltip("ミュート状態の保存キー")]
    [SerializeField] private string prefsKey = "AUDIO_MUTED";

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (targetImage == null)
            targetImage = GetComponent<Image>();

        if (button != null)
            button.onClick.AddListener(ToggleMute);

        // 保存された状態を復元
        bool muted = PlayerPrefs.GetInt(prefsKey, 0) == 1;
        ApplyMute(muted);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(ToggleMute);
    }

    public void ToggleMute()
    {
        // AudioListener.pause をミュート扱いにする
        bool muted = !AudioListener.pause;
        ApplyMute(muted);

        PlayerPrefs.SetInt(prefsKey, muted ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyMute(bool muted)
    {
        AudioListener.pause = muted;

        // 画像切り替え（Sprite未設定なら何もしない）
        if (targetImage != null)
        {
            if (muted && mutedSprite != null)
                targetImage.sprite = mutedSprite;
            else if (!muted && unmutedSprite != null)
                targetImage.sprite = unmutedSprite;
        }
    }
}