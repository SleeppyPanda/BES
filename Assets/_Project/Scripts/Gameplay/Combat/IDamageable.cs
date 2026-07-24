namespace BES.Gameplay
{
    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(DamageInfo damage);
    }

    public struct DamageInfo
    {
        public float Amount;
        public bool IsCritical;
        public UnityEngine.GameObject Source;

        public DamageInfo(float amount, UnityEngine.GameObject source, bool isCritical = false)
        {
            Amount = amount;
            Source = source;
            IsCritical = isCritical;
        }
    }
}
