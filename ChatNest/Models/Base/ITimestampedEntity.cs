namespace ChatNest.Models.Base
{
    // This solely exists because ApplicationUser cannot inherit from IndentityUser and BaseEntity at the same time. 
    // So, BaseEntity now inherits an interface, so ApplicationUser can also get the interface and code can reference ITimestampedEntity
    // abstraction is great.
    public interface ITimestampedEntity
    {
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
