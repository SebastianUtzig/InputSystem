using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Gyroscope = UnityEngine.InputSystem.Gyroscope;

partial class CoreTests
{
    [Test]
    [Category("API")]
    public unsafe void API_CanReadGyroThroughGyroscopeAPI()
    {
        var gyroId = runtime.ReportNewInputDevice<Gyroscope>();
        var accelId = runtime.ReportNewInputDevice<LinearAccelerationSensor>();
        var gravityId = runtime.ReportNewInputDevice<GravitySensor>();
        var attitudeId = runtime.ReportNewInputDevice<AttitudeSensor>();

        var gyroEnabled = false;
        var accelEnabled = false;
        var gravityEnabled = false;
        var attitudeEnabled = false;

        long EnableDisable(ref bool enabled, int id, InputDeviceCommand* commandPtr)
        {
            if (commandPtr->type == QueryEnabledStateCommand.Type)
            {
                ((QueryEnabledStateCommand*)commandPtr)->isEnabled = enabled;
                return InputDeviceCommand.GenericSuccess;
            }
            if (commandPtr->type == EnableDeviceCommand.Type)
            {
                enabled = true;
                return InputDeviceCommand.GenericSuccess;
            }
            if (commandPtr->type == DisableDeviceCommand.Type)
            {
                enabled = false;
                return InputDeviceCommand.GenericSuccess;
            }

            return InputDeviceCommand.GenericFailure;
        }

        // Need to have these in place before we add the devices.
        runtime.SetDeviceCommandCallback(gyroId, (id, command) => EnableDisable(ref gyroEnabled, id, command));
        runtime.SetDeviceCommandCallback(accelId, (id, command) => EnableDisable(ref accelEnabled, id, command));
        runtime.SetDeviceCommandCallback(gravityId, (id, command) => EnableDisable(ref gravityEnabled, id, command));
        runtime.SetDeviceCommandCallback(attitudeId, (id, command) => EnableDisable(ref attitudeEnabled, id, command));

        InputSystem.Update();

        var gyro = InputSystem.GetDevice<Gyroscope>();
        var accel = InputSystem.GetDevice<LinearAccelerationSensor>();
        var gravity = InputSystem.GetDevice<GravitySensor>();
        var attitude = InputSystem.GetDevice<AttitudeSensor>();

        Assert.That(Input.isGyroAvailable, Is.True);
        Assert.That(Input.gyro, Is.Not.Null);
        Assert.That(Input.gyro.enabled, Is.False);
        Assert.That(Input.gyro.attitude, Is.EqualTo(default(Quaternion)));
        Assert.That(Input.gyro.rotationRate, Is.EqualTo(default(Vector3)));
        Assert.That(Input.gyro.rotationRateUnbiased, Is.EqualTo(default(Vector3)));
        Assert.That(Input.gyro.userAcceleration, Is.EqualTo(default(Vector3)));

        Assert.That(gyro.enabled, Is.False);
        Assert.That(accel.enabled, Is.False);
        Assert.That(gravity.enabled, Is.False);
        Assert.That(attitude.enabled, Is.False);

        Input.gyro.enabled = true;

        Assert.That(Input.gyro.enabled, Is.True);
        Assert.That(gyro.enabled, Is.True);
        Assert.That(accel.enabled, Is.True);
        Assert.That(gravity.enabled, Is.True);
        Assert.That(attitude.enabled, Is.True);
        Assert.That(gyroEnabled, Is.True);
        Assert.That(accelEnabled, Is.True);
        Assert.That(gravityEnabled, Is.True);
        Assert.That(attitudeEnabled, Is.True);
        Assert.That(Input.gyro.attitude, Is.EqualTo(default(Quaternion)));
        Assert.That(Input.gyro.rotationRate, Is.EqualTo(default(Vector3)));
        Assert.That(Input.gyro.rotationRateUnbiased, Is.EqualTo(default(Vector3)));
        Assert.That(Input.gyro.userAcceleration, Is.EqualTo(default(Vector3)));

        Set(gyro.angularVelocity, new Vector3(0.123f, 0.234f, 0.345f));
        Set(accel.acceleration, new Vector3(0.234f, 0.345f, 0.456f));
        Set(gravity.gravity, new Vector3(0.345f, 0.456f, 0.567f));
        Set(attitude.attitude, new Quaternion(0.456f, 0.567f, 0.678f, 0.789f));

        Assert.That(Input.gyro.attitude, Is.EqualTo(new Quaternion(0.456f, 0.567f, 0.678f, 0.789f)));
        Assert.That(Input.gyro.rotationRate, Is.EqualTo(new Vector3(0.123f, 0.234f, 0.345f)));
        Assert.That(Input.gyro.rotationRateUnbiased, Is.EqualTo(new Vector3(0.123f, 0.234f, 0.345f)));
        Assert.That(Input.gyro.userAcceleration, Is.EqualTo(new Vector3(0.234f, 0.345f, 0.456f)));

        Input.gyro.enabled = false;

        Assert.That(Input.gyro.enabled, Is.False);
        Assert.That(gyro.enabled, Is.False);
        Assert.That(accel.enabled, Is.False);
        Assert.That(gravity.enabled, Is.False);
        Assert.That(attitude.enabled, Is.False);
        Assert.That(gyroEnabled, Is.False);
        Assert.That(accelEnabled, Is.False);
        Assert.That(gravityEnabled, Is.False);
        Assert.That(attitudeEnabled, Is.False);
    }

