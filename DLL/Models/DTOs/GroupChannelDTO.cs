namespace BLL.Models.DTOs
{
    public sealed record GroupChannelDTO(
        int Id,
        string Uuid,
        string? Name,
        string Role,
        string Type = "PRIVATE"
    );
}
