# Issues found

## Build warnings

* **NETSDK1138:** `net6.0-windows` is out of support. This is an acknowledged baseline condition for this inspection; do not upgrade it tonight.
* **CS8622:** `MainWindow.timer_Tick` uses a non-nullable `object sender` for a timer event whose delegate permits a nullable sender. The warning does not block the build, but it undermines the project’s otherwise enabled nullable analysis.

The supplied baseline reports 0 build errors and 4 warnings total.

## Runtime risks

* `MediaElement` playback depends on Windows-installed codecs. Files advertised by the dialog may fail to load or play; MKV is especially unreliable in the built-in WPF media pipeline.
* There is no `MediaFailed` handler, so unsupported, corrupt, inaccessible, or codec-incompatible media can fail with no useful feedback to the user.
* The duration is read only when `NaturalDuration.HasTimeSpan`. Live streams and media with unavailable/indeterminate metadata will never show a useful status, and no alternate state is shown.
* Playback controls are always enabled, including before a file is selected and while a file is still opening. Their results depend on `MediaElement` internal state rather than explicit application rules.

## Bugs

* The Open dialog filter is internally inconsistent: its label claims MP3 support, but the actual extension pattern ends with `*jpg`, not `*.mp3`. It can expose JPEG files under “Video files” and does not correctly include MP3 files.
* The filter’s description says “Video files” even though the screen is intended to be a video/audio player and appears to intend MP3 support.
* The label initially reads “Not playing...”, but after the first timer tick it changes to “No file selected...”; neither state is deliberately managed. After Stop or playback completion it can continue displaying a position/duration that implies active playback.

## Fragile code

* The `DispatcherTimer` is a constructor-local variable and is started indefinitely. The dispatcher retains it, but the window has no explicit way to stop it during teardown. This risks unnecessary work and object retention if the window lifecycle ever becomes more complex.
* Reset sets `Position` immediately after `Stop` with no check that source media is loaded or seekable. Behavior is delegated to the media implementation and may vary for failed, loading, or non-seekable media.
* File opening simply assigns `new Uri(fileName)` with no validation, exception handling, or user-facing recovery path.
* No event-driven synchronization exists for source changes, media opened, media ended, failure, buffering, or position. The one-second polling interval makes status inherently coarse.

## Obsolete/deprecated code

* The target framework is out of support (NETSDK1138). This is the primary platform-obsolescence issue and is deferred by the requested scope.
* `App.xaml.cs` contains unused `using` directives (`System`, collections, configuration, data, LINQ, tasks); they are harmless but indicate generated/prototype residue.
* `MainWindow.xaml` contains unused XML namespace declarations (`d` is used only for design-time background; `local` is unused) and an unused/poorly named Open button identifier (`OpenBtnAudioFile`).

## UI issues

* The window title remains “MainWindow” rather than the product name.
* The `MediaElement` and control panel use fixed dimensions and large/negative margins. The UI is not responsive to normal window resizing and is likely brittle under DPI or text scaling.
* “Open File” uses an 8-point font and a 45-pixel width, which is difficult to read and not localization-friendly.
* There are no disabled states, keyboard affordances beyond defaults, progress/seek controls, volume controls, loading feedback, or error messages.

## Architecture issues

* `MainWindow` owns the UI, file system interaction, media commands, display formatting, periodic polling, and implicit playback state. This is simple but not independently testable.
* The only window class is in namespace `MediaPlayer`, while the project/application namespace is `VideoAudioPlayer`; the mismatch is misleading even though the XAML compiles.
* There is no test project, logging, configuration, or diagnostic path for media failures.
* The application has no explicit source-of-truth state (no selected file, duration, playback state, or error state model).

## Future modernization opportunities

These are deliberately deferred; they are not recommendations to perform tonight.

1. Stabilize behavior first: correct the filter, add media lifecycle/error handling, and define valid command states.
2. Move to a supported Windows target framework only after behavior is covered by a small regression suite and deployment support is understood.
3. Introduce a modest view model/service boundary when the player gains more state or features; avoid a broad architecture rewrite for the current scope.
4. Evaluate a maintained media engine only if the product requires reliable cross-codec/container playback beyond the Windows/WPF media stack.
5. Replace fixed layout with WPF layout primitives, accessible labels, and scalable control sizing after playback behavior is stable.
