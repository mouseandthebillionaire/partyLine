using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Rendering;

// Teensy phoneStick joysticks → phone buttons. Same firmware on every board;
// phone index is assigned here (auto, or override via phoneDeviceIds).
public class PhoneInputManager : MonoBehaviour
{
    public static PhoneInputManager S;

    const int MaxPhones = 6;
    const int NumButtons = 15;

    [Tooltip("Log button-down events")]
    public bool logButtonPresses;

    [Tooltip("Input System deviceId per phone (0 = auto-fill).")]
    public int[] phoneDeviceIds = new int[MaxPhones];

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
    readonly ButtonControl[][] _buttons = new ButtonControl[MaxPhones][];
    readonly bool[,] _wasHeld = new bool[MaxPhones, NumButtons];
    readonly bool[,] _isHeld = new bool[MaxPhones, NumButtons];
    readonly bool[,] _downThisFrame = new bool[MaxPhones, NumButtons];
    readonly List<InputDevice> _sticks = new List<InputDevice>();

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
        for (int i = 0; i < MaxPhones; i++)
            Array.Clear(_buttons[i], 0, _buttons[i].Length);

        int want = PhoneCount();
        var used = new HashSet<int>();

        for (int phone = 0; phone < want; phone++)
        {
            int id = phoneDeviceIds[phone];
            if (id == 0) continue;
            var device = FindStick(id);
            if (device == null) continue;
            Assign(phone, device, used);
        }

        int si = 0;
        for (int phone = 0; phone < want; phone++)
        {
            if (_devices[phone] != null) continue;
            while (si < _sticks.Count && used.Contains(_sticks[si].deviceId)) si++;
            if (si >= _sticks.Count) break;
            var device = _sticks[si++];
            phoneDeviceIds[phone] = device.deviceId;
            Assign(phone, device, used);
        }
    }

    void Assign(int phone, InputDevice device, HashSet<int> used)
    {
        _devices[phone] = device;
        used.Add(device.deviceId);
        for (int k = 0; k < NumButtons; k++)
            _buttons[phone][k] = ResolveButton(device, KeyIndexToTeensyButton[k]);
    }

    InputDevice FindStick(int deviceId)
    {
        foreach (var d in _sticks)
            if (d.deviceId == deviceId) return d;
        return null;
    }

    void Update()
    {
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
    }

    public bool GetButtonDown(int phoneNum, int buttonIndex)
    {
        if ((uint)phoneNum >= MaxPhones || (uint)buttonIndex >= NumButtons) return false;
        return _downThisFrame[phoneNum, buttonIndex];
    }

    public bool GetButton(int phoneNum, int buttonIndex)
    {
        if ((uint)phoneNum >= MaxPhones || (uint)buttonIndex >= NumButtons) return false;
        return _isHeld[phoneNum, buttonIndex];
    }

    public bool HasDevice(int phoneNum)
    {
        if ((uint)phoneNum >= MaxPhones) return false;
        return _devices[phoneNum] != null;
    }
}
