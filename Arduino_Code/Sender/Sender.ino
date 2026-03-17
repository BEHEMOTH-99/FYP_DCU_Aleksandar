#include <esp_now.h>
#include <WiFi.h>

// Pins for the Reverse TFT Feather (even without the screen)
const int potPin1 = A0;
const int potPin2 = A1;
const int potPin3 = A2;

uint8_t broadcastAddress[] = {0x48, 0x27, 0xE2, 0x61, 0x91, 0x8C};

typedef struct struct_message {
  int pot1;
  int pot2;
  int pot3;
} struct_message;

struct_message myData;
esp_now_peer_info_t peerInfo;

/ FIX: The first argument MUST be 'const wifi_tx_info_t *info' for Core 3.0+
void OnDataSent(const wifi_tx_info_t *info, esp_now_send_status_t status) {
  // Serial.println(status == ESP_NOW_SEND_SUCCESS ? "Delivery Success" : "Delivery Fail");
}

void setup() {
  Serial.begin(115200);
  
  // REQUIRED for S3 Native USB to show Serial output
  delay(1000);

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
  // An alpha of 0.15 means we trust the old value 85% and the new value 15%
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

  // Helpful debug to see if pots are working
  Serial.printf("P1: %d | P2: %d | P3: %d\n", myData.pot1, myData.pot2, myData.pot3);

  // Delay for a stable framerate (e.g. 20ms = 50Hz)
  delay(20);
}
