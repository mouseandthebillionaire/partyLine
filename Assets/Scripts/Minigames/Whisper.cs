using UnityEngine;
using UnityEngine.UI;

public class Whisper : MonoBehaviour
{
    public GameObject[] whisperMeters;
    Image[] fills;

    public float targetVol = 0.2f;
    public float whisperThreshold = 0.1f;
    public float tooLoud = 0.4f;
    // Fill hits 1 at this volume. Louder than this stays full.
    public float meterFull = 0.5f;
    public int gameLength = 20;

    float[] held;
    bool[] shutOut;
    bool colored;
    float endsAt;
    bool ended;

    void Start()
    {
        int phones = GlobalVariables.S != null ? GlobalVariables.S.numPhones : 0;
        held = new float[phones];
        shutOut = new bool[phones];
        fills = new Image[phones];

        if (whisperMeters != null) {
            int n = Mathf.Min(phones, whisperMeters.Length);
            for (int i = 0; i < n; i++) {
                GameObject meter = whisperMeters[i];
                if (meter == null) continue;

                bool playing = IsPlaying(i);
                meter.SetActive(playing);
                if (!playing) continue;

                Transform panel = meter.transform.Find("Panel");
                if (panel == null) continue;
                fills[i] = panel.GetComponent<Image>();
            }
        }

        for (int i = 0; i < phones; i++) {
            if (IsPlaying(i))
                ShowTime(i, 0f);
            else
                ClearTime(i);
        }

        endsAt = Time.time + gameLength;
        MinigameManager.S.TimerActive();
        ShowTimer(gameLength);
    }

    void Update()
    {
        if (ended || held == null) return;

        float left = endsAt - Time.time;
        ShowTimer(left);
        if (left <= 0f) {
            ended = true;
            if (MinigameManager.S != null)
                MinigameManager.S.EndGame();
            return;
        }

        if (PhoneInputManager.S == null) return;

        // ShowInputDisplay greys logged-out phones after this object starts.
        if (!colored) {
            colored = ColorDisplays();
            if (!colored) return;
        }

        for (int i = 0; i < held.Length; i++) {
            if (!IsPlaying(i)) continue;

            float vol = PhoneInputManager.S.GetSlider(i);
            if (fills[i] != null)
                fills[i].fillAmount = meterFull > 0f ? Mathf.Clamp01(vol / meterFull) : 0f;

            if (shutOut[i]) continue;
            if (vol > tooLoud) {
                ShutOut(i);
                continue;
            }

            if (Mathf.Abs(vol - targetVol) > whisperThreshold) continue;

            held[i] += Time.deltaTime;
            ShowTime(i, held[i]);
        }
    }

    bool ColorDisplays()
    {
        if (MinigameManager.S == null || GlobalVariables.S == null) return false;
        if (MinigameManager.S.inputDisplay != null && !MinigameManager.S.inputDisplay.activeSelf)
            return false;

        var colors = GlobalVariables.S.phoneColors;
        if (colors == null) return false;

        int n = Mathf.Min(held.Length, colors.Length);
        for (int i = 0; i < n; i++)
            SetBoxColor(i, IsPlaying(i) ? colors[i] : Color.grey);
        return true;
    }

    bool IsPlaying(int phone)
    {
        return GlobalVariables.S != null && GlobalVariables.S.IsLoggedIn(phone);
    }

    void ShutOut(int phone)
    {
        shutOut[phone] = true;
        SetBoxColor(phone, Color.grey);
        MinigameManager.S.zilch.Play();
    }

    void SetBoxColor(int phone, Color color)
    {
        if (MinigameManager.S == null) return;
        var boxes = MinigameManager.S.inputDisplayObjects;
        if (boxes == null || (uint)phone >= (uint)boxes.Length) return;

        GameObject box = boxes[phone];
        if (box == null) return;
        Image image = box.GetComponent<Image>();
        if (image != null)
            image.color = color;
    }

    void ClearTime(int phone)
    {
        if (MinigameManager.S == null) return;
        if ((uint)phone >= (uint)MinigameManager.S.inputTimes.Length) return;

        MinigameManager.S.inputTimes[phone] = 0;
        if (MinigameManager.S.inputDisplayTexts[phone] != null)
            MinigameManager.S.inputDisplayTexts[phone].text = "";
    }

    void ShowTimer(float secondsLeft)
    {
        if (MinigameManager.S == null || MinigameManager.S.timerText == null) return;
        int seconds = Mathf.CeilToInt(Mathf.Max(0f, secondsLeft));
        MinigameManager.S.timerText.text = seconds.ToString();
    }

    void ShowTime(int phone, float seconds)
    {
        if (MinigameManager.S == null) return;
        if ((uint)phone >= (uint)MinigameManager.S.inputTimes.Length) return;

        MinigameManager.S.inputTimes[phone] = Mathf.RoundToInt(seconds * 10f);
        if (MinigameManager.S.inputDisplayTexts[phone] != null)
            MinigameManager.S.inputDisplayTexts[phone].text = seconds.ToString("0.0");
    }
}
