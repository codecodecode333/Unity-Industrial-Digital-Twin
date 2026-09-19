using System;
using TwinTrace.Domain;
using TwinTrace.Telemetry;
using UnityEngine;

namespace TwinTrace.Interaction
{
    [DisallowMultipleComponent]
    public sealed class FaultInjectionController : MonoBehaviour
    {
        [SerializeField] private DeviceSelectionController selectionController;
        [SerializeField] private SimulationTelemetrySource telemetrySource;

        public void Configure(
            DeviceSelectionController controller,
            SimulationTelemetrySource source)
        {
            selectionController = controller;
            telemetrySource = source;
        }

        public FaultInjectionResult InjectDefaultFaultForSelectedDevice()
        {
            if (selectionController?.Selected == null ||
                !selectionController.Selected.IsBound)
            {
                return FaultInjectionResult.UnknownDevice;
            }

            if (telemetrySource == null)
            {
                throw new InvalidOperationException(
                    "A SimulationTelemetrySource is required for fault injection.");
            }

            DeviceState selectedState = selectionController.Selected.BoundState;
            SimulatedFaultType fault = selectedState.Descriptor.Kind switch
            {
                DeviceKind.Motor => SimulatedFaultType.MotorOverheat,
                DeviceKind.Conveyor => SimulatedFaultType.ConveyorJam,
                _ => SimulatedFaultType.None
            };

            return telemetrySource.InjectFault(selectedState.Id, fault);
        }

        public bool ClearFaultForSelectedDevice()
        {
            if (selectionController?.Selected == null ||
                !selectionController.Selected.IsBound ||
                telemetrySource == null)
            {
                return false;
            }

            return telemetrySource.ClearFault(selectionController.Selected.BoundState.Id);
        }
    }
}
