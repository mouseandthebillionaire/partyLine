using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FastestDraw : MonoBehaviour
{
	public AudioSource ring;
	
	// Start is called before the first frame update
    void Start()
    {
	    StartCoroutine(RunGame());
    }

    private IEnumerator RunGame() {
		int phones = GlobalVariables.S != null ? GlobalVariables.S.numPhones : 0;
		bool[] inThis = new bool[phones];
		for (int i = 0; i < phones; i++) {
			inThis[i] = true;
		}
		int holdSeconds = Random.Range(5, 10);
		int timeToAnswer = 5;
		
		float duration = Time.time + holdSeconds;
		
		while (duration > Time.time){

			for (int i = 0; i < phones; i++) {
				if (PhoneInputManager.S.GetButtonDown(i, 12) && inThis[i]) {
					inThis[i] = false;
					MinigameManager.S.inputDisplayObjects[i].GetComponent<Image>().color = Color.grey;
					MinigameManager.S.inputDisplayTexts[i].text = "Too Soon!";
					MinigameManager.S.zilch.Play();
					Debug.Log("Too soon!");
				}
			}
			yield return null;
		}
		
		bool ringing = true;
		ring.Play();
		Debug.Log("Riiiiiiiing!");
		
		while (ringing){
			for (int i = 0; i < phones; i++) {
				if (PhoneInputManager.S.GetButtonDown(i, 12) && inThis[i]) {
					MinigameManager.S.inputDisplayObjects[i].transform.localScale = new Vector3(1.25f, 1.25f, 1.25f);
					ringing = false;
					ring.Stop();
					MinigameManager.S.EndGame(i);
				}
			}

			if (Time.time > (duration + timeToAnswer))
			{
				ringing = false;
				ring.Stop();
				MinigameManager.S.EndGame(99);
			}
			
			yield return null;
		}
	}
}
