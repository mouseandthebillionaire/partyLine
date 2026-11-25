using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

// Teensy phoneStick joysticks → phone buttons. Same firmware on every board;
// phone index is assigned here (auto, or override via phoneDeviceIds).
public class PhoneInputManager : MonoBehaviour
{
    public static PhoneInputManager S;

    const int MaxPhones = 6;
    const int NumButtons = 15;

    [Tooltip("Log button-down events")]
    public bool logButtonPresses;

    // Slider level that counts as a yell. Whisper treats about 0.4 as too loud.
    public float yellThreshold = 0.3f;

    [Tooltip("Input System deviceId per phone (0 = auto-fill).")]
    public int[] phoneDeviceIds = new int[MaxPhones];

    // USB serial per slot, saved on this computer. Empty means the slot is not reserved.
    const string PrefKey = "partyLine.phoneKeys";
    string[] phoneKeys = new string[MaxPhones];

    // Settings screen sets this so a button press assigns a color instead of playing.
    public bool blockGameInput;

    // keyNames index → Teensy Joystick.button (1-based)
    static readonly int[] KeyIndexToTeensyButton =
    {
        10, // 0
        1, 2, 3, 4, 5, 6, 7, 8, 9,
        11, // *
        12, // #
        15, // pickup
        14, // hangup
        13, // yell
    };

    readonly InputDevice[] _devices = new InputDevice[MaxPhones];
    readonly AxisControl[] _sliders = new AxisControl[MaxPhones];
    readonly ButtonControl[][] _buttons = new ButtonControl[MaxPhones][];
    readonly bool[,] _wasHeld = new bool[MaxPhones, NumButtons];
    readonly bool[,] _isHeld = new bool[MaxPhones, NumButtons];
    readonly bool[,] _downThisFrame = new bool[MaxPhones, NumButtons];
    readonly bool[] _yellWas = new bool[MaxPhones];
    readonly bool[] _yellDown = new bool[MaxPhones];
    readonly List<InputDevice> _sticks = new List<InputDevice>();

    // Filled while documenting, for phones that have no handset plugged in.
    readonly float[] _demoLevel = new float[MaxPhones];
    readonly float[] _nextTap = new float[MaxPhones];
    readonly int[] _typed = new int[MaxPhones];
    readonly bool[] _tapped = new bool[MaxPhones];
    int _demoKind = -1;
    int _demoRound = -1;
    float _demoStart;
    float _ringStart = -1f;

    void Awake()
    {
        if (S != null)
        {
            Destroy(gameObject);
            return;
        }
        S = this;
        DontDestroyOnLoad(gameObject);

        if (phoneDeviceIds == null || phoneDeviceIds.Length != MaxPhones)
            phoneDeviceIds = new int[MaxPhones];

        for (int i = 0; i < MaxPhones; i++)
            _buttons[i] = new ButtonControl[NumButtons];

        LoadKeys();
        EnsurePhoneControls();
        if (GetComponent<GameSettings>() == null)
            gameObject.AddComponent<GameSettings>();
    }

    // The menu scene only had listeners for the first two colors. A handset moved
    // onto blue, orange, green, or black had nobody watching its login button.
    void EnsurePhoneControls()
    {
        PhoneControl[] existing = GetComponentsInChildren<PhoneControl>(true);
        var have = new bool[MaxPhones];
        for (int i = 0; i < existing.Length; i++)
        {
            int n = existing[i].phoneNum;
            if ((uint)n < MaxPhones)
                have[n] = true;
        }

        for (int i = 0; i < MaxPhones; i++)
        {
            if (have[i]) continue;
            var go = new GameObject("Phone_" + i);
            go.transform.SetParent(transform, false);
            go.AddComponent<PhoneControl>().phoneNum = i;
        }
    }

