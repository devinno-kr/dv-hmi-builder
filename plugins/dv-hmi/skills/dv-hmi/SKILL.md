---
name: dv-hmi
description: Build a C# HMI program (equipment monitoring/control screens) with Devinno.UI and Devinno.Communication. Use when the user says they want to build or continue a program with dv-hmi (e.g. "dv-hmi로 온도 모니터링 프로그램 만들거야"), or asks for dv-hmi by name.
---

# dv-hmi

Entry point of the dv-hmi plugin. It decides where to go; the procedures are in other files.

1. Read `start.md` in this skill's folder and follow it. Its step 2.1 runs `dv-hmi-cli status` in the session's working folder (the project folder) and says what to do for every result: a new folder, a prepared project, an unfinished setup, a folder that is not empty, a missing .NET SDK. `dv-hmi-cli` is on the PATH while the plugin is enabled.
2. Once the project is prepared, the working rules are in the project's `CLAUDE.md`, which setup wrote.

Rules:

- Everything the user sees from you is in the user's language (the language of their messages): the short progress lines between steps, questions, reports, and the text of any next-step box and its buttons. These documents are written in English; that does not make English the user's language.
- Usage of `dv-hmi-cli` (commands, output, exit codes) comes only from `dv-hmi-cli -h`.
- Do not read the plugin's own files (`bin/`, the CLI) or look for its source to learn how it works. What you need is in `start.md`, `dv-hmi-cli -h`, and after setup the project's `CLAUDE.md` (with the working rules it imports, which name the knowledge files to read and `workflow.md` or `assistant.md` in this skill's folder, by mode).
