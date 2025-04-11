namespace QueuingDemo.Logic.Models;

public class Item
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public Guid Lock { get; set; }
}
