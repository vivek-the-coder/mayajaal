using System.Diagnostics;
using System.IO.Pipes;
using MayaJaal.Shared.IPC;

namespace MayaJaal.SecurityCenter.Services;

/// <summary>
/// Starts Guardian in a hidden background process so faculty demos need only Security Center.
/// </summary>
public sealed class GuardianProcessHost : IAsyncDisposable, IDisposable
{
    private Process? _process;
    private bool _startedByUs;
    private bool _disposed;

    public bool IsManagedProcessRunning =>
        _startedByUs && _process is { HasExited: false };

    public async Task EnsureRunningAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (await IsPipeReadyAsync(cancellationToken).ConfigureAwait(false))
            return;

        var launch = ResolveLaunchInfo()
            ?? throw new System.IO.FileNotFoundException(
                "Could not locate MayaJaal.Guardian (exe/dll) or a usable dotnet host. Build the solution and retry.");

        var psi = new ProcessStartInfo
        {
            FileName = launch.FileName,
            Arguments = launch.Arguments,
            WorkingDirectory = launch.WorkingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        ApplyDotnetEnvironment(psi);

        _process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start MayaJaal Guardian.");
        _startedByUs = true;

        var stderrTask = _process.StandardError.ReadToEndAsync(cancellationToken);
        var stdoutTask = _process.StandardOutput.ReadToEndAsync(cancellationToken);

        for (var i = 0; i < 60; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_process.HasExited)
            {
                var err = "";
                try { err = (await stderrTask.ConfigureAwait(false)).Trim(); } catch { /* ignore */ }
                if (string.IsNullOrWhiteSpace(err))
                {
                    try { err = (await stdoutTask.ConfigureAwait(false)).Trim(); } catch { /* ignore */ }
                }

                var detail = string.IsNullOrWhiteSpace(err)
                    ? $"Guardian exited early (code {_process.ExitCode})."
                    : $"Guardian exited early (code {_process.ExitCode}).\n{err}";
                throw new InvalidOperationException(detail);
            }

            if (await IsPipeReadyAsync(cancellationToken).ConfigureAwait(false))
                return;

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException("Guardian started but IPC pipe did not become ready in time.");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopManagedProcess();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private void StopManagedProcess()
    {
        if (!_startedByUs || _process is null) return;
        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(3000);
            }
        }
        catch
        {
            // best effort
        }
        finally
        {
            _process.Dispose();
            _process = null;
            _startedByUs = false;
        }
    }

    private static async Task<bool> IsPipeReadyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var pipe = new NamedPipeClientStream(
                ".",
                IpcDefaults.PipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(400);
            await pipe.ConnectAsync(cts.Token).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void ApplyDotnetEnvironment(ProcessStartInfo psi)
    {
        var root = ResolveDotnetRoot();
        if (root is null) return;

        psi.Environment["DOTNET_ROOT"] = root;
        psi.Environment["DOTNET_ROOT(x64)"] = root;

        var path = psi.Environment.TryGetValue("PATH", out var existing) && !string.IsNullOrWhiteSpace(existing)
            ? existing
            : Environment.GetEnvironmentVariable("PATH") ?? "";
        if (!path.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Any(p => string.Equals(p, root, StringComparison.OrdinalIgnoreCase)))
        {
            psi.Environment["PATH"] = root + ";" + path;
        }
    }

    private static string? ResolveDotnetRoot()
    {
        var fromEnv = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (!string.IsNullOrWhiteSpace(fromEnv) && System.IO.Directory.Exists(fromEnv))
            return fromEnv;

        var local = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "dotnet");
        if (System.IO.File.Exists(System.IO.Path.Combine(local, "dotnet.exe")))
            return local;

        var programFiles = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "dotnet");
        if (System.IO.File.Exists(System.IO.Path.Combine(programFiles, "dotnet.exe")))
            return programFiles;

        return null;
    }

    private static string? LocateDotnetExe()
    {
        var root = ResolveDotnetRoot();
        if (root is not null)
        {
            var candidate = System.IO.Path.Combine(root, "dotnet.exe");
            if (System.IO.File.Exists(candidate))
                return candidate;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "where.exe",
                Arguments = "dotnet",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };
            using var p = Process.Start(psi);
            if (p is null) return null;
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(2000);
            var first = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return !string.IsNullOrWhiteSpace(first) && System.IO.File.Exists(first) ? first : null;
        }
        catch
        {
            return null;
        }
    }

    private static LaunchInfo? ResolveLaunchInfo()
    {
        var dir = LocateGuardianDirectory();
        if (dir is null) return null;

        var dll = System.IO.Path.Combine(dir, "MayaJaal.Guardian.dll");
        var exe = System.IO.Path.Combine(dir, "MayaJaal.Guardian.exe");
        var dotnet = LocateDotnetExe();

        // Prefer "dotnet dll" — works with user-local SDK installs (DOTNET_ROOT).
        if (dotnet is not null && System.IO.File.Exists(dll))
        {
            return new LaunchInfo(
                FileName: dotnet,
                Arguments: $"\"{dll}\" --console",
                WorkingDirectory: dir);
        }

        if (System.IO.File.Exists(exe))
        {
            return new LaunchInfo(
                FileName: exe,
                Arguments: "--console",
                WorkingDirectory: dir);
        }

        return null;
    }

    private static string? LocateGuardianDirectory()
    {
        const string fileName = "MayaJaal.Guardian.dll";
        var candidates = new List<string>();

        var baseDir = AppContext.BaseDirectory;
        candidates.Add(baseDir);
        candidates.Add(System.IO.Path.Combine(baseDir, "guardian"));

        var config = baseDir.Contains(
            $"{System.IO.Path.DirectorySeparatorChar}Debug{System.IO.Path.DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase)
            ? "Debug"
            : "Release";

        candidates.Add(System.IO.Path.GetFullPath(System.IO.Path.Combine(
            baseDir, "..", "..", "..", "MayaJaal.Guardian", "bin", config, "net8.0-windows")));

        var dir = new System.IO.DirectoryInfo(baseDir);
        for (var i = 0; i < 8 && dir is not null; i++, dir = dir.Parent)
        {
            candidates.Add(System.IO.Path.Combine(dir.FullName, "src", "MayaJaal.Guardian", "bin", config, "net8.0-windows"));
            candidates.Add(System.IO.Path.Combine(dir.FullName, "MayaJaal.Guardian", "bin", config, "net8.0-windows"));
        }

        return candidates
            .Select(System.IO.Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(d => System.IO.File.Exists(System.IO.Path.Combine(d, fileName)));
    }

    private sealed record LaunchInfo(string FileName, string Arguments, string WorkingDirectory);
}
