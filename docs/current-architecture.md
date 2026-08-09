# Current architecture

## Project structure

```text
VideoAudioPlayer.sln                 Single-solution WPF application
VideoAudioPlayer/
  VideoAudioPlayer.csproj            SDK-style WinExe targeting net6.0-windows
  App.xaml / App.xaml.cs             Application startup
  Themes/PlayerTheme.xaml            Shared dark player palette and reusable control styles
  Controls/AudioVisualPlaceholder    Reusable ambient audio-only viewport visual
  MainWindow.xaml                    Responsive playlist/sidebar, media display, and transport UI
  MainWindow.xaml.cs                 Player state, playback, UI synchronization, and navigation
  AssemblyInfo.cs                    WPF theme metadata
docs/                                Architecture, issue, and manual-test documentation
```

The project has no NuGet package dependencies, test project, view models, or services. It uses WPF `MediaElement` and `Microsoft.Win32.OpenFileDialog` from the Windows desktop framework.

## Major components

| Component | Responsibility |
| --- | --- |
| `App` | Starts `MainWindow.xaml`. |
| `PlayerTheme.xaml` | Supplies the reusable galaxy-black palette and shared styles for panels, buttons, list items, labels, and sliders. |
| `AudioVisualPlaceholder` | Uses native WPF storyboards for a slow, static-when-idle ambient display for audio items. |
| `MainWindow.xaml` | Hosts a responsive playlist sidebar, media display, status header, seek/volume controls, and transport bar. |
| `MainWindow` | Owns the explicit playback state, lightweight in-memory playlist, media events, UI synchronization, and control availability. |
| `DispatcherTimer` | Refreshes playback position only while playback is active. |

## Media playback flow

1. Opening one or more files creates an in-memory `List<Uri>` playlist and selects its first item.
2. `LoadCurrentItem` stops the progress timer, assigns the source, and reports `Loading media...`.
3. `MediaOpened` changes state to `Loaded`, exposes valid controls, and displays available duration metadata.
4. Play changes state to `Playing` and starts the timer. Pause, Stop, Reset, Ended, and Failed stop the timer deterministically.
5. `MediaEnded` changes state to `Stopped`; `MediaFailed` changes state to `Failed` and displays the media exception message.

When the selected item has a supported audio extension, the media viewport uses `AudioVisualPlaceholder` instead of showing the hidden `MediaElement` surface. Its animation runs only during `Playing` and is stopped for pause, stop, reset, end, failure, and item changes. Video entries retain the normal `MediaElement` viewport.

The explicit states are `Idle`, `Loaded`, `Playing`, `Paused`, `Stopped`, and `Failed`. `Idle` also covers the short loading interval because no separate loading state was requested.

## File selection and navigation flow

The Open Files dialog supports selecting multiple files. Its filters include MP4, MKV, AVI, MOV, WMV, MP3, WAV, WMA, M4A, and AAC, plus an All files fallback. The player maintains the selected files only in memory for the current window session. The sidebar displays file names and lets the user select an item. Previous and Next load adjacent entries; they do not auto-play them. The seek slider synchronizes with the existing timer and can set a loaded item's position; the volume slider directly controls `MediaElement.Volume`.

Container extensions are a convenience filter, not a playback guarantee. WPF `MediaElement` remains dependent on codecs installed on Windows.

## Current architectural style

The app remains deliberately small and event-driven: all UI behavior lives in `MainWindow` code-behind. Phase 1 adds narrowly scoped state and playlist fields rather than a full MVVM conversion. This supplies a clean seam for the next playlist-focused pass while retaining the working foundation.

## Technical debt and deferred work

* Target framework `net6.0-windows` is out of support; it remains unchanged by request.
* The WPF media pipeline has variable codec/container support, particularly for MKV variants.
* There is still no persistent playlist, automatic next-item behavior, logging, or automated tests.
* The new responsive dark UI is intentionally limited to WPF native controls and styles; a richer visual system is deferred.
* The window class remains in `MediaPlayer` while the application/project uses `VideoAudioPlayer`; this is a non-functional naming inconsistency to address later.
