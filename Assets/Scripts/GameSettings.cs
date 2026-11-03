using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// F1 opens a screen that points each handset at a color. The choice is saved on this computer.
public class GameSettings : MonoBehaviour
{
    public Key toggleKey = Key.F1;

    // Same order as GlobalVariables.phoneColors.
    static readonly string[] ColorNames = { "Red", "Yellow", "Blue", "Orange", "Green", "Black" };

    Canvas canvas;
    Text hint;
    Text note;
    Image[] rowBackgrounds;
    Image[] swatches;
    Text[] labels;
    Text[] statuses;
    InputDevice heard;
    bool open;

    void Awake()
    {
        rowBackgrounds = new Image[ColorNames.Length];
        swatches = new Image[ColorNames.Length];
        labels = new Text[ColorNames.Length];
        statuses = new Text[ColorNames.Length];
        Build();
        SetOpen(false);
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard[toggleKey].wasPressedThisFrame)
            SetOpen(!open);

        if (!open || PhoneInputManager.S == null) return;

        InputDevice pressed = PhoneInputManager.S.PressedStick();
        if (pressed == null) return;
        heard = pressed;
        Refresh();
    }

    void SetOpen(bool value)
    {
        open = value;
        if (canvas != null)
            canvas.gameObject.SetActive(open);
        if (PhoneInputManager.S != null)
            PhoneInputManager.S.blockGameInput = open;
        if (!open) return;

        heard = null;
        Refresh();
    }

    void Choose(int phone)
    {
        if (heard == null || PhoneInputManager.S == null)
        {
            if (hint != null)
                hint.text = "Press a button on the phone first.";
            return;
        }

        PhoneInputManager.S.AssignDeviceToPhone(heard, phone);
        heard = null;
        Refresh();
    }

    void ResetMappings()
    {
        heard = null;
        if (PhoneInputManager.S != null)
            PhoneInputManager.S.ClearSavedMappings();
        Refresh();
    }

    void Refresh()
    {
        if (hint == null) return;

        int heardSlot = PhoneInputManager.S != null ? PhoneInputManager.S.PhoneForDevice(heard) : -1;
        if (heard == null)
            hint.text = "Press any button on a phone, then click the color it should be.";
        else if (heardSlot >= 0)
            hint.text = "That phone is " + ColorNames[heardSlot] + ". Click the color it should be.";
        else
            hint.text = "Click the color that phone should be.";

        bool persist = PhoneInputManager.S == null || PhoneInputManager.S.MappingsCanPersist;
        note.text = persist
            ? "Saved on this computer. It stays the next time you open the game."
            : "These phones look the same to the computer, so this lasts until you quit.";

        for (int i = 0; i < ColorNames.Length; i++)
        {
            Color color = PhoneColor(i);
            swatches[i].color = color;
            labels[i].text = ColorNames[i] + "   " + PhoneName(i);

            InputDevice device = PhoneInputManager.S != null ? PhoneInputManager.S.DeviceForPhone(i) : null;
            bool selected = heard != null && device == heard;
            if (selected)
                statuses[i].text = "this phone";
            else if (heard != null)
                statuses[i].text = "click to use";
            else if (device != null)
                statuses[i].text = "connected";
            else
                statuses[i].text = "empty";

            rowBackgrounds[i].color = selected
                ? new Color(0.28f, 0.28f, 0.32f, 1f)
                : new Color(0.12f, 0.12f, 0.14f, 1f);
        }
    }

    static Color PhoneColor(int phone)
    {
        if (GlobalVariables.S != null && GlobalVariables.S.phoneColors != null
            && (uint)phone < (uint)GlobalVariables.S.phoneColors.Length)
            return GlobalVariables.S.phoneColors[phone];
        return Color.white;
    }

    static string PhoneName(int phone)
    {
        if (GlobalVariables.S != null && GlobalVariables.S.phoneNames != null
            && (uint)phone < (uint)GlobalVariables.S.phoneNames.Length)
            return GlobalVariables.S.phoneNames[phone];
        return "";
    }

    void Build()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        var root = new GameObject("GameSettings", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Stretch(NewImage(root.transform, "Dim", new Color(0f, 0f, 0f, 0.72f)).rectTransform);

        var panel = NewImage(root.transform, "Panel", new Color(0.05f, 0.05f, 0.06f, 1f));
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(920f, 860f);

        var column = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        column.padding = new RectOffset(36, 36, 32, 28);
        column.spacing = 12f;
        column.childAlignment = TextAnchor.UpperCenter;
        column.childControlWidth = true;
        column.childControlHeight = true;
        column.childForceExpandWidth = true;
        column.childForceExpandHeight = false;

        Text title = AddText(panel.transform, font, 40, FontStyle.Bold, TextAnchor.MiddleLeft);
        title.text = "Phone colors";
        Preferred(title, 52f);

        hint = AddText(panel.transform, font, 22, FontStyle.Normal, TextAnchor.MiddleLeft);
        Preferred(hint, 64f);

        for (int i = 0; i < ColorNames.Length; i++)
            BuildRow(panel.transform, font, i);

        note = AddText(panel.transform, font, 18, FontStyle.Italic, TextAnchor.MiddleLeft);
        note.color = new Color(0.75f, 0.75f, 0.75f, 1f);
        Preferred(note, 48f);

        var actions = new GameObject("Actions", typeof(RectTransform));
        actions.transform.SetParent(panel.transform, false);
        var actionRow = actions.AddComponent<HorizontalLayoutGroup>();
        actionRow.spacing = 16f;
        actionRow.childAlignment = TextAnchor.MiddleCenter;
        actionRow.childControlWidth = true;
        actionRow.childControlHeight = true;
        actionRow.childForceExpandWidth = true;
        actionRow.childForceExpandHeight = true;
        Preferred(actions.AddComponent<LayoutElement>(), 56f);

        BuildAction(actions.transform, font, "Reset", ResetMappings);
        BuildAction(actions.transform, font, "Close", () => SetOpen(false));
    }

    void BuildRow(Transform parent, Font font, int phone)
    {
        var row = NewImage(parent, "Phone" + phone, new Color(0.12f, 0.12f, 0.14f, 1f));
        rowBackgrounds[phone] = row;
        Preferred(row.gameObject.AddComponent<LayoutElement>(), 78f);

        var button = row.gameObject.AddComponent<Button>();
        button.targetGraphic = row;
        int slot = phone;
        button.onClick.AddListener(() => Choose(slot));

        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(16, 18, 12, 12);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        var swatch = NewImage(row.transform, "Swatch", Color.white);
        swatch.raycastTarget = false;
        swatches[phone] = swatch;
        LayoutElement swatchSize = swatch.gameObject.AddComponent<LayoutElement>();
        swatchSize.preferredWidth = 54f;
        swatchSize.preferredHeight = 54f;
        var edge = swatch.gameObject.AddComponent<Outline>();
        edge.effectColor = new Color(1f, 1f, 1f, 0.9f);
        edge.effectDistance = new Vector2(1.5f, -1.5f);

        labels[phone] = AddText(row.transform, font, 26, FontStyle.Bold, TextAnchor.MiddleLeft);
        labels[phone].gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        statuses[phone] = AddText(row.transform, font, 20, FontStyle.Normal, TextAnchor.MiddleRight);
        statuses[phone].color = new Color(0.8f, 0.8f, 0.8f, 1f);
        statuses[phone].gameObject.AddComponent<LayoutElement>().preferredWidth = 220f;
    }

    void BuildAction(Transform parent, Font font, string label, UnityEngine.Events.UnityAction onClick)
    {
        var image = NewImage(parent, label, new Color(0.2f, 0.2f, 0.22f, 1f));
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        Text text = AddText(image.transform, font, 22, FontStyle.Bold, TextAnchor.MiddleCenter);
        text.text = label;
        Stretch(text.rectTransform);
    }

    static Image NewImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    static Text AddText(Transform parent, Font font, int size, FontStyle style, TextAnchor align)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = align;
        text.color = Color.white;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    static void Preferred(Component item, float height)
    {
        LayoutElement element = item.GetComponent<LayoutElement>();
        if (element == null)
            element = item.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = height;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
