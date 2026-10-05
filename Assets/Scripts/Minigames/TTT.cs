using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TTT : MonoBehaviour {
    private string words = "DOGGOS,FRIEND,MONKEY,BANANA,SECRET,CIRCUS,SCHOOL,TURTLE,POTATO,PIRATE,DRAGON,PICKLE";
    private string wordCodes = "364467,374363,666539,226262,732738,247287,724665,887853,768286,747283,372466,742553";
    public int timeLimit;
    private float timer;

    private int wordLength = 6;
    private int[] currChar;
    private bool[] erred;
    private string[] wordLetters = new string[6];
    private int[] wordNumbers = new int[6];
    private int[,] wordCodeInputs = new int[3,6];
    private GameObject[] displayLetters = new GameObject[6];
    private bool spelling;

    private int numPhones;
    private Coroutine round;

    // Runs again each time the minigame object is turned on.
    void OnEnable()
    {
        if (round != null) StopCoroutine(round);
        // How many phones are playing?
        numPhones = GlobalVariables.S != null ? GlobalVariables.S.numPhones : 0;
        currChar = new int[numPhones];
        erred = new bool[numPhones];

        // Choose the word
        string[] wordList = words.Split(',');
        string[] codeList = wordCodes.Split(',');
        int wordIndex = Random.Range(0, wordList.Length);
        string word = wordList[wordIndex];
        string code = codeList[wordIndex];
        // Split the word
        for (int i = 0; i<wordLength; i++) {
            wordLetters[i] = word[i].ToString();
            wordNumbers[i] = int.Parse(code[i].ToString());
        }
        
        spelling = true;
        timer = 0;
        if (timeLimit <= 0) timeLimit = 20;

        // Display the letters
        for (int i = 0; i < wordLength; i++)
        {
            displayLetters[i] = GameObject.Find("letter_" + i);
            Text t = displayLetters[i].GetComponent<Text>();
            t.text = wordLetters[i];
        }

        round = StartCoroutine(RunGame());
    }

    // Update is called once per frame
    private IEnumerator RunGame() {
        int numOut = 0;

        // Get the time this launched
        float startTime = Time.time;

        while (timer < timeLimit && spelling){

            timer  = Time.time - startTime;

            if (PhoneInputManager.S != null) {
                for (int i = 0; i < numPhones; i++) {
                    if (erred[i] || currChar[i] >= wordLength) continue;

                    int expected = wordNumbers[currChar[i]];
                    if (PhoneInputManager.S.GetButtonDown(i, expected)) {
                        // Display that they got it
                        Transform pip = displayLetters[currChar[i]].transform.Find("p" + i);
                        if (pip != null)
                            pip.GetComponent<Image>().color = GlobalVariables.S.phoneColors[i];
                        MinigameManager.S.UpdatePlayerScore(i);
                        currChar[i]++;
                    }
                    else if (PressedOtherDigit(i, expected)) {
                        // They typed the wrong letter! Kick 'em out!
                        erred[i] = true;
                        MinigameManager.S.inputDisplayObjects[i].GetComponent<Image>().color = Color.grey;
                        MinigameManager.S.zilch.Play();
                        numOut++;
                    }

                    if (currChar[i] == wordLength) {
                        spelling = false;
                        MinigameManager.S.EndGame(i);
                    }
                }
            }

            if (numOut >= numPhones || timer > timeLimit) {
                    spelling = false;
                    // No One Won
                    MinigameManager.S.EndGame(null);
            }

            yield return null;
        }

    }

    // Digit buttons are key indexes 0-9. Ignores the digit this letter actually needs.
    bool PressedOtherDigit(int phone, int expected) {
        for (int key = 0; key <= 9; key++) {
            if (key == expected) continue;
            if (PhoneInputManager.S.GetButtonDown(phone, key))
                return true;
        }
        return false;
    }
}