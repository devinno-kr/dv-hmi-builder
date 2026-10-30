# Simulator, checking your work, and handing over

Read when: there is no device, before you say a piece of work is done, and when telling the user how to run the program.

## Who tests what

- Running the app — on the device or on the simulator — is the user's test, never yours. Do not start the app: the app may reach the real device, and a real device moves equipment.
- Your tests are unit tests: the app is not started, but the tests drive the app's own device code **against the simulator over the real communication path** (Modbus TCP on 127.0.0.1), so what is decoded and what is written is checked end to end. **Loopback only — a test never reaches a device and never opens a COM port** (not even a virtual one: one setting away from a real port). The one exception is the cross-connected serial ports below, for a protocol the library cannot run over TCP, and only with the user's setup.
- Every project has a simulator, also when a device exists — the tests need it. Build it first (the plan's first turn), before communication code; with no device it is also how the user tests. Say so to the user.

## Simulator

Pattern: `reference/HotAirDryerHmi/Simulation/DryerSimulator.cs` and how `Managers/DeviceManager.cs` starts it.

- A fake device inside the app: `Simulation/<Device>Simulator.cs` with Devinno.Communication `ModbusTCPSlave` and the same memory map. The app connects to 127.0.0.1 on its own port, and the slave listens only there: `LocalIP = "127.0.0.1"` (listening on all addresses makes Windows Firewall ask for permission for every new executable — every test run and worktree). Lock the slave's `SyncRoot` when touching its arrays.
- A `Simulation` switch in `Setting`, and the app's frame shows on every screen when simulation is on (`screens.md`, the frame), so nobody mistakes fake values for real ones.
- A serial device (RTU) is simulated over TCP like the reference: the app connects with the TCP master to 127.0.0.1 in simulation, so the same addresses and decoding are tested; only the serial framing (the library's) is not. Only for a protocol the library cannot run over TCP: ask the user to cross-connect two serial ports of the PC (port to port, no device); the tests' start check sends `TEST` on one and expects it on the other, answers `OK` back, and runs the communication tests only if both arrive — otherwise they are skipped and the report says so. Those tests never run in parallel.
- Add fault buttons (alarm bits, sensor fault, comm loss) so the user can see alarms and abnormal screens.
- Device behaviour that the manual does not describe is your assumption: tell the user it must be checked on the real device (in the report, under what to test), and name the simulator in the device's `simulator` cell of the memory map (`.dv-hmi/design/devices.json`).

## Checking your work

What you check against is what the user asked for and what you confirmed with them (values, limits, what a command does) — decide the expected result before writing the code, so the check is not shaped by what you built.

- Build with no warnings.
- Screens: render every page in normal and abnormal states and look at the PNGs.
- Unit tests — **when the request changes value decoding or logic**: decoding (scale, bits, word order, sign), alarm judgement, interlocks, commands, comm-loss judgement, timers, recording rules, what is saved and read back. Screens, text and layout need none. Add them to the request's work, in the project's test project `tests/<name>.Tests` (xUnit, set up by setup; the app's `internal` members are visible to it). Pattern: `reference/HotAirDryerHmi.Tests/` — `SimulatorFixture.cs`, `DryerCommTests.cs`.
  - **Rule: the simulator's TCP slave listens on 127.0.0.1 only** (`LocalIP = "127.0.0.1"`). `log done` and `test` refuse a simulator without it.
  - **The simulator fixture** (one xUnit collection fixture — **one simulator per device, shared by the whole run**, so even a filtered run starts and stops it; no test creates its own): pick a free port on 127.0.0.1 (bind port 0, read it, release it — parallel runs don't collide), start the simulator, check it accepts connections on loopback (the start check; throw if not); after the tests stop it and check the port no longer accepts (the stop check; throw if it does). One visible test asserts the start check, so the dashboard shows it. All device tests are in that one collection and run one after another: `tests/<name>.Tests/xunit.runner.json` has `{"parallelizeTestCollections": false}` (setup writes it).
  - **Through the simulator**: whatever touches the device — decode (simulator register → the app's value), commands (the app writes → the simulator's register or coil), comm loss (the simulator's stop-answering switch, `Silent`, set and cleared in try/finally; the test sets a short comm-loss timeout in its settings and asserts that loss is judged, not a tight time bound). Wait for the first reception before writing or judging (the link is asynchronous: a write before it fails, and "comm lost" is true until every read block has arrived), then poll with a generous deadline (≈10 s, every 50 ms; it returns as soon as the condition holds) — no fixed sleeps. A test that changes the shared simulator's state sets it and puts it back when done (`Preserve()` in the reference).
  - **Directly**, for logic that does not touch the device: raw values into the parse method, chosen times into the logic (`Tick(now)`, schedule checks), recipe limits, including the abnormal cases. Never open a window or wait on the clock.
  - One test class per feature (decoding of one device, recipe checks, schedule …). Start its `/// <summary>` with the feature's name in the user's language and a colon — `/// <summary>건조기 값 해석: …` — the dashboard's 테스트 tab groups the tests under that name.
  - Name every test with `DisplayName` in the user's language, stating the input and the expected result: `"챔버 온도 40001 = 1652 → 165.2 ℃"`. The user reads these names to see how you understood the device — so when an expected value changes, the name changes with it. Cases that differ only in values are one `[Theory]` with `[InlineData]` rows.
  - Making a test robust (a longer deadline, polling instead of a sleep, a short comm-loss timeout set by the test) is allowed; changing the expected value, removing an assertion or deleting the test to pass is weakening it and is not — except when the user decided a new value: then the expected value and the test name change with it, as part of that work. Fix the code, or ask the user if the test's expectation is what is in doubt.
- `log done` builds and runs the test projects itself; it refuses on any failure and records what it saw. A test that is not in the solution is not evidence, and a project that is not a test project is not run. For workflow work it runs only the tests paired with the work's checks (`--check`); the full run is `dv-hmi-cli test` in the verify stage.
- Your tests share your understanding of the request, and so does the simulator: a wrong address or scale passes them. Only the user's test on the device shows that.

## Handing over

Tell the user, in plain words: how to open the program (the 실행 button on the dashboard, or where the .exe is), what they will see (simulation on/off), what to test by running it — short and concrete, safety first —, what is assumed and must be checked on the real device, and what you could not check.
