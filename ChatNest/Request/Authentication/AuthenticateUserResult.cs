namespace ChatNest.Request.Authentication
{
    public enum LoginFailureReason
    {
        InvalidCredentials,
        LockedOut,
        NotAllowed
    }

    public sealed record AuthenticateUserResult(
        bool Succeeded,
        LoginFailureReason? FailureReason = null)
    {
        public static AuthenticateUserResult Success() =>
            new(true);

        public static AuthenticateUserResult Failure(
            LoginFailureReason reason) =>
            new(false, reason);
    }
}
