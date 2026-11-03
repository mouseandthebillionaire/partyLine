using UnityEngine;
using TMPro;

public class DDR_key : MonoBehaviour
{
    public int myKey;
    public int playerNum;
    public float targetLocalY;
    public float fallSpeed = 240f;
    // Inside this distance the yell scores. A little farther is a miss.
    public float hitSlop = 50f;
    public float missSlop = 110f;
    public string[] goodWords = { "great", "nice", "okay", "yup", "awesome", "solid" };
    public string[] badWords = { "nope", "whoops", "uh-oh", "doh", "bzz", "bad" };
    public float wordTime = 0.7f;

    TextMeshProUGUI word;

    void Awake()
    {
        // The prefab has a rigidbody. Leave it out of the simulation so it can't pull the note off the column.
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
            body.simulated = false;

        word = GetComponentInChildren<TextMeshProUGUI>(true);
        if (word != null)
            word.gameObject.SetActive(false);
    }

    void Update()
    {
        if (DDRawr.S == null) return;

        Vector3 p = transform.localPosition;
        p.y -= fallSpeed * Time.deltaTime;
        transform.localPosition = p;

        float fromTarget = p.y - targetLocalY;
        float dist = Mathf.Abs(fromTarget);
        bool nearest = ShouldJudge();
        float eighthAway = fallSpeed * Mathf.Max(0.05f, DDRawr.S.quarterNote) * 0.5f;

        // Too soon, but within an eighth note of the box. Judge it now.
        if (nearest && fromTarget > hitSlop && fromTarget <= eighthAway && DDRawr.S.TakeYell(playerNum))
        {
            Miss();
            Destroy(gameObject);
            return;
        }

        if (nearest && dist <= hitSlop && DDRawr.S.TakeYell(playerNum))
        {
            ShowWord(true);
            MinigameManager.S.UpdatePlayerScore(playerNum);
            Destroy(gameObject);
            return;
        }

        if (fromTarget >= -hitSlop) return;

        // Just past the box.
        if (fromTarget >= -missSlop && nearest && DDRawr.S.YellFresh(playerNum))
        {
            DDRawr.S.TakeYell(playerNum);
            Miss();
            Destroy(gameObject);
            return;
        }

        if (fromTarget < -missSlop)
            Destroy(gameObject);
    }

    bool ShouldJudge()
    {
        if (transform.parent == null) return true;
        float myDist = Mathf.Abs(transform.localPosition.y - targetLocalY);
        Transform parent = transform.parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            DDR_key other = parent.GetChild(i).GetComponent<DDR_key>();
            if (other == null || other == this || other.playerNum != playerNum) continue;
            float dist = Mathf.Abs(other.transform.localPosition.y - other.targetLocalY);
            if (dist < myDist) return false;
            if (Mathf.Approximately(dist, myDist) && other.transform.localPosition.y < transform.localPosition.y)
                return false;
        }
        return true;
    }

    void ShowWord(bool hit)
    {
        if (word == null) return;

        string[] choices = hit ? goodWords : badWords;
        if (choices != null && choices.Length > 0)
            word.text = choices[Random.Range(0, choices.Length)];
        word.color = hit ? Color.yellow : Color.red;

        Vector3 atBox = transform.parent != null
            ? transform.parent.TransformPoint(new Vector3(transform.localPosition.x, targetLocalY, transform.localPosition.z))
            : transform.position;

        word.gameObject.SetActive(true);
        Canvas canvas = word.canvas != null ? word.canvas : GetComponentInParent<Canvas>();
        if (canvas != null)
            word.transform.SetParent(canvas.transform, true);
        word.rectTransform.position = atBox;
        word.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-20f, 20f));
        Destroy(word.gameObject, wordTime);
        word = null;
    }

    void Miss()
    {
        ShowWord(false);
        MinigameManager mgr = MinigameManager.S;
        if (mgr == null || (uint)playerNum >= (uint)mgr.inputTimes.Length) return;

        mgr.inputTimes[playerNum]--;
        if (mgr.inputDisplayTexts != null
            && (uint)playerNum < (uint)mgr.inputDisplayTexts.Length
            && mgr.inputDisplayTexts[playerNum] != null)
            mgr.inputDisplayTexts[playerNum].text = mgr.inputTimes[playerNum].ToString();
        if (mgr.zilch != null)
            mgr.zilch.Play();
    }
}
