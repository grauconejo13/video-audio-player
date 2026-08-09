# Issues found

## Build warnings

* **NETSDK1138:** `net6.0-windows` is out of support. It is intentionally not upgraded in this stabilization pass.
* **CS8622:** The timer and media-failure callbacks previously had nullability mismatches. They now accept `object? sender`; the stabilization build verifies this warning is resolved.

## Runtime risks

* `MediaElement` uses the Windows media stack. A listed extension does not guarantee that its codec will open or play, especially for MKV files and uncommon codec variants.
* File loading is asynchronous. The controls remain disabled until `MediaOpened`, but a source that never raises either open or failure remains a platform-level edge case.

## Bugs fixed in this pass

* Corrected the malformed media-file filter that offered `*jpg` in place of `*.mp3`.
* Added media-opened, media-ended, and media-failed handling with clear status messages.
* Added explicit playback state and state-aware controls, preventing invalid player actions before media loads.
* Replaced the continuously running timer with a timer active only during playback and stopped on all requested terminal events.
* Added lightweight, in-memory multi-file navigation with Previous and Next controls.

## Fragile code / remaining limitations

* There is no timeout, cancellation, or detailed diagnostic logging for a media source that fails to resolve cleanly.
* Reset/stop behavior is intentionally basic and depends on `MediaElement` semantics.
* State, playlist, and UI remain combined in the window code-behind; this is controlled scope, not a full architecture boundary.

## Obsolete/deprecated code

* `net6.0-windows` is out of support. Framework modernization remains deferred.
* The project has leftover prototype naming and unused generated-code imports in `App.xaml.cs`.

## UI issues

* The UI still uses WPF default styling and fixed layout values.
* The future dark galaxy/near-black visual direction, custom controls, playlist panel, seek controls, and volume controls are not implemented in this pass.

## Architecture issues

* No persistent playlist format, playback history, view model, service abstraction, logging, or automated tests exist yet.
* The `MediaPlayer` window namespace differs from the `VideoAudioPlayer` application namespace.

## Future modernization opportunities

1. Add a dedicated playlist model with add/remove/reorder and an on-screen playlist panel.
2. Add media metadata, seek, volume, and configurable end-of-item behavior.
3. Establish test coverage for state transitions and playlist navigation before a framework upgrade.
4. Move to the dark custom UI in a separate visual pass, preserving the stabilized behavior.
5. Assess a different media engine only if reliable broad-codec support is a product requirement.
