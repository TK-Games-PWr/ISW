using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EnemySystem;
using UnityEngine;


namespace Classification
{
    public class DetectionDataExtractor : MonoBehaviour
    {
        public List<float> detectionData;

        [SerializeField]
        List<AICore> aiData;

        List<int> _toRemove = new List<int>();
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
            float detection = 0;
            while (true)
            {
                if (aiData != null)
                {
                    for (var i = 0; i < aiData.Count; i++)
                    {
                        if (aiData[i] && !_toRemove.Contains(i))
                        {
                            detection += aiData[i].triggerMultiplier;
                        }
                        else
                        {
                            if (!_toRemove.Contains(i))
                            {
                                _toRemove.Add(i);
                            }
                        }
                    }

                    for (int i = 0; i < _toRemove.Count; i++)
                    {
                        aiData.RemoveAt(_toRemove[i]);
                    }
                }

                _toRemove.Clear();

                float divCount = aiData != null ? aiData.Count : 0;
                float averageForThisTick = (divCount > 0) ? (detection / divCount) : 0;

                detectionData.Add(averageForThisTick);
                detection = 0;

                yield return new WaitForSecondsRealtime(0.2f);
            }
        }
    }
}
