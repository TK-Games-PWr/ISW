using System;
using TK_Shared._3DPlayerMovement;
using TK_Shared.ObjectInteractions3D;
using TMPro;
using UnityEngine;

namespace UI
{
    public class PickInformatorUI : MonoBehaviour
    {
        [SerializeField] GameObject pickInformation;
        [SerializeField] TextMeshProUGUI pickText;
        [SerializeField] String partOfText = "Press F to pick up: ";
        void OnEnable()
        {
            PlayerActionsController.OnPickableCheck += OnPickUpdate;
        }
        
        void OnDisable()
        {
            PlayerActionsController.OnPickableCheck -= OnPickUpdate;
        }
        
        void OnPickUpdate(GrabbableObject grabbableObject)
        {
            if (grabbableObject is not null)
            {
                pickInformation.SetActive(true);
                pickText.text = partOfText + grabbableObject.GetName();
            }
            else
            {
                pickInformation.SetActive(false);
            }
        }
    }
}
