namespace Chickensoft.GodotEnv.Features.Godot.Models;

using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Chickensoft.GodotEnv.Common.Clients;
using Chickensoft.GodotEnv.Common.Models;
using Chickensoft.GodotEnv.Common.Utilities;

public interface IGodotEnvironment {
  /// <summary>
  /// File client used by the platform to manipulate file paths.
  /// </summary>
  IFileClient FileClient { get; }

  IComputer Computer { get; }

  /// <summary>
  /// Godot export template installation directory base path on the local
  /// machine.
  /// </summary>
  string ExportTemplatesBasePath { get; }

  /// <summary>
  /// True if the platform has the given executable directory paths set in its
  /// environment variables correctly.
  /// </summary>
  /// <param name="execDirectoryPaths">Fully resolved paths that indicate where
  /// the Godot executables for a given installation are located.</param>
  /// <returns>True if the environment is configured correctly.</returns>
  bool HasEnvironmentPropertiesSet(HashSet<string> execDirectoryPaths);

  /// <summary>
  /// Godot installation filename suffix.
  /// </summary>
  /// <param name="isDotnetVersion">True if using the .NET-enabled version of Godot,
  /// false otherwise.</param>
  /// <returns>Godot filename suffix.</returns>
  string GetInstallerNameSuffix(bool isDotnetVersion);

  /// <summary>
  /// Computes the local path where the Godot export templates should be
  /// installed.
  /// </summary>
  /// <param name="version">Godot version.</param>
  /// <param name="platform">Platform.</param>
  /// <param name="isDotnetVersion">True if referencing the .NET version of Godot.
  /// </param>
  /// <returns>Local path to where the Godot export templates should be
  /// installed.</returns>
  string GetExportTemplatesLocalPath(SemanticVersion version, bool isDotnetVersion);

  /// <summary>
  /// Should return true if the given file is likely to be executable.
  /// </summary>
  /// <param name="shell">System shell.</param>
  /// <param name="file">File info.</param>
  /// <returns>True if likely executable, false otherwise.</returns>
  Task<bool> IsExecutable(IShell shell, IFileInfo file);

  /// <summary>
  /// Recursively searches for all the executable files in the
  /// given directory.
  /// </summary>
  /// <param name="dir">Directory to search.</param>
  /// <param name="log">Log used to output discovered files.</param>
  /// <returns>A list containing file info for each executable file.</returns>
  Task<List<IFileInfo>> FindExecutablesRecursively(
    string dir, ILog log
  );

  /// <summary>
  /// Computes the Godot download url.
  /// </summary>
  /// <param name="version">Godot version.</param>
  /// <param name="platform">Platform.</param>
  /// <param name="isDotnetVersion">True if referencing the .NET version of Godot.
  /// </param>
  /// <param name="isTemplate">True if computing the download url to the
  /// export templates, false to compute the download url to the Godot
  /// application.</param>
  string GetDownloadUrl(
    SemanticVersion version,
    bool isDotnetVersion,
    bool isTemplate
  );

  /// <summary>
  /// Outputs a description of the platform to the log.
  /// </summary>
  /// <param name="log">Output log.</param>
  void Describe(ILog log);

  /// <summary>
  /// Returns the path where the Godot executable itself is located, relative
  /// to the extracted Godot installation directory.
  /// </summary>
  /// <param name="version">Godot version.</param>
  /// <param name="isDotnetVersion">Dotnet version indicator.</param>
  /// <returns>Relative path.</returns>
  string GetRelativeExtractedExecutablePath(
    SemanticVersion version, bool isDotnetVersion
  );

  /// <summary>
  /// For dotnet-enabled versions, this gets the path to the GodotSharp debug
  /// directory that is included with Godot.
  /// </summary>
  /// <param name="version">Godot version.</param>
  /// <returns>Path to the GodotSharp debug directory.</returns>
  string GetRelativeGodotSharpDebugPath(SemanticVersion version);

  /// <summary>
  /// For dotnet-enabled versions, this gets the path to the GodotSharp release
  /// directory that is included with Godot.
  /// </summary>
  /// <param name="version">Godot version.</param>
  /// <returns>Path to the GodotSharp release directory.</returns>
  string GetRelativeGodotSharpReleasePath(SemanticVersion version);
}

public abstract class GodotEnvironment : IGodotEnvironment {
  public const string GODOT_FILENAME_PREFIX = "Godot_v";
  public const string GODOT_URL_PREFIX =
    "https://downloads.tuxfamily.org/godotengine/";

  /// <summary>
  /// Creates a platform for the given OS.
  /// </summary>
  /// <param name="os">OS Type.</param>
  /// <param name="fileClient">File client.</param>
  /// <param name="computer">Computer.</param>
  /// <returns>Platform instance.</returns>
  /// <exception cref="InvalidOperationException" />
  public static GodotEnvironment Create(
    OSType os, IFileClient fileClient, IComputer computer
  ) =>
    os switch {
      OSType.Windows => new Windows(fileClient, computer),
      OSType.MacOS => new MacOS(fileClient, computer),
      OSType.Linux => new Linux(fileClient, computer),
      OSType.Unknown => throw GetUnknownOSException(),
      _ => throw GetUnknownOSException()
    };

