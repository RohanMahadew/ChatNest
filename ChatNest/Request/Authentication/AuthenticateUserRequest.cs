namespace ChatNest.Request.Authentication
{
    public sealed record AuthenticateUserRequest(
    string Username,
    string Password);
}
