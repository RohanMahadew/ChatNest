namespace BLL.Models.DTOs
{
    public sealed record PublicGroupDTO(
        int Id,
        string Uuid,
        string Name,
        bool IsMember
    );
}

