# AlarmClock

Alarm clock application with a touch-friendly UI built with **Avalonia UI**.  
The target device is a **Raspberry Pi Zero 2 W** running the app in a kiosk-like full-screen mode.

This repo is a practical project (built to be used), so some parts evolved organically as features were added.

## Goals / constraints

- Runs on low-power ARM hardware (Raspberry Pi Zero 2 W)
- Full-screen / kiosk-style UI
- Split into small components to keep hardware-specific / platform-specific concerns isolated

## Solution structure

The repository is a multi-project .NET solution. Key projects: :contentReference[oaicite:1]{index=1}

- `AlarmClock` — main application / entry point
- `AlarmClock.Display` — UI / display-related code (Avalonia)
- `AlarmClock.Configuration` — configuration model + loading/saving
- `AlarmClock.Logging` — logging setup / helpers
- `AlarmClock.Process` — process/runtime orchestration pieces
- `AlarmClock.Audio` — audio output
- `AlarmClock.Buzzer` — buzzer / alarm signaling
- `AlarmClock.Announcer` — announcements / voice prompts (if enabled)
- `AlarmClock.Radio` — radio / stream playback (if enabled)
- `AlarmClock.Weather` — weather integration (if enabled)
- `AlarmClock.Shared` — shared types/utilities used across projects

## Build & run (development)

General workflow:

- Build on a desktop machine and run locally for UI iteration.
- Deploy/publish for Linux ARM and run on the Raspberry Pi in full-screen mode.

(Exact deployment scripts/service units are intentionally not documented here yet; the repo is still evolving.)

## Status

Work in progress. APIs and internal structure may change as the device setup and UI are refined.
