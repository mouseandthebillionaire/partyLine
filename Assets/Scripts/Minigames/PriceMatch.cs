using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PriceMatch : MonoBehaviour
{
    public PriceCatalog catalog;
    public GameObject itemPrefab;

    // Same idea as the menu code: keep the last few digits.
    const int maxDigits = 6;
    // keyNames index 10 is *, 11 is #
    const int starKey = 10;
    const int poundKey = 11;

    PriceItem current;
    string[] digits;
    bool[] submitted;
    int[] guesses;
    int phones;

    void Start()
    {
        phones = GlobalVariables.S != null ? GlobalVariables.S.numPhones : 0;
        digits = new string[phones];
        submitted = new bool[phones];
        guesses = new int[phones];
        for (int i = 0; i < phones; i++) {
            digits[i] = "";
            ShowPrice(i, digits[i]);
            SetDisplayColor(i, Color.grey);
        }

        if (catalog == null || catalog.items == null || catalog.items.Length == 0) {
            Debug.Log("Price catalog is empty");
            return;
        }

        current = catalog.items[Random.Range(0, catalog.items.Length)];
        Debug.Log("Price item: " + current.itemName);
        SpawnItem(current);
    }

    void Update()
    {
        if (current == null || PhoneInputManager.S == null || GlobalVariables.S == null) return;

        for (int phone = 0; phone < phones; phone++) {
            if (!IsPlaying(phone) || submitted[phone])
                continue;

            if (PhoneInputManager.S.GetButtonDown(phone, starKey)) {
                digits[phone] = "";
                ShowPrice(phone, digits[phone]);
                continue;
            }

            if (PhoneInputManager.S.GetButtonDown(phone, poundKey)) {
                Submit(phone);
                continue;
            }

            for (int key = 0; key <= 9; key++) {
                if (!PhoneInputManager.S.GetButtonDown(phone, key))
                    continue;

                digits[phone] += GlobalVariables.S.keyNames[key];
                if (digits[phone].Length > maxDigits)
                    digits[phone] = digits[phone].Substring(digits[phone].Length - maxDigits);

                ShowPrice(phone, digits[phone]);
                Debug.Log("Phone " + phone + " price " + FormatPrice(digits[phone]));
            }
        }
    }

    void Submit(int phone)
    {
        submitted[phone] = true;
        int guess = Cents(digits[phone]);
        guesses[phone] = guess;

        int actual = Mathf.RoundToInt(current.price * 100f);
        int off = Mathf.Abs(guess - actual);
        Debug.Log("Phone " + phone + " submitted " + FormatCents(guess) + "  actual " + FormatCents(actual) + "  off " + FormatCents(off));
        ShowPrice(phone, digits[phone]);
        if (GlobalVariables.S != null)
            SetDisplayColor(phone, GlobalVariables.S.phoneColors[phone]);

        for (int i = 0; i < phones; i++) {
            if (IsPlaying(i) && !submitted[i])
                return;
        }

        // Every phone that logged in has entered a price.
        // TODO: An animation to show the real price (and maybe the differences?)
        StartCoroutine(SendClosest(actual));
    }

    // Higher score is closer. MinigameManager picks the highest.
    private IEnumerator SendClosest(int actual)
    {
        for (int i = 0; i < phones; i++) {
            if (!submitted[i])
                continue;
            int distance = Mathf.Abs(guesses[i] - actual);
            MinigameManager.S.inputTimes[i] = int.MaxValue - distance;
        }

        // Wait a second to let the other phones see the prices
        yield return new WaitForSeconds(1f);

        MinigameManager.S.EndGame();
    }

    bool IsPlaying(int phone)
    {
        return GlobalVariables.S != null && GlobalVariables.S.IsLoggedIn(phone);
    }

    void ShowPrice(int phone, string raw)
    {
        if (MinigameManager.S == null || MinigameManager.S.inputDisplayTexts[phone] == null) return;
        MinigameManager.S.inputDisplayTexts[phone].text = FormatPrice(raw);
    }

    void SetDisplayColor(int phone, Color color)
    {
        if (MinigameManager.S == null) return;
        var box = MinigameManager.S.inputDisplayObjects[phone];
        if (box == null) return;
        box.GetComponent<Image>().color = color;
    }

    static int Cents(string raw)
    {
        int cents = 0;
        for (int i = 0; i < raw.Length; i++)
            cents = cents * 10 + (raw[i] - '0');
        return cents;
    }

    static string FormatCents(int cents)
    {
        return (cents / 100) + "." + (cents % 100).ToString("00");
    }

    void SpawnItem(PriceItem item)
    {
        if (itemPrefab == null) return;

        // Scene object: fill the one in the hierarchy. Prefab asset: spawn a copy.
        GameObject go = itemPrefab.scene.IsValid() ? itemPrefab : Instantiate(itemPrefab, transform);
        Image image = go.GetComponentInChildren<Image>();
        if (image != null)
            image.sprite = item.image;

        Text label = go.GetComponentInChildren<Text>();
        if (label != null)
            label.text = item.itemName;

        TextMeshProUGUI tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
            tmp.text = item.itemName;
    }

    // Digits shift in from the right of a fixed decimal: 2, 9, 9 → 2.99
    static string FormatPrice(string raw)
    {
        string cents = raw.Length >= 2 ? raw.Substring(raw.Length - 2) : raw.PadLeft(2, '0');
        string dollars = raw.Length > 2 ? raw.Substring(0, raw.Length - 2) : "0";
        return dollars + "." + cents;
    }
}
