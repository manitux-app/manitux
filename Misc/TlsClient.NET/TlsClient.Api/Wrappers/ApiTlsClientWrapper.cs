using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TlsClient.Api.Native;

namespace TlsClient.Api.Wrappers
{
    public static class ApiTlsClientWrapper
    {
        private static bool _isInitialized;

        private static Process? _apiProcess;
        private static string _binaryPath;
        private static string _apiUrl;
        private static bool _isDisposed;
        private static CancellationTokenSource? _healthCheckCts;

        public static bool Initialize(string? binaryPath = null)
        {
            if (_isInitialized) return true;

            _apiUrl = "https://127.0.0.1:8080/";

            binaryPath ??= ApiNativeLoader.GetBinaryPath();
            Debug.WriteLine("ApiTlsClientWrapper Initialize libraryPath: " + binaryPath);

            if (!File.Exists(binaryPath)) return false;
            
            _binaryPath = binaryPath;

            KillProcessesByName(Path.GetFileNameWithoutExtension(binaryPath));

            if (!OperatingSystem.IsWindows() && File.Exists(binaryPath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo("chmod", $"+x \"{binaryPath}\"") { CreateNoWindow = true })?.WaitForExit();
                }
                catch {}
            }

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = _binaryPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    WorkingDirectory = Path.GetDirectoryName(_binaryPath) //AppContext.BaseDirectory
                };

                _apiProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = false };

                //_apiProcess.Exited += OnProcessExited;

                _apiProcess.Start();

                // Health Check
                //bool isReady = await WaitForApiReadyAsync(TimeSpan.FromSeconds(5));
                //if (!isReady)
                //{
                //    throw new Exception("HTTP API belirtilen sürede yanıt vermedi.");
                //}

                //StartPeriodicHealthCheck();

                _isInitialized = true;

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return false;
        }

        private static async Task<bool> WaitForApiReadyAsync(TimeSpan timeout)
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
            var watch = Stopwatch.StartNew();

            while (watch.Elapsed < timeout)
            {
                try
                {
                    var response = await client.GetAsync(_apiUrl);
                    if (response.IsSuccessStatusCode) return true;
                }
                catch
                {
                    // wait
                }
                await Task.Delay(500);
            }
            return false;
        }

        private static void StartPeriodicHealthCheck()
        {
            _healthCheckCts?.Cancel();
            _healthCheckCts = new CancellationTokenSource();
            var token = _healthCheckCts.Token;

            Task.Run(async () =>
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), token);

                    if (_apiProcess == null || _apiProcess.HasExited) continue;

                    try
                    {
                        var response = await client.GetAsync(_apiUrl, token);
                        if (!response.IsSuccessStatusCode) throw new Exception("TlsClient API error!");
                    }
                    catch
                    {
                        if (!token.IsCancellationRequested)
                        {
                           RestartProcess();
                        }
                    }
                }
            }, token);
        }

        private static void OnProcessExited(object? sender, EventArgs e)
        {
            if (_isDisposed) return;
            RestartProcess();
        }

        private static void RestartProcess()
        {
            StopProcess();
            Task.Run(() =>
            {
                try { Initialize(); } catch {}
            });
        }

        private static void StopProcess()
        {
            _healthCheckCts?.Cancel();

            if (_apiProcess != null)
            {
                //_apiProcess.Exited -= OnProcessExited;
                try
                {
                    if (!_apiProcess.HasExited)
                    {
                        _apiProcess.Kill(entireProcessTree: true);
                    }
                }
                catch {}
                finally
                {
                    _apiProcess.Dispose();
                    _apiProcess = null;
                }
            }
        }

        public static void KillProcessesByName(string processName)
        {
            Process[] runningProcesses = Process.GetProcessesByName(processName);

            foreach (var process in runningProcesses)
            {
                try
                {
                    process.Kill(entireProcessTree: true);

                    process.WaitForExit(TimeSpan.FromSeconds(2));

                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"({process.Id}) is not kill: {ex.Message}");
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

        public static void KillByNameWithCommand(string processName)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            if (OperatingSystem.IsWindows())
            {
                startInfo.FileName = "taskkill";
                startInfo.Arguments = $"/F /T /IM \"{processName}.exe\"";
            }
            else // Linux, macOS
            {
                startInfo.FileName = "pkill";
                startInfo.Arguments = $"-9 -f \"{processName}\"";
            }

            try
            {
                using var process = Process.Start(startInfo);
                process?.WaitForExit(TimeSpan.FromSeconds(3));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Err: {ex.Message}");
            }
        }

        public static void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            Debug.WriteLine("ApiTlsClientWrapper Dispose");
            KillByNameWithCommand(Path.GetFileNameWithoutExtension(_binaryPath));
            //StopProcess();
            //_healthCheckCts?.Dispose();
            //GC.SuppressFinalize(this);
        }
    }
}
