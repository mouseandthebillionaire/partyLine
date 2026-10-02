using System.Collections.Generic;
using UnityEngine;

public class GlobalVariables : MonoBehaviour {

    public int             winner;
    public int             numPhones = 6;
    
    public Color[]         phoneColors = new Color[] {
        Hex("#E90000"), // red
        Hex("#F8F61D"), // yellow
        Hex("#145AFA"), // blue
        Hex("#ED7423"), // orange
        Hex("#64804F"), // green
        Hex("#000000")  // black
    };

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }

    
    public string[]        shortPhoneNames = new string[3]{"NNF", "YU", "RRR"};

    public string[]        phoneNames = new string[6]{
        "Abigail", "Brenda", "Carla", "Daniella", "Evangeline", "Felicty"
    };

	// Keep track of which minigames have been played 
	public List<int>        minigameIndexes;


    // Input Keys
    // 0-9 tied to numbers
    // Star (10), Pound(11), Pickup(12), Hangup(13), Yell(14)

    public string[]        keyNames = new string[]
        {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "*", "#", "⬆", "⬇", "YELL"};
        
    public KeyCode[,]      inputKeys = new KeyCode[,] {
        {KeyCode.X, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Q,    // 0-4
         KeyCode.W, KeyCode.E, KeyCode.A, KeyCode.S, KeyCode.D,                   // 5-9
         KeyCode.Z, KeyCode.C, KeyCode.Alpha0, KeyCode.P, KeyCode.Semicolon},     // Star, Pound, Up, Down, Yell
        
        {KeyCode.B, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.R, 
         KeyCode.T, KeyCode.Y, KeyCode.F, KeyCode.G, KeyCode.H, 
         KeyCode.V, KeyCode.N, KeyCode.Less, KeyCode.LeftBracket, KeyCode.Slash},
        
        
        {KeyCode.Comma, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.U, 
         KeyCode.I, KeyCode.O, KeyCode.J, KeyCode.K, KeyCode.L, 
         KeyCode.M, KeyCode.Period, KeyCode.Equals, KeyCode.RightBracket, KeyCode.BackQuote}
    };

    public int             gameState; // 0 = menu, 1 = loading, 2 = playing, 3 = minigame, 4 = gameOver
    
    public static GlobalVariables S;
    
    
    // Start is called before the first frame update
    void Awake()
    {
       if (S == null) {
           S = this;
       }
    }

}