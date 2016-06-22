using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
// Add using statements to the GrovePi libraries
using GrovePi;
using GrovePi.Sensors;
using Windows.System.Threading;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace Nightlight
{
    public sealed class StartupTask : IBackgroundTask
    {
        /**** DIGITAL SENSORS AND ACTUATORS ****/
        // Connect the Red LED to digital port 6
        ILed redLed;

        /**** ANALOG SENSORS ****/
        // Connect the light sensor to analog port 2
        ILightSensor lightSensor;

        /**** Constants and Variables ****/
        // Decide on a level of ambient light at which the LED should
        // be in a completely off state (e.g. sensorValue == 700)
        const int ambientLightThreshold = 700;
        // Create a variable to track the current value from the Light Sensor
        private int actualAmbientLight;
        // Create a variable to track the current LED brightness
        private int brightness;
        // Create a timer to control the rate of sensor and actuator interactions
        ThreadPoolTimer timer;
        // Create a deferral object to prevent the app from terminating
        BackgroundTaskDeferral deferral;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // Get the deferral instance
            deferral = taskInstance.GetDeferral();

            // Instantiate the sensors and actuators
            redLed = DeviceFactory.Build.Led(Pin.DigitalPin6);
            lightSensor = DeviceFactory.Build.LightSensor(Pin.AnalogPin2);

            // Start a timer to check the sensor and activate the actuator five times per second
            timer = ThreadPoolTimer.CreatePeriodicTimer(Timer_Tick, TimeSpan.FromMilliseconds(200));
        }

        private void Timer_Tick(ThreadPoolTimer timer)
        {
            try
            {
                // Capture the current value from the Light Sensor
                actualAmbientLight = lightSensor.SensorValue();

                // If the actual light measurement is lower than the defined threshold
                // then define the LED brightness based on the delta between the actual
                // ambient light and the threshold value
                if (actualAmbientLight < ambientLightThreshold)
                {
                    // Use a range mapping method to conver the difference between the
                    // actual ambient light and the threshold to a value between 0 and 255
                    // (the 8-bit range of the LED on D6 - a PWM pin).
                    // If actual ambient light is low, the differnce between it and the threshold will be
                    // high resulting in a high brightness value.
                    brightness = Map(ambientLightThreshold - actualAmbientLight, 0, ambientLightThreshold, 0, 255);
                }
                else
                {
                    // If the actual ambient light value is above the threshold then
                    // the LED should be completely off. Set the brightness to 0
                    brightness = 0;
                }

                // AnalogWrite uses Pulse Width Modulation (PWM) to 
                // control the brightness of the digital LED on pin D6.
                redLed.AnalogWrite(Convert.ToByte(brightness));
            }
            catch (Exception ex)
            {
                // NOTE: There are frequent exceptions of the following:
                // WinRT information: Unexpected number of bytes was transferred. Expected: '. Actual: '.
                // This appears to be caused by the rapid frequency of writes to the GPIO
                // These are being swallowed here

                // If you want to see the exceptions uncomment the following:
                // System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        // This Map function is based on the Arduino Map function
        // http://www.arduino.cc/en/Reference/Map
        private int Map(int src, int in_min, int in_max, int out_min, int out_max)
        {
            return (src - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }

    }
}
