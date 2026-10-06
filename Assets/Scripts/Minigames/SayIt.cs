using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SayIt : MonoBehaviour
{
    public bool[] yelling;
    private int phones;

	void Start() {
        phones = GlobalVariables.S.numPhones;   
        yelling = new bool[phones];
        
        StartCoroutine(RunGame());
	}

    private IEnumerator RunGame() {
		// How long are we playing this game for?
		int holdSeconds = 5;
		// keyNames index 14 is YELL (Teensy joystick button 13)
		const int yellKey = 14;


		if (PhoneInputManager.S != null) {
			for (int i = 0; i < phones; i++)
				yelling[i] = PhoneInputManager.S.GetButton(i, yellKey);
		}

		float duration = Time.time + holdSeconds;

		while (duration > Time.time) {
			if (PhoneInputManager.S != null) {
				for (int i = 0; i < phones; i++) {
					bool yellingNow = PhoneInputManager.S.GetButton(i, yellKey);
					if (yellingNow && !yelling[i])
						MinigameManager.S.UpdatePlayerScore(i);
					yelling[i] = yellingNow;
                    Debug.Log(yelling[i]);
				}
			}

			yield return null;
		}

		MinigameManager.S.EndGame();
	}
}