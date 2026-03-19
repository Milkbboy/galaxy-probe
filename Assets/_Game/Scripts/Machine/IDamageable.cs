namespace DrillCorp.Machine
{
    public interface IDamageable
    {
        float CurrentHealth { get; }
        float MaxHealth { get; }
        bool IsDead { get; }

        void TakeDamage(float damage);
        void Heal(float amount);
    }
}
