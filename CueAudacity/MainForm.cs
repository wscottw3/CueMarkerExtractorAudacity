using System.Text;

namespace CueAudacity;

public class MainForm : Form
{
    private readonly CueOptions _options = new();
    private List<AudacityLabel> _labels = new();
    private string? _sourcePath;

    private readonly Label _sourceLabel = new() { AutoSize = true, Text = "or drop an Audacity label .txt file anywhere in this window", ForeColor = SystemColors.GrayText, Anchor = AnchorStyles.Left };
    private readonly Button _saveButton = new() { Text = "Save CUE…", AutoSize = true, Enabled = false };
    private readonly TextBox _audioName = new() { Dock = DockStyle.Fill };
    private readonly ComboBox _fileType = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
    private readonly TextBox _albumTitle = new() { Dock = DockStyle.Fill };
    private readonly TextBox _albumPerformer = new() { Dock = DockStyle.Fill };
    private readonly CheckBox _split = new() { Text = "Split “Artist - Title” labels into PERFORMER and TITLE", AutoSize = true, Checked = true };
    private readonly CheckBox _leadIn = new() { Text = "Add a track at 00:00:00 for audio before the first label, titled", AutoSize = true };
    private readonly TextBox _leadInTitle = new() { Text = "Intro", Width = 140, Enabled = false };
    private readonly TextBox _preview = new()
    {
        Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false,
        Dock = DockStyle.Fill, Font = new Font(FontFamily.GenericMonospace, 10f), BackColor = SystemColors.Window,
    };
    private readonly ToolStripStatusLabel _status = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };

    public MainForm()
    {
        Text = "Audacity Labels → CUE";
        Width = 760;
        Height = 600;
        MinimumSize = new Size(640, 480);
        AllowDrop = true;
        KeyPreview = true;

        var openButton = new Button { Text = "Open Label File…", AutoSize = true };
        openButton.Click += (_, _) => OpenDialog();
        _saveButton.Click += (_, _) => SaveDialog();

        var top = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3 };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        top.Controls.Add(openButton, 0, 0);
        top.Controls.Add(_sourceLabel, 1, 0);
        top.Controls.Add(_saveButton, 2, 0);

        _fileType.Items.AddRange(Enum.GetNames<AudioFileType>());
        _fileType.SelectedIndex = 0;

        var form = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(0, 8, 0, 8) };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddRow(form, "Audio file name:", _audioName);
        AddRow(form, "File type:", _fileType);
        AddRow(form, "Album title (optional):", _albumTitle);
        AddRow(form, "Album performer (optional):", _albumPerformer);
        form.Controls.Add(_split);
        form.SetColumnSpan(_split, 2);
        var leadInRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty };
        leadInRow.Controls.Add(_leadIn);
        leadInRow.Controls.Add(_leadInTitle);
        form.Controls.Add(leadInRow);
        form.SetColumnSpan(leadInRow, 2);

        var previewHeader = new Label { Text = "Preview", Dock = DockStyle.Top, AutoSize = true, Font = new Font(Font, FontStyle.Bold) };

        var statusStrip = new StatusStrip();
        statusStrip.Items.Add(_status);

        var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
        // Dock order: last added docks first, so add the Fill control first.
        content.Controls.Add(_preview);
        content.Controls.Add(previewHeader);
        content.Controls.Add(form);
        content.Controls.Add(top);

        Controls.Add(content);
        Controls.Add(statusStrip);

        _audioName.TextChanged += (_, _) => { _options.AudioFileName = _audioName.Text; UpdatePreview(); };
        _albumTitle.TextChanged += (_, _) => { _options.AlbumTitle = _albumTitle.Text; UpdatePreview(); };
        _albumPerformer.TextChanged += (_, _) => { _options.AlbumPerformer = _albumPerformer.Text; UpdatePreview(); };
        _split.CheckedChanged += (_, _) => { _options.SplitArtistTitle = _split.Checked; UpdatePreview(); };
        _leadIn.CheckedChanged += (_, _) => { _options.AddLeadInTrack = _leadIn.Checked; _leadInTitle.Enabled = _leadIn.Checked; UpdatePreview(); };
        _leadInTitle.TextChanged += (_, _) => { _options.LeadInTitle = _leadInTitle.Text; UpdatePreview(); };
        _fileType.SelectedIndexChanged += (_, _) => FileTypeChanged();

        DragEnter += (_, e) =>
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true) e.Effect = DragDropEffects.Copy;
        };
        DragDrop += (_, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files) LoadFile(files[0]);
        };
        KeyDown += (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.O) { OpenDialog(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.S) { SaveDialog(); e.Handled = true; }
        };

        UpdatePreview();
    }

    private static void AddRow(TableLayoutPanel table, string caption, Control control)
    {
        table.Controls.Add(new Label { Text = caption, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 6, 3, 3) });
        table.Controls.Add(control);
    }

    private string CueText => _labels.Count == 0 ? "" : CueConverter.MakeCue(_labels, _options);

    private void UpdatePreview()
    {
        _preview.Text = _labels.Count == 0 ? "No labels loaded." : CueText;
        _saveButton.Enabled = _labels.Count > 0;
    }

    private void FileTypeChanged()
    {
        _options.FileType = Enum.Parse<AudioFileType>((string)_fileType.SelectedItem!);
        // Keep the audio file's extension in step with the chosen type.
        var baseName = Path.GetFileNameWithoutExtension(_audioName.Text);
        if (baseName.Length > 0) _audioName.Text = baseName + "." + CueConverter.DefaultExtension(_options.FileType);
        UpdatePreview();
    }

    public void LoadFile(string path)
    {
        try
        {
            var parsed = CueConverter.ParseLabels(CueConverter.ReadLabelFile(path));
            _sourcePath = path;
            _labels = parsed;
            _sourceLabel.Text = Path.GetFileName(path);
            _sourceLabel.ForeColor = SystemColors.ControlText;
            _audioName.Text = Path.GetFileNameWithoutExtension(path) + "." + CueConverter.DefaultExtension(_options.FileType);
            _status.Text = parsed.Count == 0
                ? $"No labels found in {Path.GetFileName(path)}."
                : $"Loaded {parsed.Count} label{(parsed.Count == 1 ? "" : "s")} from {Path.GetFileName(path)}.";
            UpdatePreview();
        }
        catch (Exception ex)
        {
            _status.Text = $"Couldn't read file: {ex.Message}";
        }
    }

    private void OpenDialog()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Choose an Audacity label file (File > Export > Export Labels)",
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
        };
        if (dialog.ShowDialog(this) == DialogResult.OK) LoadFile(dialog.FileName);
    }

    private void SaveDialog()
    {
        if (_labels.Count == 0) return;
        using var dialog = new SaveFileDialog
        {
            Filter = "CUE sheet (*.cue)|*.cue|All files (*.*)|*.*",
            DefaultExt = "cue",
            FileName = _sourcePath != null ? Path.GetFileNameWithoutExtension(_sourcePath) + ".cue" : "labels.cue",
            InitialDirectory = _sourcePath != null ? Path.GetDirectoryName(_sourcePath) : null,
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            File.WriteAllText(dialog.FileName, CueText, new UTF8Encoding(false));
            _status.Text = $"Saved {Path.GetFileName(dialog.FileName)}.";
        }
        catch (Exception ex)
        {
            _status.Text = $"Couldn't save: {ex.Message}";
        }
    }
}
