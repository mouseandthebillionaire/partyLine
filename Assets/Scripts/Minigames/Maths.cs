using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Maths : MonoBehaviour
{
    private string[] equations = new string[10] {
		"(4 - (2 x 2)) x (12 x 6 / 3)",
		"((1 + 1) x 100) / (50 x 4)",
		"((40 / 8) x 2) / 5",
		"(1 + 2 + 3) / 2",
		"4 + (12/2) - 6",
		"2 + 1 + 1 + 1",
		"1 + 2 + 2 + 1",
		"1 + 1 + 1 + 1 + 1 + 2",
		"(1 + 1) x 4",
		"2 + 2 + 2 + 2 + 1"
	};

	private int numPhones;
	public int numQuestions = 5;
	private int currentQuestion = 0;
	private int correctKey;
	private int numAnswered;
	private float questionStart;
	private bool running;
	private bool[,] answered; // 1 per phone, 1 per question
	private bool[] unanswered; // 1 per question
	private Text equation;

    public float timeToAnswer;

    void Start()
    {
		numPhones = GlobalVariables.S != null ? GlobalVariables.S.numPhones : 0;
		answered = new bool[numPhones, numQuestions];
		unanswered = new bool[numQuestions];
		// Equation index is the answer, and key index 0-9 is that digit.

		Text directions = transform.Find("Directions").GetComponent<Text>();
		equation = transform.Find("Equation").GetComponent<Text>();
		directions.text = "DO THE MATHS!";

		if (timeToAnswer <= 0f) timeToAnswer = 10f;
		running = true;
		NextQuestion();
    }

	void Update()
	{
		if (!running || currentQuestion >= numQuestions || !unanswered[currentQuestion]) return;

		if (Time.time - questionStart > timeToAnswer) {
			CloseQuestion();
			return;
		}

		if (PhoneInputManager.S == null) return;

		for (int i = 0; i < numPhones; i++) {
			if (answered[i, currentQuestion]) continue;

			int pressed = PressedDigit(i);
			if (pressed < 0) continue;

			if (pressed == correctKey) {
				// Correct!
				StartCoroutine(CorrectAnswer(i));
				return;
			}

			answered[i, currentQuestion] = true;
			MinigameManager.S.inputDisplayObjects[i].GetComponent<Image>().color = Color.grey;
			MinigameManager.S.zilch.Play();
			numAnswered++;
			if (numAnswered == numPhones) {
				// Nobody Got it!
				StartCoroutine(NobodyGotIt());
				return;
			}
		}
	}

	private IEnumerator CorrectAnswer(int phone) {
		// Hide the question
		equation.text = "";
		MinigameManager.S.UpdatePlayerScore(phone);
		MinigameManager.S.coin.Play();
		// TODO: Some sort of celebratory graphic
		yield return new WaitForSeconds(1f);
		CloseQuestion();
	}

	private IEnumerator NobodyGotIt() {
		// Hide the question
		equation.text = "";
		MinigameManager.S.zilch.Play();
		// TODO: Some sort of disappointed graphic
		yield return new WaitForSeconds(1f);
		CloseQuestion();
	}


	private void CloseQuestion() {
		if (currentQuestion >= numQuestions || !unanswered[currentQuestion]) return;
		unanswered[currentQuestion] = false;
		currentQuestion++;
		NextQuestion();
	}

	private void NextQuestion() {
		if (currentQuestion < numQuestions)
			AskQuestion(currentQuestion);
		else {
			running = false;
			MinigameManager.S.EndGame();
		}
	}

    private void AskQuestion(int question) {
		correctKey = Random.Range(0, equations.Length);
		Debug.Log(correctKey);
		equation.text = (question + 1) + ". \n\n" + equations[correctKey];

		numAnswered = 0;
		for (int i = 0; i < numPhones; i++) {
			if (GlobalVariables.S != null)
				MinigameManager.S.inputDisplayObjects[i].GetComponent<Image>().color = GlobalVariables.S.phoneColors[i];
		}

		questionStart = Time.time;
		unanswered[question] = true;
	}

	// Digit buttons are key indexes 0-9. Returns -1 when this phone pressed none of them.
	int PressedDigit(int phone) {
		for (int key = 0; key <= 9; key++) {
			if (PhoneInputManager.S.GetButtonDown(phone, key))
				return key;
		}
		return -1;
	}
}
