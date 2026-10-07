namespace DroneEnergyControl.Domain;

public sealed class BatteryUnit
{
    public string Id { get; }
    public decimal Capacity { get; }
    public decimal Charge { get; private set; }

    public BatteryUnit(string id, decimal capacity, decimal initialCharge)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Battery ID must be non-empty.", nameof(id));
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
        if (initialCharge < 0 || initialCharge > capacity)
            throw new ArgumentOutOfRangeException(nameof(initialCharge), "Initial charge must be between zero and capacity.");

        Id = id.Trim();
        Capacity = capacity;
        Charge = initialCharge;
    }

    public bool ChargeBy(decimal amount)
    {
        if (amount < 0 || Charge + amount > Capacity)
            return false;

        Charge += amount;
        return true;
    }

    public bool Consume(decimal amount)
    {
        if (amount < 0 || Charge - amount < 0)
            return false;

        Charge -= amount;
        return true;
    }

    public bool TransferTo(BatteryUnit target, decimal amount)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (ReferenceEquals(this, target) || amount < 0 || Charge < amount || target.Charge + amount > target.Capacity)
            return false;

        Charge -= amount;
        target.Charge += amount;
        return true;
    }
}
