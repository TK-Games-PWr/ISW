using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;


namespace Classification
{


    public class ClassificationText : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;

        void Start()
        {
            if (text == null) text = GetComponent<TextMeshProUGUI>();
        }

        public void UpdateClassificationResult()
        {

            _ = RunPythonScriptAsync();

        }

        // Changed from IEnumerator to async Task
        private async Task RunPythonScriptAsync()
        {
            UnityEngine.Debug.Log("Starting Python script...");

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            string scriptPath = Path.Combine(Application.dataPath, "Scripts/Classification/PythonModule/inference.py");

            if (!File.Exists(scriptPath))
            {
                UnityEngine.Debug.LogError($"Python script not found at: {scriptPath}");
                return;
            }

            try
            {
                // Initialize the handler (it is IDisposable, so 'using' handles cleanup)
                using (var handler = new SubProcessHandler(scriptPath))
                {
                    // The new handler handles the background thread internally.
                    // 'await' pauses this method and returns control to Unity, 
                    // then resumes here on the Main Thread when finished.
                    List<string> args = new List<string>();
                    args.Add(Path.Combine(Application.dataPath, Directory.GetCurrentDirectory() + "/playerdata.csv"));
                    args.Add(Path.Combine(Application.dataPath, "Scripts/Classification/PythonModule/model.joblib"));
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