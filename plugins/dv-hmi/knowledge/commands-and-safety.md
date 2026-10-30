# Commands, interlocks and abnormal situations (logic)

Read when: building buttons or inputs that change the device, automatic actions, start-up or comm-loss behaviour.

## Safety boundary

- The HMI is not a safety layer. Emergency stop, guards and safety interlocks belong to the hardware or PLC. If asked to build them in the HMI, say so plainly and build only an indication.
- Safety-related interpretations — what a command does to the device, interlocks, reset, start-up behaviour, stopping during a sequence — are confirmed with the user **before** you build them. State your proposal in plain words, as one bundle; the user corrects what is wrong. Do not decide them silently and report afterwards. Until the user answers, that behaviour stays unbuilt — the answer is the user's, never yours.
- A confirmed safety behaviour is an agreement. To change it later — even to make it stricter — confirm again before building the change.

## Each command

Decide (ask if unknown): who may use it, input (button, number, choice), accepted range, confirmation, when it is blocked, how it is written (value / hold / pulse — modbus.md), and what the screen shows when it fails. The button calls one method, usually on `LogicManager` (pattern: `reference/HotAirDryerHmi/Managers/LogicManager.cs`).

Library (Devinno.UI `llm-rules.md` §4.2, `dvml-ui-markup.md` §21):
- Dangerous buttons: `hold="1200"` on the button, or `await App.MessageBox(…, DvMessageButtons.YesNo)`.
- Numbers: `App.InputNumber(title, value, min, max)` — the initial value must be inside the range; `null` means cancel (never `?? 0`). Or `DvInput` with `min`/`max`.
- Blocked: `disabled="{…}"`, and show **why** — the library's rejection text is generic, so return a reason from the logic and show it (pattern: `LogicManager.StartRun()` returns a reason, `MainPage.cs` shows it).
- Toggles and knobs ignore `hold`, and `onbeforecommit` cannot await a dialog: use a button for any command that needs confirmation.
- Button handler = one line calling the logic manager; the logic manager calls the device method.

## Safe defaults

These are what you propose, in the one bundle above, for whatever the user has not decided; the user corrects what is wrong. Build only after the answer. What the user cannot decide ("I don't know, the safe way") is built as the default and written in `spec.md` as a `가정:` line (once the requirements are signed, that line is a change: show it and sign it).

| Situation | Default |
|---|---|
| Program starts | Manual mode. If the device is already running, leave it running and show it as running |
| Stop | Always available, in every mode and during a sequence. Stopping in auto switches to manual. If the stop does not reach the device (comm lost, no confirmation), tell the operator at once that it was **not sent** and to stop at the machine — a stop must never fail silently (pattern: `LogicManager.StopRun`) |
| Interlock | Acts once when the condition starts (one alarm). Does not restart anything when the condition clears |
| Comm loss | Values shown as "--", other commands locked, no judgement on old values. Offline writes are discarded (library default), so a Stop pressed now is reported as not sent |
| Settings or record file broken | Report and stop. No silent defaults |
| Schedule | Acts only when its time passes. Times missed while the program was off or paused are not run |

## Abnormal situations

Users rarely mention these. For an HMI with communication, handle all four every time: **comm loss**, **just after start (before the first response)**, **during a danger alarm**, **program started while the device is already running**. For each: how it is detected, what the HMI does automatically (if it writes to the device: address and method), what the screen shows, what is locked, and how it is cleared (who releases a signal the HMI wrote).

## Logic timer

Pattern: `reference/HotAirDryerHmi/Managers/LogicManager.cs` (`Tick`, `Judge`, `Record`).

- One monitor timer: comm-loss check → interlocks and alarms → (if comm is up) record → schedule → finally `App.Invalidate()`. The whole body in try/catch (an exception on the timer thread ends the program).
- Skip a tick while the previous one is still running (`Interlocked` flag).
- Act on state changes only: remember the previous state, so an interlock or alarm fires once.
- Sequences with waits (fan on → 3 s → heater on): `async` with a `CancellationToken`; stop or a new sequence cancels it; catch every non-cancel exception inside (the caller discards the Task, so nothing else will see it).
