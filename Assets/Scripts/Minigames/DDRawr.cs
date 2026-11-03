using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DDRawr : MonoBehaviour {
    public static DDRawr S;

    private KeyCode[] correctKeys = new KeyCode[3];
    public GameObject[] nextKeys = new GameObject[3];
    public Sprite[] keyImages = new Sprite[14];
    public GameObject goalBoxes;

    const int yellKey = 14;

    private bool gameRunning;
    private int numPhones;
    private bool[] yellHeld;
    private bool[] yellFresh;

    public AudioSource conga;

    public GameObject key, DDR_keys;

    // A quarter note is this long. An eighth pair is two notes, then a rest.
    public float quarterNote = 1f;
    public float eighthChance = 0.25f;

    void Awake()
    {
        S = this;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        numPhones = GlobalVariables.S != null ? GlobalVariables.S.numPhones : 0;
        yellHeld = new bool[numPhones];
        yellFresh = new bool[numPhones];
        for (int i = 0; i < numPhones; i++)
        {
            if (IsPlaying(i)) continue;
            Transform box = goalBoxes != null ? goalBoxes.transform.Find(i.ToString()) : null;
            if (box != null)
                box.gameObject.SetActive(false);
            GreyScore(i);
        }
        StartCoroutine(ExplainGame());
    }

    void Update()
    {
        if (!gameRunning || yellFresh == null || PhoneInputManager.S == null) return;

        for (int i = 0; i < numPhones; i++)
        {
            if (!IsPlaying(i)) continue;
            bool yelling = PhoneInputManager.S.GetButton(i, yellKey);
            if (yelling && !yellHeld[i])
                yellFresh[i] = true;
            if (!yelling)
                yellFresh[i] = false;
            yellHeld[i] = yelling;
        }
    }

    public bool YellFresh(int phone)
    {
        return yellFresh != null && (uint)phone < (uint)yellFresh.Length && yellFresh[phone];
    }

    // One roar can catch the animal in the box. The next animal needs a new sound.
    public bool TakeYell(int phone)
    {
        if (!YellFresh(phone)) return false;
        yellFresh[phone] = false;
        return true;
    }

    private IEnumerator ExplainGame()
    {
        GameObject directions  = GameObject.Find("Directions");
        directions.SetActive(true);
        // How much time do they need to read the instructions
        yield return new WaitForSeconds(3);
        directions.SetActive(false);

        StartCoroutine(RunGame());

        yield return null;
    }

    private IEnumerator RunGame()
    {
        gameRunning = true;
        conga.Play();
        goalBoxes.SetActive(true);

        StartCoroutine(CreateKeys());
        
        // Run only for as long as the music
        yield return new WaitForSeconds(conga.clip.length);
        
        // Play a symbolcrash
        
        // Kill All the DDR Keys
        foreach (Transform key in DDR_keys.transform)
        {
            Destroy(key.gameObject);
        }
        // And the target boxes
        goalBoxes.SetActive(false);
        
        // Then stop the game
        gameRunning = false;
        //  and End the game via the MinigameManager
        MinigameManager.S.EndGame();
    }

    private IEnumerator CreateKeys()
    {
        bool[] onBeat = new bool[numPhones];
        bool[] justPaired = new bool[numPhones];
        bool downbeat = true;
        float eighth = Mathf.Max(0.05f, quarterNote * 0.5f);

        while (gameRunning)
        {
            for (int i = 0; i < numPhones; i++)
            {
                if (!IsPlaying(i)) continue;
                if (downbeat)
                {
                    // A pair has to stop. The next beat is empty so three eighths never touch.
                    if (justPaired[i])
                    {
                        justPaired[i] = false;
                        onBeat[i] = false;
                        continue;
                    }
                    onBeat[i] = Random.Range(0, 100) > 20;
                    if (onBeat[i])
                        GetNextKey(i);
                }
                else if (onBeat[i] && Random.value < eighthChance)
                {
                    GetNextKey(i);
                    justPaired[i] = true;
                }
            }

            downbeat = !downbeat;
            yield return new WaitForSeconds(eighth);
        }
    }

    private void GetNextKey(int _playerNum)
    {
        Transform box = goalBoxes.transform.Find(_playerNum.ToString());
        if (box == null) return;

        int r = Random.Range(0, keyImages.Length);
        GameObject go = Instantiate(key, DDR_keys.transform, false);
        go.GetComponent<Image>().sprite = keyImages[r];

        // Match the goal column, and fall until this note's pivot meets the box.
        Vector3 local = DDR_keys.transform.InverseTransformPoint(box.position);
        DDR_key note = go.GetComponent<DDR_key>();
        note.myKey = r;
        note.playerNum = _playerNum;
        note.targetLocalY = local.y;
        go.transform.localPosition = new Vector3(local.x, local.y + 800f, local.z);
    }

    bool IsPlaying(int phone)
    {
        return GlobalVariables.S != null && GlobalVariables.S.IsLoggedIn(phone);
    }

    void GreyScore(int phone)
    {
        if (MinigameManager.S == null) return;
        GameObject[] boxes = MinigameManager.S.inputDisplayObjects;
        if (boxes == null || (uint)phone >= (uint)boxes.Length || boxes[phone] == null) return;
        Image image = boxes[phone].GetComponent<Image>();
        if (image != null)
            image.color = Color.grey;
    }
}
