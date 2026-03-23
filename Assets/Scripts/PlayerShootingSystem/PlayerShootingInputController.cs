using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerShootingSystem
{
    [RequireComponent(typeof(PlayerShootingController))]
    public class PlayerShootingInputController : MonoBehaviour
    {
        PlayerShootingController _shootingController;
        StealthKillController _stealthKillController;

        void Awake()
        {
            _shootingController = GetComponent<PlayerShootingController>();
            _stealthKillController =  GetComponent<StealthKillController>();
        }
        void OnShoot(InputValue inputValue)
        {
            _shootingController.OnShootInput(inputValue);
        }

        void OnReload(InputValue inputValue)
        {
            _shootingController.OnReloadInput(inputValue);
        }

        void OnInteract(InputValue inputValue)
        {
            _stealthKillController.OnInteractInput(inputValue);
        }
    }
}