using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MayaJaal.Shared.Contracts;
using MayaJaal.Shared.Models;

namespace MayaJaal.Infrastructure.Services;

public sealed class HoneyFileService : IHoneyFileService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly (DecoyType Type, string FileName, string Content)[] Templates =
    [
        (DecoyType.Financial, "Q4_Payroll_Export.xlsx.csv", "emp_id,name,salary,bank\n1001,Alex Rivera,92000,****4412\n1002,Sam Chen,88000,****2291\n"),
        (DecoyType.Credential, "vpn_backup_credentials.txt", "host=vpn.corp.local\nuser=backup_admin\npass=ChangeMe_Now_2024!\napi_token=sk_live_honey_decoy_token\n"),
        (DecoyType.Strategy, "M&A_Target_Shortlist.docx.txt", "CONFIDENTIAL — Acquisition shortlist\n1. Northwind Analytics\n2. Contoso Robotics\n3. Fabrikam Health\n"),
        (DecoyType.HR, "Employee_SSN_Audit.csv", "employee,ssn_last4,status\nJordan Lee,4481,active\nCasey Park,9920,leave\n"),
        (DecoyType.API, "production_api_keys.json", "{\n  \"stripe\": \"sk_live_honey_key\",\n  \"sendgrid\": \"SG.honey.decoy\",\n  \"aws\": \"AKIAHONEYDECOYEXAMP\"\n}\n"),
        (DecoyType.Personal, "CEO_Passport_Scan_Notes.txt", "Travel docs staging folder.\nPassport MRZ sample (DECOY):\nP<UTOERIKSSON<<ANNA<MARIA<<<<<<<<<<<<<<<<<<<\nL898902C36UTO7408122F1204159ZE184226B<<<<<10\n")
    ];

    private readonly ILogger<HoneyFileService> _logger;
    private readonly string _catalogPath;
    private readonly string _defaultDecoyRoot;
    private readonly object _sync = new();
    private readonly List<HoneyFile> _honeyFiles = [];

    public HoneyFileService(ILogger<HoneyFileService> logger, string? decoyRoot = null)
    {
        _logger = logger;
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "MayaJaal");
        Directory.CreateDirectory(Path.Combine(root, "Config"));
        _catalogPath = Path.Combine(root, "Config", "honey-files.json");
        _defaultDecoyRoot = decoyRoot ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "MayaJaal-Decoys");
        Load();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(_defaultDecoyRoot);
        if (_honeyFiles.Count == 0)
            await DeployDecoysAsync(_defaultDecoyRoot, Templates.Length, cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<HoneyFile>> DeployDecoysAsync(
        string? decoyRoot = null,
        int count = 6,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = decoyRoot ?? _defaultDecoyRoot;
        Directory.CreateDirectory(root);
        count = Math.Clamp(count, 1, Templates.Length);

        lock (_sync)
        {
            var created = new List<HoneyFile>();
            for (var i = 0; i < count; i++)
            {
                var template = Templates[i];
                var path = Path.Combine(root, template.FileName);
                var bytes = Encoding.UTF8.GetBytes(template.Content);
                File.WriteAllBytes(path, bytes);

                // Hide as normal-looking document: clear archive bit quirks; keep readable for demo.
                try
                {
                    File.SetAttributes(path, FileAttributes.Normal);
                }
                catch
                {
                    // best effort
                }

                var honey = new HoneyFile
                {
                    DecoyType = template.Type,
                    FilePath = path,
                    ContentHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
                    Sensitivity = 8,
                    Status = HoneyStatus.Active,
                    CreationTime = DateTime.UtcNow,
                    TemplateVersion = "1.0"
                };

                _honeyFiles.RemoveAll(h => string.Equals(h.FilePath, path, StringComparison.OrdinalIgnoreCase));
                _honeyFiles.Add(honey);
                created.Add(honey);
            }

            PersistUnlocked();
            _logger.LogInformation("Deployed {Count} honey files under {Root}", created.Count, root);
            return Task.FromResult<IReadOnlyList<HoneyFile>>(created.ToList());
        }
    }

    public Task<IReadOnlyList<HoneyFile>> GetHoneyFilesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            return Task.FromResult<IReadOnlyList<HoneyFile>>(_honeyFiles.Select(Clone).ToList());
        }
    }

    public bool IsHoneyPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        var full = Path.GetFullPath(path);
        lock (_sync)
        {
            return _honeyFiles.Any(h =>
                h.Status == HoneyStatus.Active &&
                string.Equals(Path.GetFullPath(h.FilePath), full, StringComparison.OrdinalIgnoreCase));
        }
    }

    public HoneyFile? FindByPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        var full = Path.GetFullPath(path);
        lock (_sync)
        {
            var match = _honeyFiles.FirstOrDefault(h =>
                string.Equals(Path.GetFullPath(h.FilePath), full, StringComparison.OrdinalIgnoreCase));
            return match is null ? null : Clone(match);
        }
    }

    private void Load()
    {
        if (!File.Exists(_catalogPath)) return;
        try
        {
            var json = File.ReadAllText(_catalogPath);
            var list = JsonSerializer.Deserialize<List<HoneyFile>>(json, JsonOptions) ?? [];
            lock (_sync)
            {
                _honeyFiles.Clear();
                _honeyFiles.AddRange(list);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load honey catalog from {Path}", _catalogPath);
        }
    }

    private void PersistUnlocked()
    {
        var json = JsonSerializer.Serialize(_honeyFiles, JsonOptions);
        var temp = _catalogPath + ".tmp";
        File.WriteAllText(temp, json);
        File.Copy(temp, _catalogPath, overwrite: true);
        File.Delete(temp);
    }

    private static HoneyFile Clone(HoneyFile source)
        => JsonSerializer.Deserialize<HoneyFile>(JsonSerializer.Serialize(source, JsonOptions), JsonOptions)!;
}
