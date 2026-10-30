# Values, states and settings (model)

Read when: deciding what the HMI shows, judges and stores.

## Define each value once

For every value: a name in the user's words, unit, decimals, display range, warning and danger conditions, and its source (device, address, raw format, scale — or the formula if the HMI computes it). Ranges and limits come from the user (asking-the-user.md). Screens, alarms and trends use this one definition (constants in the device model or `Setting`), never their own copies of the numbers.

Device values and commands are defined in the memory map (`.dv-hmi/design/devices.json`, modbus.md). Every other value the app holds is defined in its class — settings as `Setting` members with their default and range, stored data (recipes, schedules) as their own classes, computed values on the manager that computes them (pattern: `reference/HotAirDryerHmi/Models/`).

## Valid or not

A value is valid only while comm is up and its source is healthy (for example, no sensor-fault bit). Make "not valid" explicit (NaN or a flag) so screens show "--", alarms skip it and trends break the line.

## Device state and modes

- Decode the device state into an enum (Stopped, Running, AlarmStop, …). Screens show it with a status item or lamp tone; commands are enabled or locked per state.
- Modes (manual, auto, schedule): ask who starts and stops in each mode. Safe defaults are in commands-and-safety.md.

## Settings

- Settings live in `Models/Setting.cs`; file paths only in `Managers/DataManager.cs` (pattern: `reference/HotAirDryerHmi/`).
- Validate on load and on save, including relations between settings (for example, trend capacity × poll interval ≥ the longest trend window shown).
- A broken settings file: report it and stop. Never fall back to defaults silently — the program could start talking to the wrong address.
- Numbers and times written to files use `CultureInfo.InvariantCulture`.
- Screens edit a copy and apply it on save. Pages are created once and kept, so make the copy each time the page is shown (`OnShown`), not only in the constructor — otherwise an old copy overwrites changes made elsewhere (pattern: `Pages/SettingPage.cs`).
