using System.Collections;
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

    // Audio
    public AudioSource coin, zilch;

    // Timer
    public TextMeshProUGUI timerText;
    public float gameDuration = 10f;

    // Minigames
    public GameObject[] minigames;  
    public string[] minigameNames;
    public int currentMinigame = 0;

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
        game.SetActive(false);
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
		yield return new WaitForSeconds(2f);
		gameTitle.SetActive(false);
        // Play an end musical stab (eventually)
        LaunchGame();
	}

    private void LaunchGame() {
        game.SetActive(true);
        ShowInputDisplay();
    }

    private void ShowInputDisplay() {
        inputDisplay.SetActive(true);
    }


    public void EndGame(int? winner = null) {
        if (!winner.HasValue)
            winner = CalculateScore();
		game.SetActive(false);
        StartCoroutine(AnnounceWinner(winner.Value));
	}

    private IEnumerator AnnounceWinner(int _winner) {
        // Hide the inputDisplay
        inputDisplay.SetActive(false);
        // Show the winnerDisplay
        winnerDisplay.SetActive(true);
        if (_winner == 99)
            winnerDisplay.GetComponent<TextMeshProUGUI>().text = "nobody wins";
        else if (_winner == 98)
            winnerDisplay.GetComponent<TextMeshProUGUI>().text = "it's a tie";
        else
            winnerDisplay.GetComponent<TextMeshProUGUI>().text = GlobalVariables.S.phoneNames[_winner] + " wins!";
        // Randomly rotate the winnerDisplay
        winnerDisplay.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-20f, 20f));
        // Wait for 2 seconds
        yield return new WaitForSeconds(2f);
        // Hide the winnerDisplay
        winnerDisplay.SetActive(false);
        yield return new WaitForSeconds(1f);
        // Go back to the menu for now (but later maybe there's an version that runs through a few minigames?)
        GameManager.S.currentState = GameManager.State.MENU;
        SceneManager.LoadScene("Main");
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