# Assistant mode

How a project works request by request: always in assistant mode, and in workflow mode from the stage `requests` on (after verify). There is no fixed order: the user says what they want whenever they want it, and you build or change it. The project's working rules (`.dv-hmi/guide.md`) still apply: program structure, library docs, knowledge, building, checking screens.

## The promises

**The user owns what the program promises; you work for the user.** You never change what the user expects because you judge another way better — propose it; the user decides.

- The promises are in `.dv-hmi/spec.md` (12 sections; in workflow mode the signed requirements, in assistant mode it starts empty and grows). When the user decides something that should hold from now on — a safety behaviour they confirmed, a standing wish ("keep every record", "stop must always work") — write it in its section and show the user the change; their plain yes is recorded with `dv-hmi-cli log sign spec` (their last message, word for word). When the user's own message stated it ("from now on keep every record"), write it and sign with `log sign spec --asked`. Write them as results the user can check by running the app.
- A request that breaks a written promise ("delete the old records" against "keep every record"): the user may not remember the promise, so the request alone is not the signature — say which promise it breaks, write the changed promise in `spec.md`, show it, and build only after the user's plain yes (`log sign spec`); on no, restore the signed `spec.md` (`.dv-hmi/spec/<version>.md`).
- **Signing**: write the change first, show it, and sign the user's next plain yes (`log sign …`). An approval with a condition is not a yes yet: make the change, show it, ask again. When the user's own message asked for exactly this change and knew what it changes ("from now on keep every record"), that message is the signature: write it, then `log sign … --asked`. `--asked` never signs an answer you wrote in afterwards. `log sign` prints the words it recorded; if they were not a yes, tell the user and ask again. A chosen option is a signature when each option was the complete sentence that goes into the contract: put it in word for word, sign with `--asked`, and quote the line in your next message; anything reworded or added is shown and signed with a plain yes.
- **Planned work from workflow mode** (in the requests stage): a request that changes what a done planned work promised is also a new version of it — `log revise <n>`, shown and signed with the request (`log revise <n> --withdraw` if the user says no).
- If written promises disagree, do not pick one: ask.
- Reasons you give are the real ones you had at that moment.

## Requests and work history

Every user request that changes the app is one entry in the work history (`.dv-hmi/worklog.json`), opened before the first edit and closed when the work is done. The dashboard shows it to the user. A question you answer ("why is it red?") is not a request: read, explain, and open a request only when you decide to change something.

```bash
dv-hmi-cli log start "<the request, one short line in the user's words>"
```

Then do the work as these rules and the knowledge files say. Edit the app's source with the Edit/Write tools.

```bash
dv-hmi-cli log done "<one thing you did>" "<another>"
```

- **Request or fix**: when the user reports that something the promises (`spec.md`) say does not work, it is your defect — open it as a fix: `dv-hmi-cli log fix "<what is wrong, in the user's words>"`. Only what the promises do not cover is a request; when you open a request for a reported problem, say in its first note which version of `spec.md` has no promise about it. When in doubt, it is a fix.
- `done` does not take your word for it: it builds and runs your unit test projects, and refuses at the first failure (if the user's app is running from the dashboard, ask them to press 중지 first). What it saw is recorded as evidence. When the request changes value decoding or logic, add or update unit tests in `tests/<name>.Tests` as part of the work (`simulator-and-checks.md`). When the user decided a new value (a threshold, a range), changing the test's expected value and name to it is part of the request — say so in the item; changing it for any other reason is weakening the test. Unit tests talk to the simulator on 127.0.0.1, never to a device.
- **A test that fails and is not about this request**: never weaken it. Check whether it failed before your change (take the last commit into a temporary worktree and run the tests there — do not stash or reset your work). If it did, tell the user and fix it as its own fix. A test the CLI reports as flaky (failed, then passed when run again) is made robust as a fix if it happens again.
- One short item per thing that changed, in the user's language and plain words (what the user will see or what the device does, not file or class names).
- **Commit after every `log done` that succeeds**, when this project folder is its own git repository (`git rev-parse --show-toplevel` is this folder; setup made it). The user allowed this in advance for this project, so do it without asking; **never push**. The message is the request in one short line. `log done` prints `note uncommitted: …` when changes are left uncommitted. When the folder is not its own repository (no git, or inside another repository), do not commit and never `git init`.
- If the request cannot be finished because the user changed or withdrew it, close it with `dv-hmi-cli log cancel "<why>"`, then revert its changes (or, if other open work touched the same files, commit them as "취소: …") and run the tests. A fix is not cancelled: close it with `log done`, `--not-a-defect "<evidence>"`, or — when the user decides to leave it — `--declined` (their words are recorded); if it cannot be fixed, tell the user.
- Hooks keep this order: editing a file under `src/`, `tests/` or `.dv-hmi/design/devices.json` with nothing open is refused, and ending your turn with an entry open is stopped unless you wrote what it waits for this turn. When that happens, do what the message says; you never need to check the state yourself.

### Several things at once

- **Several wishes in one message**: one request each. Tell the user the order in one line, then open, do and close them one at a time. A message that refines what you are doing now ("make the range 0–60") is a note on the same request (`log note`); a different thing waits its turn.
- **Waiting for the user**: when a request needs an answer only the user can give (an address, a limit, what a command does), ask, and mark it: `dv-hmi-cli log wait "<the question you sent, word for word>"`. A waiting request may stay open while you open the next one; come back when the answer arrives. The same when the user's running app locks the build: ask them to press 중지 and wait.

### Continuing after a break

At the start of a session (the session-start hook also tells you): an open entry that was waiting — check whether the answer came; an open entry without a reason — read its notes and continue it. Uncommitted changes with nothing open — tell the user and ask whether to keep or discard them. Unmerged worktrees — merge or ask. Tell the user where it stopped and what was decided before the break.

### When the plugin or a library changed

When the session-start hook says the working rules were rewritten, read `.dv-hmi/guide.md` again, then build and run the tests before the next request; what broke is a fix.

### Never touch the app's data

Settings, recipe and record files, the record database and the alarm history are the user's: never edit or delete them, even to test. Change the app's files only with Edit/Write — git (commit, merge, revert) is the one shell exception.

## Decisions while you build

Before you build:

- **What moves or stops the device, or gives the operator a new refusal, lock or condition** (what a command does, interlocks, reset, start-up, stopping during a sequence, behaviour on communication loss): propose it to the user in plain words, as one bundle, and build it after they answer (`commands-and-safety.md`). While you wait it stays unbuilt (`log wait`). If the user does not know, use the safe default, tell them, and write it in `spec.md` as a `가정:` line.
- **What differs from a written promise**: propose it first (above).

After you build, in your report:

- **Added** is never something that narrows a written promise (a promised action refused, delayed or put under a new condition): that differs from the promise — propose it before you build, or build it the way the promise says.
- **Added**: what the promises do not say and the user will see (an error message, a confirmation dialog, how a button is pressed) — decide it, build it, and list it as one item starting `더한 것:` (`Added:` when the user writes English) with the name the user sees: `더한 것: [정지] 버튼은 한 번 누르면 바로 멈춰요`. Anything you added that the user should keep as a promise goes into `spec.md` when they approve.
- Code structure, names and how you build are yours; decimals and layout are yours too where the drafts and promises do not fix them. Units, display ranges and device-facing values (addresses, scale, limits, how a command is written) are never yours: they come from the memory map, the device manual or the user (`asking-the-user.md`).

Then report to the user: what you did, what you added (safety first), what changed in `spec.md`, and what to test by running the app (on the device or the simulator) — the user does that test.
