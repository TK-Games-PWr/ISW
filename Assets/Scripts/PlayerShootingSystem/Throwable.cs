using UnityEngine;
namespace PlayerShootingSystem
{
    public interface IThrowable
    {
        public void Thrown(ThrowableInfo info);
        public void Cook();
    }
}
