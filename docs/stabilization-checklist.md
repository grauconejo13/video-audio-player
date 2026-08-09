# Stabilization manual regression checklist

Run this checklist on a Windows machine with known playable media before making playlist or visual changes.

## Setup

* Start the application and confirm Play, Pause, Stop, Reset, Previous, and Next are disabled before media selection.
* Prepare a known-good MP3 and MP4. Include one intentionally unsupported/corrupt file if available.

## File selection

* Open the dialog and verify audio and video filters list the intended extensions, including MP3 and not JPG.
* Select one playable file. Confirm the current-item label updates, status first shows loading, then shows Loaded, and Play/Reset enable.
* Select two or more files. Confirm the current-item label shows `1 of N` and Next enables.

## Playback state

* Press Play. Confirm position updates about once per second; Pause and Stop enable while Play disables.
* Press Pause. Confirm time stops changing; Play, Stop, and Reset are enabled.
* Resume. Confirm time advances again.
* Press Stop. Confirm time resets/stops, the timer no longer updates, and Play/Reset remain available.
* Press Reset from a loaded, paused, or stopped item. Confirm position is zero and playback does not restart.
* Let a short file end. Confirm status says playback finished, no timer update continues, and Play can restart the item.

## Navigation and failure

* Use Next and Previous. Confirm the adjacent item loads, the index/filename updates, and unavailable navigation directions disable.
* Attempt to open the unsupported/corrupt file. Confirm status shows an unable-to-play message, playback controls remain disabled, and navigation can move to another playlist item.

## Expected limitations

* A listed container can still fail because Windows lacks its required codec.
* Selected playlist entries exist only until the application closes.
