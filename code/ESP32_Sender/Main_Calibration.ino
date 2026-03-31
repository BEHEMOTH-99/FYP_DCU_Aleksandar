#include <esp_now.h>
#include <WiFi.h>
#include <esp_wifi.h>
#include <Preferences.h> // For saving calibration to Flash

// --- PIN MAPPING (ESP32 Feather / S3) ---
const int pins[9] = {A0, A1, A2, A3, A4, A5, A6, A7, A8};

// --- DATA STRUCTURES ---
typedef struct struct_message {
  float angles[9]; // Sending pre-calculated degrees to Unity
  uint32_t timestamp;
} struct_message;

struct_message myData;
uint8_t broadcastAddress[] = {0x48, 0x27, 0xE2, 0x61, 0x91, 0x8C};
esp_now_peer_info_t peerInfo;
Preferences prefs;

// Calibration Tables: 
// 0, 1, 3, 4, 6, 7 are Vertical (4 points: 0, 30, 60, 90)
// 2, 5, 8 are Horizontal (2 points: 0, 30)
float calV[6][4]; // [joint_index][point_index]
float calH[3][2]; // [joint_index][point_index]

// Helper for mapping joint index to Table Index
int getJointType(int i) { return (i % 3 == 2) ? 1 : 0; } // 1 = Horizontal, 0 = Vertical
int getTableIdx(int i) { return (i % 3 == 2) ? (i / 3) : ((i / 3) * 2 + (i % 3)); }

void setup() {
  Serial.begin(115200);
  delay(1000);
  
  WiFi.mode(WIFI_STA);
  esp_wifi_set_channel(1, WIFI_SECOND_CHAN_NONE);
  
  if (esp_now_init() != ESP_OK) return;
  
  memcpy(peerInfo.peer_addr, broadcastAddress, 6);
  peerInfo.channel = 1;
  peerInfo.encrypt = false;
  esp_now_add_peer(&peerInfo);

  // Load from Flash
  prefs.begin("glove-cal", false);
  loadCalibration();

  Serial.println("--- GLOVE CALIBRATION READY ---");
  Serial.println("Commands: I0, I30, I60, I90 (Index Vertical)");
  Serial.println("Commands: Ih0, Ih30 (Index Horizontal)");
  Serial.println("Use M for Middle, R for Ring. Type 'save' to store memory.");
  Serial.println("Type 'export' for a CSV copy-paste for Excel.");
}

void loop() {
  // 1. READ & FILTER
  float raw[9];
  float alpha = 0.15;
  static float filtered[9];
  
  for(int i=0; i<9; i++) {
    raw[i] = analogRead(pins[i]);
    filtered[i] = (alpha * raw[i]) + ((1.0 - alpha) * filtered[i]);
  }

  // 2. INTERACTIVE CALIBRATION
  if (Serial.available() > 0) {
    String cmd = Serial.readStringUntil('\n');
    cmd.trim();
    parseCommand(cmd, filtered);
  }

  // 3. APPLY INTERPOLATION
  for(int i=0; i<9; i++) {
    if (getJointType(i) == 0) { // Vertical
      myData.angles[i] = interpolateV(getTableIdx(i), filtered[i]);
    } else { // Horizontal
      myData.angles[i] = interpolateH(getTableIdx(i), filtered[i]);
    }
  }
  
  myData.timestamp = millis();
  esp_now_send(broadcastAddress, (uint8_t *) &myData, sizeof(myData));
  
  delay(20);
}

// Simple Linear Spline Interpolation for Vertical (0, 30, 60, 90)
float interpolateV(int idx, float val) {
  float degs[4] = {0, 30, 60, 90};
  float* adcs = calV[idx];
  
  if (val <= adcs[0]) return degs[0];
  if (val >= adcs[3]) return degs[3];
  
  for (int i = 0; i < 3; i++) {
    if (val >= adcs[i] && val <= adcs[i+1]) {
      return degs[i] + (val - adcs[i]) * (degs[i+1] - degs[i]) / (adcs[i+1] - adcs[i]);
    }
  }
  return 0;
}

// Linear interpolation for Horizontal (0, 30)
float interpolateH(int idx, float val) {
  float degs[2] = {0, 30};
  float* adcs = calH[idx];
  if (val <= adcs[0]) return degs[0];
  if (val >= adcs[1]) return degs[1];
  return degs[0] + (val - adcs[0]) * (degs[1] - degs[0]) / (adcs[1] - adcs[0]);
}

void parseCommand(String cmd, float* current) {
  if (cmd == "save") { saveCalibration(); return; }
  if (cmd == "export") { exportToExcel(); return; }
  
  char f = cmd[0]; // I, M, R
  int fIdx = (f == 'I' || f == 'i') ? 0 : (f == 'M' || f == 'm') ? 1 : 2;
  
  bool horiz = false;
  int startIdx = 1;
  if (cmd[1] == 'h' || cmd[1] == 'H') { horiz = true; startIdx = 2; }
  
  int val = cmd.substring(startIdx).toInt();
  
  if (!horiz) { // Vertical
    int pIdx = (val == 0) ? 0 : (val == 30) ? 1 : (val == 60) ? 2 : 3;
    calV[fIdx*2][pIdx] = current[fIdx*3];     // PIP
    calV[fIdx*2 + 1][pIdx] = current[fIdx*3+1]; // MCP Curl
    Serial.printf("%c V-%d updated to %.1f\n", f, val, current[fIdx*3]);
  } else { // Horizontal
    int pIdx = (val == 0) ? 0 : 1;
    calH[fIdx][pIdx] = current[fIdx*3+2]; // MCP Splay
    Serial.printf("%c H-%d updated to %.1f\n", f, val, current[fIdx*3+2]);
  }
}

void saveCalibration() {
  prefs.putBytes("calV", &calV, sizeof(calV));
  prefs.putBytes("calH", &calH, sizeof(calH));
  Serial.println("Calibration Saved to Flash!");
}

void loadCalibration() {
  if (prefs.getBytesLength("calV") > 0) {
    prefs.getBytes("calV", &calV, sizeof(calV));
    prefs.getBytes("calH", &calH, sizeof(calH));
    Serial.println("Calibration Loaded successfully.");
  } else {
    // Defaults to prevent divide by zero
    for(int i=0; i<6; i++) for(int j=0; j<4; j++) calV[i][j] = (j+1) * 500;
    for(int i=0; i<3; i++) for(int j=0; j<2; j++) calH[i][j] = (j+1) * 1000;
  }
}

void exportToExcel() {
  Serial.println("\n--- START CSV EXPORT ---");
  Serial.println("Finger,Joint,Type,Angle,ADC");
  
  String fingerNames[3] = {"Index", "Middle", "Ring"};
  String jointNames[2] = {"PIP", "MCP_Curl"};
  float anglesV[4] = {0, 30, 60, 90};

  for(int f=0; f<3; f++) {
    // Verticals
    for(int j=0; j<2; j++) {
      for(int a=0; a<4; a++) {
        Serial.printf("%s,%s,Vertical,%.0f,%.1f\n", fingerNames[f].c_str(), jointNames[j].c_str(), anglesV[a], calV[f*2 + j][a]);
      }
    }
    // Horizontal
    Serial.printf("%s,MCP_Splay,Horizontal,0,%.1f\n", fingerNames[f].c_str(), calH[f][0]);
    Serial.printf("%s,MCP_Splay,Horizontal,30,%.1f\n", fingerNames[f].c_str(), calH[f][1]);
  }
  Serial.println("--- END CSV EXPORT ---");
}
