using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PlayerShootingSystem;
using Unity.VisualScripting;
using UnityEngine;

namespace Classification
{
    public class MoveWriter : MonoBehaviour
    {

        /*
         * References to scripts needed for classification
         */
        [Header("References")]
        [SerializeField]
        PlayerShootingController playerShootingController;
        [SerializeField]
        MoveDataExtractor moveDataExtractor;
        [SerializeField]
        DetectionDataExtractor detectionDataExtractor;
        [SerializeField]
        float waitTime;
        [SerializeField]
        int setSize;

        float timer;
        public List<ExtractedData> _datapointList;
        ExtractedData _datapoint;
        string _fullPath=Directory.GetCurrentDirectory()+"/playerdata.csv";
        string _header;

        void UpdateDatapoint()
        {
            _datapoint = new ExtractedData
            {
                avgAccuracy = Mathf.Max((float)playerShootingController.hits/(playerShootingController.hits+ 
                                                                              playerShootingController.shots),0),
                avgY = moveDataExtractor.yPositions.Average(),
                avgDetection = float.IsNaN(detectionDataExtractor.detectionData.Average())?
                    0:detectionDataExtractor.detectionData.Average(),
                avgSpeed = moveDataExtractor.speeds.Average(),
                shots = playerShootingController.shots
            };
            ClearData();
            
        }
        void ClearData()
        {
            playerShootingController.hits = 0;
            moveDataExtractor.yPositions.Clear();
            detectionDataExtractor.detectionData.Clear();
            playerShootingController.shots = 0;
            moveDataExtractor.speeds.Clear();
        }
        void SetNewDatapoint()
        {
            _datapointList.RemoveAt(0);
            _datapointList.Add(_datapoint);
        }

        void SaveToCsv()
        {
            if (_datapointList == null || _datapointList.Count == 0) return;
            var sb = new StringBuilder();
            string path = _fullPath;
            
            if (!File.Exists(path))
            {
                sb.AppendLine("avgAccuracy;shots;avgY;avgDetection;avgSpeed;timestamp");
            }
            string timestamp=System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            foreach (var dp in _datapointList)
            {
                sb.AppendLine(string.Join(";",
                    dp.avgAccuracy.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    dp.shots.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    dp.avgY.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    dp.avgDetection.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    dp.avgSpeed.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    timestamp
                ));
            }
            File.AppendAllText(path, sb.ToString());
            Debug.Log($"Appended to {path}");
            
        }

        void Start()
        {
            _datapointList = new List<ExtractedData>();

            for (int i = 0; i <= setSize; i++)
            {
                _datapointList.Add(_datapoint);
            }
            if (File.Exists(_fullPath))
            {
                File.Delete(_fullPath);
            }
        }


        void Update()
        {
            timer += Time.unscaledDeltaTime;

            if(timer >= waitTime)
            {
                UpdateDatapoint();
                SetNewDatapoint();
                SaveToCsv();
                timer = 0f;
            }
        
        }
    }
}