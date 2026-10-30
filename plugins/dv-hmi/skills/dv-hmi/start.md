# Start

The procedure the agent follows when a user starts building a new HMI program with dv-hmi. The plugin's `dv-hmi` skill leads here; the plugin ships a copy of this file next to the skill.

- Target: the Claude desktop app (Code tab).
- **The user may not be a developer.** Ask and explain in plain words, in the user's language — everything the user sees, including the short progress lines between steps and any next-step box (this document being English does not change that). Do not bring up technical names (WinForms, OpenTK, csproj, NuGet …) unless the user uses them first.
- The preparation work (template, packages, build, guide file, git repository) is done entirely by `dv-hmi-cli setup`. The agent asks the questions and records each answer with `dv-hmi-cli answer`; setup uses the recorded answers. Do not do the same work by hand.
- The user follows the progress on the dashboard (questions answered, setup steps). During start it shows only what the CLI recorded in `.dv-hmi/state.json`, so an answer the agent does not record with `answer` does not appear there.
- Command output and exit codes are defined by `dv-hmi-cli -h`.

## 1. When

The user opens a session on a folder and says they want to build something with dv-hmi. Example: "dv-hmi로 온도 모니터링 프로그램 만들거야" (I'm going to build a temperature monitoring program with dv-hmi). **The session's working folder is the project folder.**

Remember what the user first said they want to build. Do not build screens or code from it during start. It is input for the next stage.

## 2. Steps

### 2.1 Check the state

```bash
dv-hmi-cli status
```

| Output | What to do |
|---|---|
| `folder new` | New project. Go to 2.2. `name`, `mode` and `target` lines are answers already recorded (an earlier session stopped during the questions); do not ask them again. `suggest` is the name to propose |
| `folder project` + `setup done` | Already prepared. Open the dashboard (2.2), but do not run the rest of start again. Then continue from the `stage` line following the project `CLAUDE.md`: `stage requests` (assistant mode, or workflow mode after verify) → work on the user's requests; another stage (workflow mode) → the working rules send you to the workflow document's section for that stage |
| `folder project` + `setup failed …` or `setup running` | A previous preparation did not finish. Open the dashboard (2.2), then rerun 2.4 without asking again |
| `folder not-empty` | Start only works in an empty folder. Tell the user: "A new project has to start in an empty folder. Please create an empty folder and open it again." Then stop |
| `sdk none` or `sdk … old` | The development tools are missing or too old. Tell the user: ".NET 9 SDK or later is required. Please install it from https://dotnet.microsoft.com/download and tell me when it's done." Then stop |
| `git none` together with `folder new` | git is recommended, not required. After opening the dashboard (2.2) and before question ①, tell the user **once**, as a note in the same message as the question: "git (a version-history tool) is not installed on this PC. Without it I cannot split the work to build faster, and earlier states cannot be restored. If you want it, run `winget install Git.Git` in a terminal (or install it from https://git-scm.com); otherwise we continue without it." Do not wait for an answer and do not mention it again. This is an environment check like the SDK, not a fourth question |

Exit code 1 means one of the `folder not-empty` or `sdk` cases above (`git none` does not change it).

### 2.2 Open the dashboard

```bash
dv-hmi-cli dashboard --lang <ko|en>
```

- `--lang` is the user's language (the one they wrote in): `ko` for Korean, `en` otherwise. The page opens in it, and the user can switch it there.
- Record the same language for setup, which writes it into the project's `CLAUDE.md`: `dv-hmi-cli answer lang <ko|en>`. It is not a question; do not ask it.
- `started <url> …` or `running <url> …` (exit code 0): open `<url>` in the built-in browser pane on the right of the app with the browser tools (`preview_start` with `url`, or `navigate` if a dashboard tab is already open). Do not ask the user to open it.
- The address changes every run. Always use the printed `<url>`.
- Opening a new address may ask the user for permission. If it is denied, do not retry; give the user the `<url>`.
- Exit code 1: tell the user the reason from standard error and continue without the dashboard.

### 2.3 Three questions

Ask in chat, **one at a time**, and move on only after the answer. Keep the content below; adapting the wording to the conversation is fine.

As soon as the user answers, record it, then ask the next question:

```bash
dv-hmi-cli answer name <name>
dv-hmi-cli answer mode workflow|assistant
dv-hmi-cli answer target windows|senvas
```

| Output | Exit code | What to do |
|---|---|---|
| `saved <key> <value>` | 0 | Next question |
| `failed name: <reason>` | 1 | If the name does not fit the rule, explain why in plain words, suggest one that fits, and ask again. Otherwise follow the 2.1 table |
| `failed mode: …` or `failed target: …` | 1 | Folder not empty or already prepared: follow the 2.1 table |
| Invalid arguments | 2 | Fix the arguments and run again |

If the user changes an answer later, record it again with the same command.

**① Project name** — propose the `suggest` name from `status`.

> Is the project name '**TempMonitor24CH**'?
> If so, the program (source project) will get the same name.

- If the user gives another name, use it. A name starts with an English letter and uses only English letters, digits and `_` (64 characters or fewer). If it does not fit, explain why in plain words and suggest one that fits. Example: "Only English letters and digits can be used in the name. How about 'TempMonitor'?"

**② How to proceed**

The stage names are the ones the dashboard shows; in Korean they are 인터뷰 → 계획 → 구현 → 검증 (구현, not 제작).

> Please choose how we proceed.
>
> 1. **Workflow mode** — We go in order: interview → plan → build → verify. In the interview we draft the screens together and I write down the requirements; once you approve them, I plan the work and build it, and at the end I give you a list of what to test. After that, I handle your requests one at a time. Best when building a program properly from start to finish.
> 2. **Assistant mode** — No fixed order. Tell me what you want whenever you want it, and I build or change it right away. Best for small changes or trying things out.

**③ Where it runs**

> Where will the program be used?
>
> 1. **Windows PC** — a program used on a regular PC monitor with mouse and keyboard
> 2. **Touchscreen panel (Senvas)** — a program operated by finger on a touch screen mounted next to the equipment

Mapping answers to `answer` values:

| Answer | Command |
|---|---|
| Workflow mode / Assistant mode | `answer mode workflow` / `answer mode assistant` |
| Windows PC / Touchscreen panel | `answer target windows` / `answer target senvas` |

Rules:

- Do not ask again what the user already said; just confirm it. Example: if the user first said "for a touch screen", ③ becomes "I'll build it for the touchscreen panel (Senvas), right?"
- **Ask nothing besides these three** (the git note of 2.1 is information, not a question). Screen size, theme, screen language, communication devices and screen layout are decided in the next stage or changed when the user asks. Start asks only what is hard to change later (name, mode, where it runs). The generated program shows English by default with Korean selectable; where its switch goes is decided with the app's frame when the screens are designed (`screens.md`). Change it when the user asks.

### 2.4 Preparation

Tell the user in one line:

- Workflow mode: "I'll now prepare the project. When it's done, we'll start the interview."
- Assistant mode: "I'll now prepare the project. When it's done, just tell me what you'd like to do."

```bash
dv-hmi-cli setup
```

- Without options it uses the recorded answers.
- It downloads packages and builds, so it can take 1–3 minutes. Give the command a generous timeout (e.g. 10 minutes). The dashboard shows each step.
- Do not report each output line. Report only the result at the end.

| Output (last line) | Exit code | What to do |
|---|---|---|
| `done <path>` | 0 | 2.5 |
| `failed check: not answered yet: …` | 1 | Ask and record the missing answers (2.3; `lang` is recorded without asking, 2.2), then run again |
| `failed check: <other reason>` | 1 | Rejected by the check before starting (not empty, SDK, already prepared): follow the 2.1 table |
| `failed packages: …` | 1 | The libraries could not be downloaded. Tell the user: "Please check the internet connection." When the user says it's checked, run the same command again |
| `failed build: …` or any other `failed <step>: …` | 1 | Read `.dv-hmi/setup.log` and explain the cause in plain words. It is most likely a plugin problem, so tell the user as it is |
| Invalid arguments | 2 | Fix the arguments and run again |

Running the same command again starts over from the beginning. After setup has started the name cannot change; mode and target can still be changed with `answer` before running it again.

### 2.5 Finish

Tell the user in one line. Example: "The TempMonitor24CH project is ready."

From here on, the working rules for this folder are in the `CLAUDE.md` that setup wrote and the `.dv-hmi/guide.md` it imports (program structure, where the library docs are, the index of the knowledge files, which stay in the plugin, and where `workflow.md` and `assistant.md` are). **This session started before they existed, so it has not loaded them: read `CLAUDE.md` and `.dv-hmi/guide.md` first.** Then:

- Workflow mode: setup moved the stage to `interview`. Read `workflow.md` next to this file and start its section 1 (Interview). If the user said something at the beginning ("a temperature monitoring program"), that is the first part of the description — do not ask for it again.
- Assistant mode: setup moved the stage to `requests`. Read `assistant.md` next to this file. Wait for the user's request. If the user said something at the beginning, ask what to do first.

## 3. Do not

- Do not do setup's work (writing the template, adding packages, building, the guide file, git) by hand. If it fails, fix the cause and run setup again.
- Do not write or edit files inside `.dv-hmi/` during start (answers go through `dv-hmi-cli answer`). After setup, the working rules (`.dv-hmi/guide.md`) name the files you write there.
- Do not build screen or communication code during start.

## 4. Not decided yet

- Folders that already contain files (adding dv-hmi to an existing program)
