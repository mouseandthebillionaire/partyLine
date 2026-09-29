using UnityEngine;

public class PhoneControl : MonoBehaviour
{
    //   Buttons 1-9  = digits 1-9
    //   Button  10   = digit 0
    //   Button  11   = *
    //   Button  12   = #
    //   Button  13   = yell
    //   Button  14   = pickup  
    //   Button  15   = hangup 
    
    public int phoneNum;

    private string inputString = "";

    void Update()
    {
        if (PhoneInputManager.S == null || GlobalVariables.S == null) return;

        
        // if we are in the menu
        if (GameManager.S.currentState == GameManager.State.MENU)
        {
            for (int i = 0; i < GlobalVariables.S.keyNames.Length-3; i++){ // -3 because we don't want to include pickup, hangup, or yell
                if (PhoneInputManager.S.GetButtonDown(phoneNum, i)){
                    inputString += GlobalVariables.S.keyNames[i];

                    // Keep only the last few digits once the buffer is long enough.
                    // Substring throws if the start index is negative.
                    if (inputString.Length > MinigameMenu.S.minigameNumberLength)
                        inputString = inputString.Substring(inputString.Length - MinigameMenu.S.minigameNumberLength, MinigameMenu.S.minigameNumberLength);
                    Debug.Log("Phone " + phoneNum + " pressed " + inputString);
                    if (inputString == MinigameMenu.S.minigameNumbers[0])
                    {
                        Debug.Log("Phone " + phoneNum + " pressed " + MinigameMenu.S.minigameNumbers[0]);
                        MinigameMenu.S.LaunchMinigame(0);
                    }
                }
            }
        }
    }
}
