//! Tauri shell for Cheddar & Cocoa.
//!
//! The whole game is the bundled web frontend (a single self-contained `dist/index.html`,
//! zero runtime network — see CLAUDE.md). This shell just opens a window onto it and adds no
//! commands or plugins, so there is no extra attack surface and nothing to fetch remotely.
//! `#[mobile_entry_point]` lets the identical crate build the iOS/Android app under Tauri v2.

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .run(tauri::generate_context!())
        .expect("error while running the Cheddar & Cocoa Tauri application");
}
