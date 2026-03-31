// Pins for the analog inputs (matching the Sender code)
const int potPin1 = A0;
const int potPin2 = A1;
const int potPin3 = A2;

// --- CALIBRATION SETTINGS ---
// Set the maximum angle of your potentiometers
const float MAX_ANGLE = 360.0;

// ESP32 ADC values typically range from 0 to 4095. 
// However, potentiometers and the ESP32 ADC are often not perfectly linear at the extreme ends.
// If your readings don't reach 0 or 360, you can adjust these min/max ADC bounds:
const float MIN_ADC = 0.0;
const float MAX_ADC = 4095.0;

// If the angle is consistently shifted (e.g., reading 80 instead of 90, 170 instead of 180),
// you can add a constant angle offset here:
const float OFFSET_ANGLE = 10.0; // Adds 10 degrees to all Pots

void setup() {
  Serial.begin(115200);
  
  // REQUIRED for S3 Native USB to show Serial output
  delay(1000);
  Serial.println("Starting Angle Check...");
}

// Float version of Arduino's map() for better precision
float mapFloat(float x, float in_min, float in_max, float out_min, float out_max) {
  return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
}

void loop() {
  // Read analog inputs from the potentiometers
  int raw1 = analogRead(potPin1);
  int raw2 = analogRead(potPin2);
  int raw3 = analogRead(potPin3);

  // Apply Exponential Moving Average (EMA) filter
  float alpha = 0.15; // Lower = smoother but slower response. 
  
  static float filtered1 = raw1; // initialize with first reading
  static float filtered2 = raw2;
  static float filtered3 = raw3;

  filtered1 = (alpha * raw1) + ((1.0 - alpha) * filtered1);
  filtered2 = (alpha * raw2) + ((1.0 - alpha) * filtered2);
  filtered3 = (alpha * raw3) + ((1.0 - alpha) * filtered3);

  // Convert the filtered ADC values to angles in degrees
  float angle1 = mapFloat(filtered1, MIN_ADC, MAX_ADC, 0, MAX_ANGLE) + OFFSET_ANGLE;
  float angle2 = mapFloat(filtered2, MIN_ADC, MAX_ADC, 0, MAX_ANGLE) + OFFSET_ANGLE;
  float angle3 = mapFloat(filtered3, MIN_ADC, MAX_ADC, 0, MAX_ANGLE) + OFFSET_ANGLE;

  // Print the angles to the Serial monitor
  // Formatted like this so it works nicely with the Arduino IDE Serial Plotter
  Serial.print("Angle1:");
  Serial.print(angle1);
  Serial.print("\tAngle2:");
  Serial.print(angle2);
  Serial.print("\tAngle3:");
  Serial.println(angle3);

  // Delay for a stable framerate (e.g., 20ms = 50Hz)
  delay(20);
}

