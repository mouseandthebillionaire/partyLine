using UnityEngine;

public class ObstacleScript : MonoBehaviour
{
    public float ySpeed = 20f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float dy = ySpeed * Time.deltaTime;
        if (GetComponentInChildren<SpriteRenderer>() != null) {
            Vector3 pos = transform.localPosition;
            pos.y += dy;
            transform.localPosition = pos;
            return;
        }

        RectTransform rect = transform as RectTransform;
        if (rect != null) {
            Vector2 pos = rect.anchoredPosition;
            pos.y += dy;
            rect.anchoredPosition = pos;
            return;
        }

        transform.localPosition += new Vector3(0f, dy, 0f);
    }

    private void OnCollisionEnter2D(Collision2D other) {
        if (other.gameObject.CompareTag("skier") && Skiier.S != null)
            Skiier.S.Crash(other.gameObject);
    }
}
