using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MinigameManager : MonoBehaviour
{
    public GameObject game;

    public GameObject   phoneInput;
    public int[]        inputKeys = new int[6];
    public int[]        inputTimes = new int[6];
    public GameObject[] inputDisplayObjects;
    public Text[]       inputDisplayTexts;
    
    // Display Objects
    public GameObject   inputDisplay;
    public GameObject   winnerDisplay;

    // Audio
    public AudioSource coin;
    
    public static MinigameManager S;

    void Awake() {
        S = this;
    }

    // Start is called before the first frame update
    void Start(){
		Reset();
        
        game.SetActive(false);
		StartCoroutine(ShowTitle());
    }

	private IEnumerator ShowTitle() {
		GameObject title = GameObject.Find("Title");
        // Randomly rotate the title slightly
        title.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-20f, 20f));
		title.SetActive(false);
        // Play the title music (eventually)
		yield return new WaitForSeconds(1f);
		title.SetActive(true);
        // Announce the title (eventually)
		yield return new WaitForSeconds(2f);
		title.SetActive(false);
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

    // Update is called once per frame
    void Update()
    {
        
    }

    public void EndGame() {
        int winner = CalculateScore();
		game.SetActive(false);
        StartCoroutine(AnnounceWinner(winner));
	}

    private IEnumerator AnnounceWinner(int _winner) {
        winnerDisplay.GetComponent<Text>().text = GlobalVariables.S.phoneNames[_winner] + " wins!";
        winnerDisplay.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-20f, 20f));
        winnerDisplay.SetActive(true);
        yield return new WaitForSeconds(2f);
        winnerDisplay.SetActive(false);
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
        // Hide the inputDisplay
        inputDisplay.SetActive(false);

        // Hide the winnerDisplay
        winnerDisplay.SetActive(false);

        // Clear the Keys
        for (int i = 0; i < inputKeys.Length; i++) {
            inputKeys[i] = 99;
            inputTimes[i] = 0;
        }

        // Get the inputDisplayTexts
        for (int i = 0; i < inputDisplayTexts.Length; i++) {
            inputDisplayTexts[i] = inputDisplayObjects[i].GetComponentInChildren<Text>();
        }

         // Set the inputDisplay Box colors
        for (int i = 0; i < inputDisplayObjects.Length; i++) {
            inputDisplayObjects[i].GetComponent<Image>().color = GlobalVariables.S.phoneColors[i];
        }
    }
}
