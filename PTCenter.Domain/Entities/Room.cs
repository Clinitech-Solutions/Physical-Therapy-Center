namespace PTCenter.Domain.Entities;

public class Room : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int Capacity { get; set; }
    public bool IsActive { get; set; } = true;
}
