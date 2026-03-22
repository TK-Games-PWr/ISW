using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Classification
{
    public class MoveDataExtractor:MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        CharacterController characterController;
        
        public List<float> yPositions;
        public List<float> speeds;
        void Start()
        {
            StartCoroutine(Extractor());
        }
        IEnumerator Extractor()
        {
            while (true)
            {
                yPositions.Add(transform.position.y);yPositions.Add(transform.position.y);
                speeds.Add(characterController.velocity.magnitude);
                yield return new WaitForSecondsRealtime(0.2f);
            }
        }
    }
}
