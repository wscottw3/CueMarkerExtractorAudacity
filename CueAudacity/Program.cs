using System.Runtime.InteropServices;
using System.Text;

namespace CueAudacity;

internal static class Program
{
    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int dwProcessId);

    [STAThread]
    private static int Main(string[] args)
    {
        // Command-line mode: CueAudacity.exe --convert labels.txt [out.cue] [audio-file-name]
        if (args.Length >= 2 && args[0] == "--convert")
        {
            AttachConsole(-1); // show output in the console that launched us
            try
            {
                var input = args[1];
                var output = args.Length >= 3 ? args[2] : Path.ChangeExtension(input, ".cue");
                var options = new CueOptions
                {
                    AudioFileName = args.Length >= 4 ? args[3] : Path.GetFileNameWithoutExtension(input) + ".wav",
                };
                var cue = CueConverter.MakeCue(CueConverter.ParseLabels(CueConverter.ReadLabelFile(input)), options);
                File.WriteAllText(output, cue, new UTF8Encoding(false));
                Console.WriteLine($"Wrote {output}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        ApplicationConfiguration.Initialize();
        var form = new MainForm();
        if (args.Length >= 1 && File.Exists(args[0])) form.LoadFile(args[0]); // drag-onto-exe / Open With
        Application.Run(form);
        return 0;
    }
}
