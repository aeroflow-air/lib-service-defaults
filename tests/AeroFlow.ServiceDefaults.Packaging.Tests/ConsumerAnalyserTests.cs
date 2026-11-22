using System.Diagnostics;
using System.Text;
using Xunit;

namespace AeroFlow.ServiceDefaults.Packaging.Tests;

/// <summary>
/// Packs AeroFlow.ServiceDefaults and builds throwaway consumers against the nupkg,
/// proving PackageReference consumers get RS0030 (banned System.Console) automatically.
/// </summary>
public sealed class ConsumerAnalyserTests
{
    private const string PackageVersion = "0.0.0-packtest";

    [Fact]
    public void Console_WriteLine_fails_the_consumer_build_with_RS0030()
    {
        using var scratch = new Scratch();
        PackServiceDefaults(scratch.FeedDirectory);

        WriteConsumer(scratch.ConsumerDirectory, scratch.FeedDirectory, """
            Console.WriteLine("this should fail the build");
            """);

        var result = RunDotnet(["build", "consumer.csproj", "--nologo"], scratch.ConsumerDirectory);

        Assert.True(result.ExitCode != 0, $"Consumer with Console.WriteLine was expected to fail, but exited {result.ExitCode}. Output:\n{result.Output}");
        Assert.Contains("RS0030", result.Output, StringComparison.Ordinal);
        Assert.Contains("System.Console", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Consumer_without_Console_builds_successfully()
    {
        using var scratch = new Scratch();
        PackServiceDefaults(scratch.FeedDirectory);

        WriteConsumer(scratch.ConsumerDirectory, scratch.FeedDirectory, """
            System.Threading.Thread.Sleep(0);
            """);

        var result = RunDotnet(["build", "consumer.csproj", "--nologo"], scratch.ConsumerDirectory);

        Assert.True(result.ExitCode == 0, $"Clean consumer was expected to succeed, but exited {result.ExitCode}. Output:\n{result.Output}");
        Assert.DoesNotContain("RS0030", result.Output, StringComparison.Ordinal);
    }

    private static void PackServiceDefaults(string feedDirectory)
    {
        var repoRoot = FindRepoRoot();
        var project = Path.Combine(repoRoot, "src", "AeroFlow.ServiceDefaults", "AeroFlow.ServiceDefaults.csproj");
        var result = RunDotnet(
            [
                "pack", project,
                "-c", "Release",
                "-o", feedDirectory,
                "--nologo",
                $"-p:Version={PackageVersion}",
                $"-p:PackageVersion={PackageVersion}",
            ],
            repoRoot);

        Assert.True(result.ExitCode == 0, $"dotnet pack failed (exit {result.ExitCode}):\n{result.Output}");
        Assert.True(
            File.Exists(Path.Combine(feedDirectory, $"AeroFlow.ServiceDefaults.{PackageVersion}.nupkg")),
            $"Packed nupkg was not found in {feedDirectory}. Output:\n{result.Output}");
    }

    private static void WriteConsumer(string consumerDirectory, string feedDirectory, string programCs)
    {
        Directory.CreateDirectory(consumerDirectory);
        File.WriteAllText(Path.Combine(consumerDirectory, "consumer.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <OutputType>Exe</OutputType>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="AeroFlow.ServiceDefaults" Version="0.0.0-packtest" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(consumerDirectory, "Program.cs"), programCs);
        File.WriteAllText(Path.Combine(consumerDirectory, "nuget.config"), $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
                <add key="local-packtest" value="{feedDirectory}" />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
              </packageSources>
            </configuration>
            """);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AeroFlow.ServiceDefaults.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not find AeroFlow.ServiceDefaults.sln from " + AppContext.BaseDirectory);
    }

    private static CommandResult RunDotnet(IEnumerable<string> args, string workingDirectory)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        using var process = Process.Start(start) ?? throw new InvalidOperationException("Failed to start dotnet.");
        var output = new StringBuilder();
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                output.AppendLine(e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                output.AppendLine(e.Data);
            }
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        if (!process.WaitForExit(180_000))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // ignore
            }

            throw new TimeoutException("dotnet " + string.Join(' ', args) + " timed out after 3 minutes.");
        }

        // Ensure async readers flush after exit.
        process.WaitForExit();
        return new CommandResult(process.ExitCode, output.ToString());
    }

    private sealed record CommandResult(int ExitCode, string Output);

    private sealed class Scratch : IDisposable
    {
        public Scratch()
        {
            Root = Directory.CreateTempSubdirectory("sd-packtest-");
            FeedDirectory = Path.Combine(Root.FullName, "feed");
            ConsumerDirectory = Path.Combine(Root.FullName, "consumer");
            Directory.CreateDirectory(FeedDirectory);
            Directory.CreateDirectory(ConsumerDirectory);
        }

        public DirectoryInfo Root { get; }

        public string FeedDirectory { get; }

        public string ConsumerDirectory { get; }

        public void Dispose()
        {
            try
            {
                if (Root.Exists)
                {
                    Root.Delete(recursive: true);
                }
            }
            catch
            {
                // Best-effort cleanup of the temp feed and consumer.
            }
        }
    }
}
