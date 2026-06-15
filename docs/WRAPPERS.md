# Wrappers — desktop (TV) + iOS (M11)

How to build Cheddar & Cocoa as an installable **desktop app** (Mac/PC at the TV) and an
**iOS app** (iPad/iPhone), from the same web build, with **zero runtime network requests**.

> We use **Tauri v2** for *both* desktop and iOS (Tauri v2 added native iOS/Android). This is a
> deliberate simplification of the original plan (which paired Tauri-desktop with Capacitor-iOS):
> one shell, one config, one CSP, one place to keep the zero-network rule honest. The web build
> (`dist/index.html`, a single self-contained file from `vite-plugin-singlefile`) is the payload
> in every shell — the Rust/native layer just opens a window onto it and adds **no** plugins,
> commands, fs, shell, or network permissions.

## What's in the repo

- `src-tauri/` — the Tauri crate: `Cargo.toml`, `tauri.conf.json`, `build.rs`, `src/{main,lib}.rs`,
  `capabilities/default.json` (core perms only), and a generated `icons/` set.
- `assets/icon.svg` + `assets/icon.png` — the app-icon source (regenerate icons with
  `npm run tauri icon assets/icon.png`).
- npm scripts: `desktop:dev` (`tauri dev`), `build:desktop` (`tauri build`), `tauri` (raw CLI).
- `tauri.conf.json` points `frontendDist` at `../dist` and runs `npm run build` automatically
  before bundling, so the shipped app always contains the latest inlined web build.

## One-time toolchain setup

These are **not** installable headlessly in CI/sandbox — run them on your Mac once.

1. **Rust** (required for any Tauri build, desktop or mobile):
   ```sh
   curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh
   # then restart the shell, or: source "$HOME/.cargo/env"
   ```
2. **Xcode** (required for iOS only — Command Line Tools alone are not enough):
   install from the App Store, then `sudo xcode-select -s /Applications/Xcode.app` and
   `xcodebuild -runFirstLaunch`.

Check readiness any time with `npm run tauri info` (it reports rustc/Cargo/Xcode status).

## Desktop (Mac/PC → TV)

```sh
npm run desktop:dev     # hot-reload dev window onto the Vite dev server (port 5173)
npm run build:desktop   # bundles dist/ → a signed .app/.dmg (macOS), .msi/.exe (Windows)
```
Output lands in `src-tauri/target/release/bundle/`. Plug in two USB/Bluetooth gamepads and
confirm couch co-op (M10): the title "press Ⓐ on controller 2 to join", both dogs driven
independently, all four rounds.

## iOS (iPad / iPhone)

```sh
npm run tauri ios init      # one-time: generates src-tauri/gen/apple (Xcode project)
npm run tauri ios dev       # run on a simulator or tethered device
npm run tauri ios build     # archive for install / TestFlight
```
- Install on your own devices via Xcode with a **free** Apple ID, or distribute via **TestFlight**
  ($99/yr Apple Developer Program). Pair controllers over Bluetooth.
- The `gen/apple` project is generated (gitignored); re-run `ios init` on a fresh checkout.

## Verifying the hard rule (zero network) in every shell

The web build is already audited (single `dist/index.html`, no external URLs, nothing fetched).
After wrapping, re-confirm per shell because wrappers can silently reintroduce remote assets:

- The CSP in `tauri.conf.json` allows **no http(s) origin** (`default-src 'self'`,
  `connect-src 'self'`) — a remote fetch is blocked, not just discouraged.
- Run the built app **fully offline** (Wi-Fi off) and play a full game.
- Optional: watch with a proxy/Charles or `nettop -p <pid>` and confirm zero outbound connections.

## Status

Scaffolding + config + icons + scripts are committed and validated (`tauri info` parses the
config and detects the Vite bundler). The actual binary builds and on-device verification are
gated on the one-time toolchain setup above + physical devices, so they happen on your machine.
