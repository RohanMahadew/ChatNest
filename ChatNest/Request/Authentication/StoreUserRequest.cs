namespace ChatNest.Request.Authentication
{
    public sealed record StoreUserRequest(
        string Email,
        string Username,
        DateOnly DateOfBirth,
        string Password);
}