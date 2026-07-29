using System;
using TK_Shared._3DPlayerMovement;
using TMPro;
using UnityEngine;

namespace UI
{
    public class PickInformatorUI : MonoBehaviour
    {
        [SerializeField] GameObject pickInformation;
        void OnEnable()
        {
            PlayerActionsController.OnPickableCheck += OnPickUpdate;
        }
        
        void OnDisable()
        {
            PlayerActionsController.OnPickableCheck -= OnPickUpdate;
        }
        
        void OnPickUpdate(bool isPickable)
        {
            pickInformation.SetActive(isPickable);
        }
    }
}
