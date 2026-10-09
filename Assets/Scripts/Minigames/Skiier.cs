using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Skiier : MonoBehaviour
{
    public GameObject[] skiers;
    public float speed = 1f;
    public float dropSpeed = .1f;
    public float courseLeft = -8f;
    public float courseRight = 8f;
    // Share of downhill speed kept after a turn, and how fast it climbs back while they hold a line.
    public float turnCut = 0.5f;
    public float straightenRate = 0.05f;
    // How far they've dropped, scaled up so the score box climbs as a decimal.
    public float distanceScale = 10.05f;

    public Sprite snowman;

    // Obstacles
    public Transform obstacleManager;
    public GameObject tree, rock;

    // keyNames index 14 is YELL
    const int yellKey = 14;

    public static Skiier S;

    int[] direction;
    float[] ySpeed;
    float[] startY;
    bool[] inRace;
    bool[] eliminated;
    bool ended;
    bool colored;

    void Awake()
    {
        S = this;
    }

    void Start()
    {
        Transform skiersRoot = transform.Find("Skiers");
        if (skiersRoot != null && skiersRoot.childCount > 0) {
            skiers = new GameObject[skiersRoot.childCount];
            for (int i = 0; i < skiersRoot.childCount; i++)
                skiers[i] = skiersRoot.GetChild(i).gameObject;
        }

        int count = skiers != null ? skiers.Length : 0;
        direction = new int[count];
        ySpeed = new float[count];
        startY = new float[count];
        inRace = new bool[count];
        eliminated = new bool[count];
        for (int i = 0; i < count; i++)
            ySpeed[i] = dropSpeed;
        for (int i = 0; i < count; i++) {
            if (skiers[i] == null) continue;
            bool playing = GlobalVariables.S != null && GlobalVariables.S.IsLoggedIn(i);
            inRace[i] = playing;
            skiers[i].SetActive(playing);
            if (!playing) continue;
            startY[i] = PosY(skiers[i]);
            ShowDistance(i);
        }
        obstacleManager = GameObject.Find("ObstacleManager").transform;
        StartCoroutine(DropObstacle());
    }

    void Update()
    {
        if (direction == null) return;

        if (!colored)
            colored = ColorDisplays();

        float rise = ObstaclePace() * Time.deltaTime;
        for (int i = 0; i < direction.Length; i++) {
            if (!eliminated[i] || skiers[i] == null) continue;
            Move(skiers[i], 0f, rise);
            KeepOnCourse(skiers[i]);
        }

        if (ended) return;

        for (int i = 0; i < direction.Length; i++) {
            if (!inRace[i] || eliminated[i]) continue;
            if (skiers[i] == null || !skiers[i].activeSelf) {
                eliminated[i] = true;
                continue;
            }

            bool atLeft = PosX(skiers[i]) <= courseLeft;
            bool atRight = PosX(skiers[i]) >= courseRight;

            if (PhoneInputManager.S != null && PhoneInputManager.S.GetButtonDown(i, yellKey)) {
                int previous = direction[i];
                bool firstPush = previous == 0 && !atLeft && !atRight;
                if (atLeft)
                    direction[i] = 1;
                else if (atRight)
                    direction[i] = -1;
                else if (previous == 0)
                    direction[i] = Random.value < 0.5f ? -1 : 1;
                else
                    direction[i] = -direction[i];

                if (direction[i] != previous && !firstPush)
                    ySpeed[i] *= turnCut;

                Face(skiers[i], direction[i]);
            }

            if (direction[i] != 0) {
                ySpeed[i] = Mathf.Min(dropSpeed, ySpeed[i] + straightenRate * Time.deltaTime);
                float dx = direction[i] * speed * Time.deltaTime;
                if ((atLeft && direction[i] < 0) || (atRight && direction[i] > 0))
                    dx = 0f;
                float dy = -ySpeed[i] * Time.deltaTime;
                Move(skiers[i], dx, dy);
            }

            ShowDistance(i);
            KeepOnCourse(skiers[i]);
        }

        CheckWinner();
    }

    void CheckWinner()
    {
        int racers = 0;
        int alive = 0;
        int last = -1;
        int finishers = 0;
        int finisher = -1;

        for (int i = 0; i < inRace.Length; i++) {
            if (!inRace[i]) continue;
            racers++;
            if (eliminated[i] || skiers[i] == null || !skiers[i].activeSelf) {
                eliminated[i] = true;
                continue;
            }

            alive++;
            last = i;
            if (PosY(skiers[i]) < 0f) {
                finishers++;
                finisher = i;
            }
        }

        if (finishers == 1) {
            Finish(finisher);
            return;
        }
        if (finishers > 1) {
            Finish(98);
            return;
        }

        // Last one left, after somebody else has crashed.
        if (racers > 1 && alive == 1)
            Finish(last);
        else if (racers > 0 && alive == 0)
            Finish(99);
    }

    public void Crash(GameObject skier)
    {
        if (skiers == null || skier == null) return;
        int i = System.Array.IndexOf(skiers, skier);
        if (i < 0 || eliminated[i]) return;

        eliminated[i] = true;
        SpriteRenderer sprite = skier.GetComponentInChildren<SpriteRenderer>();
        if (sprite != null && snowman != null) {
            sprite.sprite = snowman;
            sprite.flipX = false;
        }

        Hold(skier);
    }

    float ObstaclePace()
    {
        if (tree != null) {
            ObstacleScript pace = tree.GetComponent<ObstacleScript>();
            if (pace != null) return pace.ySpeed;
        }
        return 1f;
    }

    void Finish(int winner)
    {
        ended = true;
        for (int i = 0; i < skiers.Length; i++) {
            if (skiers[i] == null || eliminated[i]) continue;
            Hold(skiers[i]);
        }
        StartCoroutine(Announce(winner));
    }

    static void Hold(GameObject skier)
    {
        Rigidbody2D body = skier.GetComponent<Rigidbody2D>();
        if (body == null) return;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.bodyType = RigidbodyType2D.Kinematic;
    }

    IEnumerator Announce(int winner)
    {
        yield return new WaitForSeconds(1f);
        if (MinigameManager.S != null)
            MinigameManager.S.EndGame(winner);
    }

    bool ColorDisplays()
    {
        if (MinigameManager.S == null || GlobalVariables.S == null) return false;
        // ShowInputDisplay greys logged-out phones after this object starts. Wait until that has run.
        if (MinigameManager.S.inputDisplay != null && !MinigameManager.S.inputDisplay.activeSelf)
            return false;

        var boxes = MinigameManager.S.inputDisplayObjects;
        if (boxes == null) return false;

        for (int i = 0; i < boxes.Length; i++) {
            if (boxes[i] == null) continue;
            Image image = boxes[i].GetComponent<Image>();
            if (image == null) continue;
            bool playing = i < inRace.Length && inRace[i];
            if (playing && i < GlobalVariables.S.phoneColors.Length)
                image.color = GlobalVariables.S.phoneColors[i];
            else
                image.color = Color.grey;
        }
        return true;
    }

    void ShowDistance(int phone)
    {
        if (MinigameManager.S == null || skiers[phone] == null) return;
        var texts = MinigameManager.S.inputDisplayTexts;
        if (texts == null || phone >= texts.Length || texts[phone] == null) return;

        float dropped = Mathf.Max(0f, startY[phone] - PosY(skiers[phone]));
        texts[phone].text = (dropped * distanceScale).ToString("0.00");
    }

    static float PosY(GameObject skier)
    {
        if (skier.GetComponentInChildren<SpriteRenderer>() != null)
            return skier.transform.localPosition.y;

        RectTransform rect = skier.transform as RectTransform;
        if (rect != null)
            return rect.anchoredPosition.y;

        return skier.transform.localPosition.y;
    }

    // Sprites face with flipX. UI images still flip by their width.
    static void Face(GameObject skier, int direction)
    {
        SpriteRenderer sprite = skier.GetComponentInChildren<SpriteRenderer>();
        if (sprite != null) {
            sprite.flipX = direction < 0;
            Vector3 scale = skier.transform.localScale;
            scale.x = Mathf.Abs(scale.x);
            skier.transform.localScale = scale;
            return;
        }

        Vector3 uiScale = skier.transform.localScale;
        uiScale.x = Mathf.Abs(uiScale.x) * direction;
        skier.transform.localScale = uiScale;
    }

    void KeepOnCourse(GameObject skier)
    {
        float x = PosX(skier);
        if (x >= courseLeft && x <= courseRight) return;
        SetX(skier, Mathf.Clamp(x, courseLeft, courseRight));
    }

    static float PosX(GameObject skier)
    {
        if (skier.GetComponentInChildren<SpriteRenderer>() != null)
            return skier.transform.localPosition.x;

        RectTransform rect = skier.transform as RectTransform;
        if (rect != null)
            return rect.anchoredPosition.x;

        return skier.transform.localPosition.x;
    }

    static void SetX(GameObject skier, float x)
    {
        if (skier.GetComponentInChildren<SpriteRenderer>() != null) {
            Vector3 pos = skier.transform.localPosition;
            pos.x = x;
            skier.transform.localPosition = pos;
            return;
        }

        RectTransform rect = skier.transform as RectTransform;
        if (rect != null) {
            Vector2 pos = rect.anchoredPosition;
            pos.x = x;
            rect.anchoredPosition = pos;
            return;
        }

        Vector3 local = skier.transform.localPosition;
        local.x = x;
        skier.transform.localPosition = local;
    }

    static void Move(GameObject skier, float dx, float dy)
    {
        if (skier.GetComponentInChildren<SpriteRenderer>() != null) {
            Vector3 pos = skier.transform.localPosition;
            pos.x += dx;
            pos.y += dy;
            skier.transform.localPosition = pos;
            return;
        }

        RectTransform rect = skier.transform as RectTransform;
        if (rect != null) {
            Vector2 pos = rect.anchoredPosition;
            pos.x += dx;
            pos.y += dy;
            rect.anchoredPosition = pos;
            return;
        }

        Vector3 local = skier.transform.localPosition;
        local.x += dx;
        local.y += dy;
        skier.transform.localPosition = local;
    }

    private IEnumerator DropObstacle() {
        GameObject obstacle_0 = Instantiate(Random.value < 0.5f ? tree : rock, obstacleManager);
        obstacle_0.transform.localPosition = new Vector3(Random.Range(-8f, 8f), -1f, 0f);
        // Let's do a couple
        GameObject obstacle_1 = Instantiate(Random.value < 0.5f ? tree : rock, obstacleManager);
        obstacle_1.transform.localPosition = new Vector3(Random.Range(-8f, 8f), -1f, 0f);
        yield return new WaitForSeconds(Random.Range(1f, 3f));
        StartCoroutine(DropObstacle());
    }
}