    [Test]
    [Category("API")]
    public unsafe void API_CanSetUpdateIntervalThroughGyroscopeAPI()
    {
        var gyro = InputSystem.AddDevice<Gyroscope>();
        var accel = InputSystem.AddDevice<LinearAccelerationSensor>();
        var gravity = InputSystem.AddDevice<GravitySensor>();
        var attitude = InputSystem.AddDevice<AttitudeSensor>();

        var gyroSampleFrequency = 0.123f;
        var accelSampleFrequency = 0.234f;
        var gravitySampleFrequency = 0.345f;
        var attitudeSampleFrequency = 0.456f;

        long SamplingFrequency(ref float frequency, int id, InputDeviceCommand* commandPtr)
        {
            if (commandPtr->type == QuerySamplingFrequencyCommand.Type)
            {
                ((QuerySamplingFrequencyCommand*)commandPtr)->frequency = frequency;
                return InputDeviceCommand.GenericSuccess;
            }
            if (commandPtr->type == SetSamplingFrequencyCommand.Type)
            {
                frequency = ((SetSamplingFrequencyCommand*)commandPtr)->frequency;
                return InputDeviceCommand.GenericSuccess;
            }

            return InputDeviceCommand.GenericFailure;
        }

        // Need to have these in place before we add the devices.
        runtime.SetDeviceCommandCallback(gyro, (id, command) => SamplingFrequency(ref gyroSampleFrequency, id, command));
        runtime.SetDeviceCommandCallback(accel, (id, command) => SamplingFrequency(ref accelSampleFrequency, id, command));
        runtime.SetDeviceCommandCallback(gravity, (id, command) => SamplingFrequency(ref gravitySampleFrequency, id, command));
        runtime.SetDeviceCommandCallback(attitude, (id, command) => SamplingFrequency(ref attitudeSampleFrequency, id, command));

        // Current implementation uses min of the devices.
        Assert.That(Input.gyro.updateInterval, Is.EqualTo(0.123f));

        Input.gyro.updateInterval = 0.987f;

        Assert.That(Input.gyro.updateInterval, Is.EqualTo(0.987f));
        Assert.That(gyroSampleFrequency, Is.EqualTo(0.987f));
        Assert.That(accelSampleFrequency, Is.EqualTo(0.987f));
        Assert.That(gravitySampleFrequency, Is.EqualTo(0.987f));
        Assert.That(attitudeSampleFrequency, Is.EqualTo(0.987f));
    }

    [Test]
    [Category("API")]
    public void API_CanReadGyroThroughGyroscopeAPI_WhenNoGyroIsPresent_AllValuesRemainAtDefault()
    {
        Assert.That(Input.isGyroAvailable, Is.False);

        // Presence of gyro is *not* indicated by null property. Instead, it returns
        // a gyro where all values are at default and don't change.
        Assert.That(Input.gyro, Is.Not.Null);
        Assert.That(Input.gyro.enabled, Is.False);
        Assert.That(Input.gyro.attitude, Is.EqualTo(default(Quaternion)));
        Assert.That(Input.gyro.rotationRate, Is.EqualTo(default(Vector3)));
        Assert.That(Input.gyro.rotationRateUnbiased, Is.EqualTo(default(Vector3)));
        Assert.That(Input.gyro.userAcceleration, Is.EqualTo(default(Vector3)));
        Assert.That(Input.gyro.updateInterval, Is.EqualTo(default(float)));

        // When there is no gyro, enabling it should do nothing.
        Assert.That(() => Input.gyro.enabled = true, Throws.Nothing);
        Assert.That(Input.gyro.enabled, Is.False);
        Assert.That(() => Input.gyro.updateInterval = 0.123f, Throws.Nothing);
        Assert.That(Input.gyro.updateInterval, Is.EqualTo(default(float)));
    }

    [UnityTest]
    [Category("API")]
    public IEnumerator API_CanReadLocationThroughLocationServiceAPI()
    {
        Assert.That(Input.location.isEnabledByUser, Is.True);
        Assert.That(Input.location.status, Is.EqualTo(LocationServiceStatus.Stopped));

        Input.location.Start(123f, 234f);

        Assert.That(Input.location.status, Is.EqualTo(LocationServiceStatus.Initializing));
        Assert.That(runtime.desiredLocationAccuracy, Is.EqualTo(123f));
        Assert.That(runtime.locationDistanceFilter, Is.EqualTo(234f));

        var maxWait = 20;
        while (maxWait > 0 && Input.location.status == LocationServiceStatus.Initializing)
        {
            --maxWait;
            yield return null;
        }

        Assert.That(Input.location.status, Is.EqualTo(LocationServiceStatus.Running));

        runtime.lastLocation = new LocationInfo
        {
            m_Latitude = 0.123f,
            m_Longitude = 0.234f,
            m_Altitude = 0.345f,
            m_HorizontalAccuracy = 0.456f,
            m_VerticalAccuracy = 0.567f,
            m_Timestamp = 0.678f,
        };

        Assert.That(Input.location.lastData.latitude, Is.EqualTo(0.123f));
        Assert.That(Input.location.lastData.longitude, Is.EqualTo(0.234f));
        Assert.That(Input.location.lastData.altitude, Is.EqualTo(0.345f));
        Assert.That(Input.location.lastData.horizontalAccuracy, Is.EqualTo(0.456f));
        Assert.That(Input.location.lastData.verticalAccuracy, Is.EqualTo(0.567f));
        Assert.That(Input.location.lastData.timestamp, Is.EqualTo(0.678f));

        Input.location.Stop();

        Assert.That(Input.location.status, Is.EqualTo(LocationServiceStatus.Stopped));
    }
}
