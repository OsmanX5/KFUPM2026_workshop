void setup()
{
  Serial.begin(9600);
}

void loop()
{
  int rawValue = analogRead(A0);        // Read from A0 (0 - 1023)

  float mappedValue = rawValue / 1023.0; // Convert to 0.0 - 1.0

  Serial.print("A0#");
  Serial.println(mappedValue, 4);       // Print with 4 decimal places

  delay(50); // small delay for stability
}