    void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
        RebuildDeviceMap();
    }

    void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    void OnDeviceChange(InputDevice _, InputDeviceChange change)
    {
        if (change is InputDeviceChange.Added or InputDeviceChange.Removed
            or InputDeviceChange.Reconnected or InputDeviceChange.Disconnected)
            RebuildDeviceMap();
    }

    int PhoneCount()
    {
        int n = GlobalVariables.S != null ? GlobalVariables.S.numPhones : MaxPhones;
        return Mathf.Clamp(n, 0, MaxPhones);
    }

    static bool IsStick(InputDevice d)
    {
        if (d is Keyboard or Mouse or Touchscreen) return false;
        if (d is Joystick or Gamepad) return true;
        foreach (var c in d.allControls)
        {
            if (c is ButtonControl b && b.name.StartsWith("button", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    static ButtonControl ResolveButton(InputDevice device, int teensyBtn)
    {
        // Joystick layout maps HID button 1 → "trigger"
        if (teensyBtn == 1)
        {
            var trigger = device.TryGetChildControl<ButtonControl>("trigger");
            if (trigger != null) return trigger;
        }

        return device.TryGetChildControl<ButtonControl>($"button{teensyBtn}")
            ?? device.TryGetChildControl<ButtonControl>($"button{teensyBtn - 1}");
    }

    void CollectSticks()
    {
        _sticks.Clear();
        foreach (var d in InputSystem.devices)
        {
            if (d.added && IsStick(d))
                _sticks.Add(d);
        }
    }

    void RebuildDeviceMap()
    {
        CollectSticks();

        Array.Clear(_devices, 0, _devices.Length);
        Array.Clear(_sliders, 0, _sliders.Length);
        for (int i = 0; i < MaxPhones; i++)
            Array.Clear(_buttons[i], 0, _buttons[i].Length);

        int want = PhoneCount();
        var used = new HashSet<int>();

        // Saved serials first, so a phone keeps its color across launches.
        for (int phone = 0; phone < want; phone++)
        {
            if (string.IsNullOrEmpty(phoneKeys[phone])) continue;
            var device = FindStickByKey(phoneKeys[phone]);
            if (device == null || used.Contains(device.deviceId)) continue;
            Take(phone, device, used);
        }

        // Then any device id already chosen this session (a swap that had no serial).
        for (int phone = 0; phone < want; phone++)
        {
            if (_devices[phone] != null) continue;
            int id = phoneDeviceIds[phone];
            if (id == 0) continue;
            var device = FindStick(id);
            if (device == null || used.Contains(device.deviceId)) continue;
            if (ReservedElsewhere(device, phone)) continue;
            Take(phone, device, used);
        }

        // Empty, unreserved slots pick up newly plugged phones.
        int si = 0;
        bool remembered = false;
        for (int phone = 0; phone < want; phone++)
        {
            if (_devices[phone] != null) continue;
            if (!string.IsNullOrEmpty(phoneKeys[phone])) continue;
            while (si < _sticks.Count && used.Contains(_sticks[si].deviceId)) si++;
            if (si >= _sticks.Count) break;
            var device = _sticks[si++];
            Take(phone, device, used);
            remembered |= RememberKey(phone, device);
        }

        if (remembered) SaveKeys();
    }

    void Take(int phone, InputDevice device, HashSet<int> used)
    {
        Assign(phone, device, used);
        phoneDeviceIds[phone] = device.deviceId;
    }

    bool RememberKey(int phone, InputDevice device)
    {
        string key = PersistableKey(device);
        if (key == null) return false;
        phoneKeys[phone] = key;
        return true;
    }

    bool ReservedElsewhere(InputDevice device, int phone)
    {
        string key = StableKey(device);
        if (key == null) return false;
        for (int i = 0; i < MaxPhones; i++)
        {
            if (i != phone && phoneKeys[i] == key) return true;
        }
        return false;
    }

    // Puts this handset on a color. The phone that was there takes the freed slot.
    public void AssignDeviceToPhone(InputDevice device, int phone)
    {
        if (device == null || (uint)phone >= MaxPhones) return;

        int previous = PhoneForDevice(device);
        if (previous == phone) return;

        string incomingKey = PersistableKey(device) ?? "";
        string displacedKey = phoneKeys[phone] ?? "";
        int displacedId = _devices[phone] != null ? _devices[phone].deviceId : 0;

        phoneKeys[phone] = incomingKey;
        phoneDeviceIds[phone] = device.deviceId;

        if (previous >= 0)
        {
            phoneKeys[previous] = displacedKey;
            phoneDeviceIds[previous] = displacedId;
        }

        SaveKeys();
        RebuildDeviceMap();
    }

    public void ClearSavedMappings()
    {
        for (int i = 0; i < MaxPhones; i++)
        {
            phoneKeys[i] = "";
            phoneDeviceIds[i] = 0;
        }
        SaveKeys();
        RebuildDeviceMap();
    }

    public InputDevice DeviceForPhone(int phone)
    {
        if ((uint)phone >= MaxPhones) return null;
        return _devices[phone];
    }

    public int PhoneForDevice(InputDevice device)
    {
        if (device == null) return -1;
        for (int i = 0; i < MaxPhones; i++)
            if (_devices[i] == device) return i;
        return -1;
    }

    public InputDevice PressedStick()
    {
        for (int i = 0; i < _sticks.Count; i++)
        {
            InputDevice device = _sticks[i];
            if (device == null) continue;
            foreach (var control in device.allControls)
            {
                if (control is ButtonControl button && button.wasPressedThisFrame)
                    return device;
            }
        }
        return null;
    }

    // True when every connected handset has its own USB serial, so a swap can be saved.
    public bool MappingsCanPersist
    {
        get
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < _sticks.Count; i++)
            {
                string key = StableKey(_sticks[i]);
                if (key == null || !seen.Add(key)) return false;
            }
            return true;
        }
    }

    void LoadKeys()
    {
        phoneKeys = new string[MaxPhones];
        string[] parts = PlayerPrefs.GetString(PrefKey, "").Split('\n');
        for (int i = 0; i < MaxPhones; i++)
            phoneKeys[i] = i < parts.Length ? parts[i] : "";
    }

    void SaveKeys()
    {
        var joined = new System.Text.StringBuilder();
        for (int i = 0; i < MaxPhones; i++)
        {
            if (i > 0) joined.Append('\n');
            joined.Append(phoneKeys[i] ?? "");
        }

        string next = joined.ToString();
        if (PlayerPrefs.GetString(PrefKey, "") == next) return;
        PlayerPrefs.SetString(PrefKey, next);
        PlayerPrefs.Save();
    }

    static string StableKey(InputDevice device)
    {
        if (device == null) return null;
        string serial = device.description.serial;
        if (string.IsNullOrWhiteSpace(serial))
            serial = ReadJsonString(device.description.capabilities, "serial");
        if (string.IsNullOrWhiteSpace(serial)) return null;
        return serial.Trim();
    }

    string PersistableKey(InputDevice device)
    {
        string key = StableKey(device);
        if (key == null || !KeyIsUnique(key)) return null;
        return key;
    }

    bool KeyIsUnique(string key)
    {
        int found = 0;
        for (int i = 0; i < _sticks.Count; i++)
            if (StableKey(_sticks[i]) == key) found++;
        return found == 1;
    }

    InputDevice FindStickByKey(string key)
    {
        for (int i = 0; i < _sticks.Count; i++)
            if (StableKey(_sticks[i]) == key) return _sticks[i];
        return null;
    }

    static string ReadJsonString(string json, string field)
    {
        if (string.IsNullOrEmpty(json)) return null;
        string token = "\"" + field + "\":\"";
        int start = json.IndexOf(token, StringComparison.Ordinal);
        if (start < 0) return null;
        start += token.Length;
        int end = json.IndexOf('"', start);
        if (end < 0) return null;
        return json.Substring(start, end - start);
    }

    void Assign(int phone, InputDevice device, HashSet<int> used)
    {
        _devices[phone] = device;
        used.Add(device.deviceId);
        _sliders[phone] = device.TryGetChildControl<AxisControl>("slider");
        for (int k = 0; k < NumButtons; k++)
            _buttons[phone][k] = ResolveButton(device, KeyIndexToTeensyButton[k]);

        if (_sliders[phone] == null)
            Debug.LogWarning($"[PhoneInput] phone {phone} has no slider axis");
    }

    InputDevice FindStick(int deviceId)
    {
        foreach (var d in _sticks)
            if (d.deviceId == deviceId) return d;
        return null;
    }

    void Update()
    {
        if (DocumentingNow())
            PlanDemo();

        int phones = PhoneCount();
        string[] keyNames = GlobalVariables.S != null ? GlobalVariables.S.keyNames : null;

        for (int phone = 0; phone < MaxPhones; phone++)
        {
            for (int b = 0; b < NumButtons; b++)
            {
                _wasHeld[phone, b] = _isHeld[phone, b];
                _isHeld[phone, b] = false;
                _downThisFrame[phone, b] = false;
            }

            bool loud = phone < phones && GetSlider(phone) >= yellThreshold;
            _yellDown[phone] = loud && !_yellWas[phone];
            _yellWas[phone] = loud;

            if (phone >= phones || _devices[phone] == null) continue;

            for (int key = 0; key < NumButtons; key++)
            {
                bool held = _buttons[phone][key] != null && _buttons[phone][key].isPressed;
                _isHeld[phone, key] = held;
                _downThisFrame[phone, key] = held && !_wasHeld[phone, key];

                if (_downThisFrame[phone, key] && logButtonPresses)
                {
                    string name = keyNames != null && key < keyNames.Length ? keyNames[key] : key.ToString();
                    Debug.Log($"[PhoneInput] phone {phone}  {name}");
                }
            }
        }

        if (DocumentingNow())
            PlayDemoButtons();
    }

    public bool GetButtonDown(int phoneNum, int buttonIndex)
    {
        if (blockGameInput) return false;
        if ((uint)phoneNum >= MaxPhones || (uint)buttonIndex >= NumButtons) return false;
        return _downThisFrame[phoneNum, buttonIndex];
    }

    public bool GetButton(int phoneNum, int buttonIndex)
    {
        if (blockGameInput) return false;
        if ((uint)phoneNum >= MaxPhones || (uint)buttonIndex >= NumButtons) return false;
        return _isHeld[phoneNum, buttonIndex];
    }

    public bool IsYelling(int phoneNum)
    {
        if (blockGameInput) return false;
        return GetSlider(phoneNum) >= yellThreshold;
    }

    // True on the frame the slider crosses yellThreshold.
    public bool YellStarted(int phoneNum)
    {
        if (blockGameInput) return false;
        if ((uint)phoneNum >= MaxPhones) return false;
        return _yellDown[phoneNum];
    }

    // HID presents the 10-bit slider as -1..1, with silence at -1. Map back to 0..1.
    public float GetSlider(int phoneNum)
    {
        if ((uint)phoneNum >= MaxPhones) return 0f;
        if (DocumentingNow() && PhoneIn(phoneNum) && _sliders[phoneNum] == null)
            return _demoLevel[phoneNum];
        var axis = _sliders[phoneNum];
        if (axis == null) return 0f;
        return Mathf.Clamp01((axis.ReadUnprocessedValue() + 1f) * 0.5f);
    }

    public bool HasDevice(int phoneNum)
    {
        if ((uint)phoneNum >= MaxPhones) return false;
        return _devices[phoneNum] != null;
    }

    enum DocKind
    {
        Idle,
        YellHold,
        Whisper,
        Blow,
        YellPulse,
        Mash,
        Draw,
        Maths,
        Quiz,
        Type,
        Price,
        Bugs
    }

    bool DocumentingNow()
    {
        return !blockGameInput
            && GameManager.S != null
            && GameManager.S.documenting
            && GameManager.S.currentState == GameManager.State.MINIGAME;
    }

    bool PhoneIn(int phone)
    {
        return GlobalVariables.S != null && GlobalVariables.S.IsLoggedIn(phone);
    }

    void PlanDemo()
    {
        int kind = DetectKind();
        if (kind != _demoKind)
        {
            _demoKind = kind;
            _demoRound = -1;
            _demoStart = Time.time;
            _ringStart = -1f;
            for (int i = 0; i < MaxPhones; i++)
            {
                _nextTap[i] = 0f;
                _typed[i] = 0;
                _tapped[i] = false;
                _demoLevel[i] = 0f;
            }
        }

        int phones = PhoneCount();
        for (int phone = 0; phone < phones; phone++)
        {
            if (!PhoneIn(phone) || _sliders[phone] != null)
            {
                _demoLevel[phone] = 0f;
                continue;
            }
            _demoLevel[phone] = DemoLevel(phone);
        }
    }

    void PlayDemoButtons()
    {
        int phones = PhoneCount();
        var kind = (DocKind)_demoKind;
        var mash = kind == DocKind.Mash ? FindAnyObjectByType<ButtonMasher>() : null;
        var draw = kind == DocKind.Draw ? FindAnyObjectByType<FastestDraw>() : null;
        var maths = kind == DocKind.Maths ? FindAnyObjectByType<Maths>() : null;
        var quiz = kind == DocKind.Quiz ? FindAnyObjectByType<Quizshow>() : null;
        var typing = kind == DocKind.Type ? FindAnyObjectByType<TTT>() : null;

        for (int phone = 0; phone < phones; phone++)
        {
            if (!PhoneIn(phone) || _devices[phone] != null) continue;
            switch (kind)
            {
                case DocKind.Blow:
                    if (Due(phone, 1.15f + phone * 0.22f))
                        Tap(phone, 11);
                    break;
                case DocKind.Mash:
                    PlayMash(phone, mash);
                    break;
                case DocKind.Draw:
                    PlayDraw(phone, draw);
                    break;
                case DocKind.Maths:
                    if (maths != null)
                        PlayAnswer(phone, maths.AnswerKey, maths.Round, maths.Accepting);
                    break;
                case DocKind.Quiz:
                    if (quiz != null)
                        PlayAnswer(phone, quiz.AnswerKey, quiz.Round, quiz.Accepting);
                    break;
                case DocKind.Type:
                    PlayType(phone, typing);
                    break;
                case DocKind.Price:
                    PlayPrice(phone);
                    break;
                case DocKind.Bugs:
                    if (Due(phone, 0.32f + phone * 0.05f))
                        Tap(phone, Random.Range(0, 12));
                    break;
            }
        }
    }

    int DetectKind()
    {
        if (FindAnyObjectByType<Yeeell>() != null) return (int)DocKind.YellHold;
        if (FindAnyObjectByType<Whisper>() != null) return (int)DocKind.Whisper;
        if (FindAnyObjectByType<balloonBlow>() != null) return (int)DocKind.Blow;
        if (FindAnyObjectByType<Skiier>() != null
            || FindAnyObjectByType<DDRawr>() != null
            || FindAnyObjectByType<SayIt>() != null)
            return (int)DocKind.YellPulse;
        if (FindAnyObjectByType<ButtonMasher>() != null) return (int)DocKind.Mash;
        if (FindAnyObjectByType<FastestDraw>() != null) return (int)DocKind.Draw;
        if (FindAnyObjectByType<Maths>() != null) return (int)DocKind.Maths;
        if (FindAnyObjectByType<Quizshow>() != null) return (int)DocKind.Quiz;
        if (FindAnyObjectByType<TTT>() != null) return (int)DocKind.Type;
        if (FindAnyObjectByType<PriceMatch>() != null) return (int)DocKind.Price;
        if (FindAnyObjectByType<BugSquasher>() != null) return (int)DocKind.Bugs;
        return (int)DocKind.Idle;
    }

    float DemoLevel(int phone)
    {
        switch ((DocKind)_demoKind)
        {
            case DocKind.YellHold:
                return Time.time < _demoStart + 1.4f + phone * 0.55f ? 0.9f : 0f;
            case DocKind.Whisper:
                float band = 0.14f + phone * 0.025f;
                float wobble = Mathf.Sin(Time.time * (1.2f + phone * 0.17f)) * 0.025f;
                return Mathf.Clamp(band + wobble, 0.12f, 0.32f);
            case DocKind.Blow:
                float wave = Mathf.Sin(Time.time * (0.65f + phone * 0.13f) + phone * 1.3f);
                return Mathf.Lerp(0.08f, 0.72f, wave * 0.5f + 0.5f);
            case DocKind.YellPulse:
                float period = 0.7f + phone * 0.11f;
                float phase = Mathf.Repeat(Time.time + phone * 0.19f, period);
                return phase < 0.2f ? 0.92f : 0.04f;
            default:
                return 0f;
        }
    }

    void PlayMash(int phone, ButtonMasher game)
    {
        if (game == null || game.number == null) return;
        if (!int.TryParse(game.number.text, out int key)) return;
        if (Due(phone, 0.09f + phone * 0.02f))
            Tap(phone, Mathf.Clamp(key, 0, 9));
    }

    void PlayDraw(int phone, FastestDraw game)
    {
        if (game == null || game.ring == null || !game.ring.isPlaying)
        {
            _ringStart = -1f;
            return;
        }

        if (_ringStart < 0f)
            _ringStart = Time.time;
        if (_tapped[phone]) return;
        if (Time.time < _ringStart + 0.08f + phone * 0.12f) return;
        _tapped[phone] = true;
        Tap(phone, 12);
    }

    void PlayAnswer(int phone, int answer, int round, bool accepting)
    {
        if (round != _demoRound)
        {
            _demoRound = round;
            _demoStart = Time.time;
            for (int i = 0; i < MaxPhones; i++)
                _tapped[i] = false;
        }

        if (!accepting || _tapped[phone]) return;
        int players = PhoneCount();
        if (players <= 0) return;

        bool mine = phone == Mod(answer, players);
        // Wrong guesses land first. The phone that has the answer follows them.
        if (Time.time < _demoStart + (mine ? 0.95f : 0.4f + phone * 0.06f)) return;
        _tapped[phone] = true;
        Tap(phone, Mathf.Clamp(mine ? answer : (answer + 1 + phone) % 10, 0, 9));
    }

    void PlayType(int phone, TTT game)
    {
        if (game == null) return;
        int key = game.NextKey(phone);
        if (key < 0) return;
        if (Due(phone, 0.32f + phone * 0.06f))
            Tap(phone, Mathf.Clamp(key, 0, 9));
    }

    void PlayPrice(int phone)
    {
        if (!Due(phone, 0.38f)) return;
        if (_typed[phone] < 3)
        {
            Tap(phone, (_typed[phone] * 3 + phone) % 10);
            _typed[phone]++;
            return;
        }

        if (_typed[phone] == 3)
        {
            Tap(phone, 11);
            _typed[phone]++;
        }
    }

    void Tap(int phone, int key)
    {
        if ((uint)phone >= MaxPhones || (uint)key >= NumButtons) return;
        _downThisFrame[phone, key] = true;
        _isHeld[phone, key] = true;
    }

    bool Due(int phone, float interval)
    {
        if (Time.time < _nextTap[phone]) return false;
        _nextTap[phone] = Time.time + interval;
        return true;
    }

    static int Mod(int value, int length)
    {
        if (length <= 0) return 0;
        int m = value % length;
        return m < 0 ? m + length : m;
    }
}
