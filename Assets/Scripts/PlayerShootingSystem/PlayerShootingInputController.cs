using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerShootingSystem
{
    [RequireComponent(typeof(PlayerShootingController))]
    public class PlayerShootingInputController : MonoBehaviour
    {
        PlayerShootingController _shootingController;

        void Awake()
        {
            _shootingController = GetComponent<PlayerShootingController>();
        }
        void OnShoot(InputValue inputValue)
        {
            if (Time.timeScale == 0f) return;
            _shootingController.OnShootInput(inputValue);
        }

        void OnReload(InputValue inputValue)
        {
            if (Time.timeScale == 0f) return;
            _shootingController.OnReloadInput(inputValue);
        }
        void OnZoom(InputValue inputValue)
        {
            if (Time.timeScale == 0f) return;
            _shootingController.Zoom(inputValue);
        }
        
        void OnMelee(InputValue inputValue)
        {
            if (Time.timeScale == 0f) return;
            _shootingController.OnMeleeInput(inputValue);
        }
        void OnThrow(InputValue inputValue)
        {
            _shootingController.OnThrowInput(inputValue);
        }
        void OnSwapThrowable(InputValue inputValue)
        {
            _shootingController.OnSwapThrowableInput(inputValue);
        }
    }
}