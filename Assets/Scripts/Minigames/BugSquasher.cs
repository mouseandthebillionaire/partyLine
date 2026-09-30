using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BugSquasher : MonoBehaviour
{
	public Sprite[]		splats;
	public GameObject[]	bugKeys = new GameObject[12];
	public AudioSource splatSound;

	private int correctKey, tempCorrectKey;
	private int[] inputTimes = new int[3];

	public int gameLength = 20; // Seconds

	public void Start() {
		StartCoroutine(RunGame());
	}

	private IEnumerator MoveBug(GameObject _bug) {
		while (_bug.GetComponent<Image>().color == Color.white) {
			_bug.transform.position = new Vector3(
				_bug.transform.position.x + Random.Range(-2f, 2f),
				_bug.transform.position.y + Random.Range(-2f, 2f), 0);
			yield return new WaitForSeconds(0.1f);
		}
		
	}

    private IEnumerator RunGame() {
		// overall time based
		int minigameLength = gameLength;
		float duration = Time.time + minigameLength;
		
		// game variables
		float bugFrequency = 1f;
		bool bugPresent = false;
		for (int i = 0; i < bugKeys.Length; i++) {
			GameObject go = GameObject.Find(bugKeys[i].name + "/bug");
			go.GetComponent<Image>().color = Color.clear;
		}
	
		while (duration > Time.time) {
			if (!bugPresent) {
				tempCorrectKey = Random.Range(0, bugKeys.Length);
				// Did we just have that bug
				if (tempCorrectKey == correctKey)
				{
					tempCorrectKey = Random.Range(0, bugKeys.Length);
					Debug.Log("It hapenned");
				}
				correctKey = tempCorrectKey;
				
				GameObject go = GameObject.Find(bugKeys[correctKey].name + "/bug");
				go.GetComponent<Image>().color = Color.white;
				// Move the bug around
				StartCoroutine(MoveBug(go));
				bugPresent = true;
			}
			
			bool squashed = false;
			int winner = -1;
			for (int i = 0; i < 12; i++) {
				if (!squashed && PhoneInputManager.S.GetButtonDown(i, correctKey)) {
					// First player to match this frame claims the bug
					squashed = true;
					winner = i;
				}
				//MinigameManager.S.inputKeys[i] = 99;
			}

			if (squashed) {
				//MinigameManager.S.UpdatePlayerScore(winner);
				GameObject go = GameObject.Find(bugKeys[correctKey].name + "/bug");
				go.GetComponent<Image>().color = Color.clear;

				GameObject splat = GameObject.Find(bugKeys[correctKey].name + "/splat");
				splat.GetComponent<Image>().sprite = splats[Random.Range(0, splats.Length)];
				splat.GetComponent<Image>().color = GlobalVariables.S.phoneColors[winner];
				splatSound.Play();

				bugPresent = false;

				// Only show splat for half a second, then wait before next bug
				yield return new WaitForSeconds(0.5f);
				splat.GetComponent<Image>().color = Color.clear;

				yield return new WaitForSeconds(bugFrequency);
				bugFrequency -= 0.05f;
			}
			else {
				yield return null;
			}
		}
	
		MinigameManager.S.EndGame();
	}
}
