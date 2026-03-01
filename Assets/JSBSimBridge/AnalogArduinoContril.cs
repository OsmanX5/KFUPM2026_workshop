using System;
using ThreeLines.IOT.Arduino;
using UnityEngine;

public class AnalogArduinoContril : MonoBehaviour,IArduinoInputHandler
{
    [SerializeField] private JSBSimBridgeF15 f15;
    [SerializeField] float pinValue = 0f;
    void Start()
    {
        AllArduinoInputHandlers.RegisterHandler(this);
    }

    private void Update()
    {
        if (f15)
        {
            f15.Elevator = pinValue;
        }
    }

    public void ProcessInput(ArduinoPin pin, float value)
    {
        if (pin == ArduinoPin.A0)
        {
            pinValue = Mathf.Clamp(value, 0f, 1f);
            Debug.Log($"[ThreeLines.IOT.Arduino] AnalogArduinoContril received input on pin {pin} with value {value}");
        }


    }
}
