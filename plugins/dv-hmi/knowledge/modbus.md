# Device communication (Modbus)

Read when: connecting a device, turning an address table into code, writing commands, handling comm loss.

The library is Devinno.Communication. Its rules file is the authority: `src/<app>/.devinno/Devinno.Communication/llms.txt` → `llm-rules.md`. This file adds only what an HMI needs on top, and points to sections instead of repeating them.

Pattern to copy: `reference/HotAirDryerHmi/Models/Dryer.cs` + `dryer.xml` — one device = one model class that owns its master, address constants, decoding and named writes. Screens and logic never see addresses.

## Address table → code

- Manual notation (40001, 00001, 30001, 10001) → 0-based `Start`: llm-rules.md §4.5. Never put the 4xxxx number in XML.
- The library returns raw values only (words 0–65535, bools). Signed, 32-bit (word order is device-specific), float and scale are your code: llm-rules.md §4.4 and the `Raw` helper in §7.4.
- The device's **memory map** goes in `.dv-hmi/design/devices.json`: one entry per device with its connection and `map`, written from the manual and the user. The dashboard's 통신 tab shows it next to what the XML reads and writes. Example: `reference/HotAirDryerHmi/design/devices.json` (it also has `basis`, `confirm` and `device` cells — the workflow mode's; you may leave them out).

  ```json
  { "devices": [ { "id": "d.bath", "name": "항온 수조", "code": "WaterBath", "xml": "waterbath.xml", "protocol": "Modbus TCP",
                   "connection": { "ip": "192.168.0.50", "port": 502, "unit": 1 }, "simulator": null,
    "map": [
      { "address": "40002", "name": "설정 온도", "code": "SetTemp", "type": "int16", "scale": 0.1, "unit": "℃", "range": "20.0–90.0", "access": "RW", "how": "value" },
      { "address": "00001", "name": "운전 명령", "code": "RunStart", "type": "coil", "access": "W", "how": "hold", "note": "1 운전, 0 정지" } ] } ] }
  ```

  - Device: `name` (the user's words), `code` (the model class), `xml` (the file it loads), `protocol`, `connection` (`ip`·`port`·`unit`, or `serial`·`baud`·`unit` — the defaults; a settings screen may change them), `simulator` (`{ "code", "port", "note" }` or `null`).
  - Row: `address` = the manual's notation (40001, 00001, 30001, 10001, or the 6-digit form). `name`: the user's words. `code`: the property of the value record that holds it (`Temp`), or the device method that writes it (`RunStart`). `type`: `int16`, `uint16`, `int32`, `uint32`, `float32` (word order in `note`), `bits`, `coil`, `input`. `access`: `R`, `W` or `RW`. Write rows: `how` = `value` / `hold` / `pulse`. State values and bits: `codes` / `bits` (`{"0": "정지", "1": "운전"}`). Your interpretations (scale, signedness, word order, write method) go in `note`.
  - One row for every value the program reads or writes. The 통신 tab marks rows the XML does not read or write, and writes that are not in the map.
  - No address table comment in the code: the memory map is the one source.
- When a device or simulator is available, check one value against a known reading (the device panel shows 25.3 ℃ → you decode 253 as 25.3).

## Reads

- One `<Auto>` per contiguous block, at most 125 words / 2000 bits (the library does not split; over the limit fails at load).
- Merge across a gap only if the gap registers are documented as readable.
- Auto items run in turn, `Interval` is waited after every job, and Auto reads are **not retried** (llm-rules.md §5). One cycle ≈ Σ(round trip + Interval).

## Writes

- Named writes (`<Write Name=…>` + `NamedWrite`) wrapped in device methods with meaningful names (`SendRunCommand(bool)`).
- How each command is written is a question for the user: value, hold (coil left at 1 or 0), or pulse. There is no pulse helper: either the device resets the coil itself, or write 1 then 0 with your own timing. Never `RepeatCount` on pulse or counter writes.
- Every write ends in exactly one event: success, exception, final timeout, or `WriteDiscarded` (llm-rules.md §7.1.1). Treat anything but success as "not done" and show it; do not assume the command worked. The success event depends on the function code — coil FC5 `BitWriteReceived`, FC15 `MultiBitWriteReceived`, register FC6 `WordWriteReceived`, FC16 `MultiWordWriteReceived` — so subscribe to the ones your writes use.
- When the result decides what happens next (a recipe must be applied before Start), wait for that write's result event and continue only on success: pattern `Dryer.WriteAndWait` / `WriteRecipeAsync` and `LogicManager.ApplyRecipe`.
- A final timeout with `RequestSent` true may still have executed: read back before sending again.
- Write only while connected. Keep `DiscardWritesWhileDisconnected` at its default `true`: offline writes are discarded, never queued and sent later. Do not set it to false for commands.
- A slider or knob that writes on every change queues every value. Write once on commit.

## Comm loss and stale values

- There is no per-value quality and no last-good time in the library. Stamp a time per read block at the end of its handler; `LastReceived` is the **oldest** of them, so one stale block (for example coils that stopped answering) counts as comm loss. Judge by age in the logic timer (`LastReceived` and `IsCommLost(now, timeout)` in the pattern).
- While comm is lost: show values as "--" (not 0, not the last value as if live), lock commands, raise one comm-loss alarm, do not judge alarms or interlocks from old values, record NaN in trends.
- Always show comm state where every screen shows it (the app's frame, `screens.md`). Keep the last error text for a diagnostics view.

## Threads and errors

- Events run on the master's own thread. Exceptions thrown in your handler are **swallowed silently**: wrap every handler in try/catch and keep `LastError`.
- In a handler: decode into locals, then assign one snapshot object at once, then stamp `LastReceived`. No UI, alarms or file writes inside the handler.
- `LoadXml` / `ApplyConfig` only while stopped. To change IP/port from a settings screen: Stop → set → Start, off the UI thread (pattern: `Managers/DeviceManager.cs`).
