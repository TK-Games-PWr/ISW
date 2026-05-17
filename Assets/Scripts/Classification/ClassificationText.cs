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
        [SerializeField] string classifierExeRelativePath = "Scripts/Classification/.PythonModule/inference.exe";
        [SerializeField] string modelRelativePath = "Scripts/Classification/.PythonModule/model.joblib";
        [SerializeField] string datapointsFileName = "playerdata.csv";

        bool _isClassifierReady;

        void Start()
        {
            if (text == null) text = GetComponent<TextMeshProUGUI>();

            if (text != null) text.text = "Initializing classifier...";

            _ = SetupClassifierAsync();
        }

        public void UpdateClassificationResult()
        {
            if (!_isClassifierReady)
            {
                UnityEngine.Debug.LogWarning("Classifier is not ready. Please wait.");
                return;
            }

            _ = RunClassifierAsync();
        }

        private Task SetupClassifierAsync()
        {
            string exePath = Path.Combine(Application.dataPath, classifierExeRelativePath);

            if (!File.Exists(exePath))
            {
                UnityEngine.Debug.LogError($"Classifier executable not found at: {exePath}");
                if (text != null) text.text = $"Error: classifier not found at {exePath}";
                _isClassifierReady = false;
                return Task.CompletedTask;
            }

            string modelPath = Path.Combine(Application.dataPath, modelRelativePath);
            if (!File.Exists(modelPath))
            {
                UnityEngine.Debug.LogError($"Model file not found at: {modelPath}");
                if (text != null) text.text = $"Error: model not found at {modelPath}";
                _isClassifierReady = false;
                return Task.CompletedTask;
            }

            UnityEngine.Debug.Log("Classifier executable found.");
            if (text != null) text.text = "Ready.";
            _isClassifierReady = true;
            return Task.CompletedTask;
        }

        private async Task RunClassifierAsync()
        {
            UnityEngine.Debug.Log("Starting classifier...");

            Stopwatch stopwatch = Stopwatch.StartNew();

            string exePath = Path.GetFullPath(Path.Combine(Application.dataPath, classifierExeRelativePath));
            string datapointsPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), datapointsFileName));
            string modelPath = Path.GetFullPath(Path.Combine(Application.dataPath, modelRelativePath));

            if (!File.Exists(exePath))
            {
                UnityEngine.Debug.LogError($"Classifier executable not found at: {exePath}");
                return;
            }

            if (!File.Exists(datapointsPath))
            {
                UnityEngine.Debug.LogError($"Datapoints file not found at: {datapointsPath}");
                if (text != null) text.text = $"Error: {datapointsFileName} not found.";
                return;
            }

            try
            {
                UnityEngine.Debug.Log($"Classifier paths: exe={exePath}, data={datapointsPath}, model={modelPath}");

                using (var handler = SubProcessHandler.ForExecutable(exePath))
                {
                    var args = new List<string> { datapointsPath, modelPath };
                    SubProcessResponse response = await handler.ExecuteAsync(args);

                    stopwatch.Stop();
                    HandleResponse(response, (float)stopwatch.Elapsed.TotalSeconds);
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Error calling classifier: {e.Message}");
            }
        }

        private void HandleResponse(SubProcessResponse response, float time)
        {
            if (response.Status == Status.OK)
            {
                UnityEngine.Debug.Log($"Classifier finished in {time:F3}s!");

                if (text != null)
                {
                    text.text = $"{response.Content}\n\nProbability: {(response.Probability * 100):F1}%";
                }
            }
            else
            {
                string message = response.ErrorMessage;
                if (message != null && message.Contains("charmap") && message.Contains("\\u274c"))
                {
                    message = "Outdated inference.exe (rebuild required). Run rebuild-inference.bat in .PythonModule, then copy inference.exe into that folder.";
                }

                UnityEngine.Debug.LogError($"Classifier error: {message}");
                if (text != null) text.text = $"Error: {message}";
            }
        }
    }
}
