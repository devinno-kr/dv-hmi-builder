# Alarms

Read when: adding or changing alarms.

- An alarm is a condition the operator must act on. A status (running, door closed, heater on) is not an alarm: show it as status.
- The alarm list comes from the user and stays small and closed. For each alarm: condition (using the defined values), priority, and what acknowledging means (only silences and marks, or a record of who and when).
- Priorities: the library has Critical / Major / Minor. Choose by the consequence if the operator does not respond; most alarms should be Minor.
- Raise on the rising edge only (remember the previous state). Give analog limits a deadband (hysteresis) or a short delay so they do not chatter. Never judge from invalid values; comm loss is one alarm by itself and suppresses the value alarms behind it.

## Library

Devinno.UI `dvml-ui-markup.md` §13 and the `AlarmLog` remarks in `Devinno.UI.xml`:
- `AlarmLog` — `Raise(priority, message, source)`, `Ack`, `AckAll`, `UnAckedCount`; thread-safe; memory only, with a row limit.
- `<DvAlarmBanner>` — top unacknowledged alarm and the comm-fault strip (fixed area at the bottom or top).
- `<DvAlarmList>` — does **not** scroll: rows beyond its area are not drawn. For long histories use paging or a `DvDataGrid`.
- Alarm text goes through `Texts.T(key)` like all screen text.

Not in the library — build only if the user needs it, and ask first: return-to-normal time, latch and reset, shelving, sound, alarm history file.

Pattern for raising and acknowledging: `reference/HotAirDryerHmi/Managers/LogicManager.cs` (`Judge`) and `Pages/AlarmBar.cs` (banner, ack). An alarm list page is a screen like any other: design its layout for this program (screens.md) — the reference's alarm page is not a template for it.
