namespace CometFoundry.Core
{
    public static class Currency
    {
        public const string Name = "솔라";
        public const string Code = "SOL";

        public static string Format(double amount)
        {
            return $"{amount:0.#} {Name}";
        }
    }
}
