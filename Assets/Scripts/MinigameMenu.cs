using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MinigameMenu : MonoBehaviour
{
    public string[] minigameNames;
    public string[] minigameNumbers;
    public GameObject[] minigameThumbnails;
    public TextMeshProUGUI[] minigameNumberTexts;

    public GameObject[] phones;
    public int minigameNumberLength = 3;

    public AudioSource loginSound, loadSound;
    public float launchFillDuration = 0.6f;

    bool[] loggedIn;
    private float loginPitch;
    bool launching;

    static GameObject launchFill;
    
    public static MinigameMenu S;
    
    void Awake()
    {
        ClearLaunchFill();
        S = this;
    }

    void Start(){
        loginPitch = 1f;
        int phoneCount = phones != null ? phones.Length : 0;
        loggedIn = new bool[phoneCount];
        if (GlobalVariables.S != null && GlobalVariables.S.loggedIn != null) {
            for (int i = 0; i < GlobalVariables.S.loggedIn.Length; i++)
                GlobalVariables.S.loggedIn[i] = false;
        }
        for (int i = 0; i < phoneCount; i++)
        {
            SetPhoneColor(i, Color.grey);
            SetPhoneDigits(i, "");
        }

        for(int i = 0; i < minigameThumbnails.Length; i++){
            minigameThumbnails[i].GetComponent<Image>().color = GlobalVariables.S.gameColors[i];
            minigameThumbnails[i].GetComponentInChildren<TextMeshProUGUI>().text = minigameNumbers[i];
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LogIn(int phone)
    {
        if (loggedIn == null || phone < 0 || phone >= loggedIn.Length) return;
        if (GlobalVariables.S == null || phone >= GlobalVariables.S.phoneColors.Length) return;
        loggedIn[phone] = true;
        if (GlobalVariables.S.loggedIn != null && phone < GlobalVariables.S.loggedIn.Length)
            GlobalVariables.S.loggedIn[phone] = true;
        SetPhoneColor(phone, GlobalVariables.S.phoneColors[phone]);
        loginSound.pitch = loginPitch;
        loginSound.Play();
        loginPitch += 0.1f;
    }

    public bool IsLoggedIn(int phone)
    {
        return loggedIn != null && phone >= 0 && phone < loggedIn.Length && loggedIn[phone];
    }

    void SetPhoneColor(int phone, Color color)
    {
        Image image = PhoneImage(phone);
        if (image != null)
            image.color = color;
    }

    public void SetPhoneDigits(int phone, string digits)
    {
        TextMeshProUGUI tmp = PhoneText<TextMeshProUGUI>(phone);
        if (tmp != null)
            tmp.text = digits;

        Text legacy = PhoneText<Text>(phone);
        if (legacy != null)
            legacy.text = digits;
    }

    Image PhoneImage(int phone)
    {
        Transform phoneImage = FindPhoneChild(phone, "phoneImage");
        if (phoneImage == null)
            phoneImage = FindPhoneChild(phone, "Image");
        if (phoneImage != null)
            return phoneImage.GetComponent<Image>();
        if (phones == null || phone < 0 || phone >= phones.Length || phones[phone] == null)
            return null;
        return phones[phone].GetComponent<Image>();
    }

    T PhoneText<T>(int phone) where T : Component
    {
        if (phones == null || phone < 0 || phone >= phones.Length || phones[phone] == null)
            return null;
        return phones[phone].GetComponentInChildren<T>(true);
    }

    Transform FindPhoneChild(int phone, string childName)
    {
        if (phones == null || phone < 0 || phone >= phones.Length || phones[phone] == null)
            return null;
        return phones[phone].transform.Find(childName);
    }

    public void LaunchMinigame(int minigameIndex)
    {
        if (launching) return;
        launching = true;
        GameManager.S.currentMinigame = minigameIndex;
        GameManager.S.currentState = GameManager.State.MINIGAME;
        StartCoroutine(FillThenLaunch(minigameIndex));
    }

    // Drops the full-screen color left behind by a launch, including one that survived the scene change.
    public static void ClearLaunchFill()
    {
        if (launchFill == null) return;
        Destroy(launchFill);
        launchFill = null;
    }

    IEnumerator FillThenLaunch(int minigameIndex)
    {
        GameObject thumbnail = null;
        if (minigameThumbnails != null && minigameIndex >= 0 && minigameIndex < minigameThumbnails.Length)
            thumbnail = minigameThumbnails[minigameIndex];

        if (thumbnail != null)
            yield return GrowThumbnail(thumbnail);

        SceneManager.LoadScene("Minigame");
    }

    IEnumerator GrowThumbnail(GameObject thumbnail)
    {
        // Play the loading sound
        float pitchChnage = Random.Range(0.9f, 1.1f);
        loadSound.pitch = pitchChnage;
        loadSound.Play();

        // Grow the thumbnail
        Image sourceImage = thumbnail.GetComponent<Image>();
        RectTransform sourceRect = thumbnail.GetComponent<RectTransform>();
        Canvas rootCanvas = thumbnail.GetComponentInParent<Canvas>();
        if (sourceImage == null || sourceRect == null || rootCanvas == null) yield break;

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        ClearLaunchFill();

        GameObject fill = new GameObject("MinigameLaunchFill", typeof(RectTransform));
        fill.transform.SetParent(canvasRect, false);
        fill.transform.SetAsLastSibling();
        launchFill = fill;

        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = sourceImage.color;
        fillImage.raycastTarget = false;

        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = fillRect.anchorMax = new Vector2(0.5f, 0.5f);
        fillRect.pivot = new Vector2(0.5f, 0.5f);
        fillRect.localScale = Vector3.one;

        Vector3[] corners = new Vector3[4];
        sourceRect.GetWorldCorners(corners);
        Vector3 center = (corners[0] + corners[2]) * 0.5f;
        float worldWidth = Vector3.Distance(corners[0], corners[3]);
        float worldHeight = Vector3.Distance(corners[0], corners[1]);

        Vector3 canvasScale = canvasRect.lossyScale;
        float scaleX = Mathf.Abs(canvasScale.x) > 0.0001f ? canvasScale.x : 1f;
        float scaleY = Mathf.Abs(canvasScale.y) > 0.0001f ? canvasScale.y : 1f;

        fillRect.position = center;
        Vector2 startSize = new Vector2(worldWidth / scaleX, worldHeight / scaleY);
        fillRect.sizeDelta = startSize;

        canvasRect.GetWorldCorners(corners);
        float halfWidth = 0f;
        float halfHeight = 0f;
        for (int i = 0; i < 4; i++)
        {
            halfWidth = Mathf.Max(halfWidth, Mathf.Abs(corners[i].x - center.x));
            halfHeight = Mathf.Max(halfHeight, Mathf.Abs(corners[i].y - center.y));
        }

        Vector2 endSize = new Vector2(halfWidth * 2f / scaleX, halfHeight * 2f / scaleY);
        endSize += new Vector2(8f, 8f);

        float duration = Mathf.Max(0.05f, launchFillDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - (1f - t) * (1f - t);
            fillRect.sizeDelta = Vector2.LerpUnclamped(startSize, endSize, eased);
            yield return null;
        }

        fillRect.sizeDelta = endSize;
        StickFillOverSceneLoad(fillRect);
    }

    // Keeps the color on screen across the load, then ClearLaunchFill removes it.
    static void StickFillOverSceneLoad(RectTransform fillRect)
    {
        if (fillRect == null) return;

        GameObject overlay = new GameObject("MinigameLaunchFill");
        Canvas overlayCanvas = overlay.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 1000;

        fillRect.SetParent(overlay.transform, false);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillRect.localScale = Vector3.one;

        DontDestroyOnLoad(overlay);
        launchFill = overlay;
    }
}
