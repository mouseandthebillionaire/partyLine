using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class Quizshow : MonoBehaviour
{
    
	// Eventually do this from the RESOURCES folder.
	private string[] questions;
	private int[] deck;
	private int deckIndex;

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
		
		// A blank line separates questions. The first line is "answer#question", then one choice per line.
		string raw = Resources.Load<TextAsset>("quizQuestions").text.Replace("\r\n", "\n").Replace("\r", "\n");
		var loaded = new List<string>();
		foreach (string block in raw.Split(new[] { "\n\n" }, System.StringSplitOptions.RemoveEmptyEntries)) {
			string trimmed = block.Trim();
			if (trimmed.IndexOf('#') >= 0)
				loaded.Add(trimmed);
		}
		questions = loaded.ToArray();
		ShuffleDeck();
		
		answered = new bool[numPhones, numQuestions];
		unanswered = new bool[numQuestions];
		// Equation index is the answer, and key index 0-9 is that digit.

		Text directions = transform.Find("Directions").GetComponent<Text>();
		equation = transform.Find("Question").GetComponent<Text>();
		directions.text = "";

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
		if (deckIndex >= deck.Length)
			ShuffleDeck();

		string questionAnswerCombo = questions[deck[deckIndex++]];
		// The answer is at the beginning of the string
		correctKey = int.Parse(questionAnswerCombo.Split('#')[0]);
		string questionText = questionAnswerCombo.Split('#')[1];
		int lineBreak = questionText.IndexOf('\n');
		if (lineBreak >= 0)
			questionText = questionText.Substring(0, lineBreak) + "\n" + questionText.Substring(lineBreak);
		Debug.Log(correctKey);
		equation.text = (question + 1) + ") " + questionText;

		numAnswered = 0;
		for (int i = 0; i < numPhones; i++) {
			if (GlobalVariables.S != null)
				MinigameManager.S.inputDisplayObjects[i].GetComponent<Image>().color = GlobalVariables.S.phoneColors[i];
		}

		questionStart = Time.time;
		unanswered[question] = true;
	}

	void ShuffleDeck() {
		deck = new int[questions.Length];
		for (int i = 0; i < deck.Length; i++)
			deck[i] = i;
		for (int i = deck.Length - 1; i > 0; i--) {
			int j = Random.Range(0, i + 1);
			int swap = deck[i];
			deck[i] = deck[j];
			deck[j] = swap;
		}
		deckIndex = 0;
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
