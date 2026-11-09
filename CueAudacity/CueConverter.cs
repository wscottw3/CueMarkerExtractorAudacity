using System.Globalization;
using System.Text;

namespace CueAudacity;

/// <summary>One label from an Audacity label export (start&lt;TAB&gt;end&lt;TAB&gt;text).</summary>
public record AudacityLabel(double Start, double End, string Text);

public enum AudioFileType { WAVE, MP3, AIFF }

public class CueOptions
{
    public string AlbumTitle { get; set; } = "";
    public string AlbumPerformer { get; set; } = "";
    public string AudioFileName { get; set; } = "audio.wav";
    public AudioFileType FileType { get; set; } = AudioFileType.WAVE;
    /// <summary>Split "Artist - Title" labels into PERFORMER and TITLE.</summary>
    public bool SplitArtistTitle { get; set; } = true;
    /// <summary>Insert a track at 00:00:00 for audio that precedes the first label.</summary>
    public bool AddLeadInTrack { get; set; } = false;
    public string LeadInTitle { get; set; } = "Intro";
}

public static class CueConverter
{
    /// <summary>Parses Audacity label text. Skips blank lines and spectral-selection lines (which start with "\").</summary>
    public static List<AudacityLabel> ParseLabels(string text)
    {
        var labels = new List<AudacityLabel>();
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim(' ', '\r', '﻿');
            if (line.Length == 0 || line.StartsWith('\\')) continue;
            var parts = line.Split('\t', 3);
            if (parts.Length < 2 || !TryParseNumber(parts[0], out var start) || !TryParseNumber(parts[1], out var end))
                continue;
            var label = parts.Length > 2 ? parts[2].Trim() : "";
            labels.Add(new AudacityLabel(start, end, label));
        }
        return labels.OrderBy(l => l.Start).ToList();
    }

    /// <summary>Audacity may write a comma decimal separator in some locales.</summary>
    private static bool TryParseNumber(string s, out double value) =>
        double.TryParse(s.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    /// <summary>Formats seconds as CUE MM:SS:FF (75 frames per second).</summary>
    public static string CueTime(double seconds)
    {
        var totalFrames = Math.Max(0, (long)Math.Round(seconds * 75, MidpointRounding.AwayFromZero));
        var frames = totalFrames % 75;
        var totalSeconds = totalFrames / 75;
        return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}:{frames:00}";
    }

    /// <summary>CUE has no escape mechanism for quotes, so swap double quotes for single.</summary>
    private static string Quoted(string s) => "\"" + s.Replace("\"", "'") + "\"";

    private static (string? Performer, string Title) SplitArtistTitle(string text)
    {
        var i = text.IndexOf(" - ", StringComparison.Ordinal);
        if (i < 0) return (null, text);
        var performer = text[..i].Trim();
        var title = text[(i + 3)..].Trim();
        return performer.Length == 0 || title.Length == 0 ? (null, text) : (performer, title);
    }

    public static string MakeCue(IReadOnlyList<AudacityLabel> labels, CueOptions options)
    {
        var sb = new StringBuilder();
        void Line(string s) => sb.Append(s).Append("\r\n");

        if (options.AlbumPerformer.Length > 0) Line($"PERFORMER {Quoted(options.AlbumPerformer)}");
        if (options.AlbumTitle.Length > 0) Line($"TITLE {Quoted(options.AlbumTitle)}");
        Line($"FILE {Quoted(options.AudioFileName)} {options.FileType}");

        var tracks = new List<(string? Performer, string Title, double Start)>();
        if (options.AddLeadInTrack && labels.Count > 0 && labels[0].Start > 0)
            tracks.Add((null, options.LeadInTitle, 0));
        foreach (var label in labels)
        {
            if (options.SplitArtistTitle)
            {
                var (p, t) = SplitArtistTitle(label.Text);
                tracks.Add((p, t, label.Start));
            }
            else tracks.Add((null, label.Text, label.Start));
        }

        for (var i = 0; i < tracks.Count; i++)
        {
            var track = tracks[i];
            Line($"  TRACK {i + 1:00} AUDIO");
            Line($"    TITLE {Quoted(track.Title)}");
            if (track.Performer != null) Line($"    PERFORMER {Quoted(track.Performer)}");
            else if (options.AlbumPerformer.Length > 0) Line($"    PERFORMER {Quoted(options.AlbumPerformer)}");
            Line($"    INDEX 01 {CueTime(track.Start)}");
        }
        return sb.ToString();
    }

    public static string DefaultExtension(AudioFileType type) => type switch
    {
        AudioFileType.MP3 => "mp3",
        AudioFileType.AIFF => "aiff",
        _ => "wav",
    };

    /// <summary>Reads a label file as UTF-8, falling back to the system ANSI code page if it isn't valid UTF-8.</summary>
    public static string ReadLabelFile(string path)
    {
        var bytes = File.ReadAllBytes(path);
        try
        {
            return new UTF8Encoding(false, true).GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Latin1.GetString(bytes);
        }
    }
}