  protected GodotEnvironment(IFileClient fileClient, IComputer computer) {
    FileClient = fileClient;
    Computer = computer;
  }

  public IFileClient FileClient { get; }
  public IComputer Computer { get; }

  public abstract string ExportTemplatesBasePath { get; }
  public abstract string GetInstallerNameSuffix(bool isDotnetVersion);
  public abstract void Describe(ILog log);
  public abstract Task<bool> IsExecutable(IShell shell, IFileInfo file);
  public abstract string GetRelativeExtractedExecutablePath(
    SemanticVersion version, bool isDotnetVersion
  );
  public abstract string GetRelativeGodotSharpDebugPath(
    SemanticVersion version
  );
  public abstract string GetRelativeGodotSharpReleasePath(
    SemanticVersion version
  );

  // TODO: Implement
  public bool HasEnvironmentPropertiesSet(
    HashSet<string> execDirectoryPaths
  ) => true;

  public string GetExportTemplatesLocalPath(
    SemanticVersion version, bool isDotnetVersion
  ) {
    var major = version.Major;
    var minor = version.Minor;
    var patch = version.Patch;
    var label = version.Label;

    var folderName = $"{major}.{minor}";

    if (patch is not "" and not "0") {
      folderName += $".{patch}";
    }

    if (label != "") {
      folderName += $".{label}";
    }
    else {
      folderName += ".stable";
    }

    if (isDotnetVersion) {
      folderName += ".mono";
    }

    return FileClient.GetFullPath(
      FileClient.Combine(
        ExportTemplatesBasePath,
        "export_templates",
        folderName
      )
    );
  }

  public async Task<List<IFileInfo>> FindExecutablesRecursively(
    string dir, ILog log
   ) {
    var shell = Computer.CreateShell(dir);

    log.Info($"🔍 Searching for executables in {dir}...");
    var execFiles = await FileClient.SearchRecursively(
      dir,
      selector: async (fileInfo, indent) => {
        var isExecutable =
          fileInfo.Name == "GodotSharp.dll" ||
          await IsExecutable(shell, fileInfo);

        if (isExecutable) {
          log.Info($"{indent}🚀 {fileInfo.Name}");
        }
        else {
          // log.Info($"{indent}📄 {fileInfo.Name}");
        }

        return isExecutable;
      },
      dirSelector: (dirInfo) => {
        // Don't look for debug executables.
        var name = dirInfo.Name.ToLower();
        if (name.Contains("debug") || name.EndsWith(".lproj")) {
          return Task.FromResult(false);
        }
        return Task.FromResult(true);
      },
      onDirectory: (dir, indent) => log.Info($"{indent}📁 {dir.Name}")
    );

    if (execFiles.Count > 0) {
      log.Success($"✅ Found {execFiles.Count} executable files.");
    }
    else {
      log.Warn("⚠️ No executable files found!");
    }

    return execFiles;
  }

  public string GetDownloadUrl(
    SemanticVersion version,
    bool isDotnetVersion,
    bool isTemplate
  ) {
    var major = version.Major;
    var minor = version.Minor;
    var patch = version.Patch;
    // Remove dots from label.
    var label = version.LabelNoDots;

    var url = $"{GODOT_URL_PREFIX + major}.{minor}";
    if (patch is not "" and not "0") {
      url += $".{patch}";
    }

    url += "/";
    if (label != "") {
      url += $"{label}/";
    }

    if (isDotnetVersion) {
      url += "mono/";
    }

    // Godot application download url.
    if (!isTemplate) {
      return url + GetInstallerFilename(version, isDotnetVersion);
    }

    // Export template download url.
    return
      url + GetExportTemplatesInstallerFilename(version, isDotnetVersion);
  }

  protected static string GetFilenameVersionString(SemanticVersion version) {
    var major = version.Major;
    var minor = version.Minor;
    var patch = version.Patch;
    // Remove dots from label.
    var label = version.LabelNoDots;

    var filename = GODOT_FILENAME_PREFIX + major;

    if (minor != "") {
      filename += $".{minor}";
    }

    if (patch is not "" and not "0") {
      filename += $".{patch}";
    }

    if (label != "") {
      filename += $"-{label}";
    }
    else {
      filename += "-stable";
    }

    return filename;
  }

  // Gets the filename of the Godot installation download for the platform.
  private string GetInstallerFilename(
    SemanticVersion version, bool isDotnetVersion
  ) => GetFilenameVersionString(version) + GetInstallerNameSuffix(isDotnetVersion) +
    ".zip";

  // Gets the filename of the Godot export templates installation download for
  // the platform.
  private static string GetExportTemplatesInstallerFilename(
    SemanticVersion version, bool isDotnetVersion
  ) => GetFilenameVersionString(version) + (isDotnetVersion ? "_mono" : "") +
      "_export_templates.tpz";

  private static Exception GetUnknownOSException() =>
    new InvalidOperationException(
      "🚨 Cannot create a platform an unknown operating system."
    );
}
