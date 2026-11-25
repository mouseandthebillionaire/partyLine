using UnityEngine;

public class GameManager : MonoBehaviour
{    
    public int currentMinigame;

    // Backdrop used when a dialed game has no entry in gameColors.
    public Color launchColor = Color.black;

    // Signs every phone in on the menu, then plays the minigame for phones with no handset.
    public bool documenting;

    // On the menu, type a minigame number and press return to launch it.
    public bool testing;
    
    public static GameManager S;
    
    public enum State
    {
        MENU,
        MINIGAME
    }

    public State currentState;

    void Awake()
    {
        S = this;
        DontDestroyOnLoad(this.gameObject);
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentState = State.MENU;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Menu setup clears logins first, then calls this.
    public void LogInAllPhones()
    {
        if (!documenting || MinigameMenu.S == null || MinigameMenu.S.phones == null) return;
        for (int i = 0; i < MinigameMenu.S.phones.Length; i++)
            MinigameMenu.S.LogIn(i);
    }
}