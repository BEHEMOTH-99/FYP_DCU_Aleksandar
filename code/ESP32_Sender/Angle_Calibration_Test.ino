// Pins for the analog inputs (matching the Sender code)
const int potPin1 = A0;
const int potPin2 = A1;

// --- CALIBRATION SETTINGS ---
// Default starting raw ADC points for the 2-point calibration.
// These are rough guesses assuming ~11.37 ADC per degree
float pot1_0 = 0.0, pot1_90 = 1024.0; // Pot 1 handles 0 to 90 degrees
float pot2_0 = 0.0, pot2_25 = 284.0;  // Pot 2 handles 0 to 25 degrees (MCP sideways)

void setup() {
  Serial.begin(115200);
  
  // REQUIRED for S3 Native USB to show Serial output
  delay(1000);
  Serial.println("Starting Angle Check...");
  Serial.println("--- 2-POINT CALIBRATION ACTIVE ---");
  Serial.println("Type 'zero' to set current position as 0 degrees for BOTH pots");
  Serial.println("Type '90' to set current position as 90 degrees for Pot 1");
  Serial.println("Type '25' to set current position as 25 degrees for Pot 2");
}

// Float version of Arduino's map() for better precision
float mapFloat(float x, float in_min, float in_max, float out_min, float out_max) {
  if (in_max == in_min) return out_min; // Prevent divide by zero
  return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
}

void loop() {
  // Read analog inputs from the potentiometers
  int raw1 = analogRead(potPin1);
  int raw2 = analogRead(potPin2);

  // Apply Exponential Moving Average (EMA) filter
  float alpha = 0.15; // Lower = smoother but slower response. 
  
  static float filtered1 = raw1; // initialize with first reading
  static float filtered2 = raw2;

  filtered1 = (alpha * raw1) + ((1.0 - alpha) * filtered1);
  filtered2 = (alpha * raw2) + ((1.0 - alpha) * filtered2);

  // Check if there is a message from the Serial Monitor
  if (Serial.available() > 0) {
    String message = Serial.readStringUntil('\n');
    message.trim(); // Remove any extra spaces or hidden newline characters
    
    if (message.equalsIgnoreCase("zero")) {
      pot1_0 = filtered1; 
      pot2_0 = filtered2;
      
      // Shift the 90 and 25 points so we maintain a positive slope
      // even if the new zero point is very high.
      pot1_90 = pot1_0 + 1024.0;
      pot2_25 = pot2_0 + 284.0;
      
      Serial.println("\n--- 0 DEGREE POINT SET (Pot 1 & 2) ---");
    } 
    else if (message.equalsIgnoreCase("90")) {
      pot1_90 = filtered1;
      Serial.println("\n--- 90 DEGREE POINT SET (Pot 1) ---");
    }
    else if (message.equalsIgnoreCase("25")) {
      pot2_25 = filtered2;
      Serial.println("\n--- 25 DEGREE POINT SET (Pot 2) ---");
    }
  }

  // Calculate the final angles mapFloat(adc, zeroAdc, maxAdc, 0.0, maxAngle)
  float angle1 = mapFloat(filtered1, pot1_0, pot1_90, 0.0, 90.0);
  float angle2 = mapFloat(filtered2, pot2_0, pot2_25, 0.0, 25.0);

  // Print the angles to the Serial monitor
  Serial.print("Angle1:");
  Serial.print(angle1);
  Serial.print("\tAngle2:");
  Serial.println(angle2);

  // Delay for a stable framerate (e.g., 20ms = 50Hz)
  delay(20);
}
