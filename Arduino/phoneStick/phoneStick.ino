#include <Audio.h>
#include <Wire.h>
#include <SPI.h>
#include <SerialFlash.h>
#include <Keypad.h>

// Same sketch on every Teensy. Assign phone index in Unity (SerialInputManager).
// USB Type must include Joystick, e.g.:
//   Tools → USB Type → Serial + Keyboard + Mouse + Joystick
//
// Mapping (each Teensy = one joystick device):
//   Buttons 1-9  = digits 1-9
//   Button  10   = digit 0
//   Button  11   = *
//   Button  12   = #
//   Button  13   = yell
//   Button  14   = pickup   (+ hat North pulse)
//   Button  15   = hangup   (+ hat South pulse)

// GUItool: begin automatically generated code
AudioSynthWaveformSine   sine1;
AudioSynthWaveformSine   sine2;
AudioPlayMemory          playMem1;
AudioInputI2S            i2s1;
AudioMixer4              mixer1;
AudioAnalyzeRMS          rms1;
AudioOutputI2S           i2s2;
AudioConnection          patchCord1(sine1, 0, mixer1, 1);
AudioConnection          patchCord2(sine2, 0, mixer1, 2);
AudioConnection          patchCord3(playMem1, 0, mixer1, 3);
AudioConnection          patchCord4(i2s1, 0, mixer1, 0);
AudioConnection          patchCord5(i2s1, 0, rms1, 0);
AudioConnection          patchCord6(mixer1, 0, i2s2, 0);
AudioConnection          patchCord7(mixer1, 0, i2s2, 1);
AudioControlSGTL5000     sgtl5000_1;
// GUItool: end automatically generated code

float yellThreshold = 0.3;
float toneVol = 0.5;

unsigned long prevMillis1 = 0;
const long interval1 = 300;

bool dialToneOn = false;
bool busySignalOn = false;
bool yellHeld = false;

int receiverState;

const int hangupPin = 10;

const byte ROWS = 4;
const byte COLS = 3;
byte keys[ROWS][COLS] = {
  {1, 2, 3},
  {4, 5, 6},
  {7, 8, 9},
  {10, 11, 12}, // *  0  #
};

// Joystick button numbers (Teensy is 1-based)
const uint8_t BTN_DIGIT0 = 10;
const uint8_t BTN_STAR   = 11;
const uint8_t BTN_POUND  = 12;
const uint8_t BTN_YELL   = 13;
const uint8_t BTN_PICKUP = 14;
const uint8_t BTN_HANGUP = 15;

#define PHONE_BLUE   0
#define PHONE_YELLOW 1
#define PHONE_GREEN  2
#define PHONE_RED    3
#define PHONE_ORANGE 4
#define PHONE_WIRING PHONE_BLUE

#if PHONE_WIRING == PHONE_BLUE
byte rowPins[ROWS] = {5, 6, 7, 8};
byte colPins[COLS] = {4, 3, 2};
#elif PHONE_WIRING == PHONE_YELLOW
byte rowPins[ROWS] = {5, 4, 3, 2};
byte colPins[COLS] = {6, 7, 8};
#elif PHONE_WIRING == PHONE_GREEN
byte rowPins[ROWS] = {6, 5, 4, 3};
byte colPins[COLS] = {0, 1, 2};
#elif PHONE_WIRING == PHONE_RED
byte rowPins[ROWS] = {8, 6, 5, 4};
byte colPins[COLS] = {2, 3, 7};
#else
byte rowPins[ROWS] = {3, 4, 5, 6};
byte colPins[COLS] = {2, 1, 0};
#endif

Keypad keypad = Keypad(makeKeymap(keys), rowPins, colPins, ROWS, COLS);

// Matrix id 1-12 → joystick button 1-12 (with 0/*/# remapped)
uint8_t buttonForKey(byte key) {
  if (key >= 1 && key <= 9) return key;          // 1-9
  if (key == 11) return BTN_DIGIT0;              // 0
  if (key == 10) return BTN_STAR;                // *
  if (key == 12) return BTN_POUND;               // #
  return 0;
}

void setButton(uint8_t button, bool pressed) {
  if (button == 0) return;
  Joystick.button(button, pressed);
}

