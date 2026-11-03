using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Maths : MonoBehaviour
{
    struct Problem {
		public string text;
		public int answer;
		public Problem(string text, int answer) {
			this.text = text;
			this.answer = answer;
		}
	}

	// Each answer is a single digit, so it maps straight onto keys 0-9.
	private Problem[] problems = new Problem[] {
		new Problem("(4 - (2 x 2)) x (12 x 6 / 3)", 0),
		new Problem("((1 + 1) x 100) / (50 x 4)", 1),
		new Problem("((40 / 8) x 2) / 5", 2),
		new Problem("(1 + 2 + 3) / 2", 3),
		new Problem("4 + (12/2) - 6", 4),
		new Problem("2 + 1 + 1 + 1", 5),
		new Problem("1 + 2 + 2 + 1", 6),
		new Problem("1 + 1 + 1 + 1 + 1 + 2", 7),
		new Problem("(1 + 1) x 4", 8),
		new Problem("2 + 2 + 2 + 2 + 1", 9),

		new Problem("((12 / 3) - 4) x 9", 0),
		new Problem("(4 x 5) - (2 x 10)", 0),
		new Problem("((3 x 5) + 1) / (4 x 4)", 1),
		new Problem("(7 x 3) - (4 x 5)", 1),
		new Problem("((5 x 4) + 2) / (5 + 6)", 2),
		new Problem("(18 / 3) - (2 x 2)", 2),
		new Problem("(24 / 4) - (2 + 1)", 3),
		new Problem("(5 x 3) - (4 x 3)", 3),
		new Problem("(8 + 8) / (2 x 2)", 4),
		new Problem("(36 / 6) - (4 / 2)", 4),
		new Problem("(45 / 5) - (2 x 2)", 5),
		new Problem("(3 x 3) - (2 + 2)", 5),
		new Problem("(5 x 4) - (2 x 7)", 6),
		new Problem("(4 x 4) - (5 x 2)", 6),
		new Problem("(9 x 3) - (4 x 5)", 7),
		new Problem("(3 x 5) - (4 x 2)", 7),
		new Problem("(6 x 3) - (2 x 5)", 8),
		new Problem("(4 x 4) - (2 x 4)", 8),
		new Problem("(5 x 5) - (4 x 4)", 9),
		new Problem("(7 x 3) - (3 x 4)", 9)
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
			if (!IsPlaying(i) || answered[i, currentQuestion]) continue;

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
			if (numAnswered == PlayingCount()) {
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
		Problem problem = problems[Random.Range(0, problems.Length)];
		correctKey = problem.answer;
		Debug.Log(correctKey);
		equation.text = (question + 1) + ". \n\n" + problem.text;

		numAnswered = 0;
		for (int i = 0; i < numPhones; i++) {
			Image box = MinigameManager.S.inputDisplayObjects[i].GetComponent<Image>();
			if (IsPlaying(i))
				box.color = GlobalVariables.S.phoneColors[i];
			else
				box.color = Color.grey;
		}

		questionStart = Time.time;
		unanswered[question] = true;
	}

	bool IsPlaying(int phone)
	{
		return GlobalVariables.S != null && GlobalVariables.S.IsLoggedIn(phone);
	}

	int PlayingCount()
	{
		int n = 0;
		for (int i = 0; i < numPhones; i++)
			if (IsPlaying(i)) n++;
		return n;
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
