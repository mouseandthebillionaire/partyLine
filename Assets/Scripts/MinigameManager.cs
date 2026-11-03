using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MinigameManager : MonoBehaviour
{
    private GameObject game;

    public GameObject               phoneInput;
    public int[]                    inputKeys = new int[6];
    public int[]                    inputTimes = new int[6];
    public GameObject[]             inputDisplayObjects;
    public TextMeshProUGUI[]        inputDisplayTexts = new TextMeshProUGUI[6];

    public GameObject   gameTitle;

    // Display Objects
    public GameObject   inputDisplay;
    public GameObject   winnerDisplay;

    public GameObject   splat;
    public Sprite[]     splatImages;
    public AudioSource  splatAudio;

    // keyNames index 10 is *
    const int starKey = 10;
    readonly List<GameObject> thrownSplats = new List<GameObject>();

    // Audio
    public AudioSource coin, zilch, announcer, gameMusic, crowdReaction, resultRiff;
    public AudioClip[] gameAnnouncements;
    public AudioClip[] gameTracks;
    public AudioClip[] crowdReactions;
    public AudioClip[] resultRiffs;

    // Timer
    public TextMeshProUGUI timerText;
    public float gameDuration = 10f;

    // Minigames
    public GameObject[] minigames;  
    public string[] minigameNames;
    public int currentMinigame = 0;

    // Phone Characters
    public Sprite[] phoneCharacters;
    public Image    phoneCharacterImage;

    // For testing
    public bool testMode = false;
    public int testMinigame = 0;

    public static MinigameManager S;

    void Awake() {
        S = this;
    }

    // Start is called before the first frame update
    void Start(){
		Reset();
        LoadMinigame();
		StartCoroutine(ShowTitle());
    }

    private void LoadMinigame() {

        game = minigames[testMode ? testMinigame : GameManager.S.currentMinigame];
        currentMinigame = testMode ? testMinigame : GameManager.S.currentMinigame;
        // Set the Background Color (Camera.main.backgroundColor)
        Camera.main.backgroundColor = GlobalVariables.S.gameColors[currentMinigame];
        MinigameMenu.ClearLaunchFill();
    }

	private IEnumerator ShowTitle() {
        // Show the title text
        gameTitle.GetComponent<TextMeshProUGUI>().text = minigameNames[currentMinigame];
        // Randomly rotate the title slightly
        gameTitle.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-20f, 20f));
		gameTitle.SetActive(false);
        // Play the title music (eventually)
		yield return new WaitForSeconds(1f);
		gameTitle.SetActive(true);
        // Announce the title (eventually)
        // Get the announcement for the current minigame
        AudioClip announcement = gameAnnouncements[currentMinigame];
        if (announcement != null) {
            announcer.clip = announcement;
            announcer.Play();
        }
		yield return new WaitForSeconds(2f);
		gameTitle.SetActive(false);
        // Play an end musical stab (eventually)
        LaunchGame();
	}

    private void LaunchGame() {
        game.SetActive(true);
        ShowInputDisplay();

        // Play Game's music
        if (gameTracks != null && (uint)currentMinigame < (uint)gameTracks.Length) {
            AudioClip track = gameTracks[currentMinigame];
            if (track != null) {
                gameMusic.clip = track;
                gameMusic.Play();
            }
        }
    }

    private void ShowInputDisplay() {
        inputDisplay.SetActive(true);
        if (GlobalVariables.S == null) return;

        for (int i = 0; i < inputDisplayObjects.Length; i++) {
            if (GlobalVariables.S.IsLoggedIn(i)) continue;
            inputDisplayObjects[i].GetComponent<Image>().color = Color.grey;
        }
    }

    public void TimerActive(){
        timerText.gameObject.SetActive(true);
    }


    public void EndGame(int? winner = null) {
        if (!winner.HasValue)
            winner = CalculateScore();
		game.SetActive(false);
        timerText.gameObject.SetActive(false);
        StartCoroutine(AnnounceWinner(winner.Value));
	}

    private IEnumerator AnnounceWinner(int _winner) {
        // Stop the game music
        gameMusic.Stop();

        // Hide the inputDisplay
        inputDisplay.SetActive(false);
        // Show the winnerDisplay
        winnerDisplay.SetActive(true);
        TextMeshProUGUI label = winnerDisplay.GetComponentInChildren<TextMeshProUGUI>(true);
        bool hasWinner = _winner != 98 && _winner != 99;
        if (label != null) {
            if (_winner == 99){
                label.text = "nobody wins";
                crowdReaction.clip = crowdReactions[0];
                resultRiff.clip = resultRiffs[0];
            } else if (_winner == 98){
                label.text = "it's a tie";
                crowdReaction.clip = crowdReactions[1];
                resultRiff.clip = resultRiffs[1];
            } else {
                label.text = GlobalVariables.S.phoneNames[_winner] + " wins!";
                crowdReaction.clip = crowdReactions[1];
                resultRiff.clip = resultRiffs[1];
            }
        }
        ShowWinnerCharacter(hasWinner ? _winner : -1);
        // Randomly rotate the winnerDisplay
        winnerDisplay.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-20f, 20f));

        
        // Play the win sound
        resultRiff.Play();
        // Play the yay sound
        crowdReaction.Play();

        // Losers can throw splats for the whole announcement, including the beat after the text hides.
        float hideAt = Time.time + 2f;
        float leaveAt = hideAt + 1f;
        while (Time.time < leaveAt) {
            if (winnerDisplay.activeSelf && Time.time >= hideAt)
                winnerDisplay.SetActive(false);
            PollSplats(_winner);
            yield return null;
        }

        ClearSplats();
        // Go back to the menu for now (but later maybe there's an version that runs through a few minigames?)
        MinigameMenu.ClearLaunchFill();
        GameManager.S.currentState = GameManager.State.MENU;
        SceneManager.LoadScene("Main");
    }

    void ShowWinnerCharacter(int phone)
    {
        if (phoneCharacterImage == null && winnerDisplay != null)
            phoneCharacterImage = winnerDisplay.GetComponentInChildren<Image>(true);
        if (phoneCharacterImage == null) return;

        bool show = phoneCharacters != null
            && (uint)phone < (uint)phoneCharacters.Length
            && phoneCharacters[phone] != null;
        phoneCharacterImage.enabled = show;
        if (show)
            phoneCharacterImage.sprite = phoneCharacters[phone];
    }

    void PollSplats(int winner)
    {
        if (splat == null || PhoneInputManager.S == null || GlobalVariables.S == null) return;

        int phones = GlobalVariables.S.numPhones;
        for (int i = 0; i < phones; i++) {
            if (i == winner) continue;
            if (PhoneInputManager.S.GetButtonDown(i, starKey))
                ThrowSplat(i);
        }
    }

    void ThrowSplat(int phone)
    {
        Transform parent = winnerDisplay.transform.parent;
        GameObject blob = Instantiate(splat, parent);
        thrownSplats.Add(blob);

        // Audio
        splatAudio.pitch = Random.Range(0.75f, 1.75f);
        splatAudio.Play();

        // Position the splat
        RectTransform text = winnerDisplay.transform as RectTransform;
        RectTransform rect = blob.transform as RectTransform;
        if (text != null && rect != null) {
            Vector2 scatter = text.rect.size * 0.3f;
            Vector2 local = new Vector2(Random.Range(-scatter.x, scatter.x), Random.Range(-scatter.y, scatter.y));
            rect.position = text.TransformPoint(local);
            rect.localRotation = Quaternion.Euler(0, 0, Random.Range(-25f, 25f));
            blob.transform.SetSiblingIndex(winnerDisplay.transform.GetSiblingIndex());
        }

        Image image = blob.GetComponent<Image>();
        if (image == null)
            image = blob.GetComponentInChildren<Image>();
        if (image == null) return;

        if (splatImages != null && splatImages.Length > 0)
            image.sprite = splatImages[Random.Range(0, splatImages.Length)];
        if ((uint)phone < (uint)GlobalVariables.S.phoneColors.Length)
            image.color = GlobalVariables.S.phoneColors[phone];
    }

    void ClearSplats()
    {
        for (int i = 0; i < thrownSplats.Count; i++) {
            if (thrownSplats[i] != null)
                Destroy(thrownSplats[i]);
        }
        thrownSplats.Clear();
    }

    public void UpdatePlayerScore(int _playerNum){
        inputTimes[_playerNum]++;
        inputDisplayTexts[_playerNum].text = inputTimes[_playerNum].ToString();
        coin.Play();

    }

    private int CalculateScore()
    {
        int highest = 0;
        int winner  = 99;
        bool tie    = false;

        for (int i = 0; i < inputTimes.Length; i++) {
            if (inputTimes[i] > highest) {
                highest = inputTimes[i];
                winner = i;
                tie = false;
            }
            else if (inputTimes[i] == highest && highest > 0) {
                tie = true;
            }
        }

        // 98 = tie, 99 = nobody scored
        if (tie) return 98;
        return winner;
    }


    private void Reset() {
        // Clear the Keys
        for (int i = 0; i < inputKeys.Length; i++) {
            inputKeys[i] = 99;
            inputTimes[i] = 0;
        }

        // Get the inputDisplayTexts
        for (int i = 0; i < inputDisplayTexts.Length; i++) {
            inputDisplayTexts[i] = inputDisplayObjects[i].GetComponentInChildren<TextMeshProUGUI>();
        }

         // Set the inputDisplay Box colors
        for (int i = 0; i < inputDisplayObjects.Length; i++) {
            inputDisplayObjects[i].GetComponent<Image>().color = GlobalVariables.S.phoneColors[i];
        }

        // Hide the inputDisplay
        inputDisplay.SetActive(false);

        // Hide the winnerDisplay
        winnerDisplay.SetActive(false);
        
    }
}