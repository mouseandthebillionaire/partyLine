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

    public int minigameNumberLength = 3;
    
    public static MinigameMenu S;
    
    void Awake()
    {
        S = this;
    }

    void Start(){
        for(int i = 0; i < minigameThumbnails.Length; i++){
            minigameThumbnails[i].GetComponent<Image>().color = GlobalVariables.S.gameColors[i];
            minigameThumbnails[i].GetComponentInChildren<TextMeshProUGUI>().text = minigameNumbers[i];
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LaunchMinigame(int minigameIndex)
    {
        GameManager.S.currentMinigame = minigameIndex;
        GameManager.S.currentState = GameManager.State.MINIGAME;
        SceneManager.LoadScene("Minigame");
    }
}
