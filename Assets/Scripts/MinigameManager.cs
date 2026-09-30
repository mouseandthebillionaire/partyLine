using UnityEngine;

public class MinigameManager : MonoBehaviour
{
    public GameObject game;

    public static MinigameManager S;

    void Awake() {
        S = this;
    }

    // Start is called before the first frame update
    void Start()
    {
		game.SetActive(false);
		StartCoroutine(ShowTitle());
    }

	private IEnumerator ShowTitle() {
		GameObject title = GameObject.Find("Title");
		title.SetActive(false);
		yield return new WaitForSeconds(1f);
		title.SetActive(true);
		yield return new WaitForSeconds(2f);
		title.SetActive(false);
		game.SetActive(true);
	}

    // Update is called once per frame
    void Update()
    {
        
    }
}
