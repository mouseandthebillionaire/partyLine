using UnityEngine;

public class PhoneControl : MonoBehaviour
{
    //    10, // 0
    //    1, 2, 3, 4, 5, 6, 7, 8, 9,
    //    11, // *
    //    12, // #
    //    15, // pickup
    //    14, // hangup
    //    13, // yell
    
    public int phoneNum;

    private string inputString = "";

    void Update()
    {
        if (PhoneInputManager.S == null || GlobalVariables.S == null) return;

        
        // if we are in the menu
        if (GameManager.S.currentState == GameManager.State.MENU)
        {
            if (MinigameMenu.S == null) return;

            // Key index 0 is the 0 button. That logs this phone in.
            if (!MinigameMenu.S.IsLoggedIn(phoneNum)) {
                if (PhoneInputManager.S.GetButtonDown(phoneNum, 0)) {
                    inputString = "";
                    MinigameMenu.S.LogIn(phoneNum);
                    MinigameMenu.S.SetPhoneDigits(phoneNum, inputString);
                }
                return;
            }

            bool pressed = false;
            for (int i = 0; i < GlobalVariables.S.keyNames.Length-3; i++){ // -3 because we don't want to include pickup, hangup, or yell
                if (PhoneInputManager.S.GetButtonDown(phoneNum, i)){
                    pressed = true;
                    inputString += GlobalVariables.S.keyNames[i];

                    // Keep only the last few digits once the buffer is long enough.
                    // Substring throws if the start index is negative.
                    if (inputString.Length > MinigameMenu.S.minigameNumberLength)
                        inputString = inputString.Substring(inputString.Length - MinigameMenu.S.minigameNumberLength, MinigameMenu.S.minigameNumberLength);
                    Debug.Log("Phone " + phoneNum + " pressed " + inputString);
                }
            }

            if (!pressed) return;

            MinigameMenu.S.SetPhoneDigits(phoneNum, inputString);

            for (int i = 0; i < MinigameMenu.S.minigameNumbers.Length; i++){
                if (inputString == MinigameMenu.S.minigameNumbers[i]){
                    inputString = "";
                    Debug.Log("Phone " + phoneNum + " pressed " + MinigameMenu.S.minigameNumbers[i]);
                    MinigameMenu.S.LaunchMinigame(i);
                    return;
                }
            }
        }
    }
}
