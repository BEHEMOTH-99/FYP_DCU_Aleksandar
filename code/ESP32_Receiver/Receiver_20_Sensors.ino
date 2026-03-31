#include <esp_now.h>
#include <WiFi.h>
#include <esp_wifi.h>

// Must match Sender exactly (84 bytes)
typedef struct struct_message {
  int32_t sensors[20];
  uint32_t timestamp; 
} struct_message;

struct_message myData;

// Callback function that will be executed when data is received
void OnDataRecv(const esp_now_recv_info_t * mac, const uint8_t *incomingData, int len) {
  memcpy(&myData, incomingData, sizeof(myData));
  
  // We use a loop to cleanly print all 20 sensors as a CSV string
  for(int i = 0; i < 20; i++) {
    Serial.print(myData.sensors[i]);
    Serial.print(",");
  }
  
  // At the end of the 20 variables, print the timestamp and move to a new line
  Serial.println(myData.timestamp);
}

void setup() {
  Serial.begin(115200);
  delay(1000); 
  
  WiFi.mode(WIFI_STA);
  delay(100);

  esp_wifi_set_channel(1, WIFI_SECOND_CHAN_NONE);

  if (esp_now_init() != ESP_OK) {
    Serial.println("Error initializing ESP-NOW");
    return;
  }
  
  esp_now_register_recv_cb(OnDataRecv);
  
  Serial.println("20-Sensor Receiver Ready! Waiting for data...");
}

void loop() {
  delay(500);
}
