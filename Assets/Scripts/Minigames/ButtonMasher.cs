using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonMasher : MonoBehaviour {
	public Text number;
	private int[] inputTimes = new int[3];
	
	// Start is called before the first frame update
    void Start() {
		StartCoroutine(RunGame());
	}

	private IEnumerator RunGame() {
		// How long are we playing this game for?
		int holdSeconds = 5;
		// keyNames index 0-9 is the digit printed on that button
		int correctKey = Random.Range(0, 10);

		float duration = Time.time + holdSeconds;

		number.text = correctKey.ToString();

		while (duration > Time.time) {
			int phones = GlobalVariables.S != null ? GlobalVariables.S.numPhones : 0;
			for (int i = 0; i < phones; i++) {
				if (PhoneInputManager.S != null && PhoneInputManager.S.GetButtonDown(i, correctKey))
					MinigameManager.S.UpdatePlayerScore(i);
			}

			yield return null;
		}

		MinigameManager.S.EndGame();
	}
}
