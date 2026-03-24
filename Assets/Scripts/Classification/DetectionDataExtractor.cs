using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EnemySystem;
using UnityEngine;

namespace Classification
{
    public class DetectionDataExtractor:MonoBehaviour
    {
        public List<float> detectionData;
        
        [SerializeField]
        List<AICore> aiData;

        List<int> _toRemove=new List<int>();
        void Awake()
        {
           aiData = FindObjectsByType<AICore>(FindObjectsSortMode.None).ToList();
        }
        void Start()
        {
            StartCoroutine(Extractor());
        }
        IEnumerator Extractor()
        {
            float detection=0;
            while (true)
            {
                for(var i = 0;i<aiData.Count;i++)
                {
                    if (aiData[i] && !_toRemove.Contains(i))
                    {
                        detection += aiData[i].triggerMultiplier;
                    }
                    else
                    {
                        _toRemove.Add(i);
                    }
                }
                for (int i = 0; i < _toRemove.Count; i++)
                {
                    aiData.RemoveAt(_toRemove[i]);
                }
                _toRemove.Clear();
                detectionData.Add(detection/aiData.Count);
                detection = 0;
                yield return new WaitForSecondsRealtime(0.2f);

            }
        }
    }
}
