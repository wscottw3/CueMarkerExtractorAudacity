# CueAudacity for Windows

Converts an Audacity label file (File ▸ Export ▸ Export Labels…) into a `.cue` sheet.

Each label becomes a track. Labels written as `Artist - Title` are split into `PERFORMER` and `TITLE`, and label start times become `INDEX 01` times (MM:SS:FF, 75 frames per second).

## Download

Get `CueAudacity.exe` from the [Releases](../../releases) page. It is a single self-contained file — no .NET install needed. Windows may show a SmartScreen warning because the app is unsigned; choose **More info ▸ Run anyway**.

## Use

1. Open a label file: **Open Label File…** (Ctrl+O), drop it on the window, or drop it onto `CueAudacity.exe`.
2. Adjust options: audio file name and type (WAVE / MP3 / AIFF), optional album title and performer, Artist–Title splitting, and an optional track at 00:00:00 for audio before the first label.
3. Check the preview, then **Save CUE…** (Ctrl+S).

Command line:

```
CueAudacity.exe --convert labels.txt [output.cue] [audio-file-name]
```

## Example

Input (`HOUR_1_SEGMENT_1A.txt`):

```
82.153262	82.153262	The Grassroots - Midnight Confessions
254.495862	254.495862	Marvin Gaye & Tammi Terrell - Ain't Nothin Like the Real Thing
```

Output:

```
FILE "HOUR_1_SEGMENT_1A.wav" WAVE
  TRACK 01 AUDIO
    TITLE "Midnight Confessions"
    PERFORMER "The Grassroots"
    INDEX 01 01:22:11
  TRACK 02 AUDIO
    TITLE "Ain't Nothin Like the Real Thing"
    PERFORMER "Marvin Gaye & Tammi Terrell"
    INDEX 01 04:14:37
```

## Build

Requires the .NET 10 SDK. On Windows run `build.cmd`; on macOS/Linux run `./build.sh`. The exe is written to `build\CueAudacity.exe`.
