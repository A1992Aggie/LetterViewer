using System.Runtime.Versioning;

namespace LetterViewer;

[SupportedOSPlatform("windows")]
internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
