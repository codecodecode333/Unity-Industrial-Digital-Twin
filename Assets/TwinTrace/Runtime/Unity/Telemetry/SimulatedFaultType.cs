namespace TwinTrace.Telemetry
{
    public enum SimulatedFaultType
    {
        None = 0,
        MotorOverheat = 1,
        ConveyorJam = 2
    }

    public enum FaultInjectionResult
    {
        Injected = 0,
        UnknownDevice = 1,
        IncompatibleDeviceKind = 2,
        InvalidFault = 3
    }
}
