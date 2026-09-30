namespace ChatNest.Request.Authentication
{
    public sealed record StoreUserResult(
    bool Succeeded,
    IReadOnlyCollection<string> Errors)
    {
        public static StoreUserResult Success() =>
            new(true, Array.Empty<string>());

        public static StoreUserResult Failure(
            IEnumerable<string> errors) =>
            new(false, errors.ToArray());
    }
}
