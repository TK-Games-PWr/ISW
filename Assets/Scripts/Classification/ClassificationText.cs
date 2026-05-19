using UnityEngine;
using TMPro;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Networking; // For downloading

namespace Classification
{
    public class ClassificationText : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI text;
        [SerializeField] string classifierExeRelativePath = "Scripts/Classification/.PythonModule/dist/";
        [SerializeField] string classifierBinaryName = "inference";
        [SerializeField] string modelRelativePath = "Scripts/Classification/.PythonModule/";
        [SerializeField] string modelFileName = "model.joblib";
        [SerializeField] string datapointsFileName = "playerdata.csv";
        
        [SerializeField] string githubRepo = "rybydrapiezne/ISW";
        [SerializeField] string releaseTag = "module";

        [SerializeField] bool logsEnabled = false;
        
        string _classifierFileName;
        
        bool _isClassifierReady;

        void Start()
        {
            if (text == null) text = GetComponent<TextMeshProUGUI>();

            if (text != null) text.text = "Initializing classifier...";
            
            if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
            {
                _classifierFileName = classifierBinaryName + ".exe";
            }
            else if (Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.LinuxEditor)
            {
                _classifierFileName = classifierBinaryName;
            }
            else
            {
                if(logsEnabled) UnityEngine.Debug.LogError($"Unsupported platform: {Application.platform}");
                return;
            }
            
            classifierExeRelativePath = Path.Combine(classifierExeRelativePath, _classifierFileName);

            _ = SetupClassifierAsync();
        }

        public void UpdateClassificationResult()
        {
            if (!_isClassifierReady)
            {
                if(logsEnabled) UnityEngine.Debug.LogWarning("Classifier is not ready. Please wait.");
                return;
            }

            _ = RunClassifierAsync();
        }

        private async Task SetupClassifierAsync()
        {
            string exePath = Path.Combine(Application.dataPath, classifierExeRelativePath);
            
            if (!File.Exists(exePath))
            {
                string downloadUrl = $"https://github.com/{githubRepo}/releases/download/{releaseTag}/{_classifierFileName}";
                
                if(logsEnabled) UnityEngine.Debug.Log($"Classifier executable not found. Attempting download... {downloadUrl}");
                if (text != null) text.text = "Downloading latest classifier...";
                
                bool downloadSuccess = await DownloadBinaryAsync(downloadUrl, exePath);

                if (!downloadSuccess)
                {
                    if (text != null) text.text = $"Error: Failed to download classifier from {downloadUrl}";
                    _isClassifierReady = false;
                    return;
                }
            }
            
            string modelPath = Path.Combine(Application.dataPath, modelRelativePath, modelFileName);
            if (!File.Exists(modelPath))
            {
                string downloadUrl = $"https://github.com/{githubRepo}/releases/download/{releaseTag}/{modelFileName}";
                
                if(logsEnabled) UnityEngine.Debug.Log($"Model file not found. Attempting download... {downloadUrl}");
                if (text != null) text.text = "Downloading latest model...";
                
                bool downloadSuccess = await DownloadBinaryAsync(downloadUrl, modelPath);

                if (!downloadSuccess)
                {
                    if (text != null) text.text = $"Error: Failed to download model from {downloadUrl}";
                    _isClassifierReady = false;
                    return;
                }
            }

            if(logsEnabled) UnityEngine.Debug.Log("Classifier executable and model found.");
            if (text != null) text.text = "Ready.";
            _isClassifierReady = true;
        }

        private async Task<bool> DownloadBinaryAsync(string url, string savePath)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                var operation = webRequest.SendWebRequest();
                
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    if(logsEnabled) UnityEngine.Debug.LogError($"[Downloader] Error downloading binary: {webRequest.error}");
                    return false;
                }

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                    
                    await File.WriteAllBytesAsync(savePath, webRequest.downloadHandler.data);
                    if(logsEnabled) UnityEngine.Debug.Log($"[Downloader] Successfully downloaded and saved to: {savePath}");

                    if (Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.LinuxEditor)
                    {
                        AssignLinuxPermissions(savePath);
                    }

                    return true;
                }
                catch (System.Exception e)
                {
                    if(logsEnabled) UnityEngine.Debug.LogError($"[Downloader] Failed to save binary to disk: {e.Message}");
                    return false;
                }
            }
        }

        private void AssignLinuxPermissions(string filePath)
        {
            try
            {
                Process permissionProcess = new Process();
                permissionProcess.StartInfo.FileName = "chmod";
                permissionProcess.StartInfo.Arguments = $"+x \"{filePath}\"";
                permissionProcess.StartInfo.UseShellExecute = false;
                permissionProcess.StartInfo.CreateNoWindow = true;
                permissionProcess.Start();
                permissionProcess.WaitForExit();
                if(logsEnabled) UnityEngine.Debug.Log("[Downloader] Applied execution permissions (chmod +x) to Linux binary.");
            }
            catch (System.Exception e)
            {
                if(logsEnabled) UnityEngine.Debug.LogWarning($"[Downloader] Could not automatically set execution permissions: {e.Message}");
            }
        }

        private async Task RunClassifierAsync()
        {
            if(logsEnabled) UnityEngine.Debug.Log("Starting classifier...");

            Stopwatch stopwatch = Stopwatch.StartNew();

            string exePath = Path.GetFullPath(Path.Combine(Application.dataPath, classifierExeRelativePath));
            string datapointsPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), datapointsFileName));
            string modelPath = Path.GetFullPath(Path.Combine(Application.dataPath, modelRelativePath, modelFileName));

            if (!File.Exists(exePath))
            {
                if(logsEnabled) UnityEngine.Debug.LogError($"Classifier executable not found at: {exePath}");
                return;
            }

            if (!File.Exists(datapointsPath))
            {
                if(logsEnabled) UnityEngine.Debug.LogError($"Datapoints file not found at: {datapointsPath}");
                if (text != null) text.text = $"Error: {datapointsFileName} not found.";
                return;
            }

            try
            {
                if(logsEnabled) UnityEngine.Debug.Log($"Classifier paths: exe={exePath}, data={datapointsPath}, model={modelPath}");

                using (var handler = SubProcessHandler.ForExecutable(exePath))
                {
                    var args = new List<string> { datapointsPath, modelPath };
                    SubProcessResponse response = await handler.ExecuteAsync(args, logsEnabled);

                    stopwatch.Stop();
                    HandleResponse(response, (float)stopwatch.Elapsed.TotalSeconds);
                }
            }
            catch (System.Exception e)
            {
                if(logsEnabled) UnityEngine.Debug.LogError($"Error calling classifier: {e.Message}");
            }
        }

        private void HandleResponse(SubProcessResponse response, float time)
        {
            if (response.Status == Status.OK)
            {
                if(logsEnabled) UnityEngine.Debug.Log($"Classifier finished in {time:F3}s!");

                if (text != null)
                {
                    ClassificationGameplay.Instance.SetCategory(response.Content);
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

                if(logsEnabled) UnityEngine.Debug.LogError($"Classifier error: {message}");
                if (text != null) text.text = $"Error: {message}";
            }
        }
    }
}