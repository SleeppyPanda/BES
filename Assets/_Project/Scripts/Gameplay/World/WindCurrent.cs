using UnityEngine;

namespace BES.Gameplay
{
    public class WindCurrent : MonoBehaviour
    {
        [SerializeField] float windForce = 10f; // Vận tốc bốc lên (m/s)
        [SerializeField] float currentHeight = 8f; // Chiều cao tối đa cột gió

        void OnTriggerStay(Collider other)
        {
            var motor = other.GetComponent<PlayerMotor>();
            if (motor == null)
            {
                // Fallback check parent or children just in case
                motor = other.GetComponentInParent<PlayerMotor>();
            }

            if (motor != null)
            {
                // Thiết lập vận tốc đi lên liên tục để thắng trọng lực
                motor.SetVerticalVelocity(windForce);
            }
        }
    }
}
