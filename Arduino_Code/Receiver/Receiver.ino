#include <esp_now.h>
#include <WiFi.h>

typedef struct struct_message {
  int pot1;
  int pot2;
  int pot3;
} struct_message;

struct_message myData;

// Callback function that will be executed when data is received
void OnDataRecv(const esp_now_recv_info_t * mac, const uint8_t *incomingData, int len) {
  memcpy(&myData, incomingData, sizeof(myData));
  
  // Print incoming data in a CSV format: pot1,pot2,pot3
  Serial.print(myData.pot1);
  Serial.print(",");
  Serial.print(myData.pot2);
  Serial.print(",");
  Serial.println(myData.pot3);
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
  
  // Register the receive callback
  esp_now_register_recv_cb(OnDataRecv);
}

void loop() {
  // ESP-NOW runs asynchronously in the background. Nothing needed here.
  delay(10);
}
