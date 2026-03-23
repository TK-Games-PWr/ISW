using UnityEngine;

namespace PlayerShootingSystem
{
    public class MeleeAnimCallback : MonoBehaviour
    {
        [SerializeField] PlayerShootingController playerShootingController;
        public void OnAnimEnd()
        {
            playerShootingController.OnMeleeAnimEnd();
        }
    }
}
