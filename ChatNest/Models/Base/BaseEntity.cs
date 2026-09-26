namespace ChatNest.Models.Base
{
    public abstract class BaseEntity : ITimestampedEntity
    {
        // One class handling CreatedAt and UpdatedAt
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
