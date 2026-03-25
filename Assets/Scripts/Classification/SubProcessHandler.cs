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
        private readonly string _pythonPath;
        private readonly string _scriptPath;
        private Process _process;

        public SubProcessHandler(string scriptPath) : this("python", scriptPath) { }

        public SubProcessHandler(string pythonPath, string scriptPath)
        {
            _pythonPath = pythonPath ?? throw new ArgumentNullException(nameof(pythonPath));
            _scriptPath = scriptPath ?? throw new ArgumentNullException(nameof(scriptPath));

            if (!File.Exists(_scriptPath))
            {
                throw new FileNotFoundException($"Python script not found: {_scriptPath}");
            }
        }

        public async Task<SubProcessResponse> ExecutePythonAsync(List<string> additionalArguments = null)
        {
            try
            {
                additionalArguments ??= new List<string>();

                var arguments = $"\"{_scriptPath}\"";
                if (additionalArguments.Any())
                {
                    arguments += " " + string.Join(" ", additionalArguments.Select(a => $"\"{a}\""));
                }

                return await Task.Run(() => ExecuteProcessInternal(_pythonPath, arguments));
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"ExecutePython error: {ex.Message}");
                return new SubProcessResponse
                {
                    Status = Status.ERROR,
                    ErrorMessage = ex.Message
                };
            }
        }

        private SubProcessResponse ExecuteProcessInternal(string fileName, string arguments)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                startInfo.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";

                using (var process = new Process
                {
                    StartInfo = startInfo
                })
                {
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();


                    UnityEngine.Debug.Log($"OUTPUTTING: {output}");
                    process.WaitForExit(TIMEOUT);

                    return ParseResponse(output, error);
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

        private SubProcessResponse ParseResponse(string output, string error)
        {
            if (!string.IsNullOrEmpty(error))
            {
                UnityEngine.Debug.LogError($"Standard error output appeared: {error}");
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

                        UnityEngine.Debug.Log($"Returned what python returned");
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