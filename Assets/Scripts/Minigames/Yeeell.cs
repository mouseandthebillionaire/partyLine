using System.Collections;
using UnityEngine;

public class Yeeell : MonoBehaviour
{
	private int phones;

	void Start() {
		phones = GlobalVariables.S.numPhones;
		StartCoroutine(RunGame());
	}

	private IEnumerator RunGame() {
		// keyNames index 14 is YELL (Teensy joystick button 13)
		const int yellKey = 14;

		float[] screamed = new float[phones];
		bool[] yelling = new bool[phones];
		bool[] stopped = new bool[phones];
		bool anyoneScreamed = false;
		int winner = 99;

		for (int i = 0; i < phones; i++)
			ShowTime(i, 0f);

		// Stay open until the last scream ends. That player wins.
		while (true) {
			int stillScreaming = 0;
			int stoppedThisFrame = 0;
			int lastStopped = -1;

			if (PhoneInputManager.S != null) {
				for (int i = 0; i < phones; i++) {
					if (stopped[i])
						continue;

					bool yellingNow = PhoneInputManager.S.GetButton(i, yellKey);
					if (yellingNow && !yelling[i]) {
						anyoneScreamed = true;
						Debug.Log("phone " + i + " scream start");
					}
					// A dip or a stop releases the yell button. That scream is over.
					if (yelling[i] && !yellingNow) {
						stopped[i] = true;
						stoppedThisFrame++;
						lastStopped = i;
						Debug.Log("phone " + i + " scream stopped");
					}
					yelling[i] = yellingNow;
					if (stopped[i] || !yellingNow)
						continue;

					stillScreaming++;
					screamed[i] += Time.deltaTime;
					ShowTime(i, screamed[i]);
				}
			}

			if (anyoneScreamed && stillScreaming == 0) {
				winner = stoppedThisFrame > 1 ? 98 : lastStopped;
				break;
			}

			yield return null;
		}

		for (int i = 0; i < phones; i++)
			Debug.Log("phone " + i + " screamed " + screamed[i].ToString("0.0") + "s");

		MinigameManager.S.EndGame(winner);
	}

	void ShowTime(int phone, float seconds) {
		MinigameManager.S.inputTimes[phone] = Mathf.RoundToInt(seconds * 10f);
		if (MinigameManager.S.inputDisplayTexts[phone] != null)
			MinigameManager.S.inputDisplayTexts[phone].text = seconds.ToString("0.0");
	}
}
