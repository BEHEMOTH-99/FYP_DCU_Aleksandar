#include <esp_now.h>
#include <WiFi.h>

// Potentiometer pins on the ESP32 Feather
const int potPin1 = A0;
const int potPin2 = A1;
const int potPin3 = A2;

// REPLACE THIS with the MAC Address of your ESP32-S3 Receiver
uint8_t broadcastAddress[] = {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF};

typedef struct struct_message {
  int pot1;
  int pot2;
  int pot3;
} struct_message;

struct_message myData;
esp_now_peer_info_t peerInfo;

// Callback when data is sent
void OnDataSent(const uint8_t *mac_addr, esp_now_send_status_t status) {
  // Serial.println(status == ESP_NOW_SEND_SUCCESS ? "Delivery Success" : "Delivery Fail");
}

void setup() {
  Serial.begin(115200);
  
  // Set device as a Wi-Fi Station
  WiFi.mode(WIFI_STA);

  // Init ESP-NOW
  if (esp_now_init() != ESP_OK) {
    Serial.println("Error initializing ESP-NOW");
    return;
  }

  // Register the send callback
  esp_now_register_send_cb(OnDataSent);

  // Register peer (the Receiver)
  memcpy(peerInfo.peer_addr, broadcastAddress, 6);
  peerInfo.channel = 0;  
  peerInfo.encrypt = false;
  
  if (esp_now_add_peer(&peerInfo) != ESP_OK){
    Serial.println("Failed to add peer !");
    return;
  }
}

void loop() {
  // Reading analog inputs from the potentiometers
  // ESP32 ADC is 12-bit (returns 0-4095)
  int raw1 = analogRead(potPin1);
  int raw2 = analogRead(potPin2);
  int raw3 = analogRead(potPin3);

  // Apply Exponential Moving Average (EMA) filter
  // Formula: new_filtered = (alpha * raw_value) + ((1 - alpha) * old_filtered)
  // An alpha of 0.1 means we trust the old value 90% and the new value 10%
  float alpha = 0.15; // Lower = smoother but slower response. Max 1.0 (no filter)
  
  static float filtered1 = raw1; // initialize with first reading
  static float filtered2 = raw2;
  static float filtered3 = raw3;

  filtered1 = (alpha * raw1) + ((1.0 - alpha) * filtered1);
  filtered2 = (alpha * raw2) + ((1.0 - alpha) * filtered2);
  filtered3 = (alpha * raw3) + ((1.0 - alpha) * filtered3);

  myData.pot1 = (int)filtered1;
  myData.pot2 = (int)filtered2;
  myData.pot3 = (int)filtered3;

  // Send message via ESP-NOW
  esp_err_t result = esp_now_send(broadcastAddress, (uint8_t *) &myData, sizeof(myData));

  // Delay for a stable framerate (e.g. 20ms = 50Hz)
  delay(20);
}
