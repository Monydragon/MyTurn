namespace MyTurn.Desktop;

internal sealed record DesktopRunOptions(bool CaptureMode, string CaptureDirectory)
{
    public static DesktopRunOptions Parse(IReadOnlyList<string> args)
    {
        var captureMode = args.Any(arg => string.Equals(arg, "--capture", StringComparison.OrdinalIgnoreCase));
        var captureDirectory = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "artifacts", "render-captures", "latest");

        for (var index = 0; index < args.Count - 1; index++)
        {
            if (string.Equals(args[index], "--capture-dir", StringComparison.OrdinalIgnoreCase))
            {
                captureDirectory = args[index + 1];
                break;
            }
        }

        return new DesktopRunOptions(captureMode, Path.GetFullPath(captureDirectory));
    }
}
