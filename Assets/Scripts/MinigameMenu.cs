using UnityEngine;
using UnityEngine.SceneManagement;

public class MinigameMenu : MonoBehaviour
{
    public string[] minigameNames;
    public string[] minigameNumbers;
    public int minigameNumberLength = 3;
    
    public static MinigameMenu S;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        S = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LaunchMinigame(int minigameIndex)
    {
        Debug.Log("Launching " + minigameNames[minigameIndex]);
        GameManager.S.currentMinigame = minigameIndex;
        GameManager.S.currentState = GameManager.State.MINIGAME;
        SceneManager.LoadScene("Minigame");
    }
}
