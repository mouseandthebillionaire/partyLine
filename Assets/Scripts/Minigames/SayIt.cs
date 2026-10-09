using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SayIt : MonoBehaviour
{
    public bool[] yelling;
    private int phones;

	public string[] words;
	public TextMeshProUGUI wordText;

	void Start() {
        phones = GlobalVariables.S.numPhones;   
        yelling = new bool[phones];
        
        StartCoroutine(RunGame());
	}

    private IEnumerator RunGame() {
		// How long are we playing this game for?
		int holdSeconds = 5;
		string currWord = words[Random.Range(0, words.Length)];	
		wordText.text = currWord;


		if (PhoneInputManager.S != null) {
			for (int i = 0; i < phones; i++)
				yelling[i] = PhoneInputManager.S.IsYelling(i);
		}

		float duration = Time.time + holdSeconds;

		while (duration > Time.time) {
			if (PhoneInputManager.S != null) {
				for (int i = 0; i < phones; i++) {
					bool yellingNow = PhoneInputManager.S.IsYelling(i);
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