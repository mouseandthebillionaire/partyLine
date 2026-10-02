using UnityEngine;

public class GameManager : MonoBehaviour
{    
    public int currentMinigame;
    
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
}