using UnityEngine;
using TMPro;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;


namespace Classification
{
    public class ClassificationText : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI text;

        bool _isPythonReady = false;

        void Start()
        {
            if (text == null) text = GetComponent<TextMeshProUGUI>();

            if (text != null) text.text = "Initializing Python Environment...";

            _ = SetupPythonEnvironmentAsync();
        }

        public void UpdateClassificationResult()
        {
            if (!_isPythonReady)
            {
                UnityEngine.Debug.LogWarning("Python environment is still setting up. Please wait.");
                return;
            }

            _ = RunPythonScriptAsync();
        }

        private async Task SetupPythonEnvironmentAsync()
        {
            string modulePath = Path.Combine(Application.dataPath, "Scripts/Classification/.PythonModule");
            string venvPath = Path.Combine(modulePath, "venv");

            if (!Directory.Exists(modulePath))
            {
                UnityEngine.Debug.Log("Module directory doesn't exist at " + modulePath);
                if (text != null) text.text = "Module directory doesn't exist at " + modulePath;
                _isPythonReady = false;
                return;
            }

            if (Directory.Exists(venvPath))
            {
                UnityEngine.Debug.Log("Python venv already exists. Skipping installation.");
                if (text != null) text.text = "Ready.";
                _isPythonReady = true;
                return;
            }

            UnityEngine.Debug.Log("Setting up Python virtual environment... This might take a minute or two.");

            string processFileName;
            string processArguments = "";
            string scriptToCheck;

            if (Application.platform == RuntimePlatform.WindowsEditor ||
                Application.platform == RuntimePlatform.WindowsPlayer)
            {
                processFileName = "cmd.exe";
                string batPath = Path.Combine(modulePath, "install-packages.bat");

                processArguments = $"/c \"{batPath}\"";
                scriptToCheck = batPath;
            }
            else
            {
                // mac/linux
                processFileName = "/bin/bash";
                scriptToCheck = Path.Combine(modulePath, "install-packages.sh");
                processArguments = $"\"{scriptToCheck}\"";
            }

            if (!File.Exists(scriptToCheck))
            {
                UnityEngine.Debug.LogError($"Setup script not found at: {scriptToCheck}");
                if (text != null) text.text = "Error: Setup script missing.";
                return;
            }

            await Task.Run(() =>
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = processFileName,
                    Arguments = processArguments,
                    WorkingDirectory = modulePath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                try
                {
                    using (Process process = Process.Start(startInfo))
                    {
                        process.WaitForExit();

                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();

                        UnityEngine.Debug.Log($"Setup Output:\n{output}");

                        if (!string.IsNullOrEmpty(error))
                        {
                            UnityEngine.Debug.LogWarning($"Setup Warnings/Errors:\n{error}");
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError($"Failed to run setup script: {ex.Message}");
                }
            });

            UnityEngine.Debug.Log("Python environment setup complete!");
            if (text != null) text.text = "Ready.";
            _isPythonReady = true;
        }

        // Changed from IEnumerator to async Task
        private async Task RunPythonScriptAsync()
        {
            UnityEngine.Debug.Log("Starting Python script...");

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            string scriptPath = Path.Combine(Application.dataPath, "Scripts/Classification/.PythonModule/inference.py");
            string venvPath = Path.Combine(Application.dataPath, "Scripts/Classification/.PythonModule/venv");

            string pythonExecutable;
            if (Application.platform == RuntimePlatform.WindowsEditor ||
                Application.platform == RuntimePlatform.WindowsPlayer)
            {
                pythonExecutable = Path.Combine(venvPath, "Scripts/python.exe");
            }
            else
            {
                // macOS / Linux
                pythonExecutable = Path.Combine(venvPath, "bin/python");
            }

            if (!File.Exists(scriptPath))
            {
                UnityEngine.Debug.LogError($"Python script not found at: {scriptPath}");
                return;
            }

            try
            {
                // Initialize the handler (it is IDisposable, so 'using' handles cleanup)
                using (var handler = new SubProcessHandler(pythonExecutable, scriptPath))
                {
                    // The new handler handles the background thread internally.
                    // 'await' pauses this method and returns control to Unity, 
                    // then resumes here on the Main Thread when finished.
                    List<string> args = new List<string>();
                    args.Add(Path.Combine(Application.dataPath, Directory.GetCurrentDirectory() + "/playerdata.csv"));
                    args.Add(Path.Combine(Application.dataPath, "Scripts/Classification/.PythonModule/model.joblib"));
                    SubProcessResponse response = await handler.ExecutePythonAsync(args);

                    stopwatch.Stop();
                    float totalSeconds = (float)stopwatch.Elapsed.TotalSeconds;

                    HandleResponse(response, totalSeconds);
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Error calling python: {e.Message}");
            }
        }

        private void HandleResponse(SubProcessResponse response, float time)
        {
            if (response.Status == Status.OK)
            {
                UnityEngine.Debug.Log($"Python finished in {time:F3}s!");

                if (text != null)
                {
                    // Because we 'awaited' the task, we are safely back on the 
                    // Main Thread and can update UI components directly.
                    text.text = $"{response.Content}\n\nProbability: {(response.Probability * 100):F1}%";
                }
            }
            else
            {
                UnityEngine.Debug.LogError($"Python Error: {response.ErrorMessage}");
                if (text != null) text.text = $"Error: {response.ErrorMessage}";
            }
        }
    }
}