void clearAllButtons() {
  for (uint8_t b = 1; b <= 15; b++)
    Joystick.button(b, false);
  Joystick.hat(-1);
}

// Short press so Unity GetButtonDown / hat bindings see an edge
void pulseButton(uint8_t button, int hatDeg) {
  setButton(button, true);
  if (hatDeg >= 0) Joystick.hat(hatDeg);
  delay(40);
  setButton(button, false);
  Joystick.hat(-1);
}

void onKeypadEvent(KeypadEvent key) {
  uint8_t btn = buttonForKey(key);
  if (btn == 0) return;

  switch (keypad.getState()) {
    case PRESSED:
    case HOLD:
      setButton(btn, true);
      Serial.print(F("joy btn "));
      Serial.print(btn);
      Serial.println(F(" DOWN"));
      break;
    case RELEASED:
      setButton(btn, false);
      Serial.print(F("joy btn "));
      Serial.print(btn);
      Serial.println(F(" UP"));
      break;
    case IDLE:
    default:
      break;
  }
}

void setup() {
  Serial.begin(9600);
  while (!Serial && millis() < 2000) {}

  pinMode(hangupPin, INPUT_PULLUP);
  receiverState = 1;

  keypad.addEventListener(onKeypadEvent);

  Joystick.useManualSend(false);
  clearAllButtons();

  Serial.print(F("phoneStick  PHONE_WIRING="));
  Serial.println(PHONE_WIRING);

  AudioMemory(15);
  sgtl5000_1.enable();
  sgtl5000_1.volume(0.5);
  sgtl5000_1.inputSelect(AUDIO_INPUT_MIC);
  sgtl5000_1.micGain(30);

  mixer1.gain(0, 0.4);
  mixer1.gain(1, 0.3);
  mixer1.gain(2, 0.3);
  mixer1.gain(3, 0.4);

  delay(1000);
}

void loop() {
  signalTone();
  checkRMS();
  pickupHangup();
  readKeypad();
}

void readKeypad() {
  int dtmfCol[] = {440, 1209, 1336, 1477, 1209, 1336, 1477, 1209, 1336, 1477, 1209, 1336, 1477};
  int dtmfRow[] = {440, 697, 770, 852, 941, 697, 770, 852, 941, 697, 770, 852, 941};

  // Drives Keypad state machine + event listener (press/release → joystick)
  byte key = keypad.getKey();

  if (key) {
    sine1.frequency(dtmfCol[key]);
    sine2.frequency(dtmfRow[key]);
    dialToneOn = false;
  }

  if (keypad.getState()) {
    sine1.amplitude(toneVol);
    sine2.amplitude(toneVol);
  } else if (!dialToneOn) {
    sine1.amplitude(0.0);
    sine2.amplitude(0.0);
  }
}

void pickupHangup() {
  if (digitalRead(hangupPin) == LOW && receiverState == 1) {
    pulseButton(BTN_HANGUP, 180); // hat down
    Serial.println(F("hangup → btn 15 + hat DOWN"));
    receiverState = 0;
    dialToneOn = false;
    busySignalOn = false;
  } else if (digitalRead(hangupPin) == HIGH && receiverState == 0) {
    pulseButton(BTN_PICKUP, 0); // hat up
    Serial.println(F("pickup → btn 14 + hat UP"));
    receiverState = 1;
    dialToneOn = true;
  }
}

void checkRMS() {
  unsigned long now = millis();
  if (now - prevMillis1 < interval1) return;
  prevMillis1 = now;

  if (!rms1.available()) return;
  float rms = rms1.read();
  bool yelling = rms > yellThreshold;

  if (yelling != yellHeld) {
    yellHeld = yelling;
    setButton(BTN_YELL, yelling);
    if (yelling) {
      Serial.print(F("yell → btn "));
      Serial.println(BTN_YELL);
      Serial.println(rms);
    }
  }
}

void signalTone() {
  if (dialToneOn) {
    sine1.amplitude(0.8);
    sine2.amplitude(0.8);
    sine1.frequency(350);
    sine2.frequency(440);
  }
  if (busySignalOn) {
    sine1.frequency(480);
    sine2.frequency(620);
    sine1.amplitude(0.7);
    sine2.amplitude(0.7);
    delay(500);
    sine1.amplitude(0.0);
    sine2.amplitude(0.0);
    delay(500);
  }
}
