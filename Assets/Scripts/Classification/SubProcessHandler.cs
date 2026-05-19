using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;


namespace Classification
{

    public class SubProcessHandler : IDisposable
    {
        const string RESPONSE_MARKER = "RESPONSE_DATA:";
        const int TIMEOUT = 10000;
        private readonly string _executablePath;
        private readonly string _scriptPath;
        private Process _process;

        public SubProcessHandler(string executablePath, string scriptPath)
        {
            _executablePath = executablePath ?? throw new ArgumentNullException(nameof(executablePath));
            _scriptPath = scriptPath;

            if (_scriptPath != null)
            {
                if (!File.Exists(_scriptPath))
                    throw new FileNotFoundException($"Python script not found: {_scriptPath}");
            }
            else if (!File.Exists(_executablePath))
            {
                throw new FileNotFoundException($"Classifier executable not found: {_executablePath}");
            }
        }

        public static SubProcessHandler ForExecutable(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
                throw new ArgumentNullException(nameof(executablePath));

            return new SubProcessHandler(executablePath, null);
        }

        public async Task<SubProcessResponse> ExecutePythonAsync(List<string> additionalArguments = null)
        {
            return await ExecuteAsync(additionalArguments);
        }

        public async Task<SubProcessResponse> ExecuteAsync(List<string> additionalArguments = null, bool logsEnabled = true)
        {
            try
            {
                additionalArguments ??= new List<string>();

                string arguments;
                if (_scriptPath != null)
                {
                    arguments = $"\"{_scriptPath}\"";
                    if (additionalArguments.Any())
                    {
                        arguments += " " + string.Join(" ", additionalArguments.Select(a => $"\"{a}\""));
                    }
                }
                else
                {
                    arguments = string.Join(" ", additionalArguments.Select(a => $"\"{a}\""));
                }

                return await Task.Run(() => ExecuteProcessInternal(_executablePath, arguments, logsEnabled));
            }
            catch (Exception ex)
            {
                if(logsEnabled) UnityEngine.Debug.LogError($"ExecutePython error: {ex.Message}");
                return new SubProcessResponse
                {
                    Status = Status.ERROR,
                    ErrorMessage = ex.Message
                };
            }
        }

        private SubProcessResponse ExecuteProcessInternal(string fileName, string arguments, bool logsEnabled = true)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = Path.GetDirectoryName(fileName) ?? "",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                startInfo.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
                startInfo.EnvironmentVariables["PYTHONUTF8"] = "1";

                using (var process = new Process
                {
                    StartInfo = startInfo
                })
                {
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();


                    if(logsEnabled) UnityEngine.Debug.Log($"OUTPUTTING: {output}");
                    process.WaitForExit(TIMEOUT);

                    return ParseResponse(output, error, logsEnabled);
                }
            }
            catch (Exception ex)
            {
                return new SubProcessResponse
                {
                    Status = Status.ERROR,
                    ErrorMessage = ex.Message
                };
            }
        }

        private SubProcessResponse ParseResponse(string output, string error, bool logsEnabled = true)
        {
            if (!string.IsNullOrEmpty(error))
            {
                if(logsEnabled) UnityEngine.Debug.LogError($"Standard error output appeared: {error}");
                return new SubProcessResponse { Status = Status.ERROR, ErrorMessage = error.Trim() };
            }

            string[] lines = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            bool foundMarker = false;

            foreach (string line in lines)
            {
                if (foundMarker)
                {
                    try
                    {

                        if(logsEnabled) UnityEngine.Debug.Log($"Returned what python returned");
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<SubProcessResponse>(line.Trim());
                    }
                    catch (Newtonsoft.Json.JsonException ex)
                    {
                        return new SubProcessResponse { Status = Status.ERROR, ErrorMessage = $"JSON Parse Error: {ex.Message}" };
                    }
                }

                if (line.Trim() == RESPONSE_MARKER) foundMarker = true;
            }

            // Fallback: try parsing whole output
            try
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<SubProcessResponse>(output.Trim());
            }
            catch
            {
                return new SubProcessResponse { Status = Status.ERROR, ErrorMessage = "No valid JSON found." };
            }
        }

        public void Dispose()
        {
            _process?.Dispose();
        }
    }
}