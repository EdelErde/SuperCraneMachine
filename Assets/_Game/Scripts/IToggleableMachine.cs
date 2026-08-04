using System;

namespace CraneMachine
{
    // A machine that can be switched on/off by the player via a MachineToggleButton.
    // A disabled machine does no work and draws no fuel; it simply idles until switched
    // back on. Kept separate from IFuelConsumer so a machine can be toggleable without
    // burning fuel (and vice-versa), but the two are usually implemented together.
    public interface IToggleableMachine
    {
        // Name shown on / near the toggle (e.g. "Leaf Blower"). May reuse FuelLabel.
        string ToggleLabel { get; }

        // Current on/off state. Setting it should take effect immediately and raise
        // OnToggled if the value actually changed.
        bool MachineEnabled { get; set; }

        // Raised whenever MachineEnabled changes, with the new value. Lets the button
        // refresh even if something other than the button flips the machine.
        event Action<bool> OnToggled;
    }
}