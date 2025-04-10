namespace QueuingDemo.Logic;

public class Item
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public int Attempts { get; set; } = 1;
    public Guid Lock { get; set; }
    public bool IsProcessing { get; set; }
}
