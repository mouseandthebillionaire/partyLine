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

					bool yellingNow = PhoneInputManager.S.IsYelling(i);
					if (yellingNow && !yelling[i]) {
						anyoneScreamed = true;
						Debug.Log("phone " + i + " scream start");
					}
					// A dip under the yell threshold ends that scream.
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
