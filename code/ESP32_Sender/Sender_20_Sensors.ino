#include <esp_now.h>
#include <WiFi.h>
#include <esp_wifi.h> 

uint8_t broadcastAddress[] = {0x48, 0x27, 0xE2, 0x61, 0x91, 0x8C}; // Receiver MAC

// We use an array to cleanly handle 20 sensors.
// 20 * 4 bytes (int32_t) + 4 bytes (timestamp) = 84 bytes total.
// ESP-NOW limit is 250 bytes, so this fits perfectly!
typedef struct struct_message {
  int32_t sensors[20];
  uint32_t timestamp; 
} struct_message;

struct_message myData;
esp_now_peer_info_t peerInfo;

void OnDataSent(const wifi_tx_info_t *info, esp_now_send_status_t status) {
  // Uncomment to see Delivery Success on 20-var payloads
  // Serial.println(status == ESP_NOW_SEND_SUCCESS ? "Success" : "Fail");
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

  esp_now_register_send_cb(OnDataSent);

  memcpy(peerInfo.peer_addr, broadcastAddress, 6);
  peerInfo.channel = 1;  
  peerInfo.encrypt = false;
  
  if (esp_now_add_peer(&peerInfo) != ESP_OK){
    Serial.println("Failed to add peer");
    return;
  }
}

void loop() {
  // Generate 20 fake "sensor" readings (e.g., simulating 14 joints + 6 IMU axes)
  for(int i = 0; i < 20; i++) {
    // Generate some fake data between 0 and 4095
    // In reality, this would be: myData.sensors[i] = analogRead(pins[i]);
    myData.sensors[i] = random(1000, 3000);
  }
  
  // Inject the timestamp at the very end
  myData.timestamp = millis();

  // Send the massive 84-byte block over the air
  esp_now_send(broadcastAddress, (uint8_t *) &myData, sizeof(myData));

  // Send 50 packets per second (20ms delay) to test throughput bandwidth
  delay(20);
}
