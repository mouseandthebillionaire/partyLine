using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class balloonBlow : MonoBehaviour
{
    // Upward push while the phone is quiet. Gravity is stronger than this, so a quiet balloon still drops.
    public float floatForce = 100f;
    // Extra upward push at a full blow.
    public float blowForce = 1300f;
    // Canvas pixels are about one physics unit, so gravity has to be scaled up to read on screen.
    public float gravityScale = 40f;
    public float damping = 1.2f;
    public int gameLength = 20;

    Rigidbody2D[] balloons;
    readonly Vector3[] corners = new Vector3[4];
    int contenders;
    bool ended;
    bool scoresHidden;

    public Image[] crosshairs;
    // Canvas pixels per second.
    public float crosshairSpeed = 280f;

    // keyNames index 11 is #
    const int poundKey = 11;

    void Start()
    {
        balloons = GetComponentsInChildren<Rigidbody2D>(true);
        System.Array.Sort(balloons, (a, b) => AnchoredX(a).CompareTo(AnchoredX(b)));

        bool anyone = AnyoneLoggedIn();
        for (int i = 0; i < balloons.Length; i++) {
            Rigidbody2D body = balloons[i];
            bool playing = !anyone || (GlobalVariables.S != null && GlobalVariables.S.IsLoggedIn(i));
            body.gameObject.SetActive(playing);
            if (!playing) {
                balloons[i] = null;
                continue;
            }
            contenders++;

            body.gravityScale = gravityScale;
            body.linearDamping = damping;
            body.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;

            Image image = body.GetComponent<Image>();
            if (image != null && GlobalVariables.S != null && (uint)i < (uint)GlobalVariables.S.phoneColors.Length)
                image.color = GlobalVariables.S.phoneColors[i];
        }

        StartCoroutine(RunGame());
    }

    IEnumerator RunGame()
    {
        StartCoroutine(CrosshairMovement());
        float duration = Time.time + gameLength;
        while (duration > Time.time && !ended)
            yield return null;

        if (!ended)
            Finish(Survivor());
    }

    IEnumerator CrosshairMovement()
    {
        if (crosshairs == null || crosshairs.Length == 0) yield break;

        int n = crosshairs.Length;
        var velocity = new Vector2[n];
        bool anyone = AnyoneLoggedIn();

        for (int i = 0; i < n; i++) {
            Image cross = crosshairs[i];
            if (cross == null) continue;

            bool playing = !anyone || (GlobalVariables.S != null && GlobalVariables.S.IsLoggedIn(i));
            cross.gameObject.SetActive(playing);
            if (!playing) continue;

            if (GlobalVariables.S != null && (uint)i < (uint)GlobalVariables.S.phoneColors.Length)
                cross.color = GlobalVariables.S.phoneColors[i];

            RectTransform rect = cross.rectTransform;
            if (!Playfield(rect, out Vector2 min, out Vector2 max, out float pixels)) continue;

            rect.position = new Vector3(Random.Range(min.x, max.x), Random.Range(min.y, max.y), rect.position.z);
            float sx = Random.value < 0.5f ? -1f : 1f;
            float sy = Random.value < 0.5f ? -1f : 1f;
            velocity[i] = new Vector2(sx, sy).normalized * crosshairSpeed * pixels;
        }

        while (true) {
            for (int i = 0; i < n; i++) {
                Image cross = crosshairs[i];
                if (cross == null || !cross.gameObject.activeSelf) continue;

                RectTransform rect = cross.rectTransform;
                if (!Playfield(rect, out Vector2 min, out Vector2 max, out _)) continue;

                Vector3 pos = rect.position;
                Vector2 v = velocity[i];
                pos.x += v.x * Time.deltaTime;
                pos.y += v.y * Time.deltaTime;

                if (pos.x >= max.x) {
                    pos.x = max.x;
                    v.x = -Mathf.Abs(v.x);
                } else if (pos.x <= min.x) {
                    pos.x = min.x;
                    v.x = Mathf.Abs(v.x);
                }
                if (pos.y >= max.y) {
                    pos.y = max.y;
                    v.y = -Mathf.Abs(v.y);
                } else if (pos.y <= min.y) {
                    pos.y = min.y;
                    v.y = Mathf.Abs(v.y);
                }

                velocity[i] = v;
                rect.position = pos;
            }
            yield return null;
        }
    }

    // World-space box the crosshair's center can sit in, plus one canvas pixel in world units.
    bool Playfield(RectTransform rect, out Vector2 min, out Vector2 max, out float pixels)
    {
        min = max = Vector2.zero;
        pixels = 1f;

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        RectTransform space = canvas != null ? canvas.transform as RectTransform : null;
        if (space == null) return false;

        space.GetWorldCorners(corners);
        float halfW = rect.rect.width * Mathf.Abs(rect.lossyScale.x) * 0.5f;
        float halfH = rect.rect.height * Mathf.Abs(rect.lossyScale.y) * 0.5f;
        min = new Vector2(corners[0].x + halfW, corners[0].y + halfH);
        max = new Vector2(corners[2].x - halfW, corners[2].y - halfH);
        pixels = Mathf.Abs(space.lossyScale.y);
        if (pixels < 0.0001f) pixels = 1f;
        return max.x > min.x && max.y > min.y;
    }

    void Update()
    {
        if (ended) return;
        TryPop();
        EliminateOffscreen();
        CheckLastStanding();
    }

    void LateUpdate()
    {
        if (scoresHidden || MinigameManager.S == null || MinigameManager.S.inputDisplay == null) return;
        if (!MinigameManager.S.inputDisplay.activeSelf) return;
        MinigameManager.S.inputDisplay.SetActive(false);
        scoresHidden = true;
    }

    void TryPop()
    {
        if (balloons == null || crosshairs == null || PhoneInputManager.S == null) return;

        for (int phone = 0; phone < crosshairs.Length; phone++) {
            if (!PhoneInputManager.S.GetButtonDown(phone, poundKey)) continue;

            Image cross = crosshairs[phone];
            if (cross == null || !cross.gameObject.activeSelf) continue;

            int hit = BalloonUnder(cross.rectTransform.position);
            if (hit < 0) continue;

            Eliminate(hit);
            PlayPop();
        }
    }

    void EliminateOffscreen()
    {
        if (balloons == null) return;

        for (int i = 0; i < balloons.Length; i++) {
            Rigidbody2D body = balloons[i];
            if (body == null) continue;
            RectTransform rect = body.transform as RectTransform;
            if (rect != null && OnScreen(rect)) continue;
            Eliminate(i);
        }
    }

    // Still counts until the whole image, string included, is outside the canvas.
    bool OnScreen(RectTransform balloon)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform space = canvas != null ? canvas.transform as RectTransform : null;
        if (space == null) return true;

        space.GetWorldCorners(corners);
        float screenL = corners[0].x;
        float screenB = corners[0].y;
        float screenR = corners[2].x;
        float screenT = corners[1].y;

        balloon.GetWorldCorners(corners);
        float left = corners[0].x;
        float bottom = corners[0].y;
        float right = corners[2].x;
        float top = corners[1].y;
        return right >= screenL && left <= screenR && top >= screenB && bottom <= screenT;
    }

    void Eliminate(int balloon)
    {
        Rigidbody2D body = balloons[balloon];
        balloons[balloon] = null;
        if (body != null)
            body.gameObject.SetActive(false);

        if (crosshairs != null && (uint)balloon < (uint)crosshairs.Length && crosshairs[balloon] != null)
            crosshairs[balloon].gameObject.SetActive(false);
    }

    void CheckLastStanding()
    {
        int alive = 0;
        int last = -1;
        for (int i = 0; i < balloons.Length; i++) {
            if (balloons[i] == null) continue;
            alive++;
            last = i;
        }

        if (alive == 0)
            Finish(99);
        else if (alive == 1 && contenders > 1)
            Finish(last);
    }

    int Survivor()
    {
        int alive = 0;
        int last = -1;
        if (balloons != null) {
            for (int i = 0; i < balloons.Length; i++) {
                if (balloons[i] == null) continue;
                alive++;
                last = i;
            }
        }
        if (alive == 1) return last;
        if (alive == 0) return 99;
        return 98;
    }

    void Finish(int winner)
    {
        if (ended || MinigameManager.S == null) return;
        ended = true;
        MinigameManager.S.EndGame(winner);
    }

    void PlayPop()
    {
        if (MinigameManager.S == null) return;
        AudioSource pop = MinigameManager.S.splatAudio;
        if (pop == null) return;
        pop.pitch = Random.Range(0.85f, 1.3f);
        pop.Play();
    }

    int BalloonUnder(Vector3 point)
    {
        int best = -1;
        float bestDist = float.MaxValue;

        for (int i = 0; i < balloons.Length; i++) {
            Rigidbody2D body = balloons[i];
            if (body == null) continue;

            RectTransform rect = body.transform as RectTransform;
            if (rect == null) continue;
            rect.GetWorldCorners(corners);
            if (point.x < corners[0].x || point.x > corners[2].x) continue;
            if (point.y < corners[0].y || point.y > corners[1].y) continue;

            float dist = (body.transform.position - point).sqrMagnitude;
            if (dist >= bestDist) continue;
            bestDist = dist;
            best = i;
        }
        return best;
    }

    void FixedUpdate()
    {
        if (balloons == null || PhoneInputManager.S == null) return;

        for (int i = 0; i < balloons.Length; i++) {
            Rigidbody2D body = balloons[i];
            if (body == null) continue;
            float blow = PhoneInputManager.S.GetSlider(i);
            body.AddForce(Vector2.up * (floatForce + blow * blowForce));
        }
    }

    static float AnchoredX(Rigidbody2D body)
    {
        RectTransform rect = body.transform as RectTransform;
        return rect != null ? rect.anchoredPosition.x : body.transform.position.x;
    }

    static bool AnyoneLoggedIn()
    {
        if (GlobalVariables.S == null) return false;
        int phones = GlobalVariables.S.numPhones;
        for (int i = 0; i < phones; i++) {
            if (GlobalVariables.S.IsLoggedIn(i)) return true;
        }
        return false;
    }
}
