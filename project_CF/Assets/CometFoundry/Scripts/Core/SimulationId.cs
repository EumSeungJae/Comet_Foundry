using System;

namespace CometFoundry.Core
{
    [Serializable]
    public readonly struct SimulationId : IEquatable<SimulationId>
    {
        public static readonly SimulationId Empty = new SimulationId(string.Empty);

        public SimulationId(string value)
        {
            Value = value ?? string.Empty;
        }

        public string Value { get; }
        public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

        public bool Equals(SimulationId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SimulationId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
