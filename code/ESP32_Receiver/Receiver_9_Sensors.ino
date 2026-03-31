#include <esp_now.h>
#include <WiFi.h>
#include <esp_wifi.h>

// Must match Sender exactly (9 floats + timestamp)
typedef struct struct_message {
  float angles[9];
  uint32_t timestamp; 
} struct_message;

struct_message myData;

void OnDataRecv(const esp_now_recv_info_t * mac, const uint8_t *incomingData, int len) {
  memcpy(&myData, incomingData, sizeof(myData));
  
  // Print incoming data in a CSV format: angle0,angle1...angle8,timestamp
  for(int i = 0; i < 9; i++) {
    Serial.print(myData.angles[i], 2); // 2 decimal precision
    Serial.print(",");
  }
  Serial.println(myData.timestamp);
}

void setup() {
  Serial.begin(115200);
  delay(1000); 
  
  WiFi.mode(WIFI_STA);
  esp_wifi_set_channel(1, WIFI_SECOND_CHAN_NONE);

  if (esp_now_init() != ESP_OK) return;
  esp_now_register_recv_cb(OnDataRecv);
  
  Serial.println("9nd-Order Receiver Ready! Waiting for data...");
}

void loop() {
  delay(500);
}
