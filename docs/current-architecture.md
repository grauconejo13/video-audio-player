# Current architecture

## Scope and baseline

This document describes the existing implementation as inspected on 2026-08-09. It is intentionally descriptive: no application-code, dependency, framework, or UI changes were made.

## Project structure

```text
VideoAudioPlayer.sln                 One solution, Debug/Release Any CPU
VideoAudioPlayer/
  VideoAudioPlayer.csproj            SDK-style WPF executable project
  App.xaml / App.xaml.cs             Application entry point
  MainWindow.xaml                    Entire UI
  MainWindow.xaml.cs                 All UI, playback, file-dialog, and timer logic
  AssemblyInfo.cs                    WPF theme metadata
```

The project targets `net6.0-windows`, uses WPF, and enables nullable reference types. There are no test projects, shared libraries, view models, services, resource dictionaries, or package references.

## Major components

| Component | Current responsibility |
| --- | --- |
| `App` | Starts `MainWindow.xaml` through `StartupUri`. |
| `MainWindow.xaml` | Defines a `MediaElement`, a status label, and Open/Play/Pause/Stop/Reset buttons. |
| `MainWindow` code-behind | Wires every button click, opens files, invokes `MediaElement` operations, and refreshes elapsed/duration text each second. |
| `MediaElement` | Relies on the WPF/Windows media stack for local media rendering and audio playback. |

There is a namespace inconsistency: the assembly/application namespace is `VideoAudioPlayer`, while `MainWindow` is declared as `MediaPlayer.MainWindow` in both XAML and code-behind. This currently compiles because XAML uses the fully qualified class, but it is confusing maintenance debt.

## Media playback flow

1. The application starts `MainWindow`.
2. The window creates and starts a one-second `DispatcherTimer`.
3. The user selects a file; its filesystem path becomes a `Uri` assigned to `mediaElement.Source`.
4. With `LoadedBehavior="Manual"`, playback waits for the user to press Play.
5. Play, Pause, and Stop directly call the corresponding `MediaElement` methods. Reset calls Stop and sets `Position` to zero.
6. On each timer tick, if a source and a duration are available, the label shows `Position / NaturalDuration`; otherwise, before selection only, it shows “No file selected...”.

The app has no explicit media lifecycle handling (`MediaOpened`, `MediaEnded`, `MediaFailed`, buffering events) and no maintained playback-state model. Codec and duration availability are therefore treated implicitly.

## File selection/opening flow

The Open button creates a `Microsoft.Win32.OpenFileDialog`, shows it modally, and assigns the selected path as a local URI. The code does not validate the file, inspect its type, reset the UI status, start playback, catch URI/media errors, or surface a failed-open message.

The intended filter appears to include MP4, MKV, AVI, and MP3. Its actual pattern is malformed: it lists `*.mp3` in the display text but uses `*jpg` in the extension pattern. MP3 is therefore not correctly filtered, and image-like `*.jpg` matches are offered despite the text saying “Video files.”

## Important dependencies

* .NET SDK 8.0.423 is the build SDK reported for this inspection.
* Target framework: `net6.0-windows`.
* Framework dependency: `Microsoft.WindowsDesktop.App.WPF`, including WPF `MediaElement`.
* File dialog: `Microsoft.Win32.OpenFileDialog` from the Windows desktop stack.
* NuGet dependencies: none declared or restored for the project.

Media support is therefore entirely dependent on the codecs/media capabilities installed on the user’s Windows system. The selected extensions do not guarantee that WPF can decode the files, particularly MKV and codec variants within otherwise familiar containers.

## Current architectural style

This is a small, event-driven, code-behind WPF application. UI layout is fixed in XAML and behavior is implemented directly in the window class. It is appropriate for a minimal prototype, but it combines presentation, application state, and media integration in one class and offers no automated verification seam.

## Technical debt

* The UI mixes absolute sizes/margins with a negative margin, making layout brittle on resize, DPI scaling, localization, and accessibility settings.
* The status timer is created as a local variable, cannot be stopped/disposed by the window, and runs continuously even while no media is loaded.
* Status text is timer-driven instead of media-event/state-driven, so it gives incomplete or stale feedback after stop, end-of-media, loading, or failure.
* No user-visible error path exists for unsupported, inaccessible, corrupt, or codec-incompatible media.
* No test project or testable separation exists for file selection, state transitions, or status formatting.
* Namespaces, XAML names, control names, comments, and window title retain prototype-era naming (`MediaPlayer`, `OpenBtnAudioFile`, “Video files”, `MainWindow`).
