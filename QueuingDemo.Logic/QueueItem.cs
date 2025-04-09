namespace QueuingDemo.Logic;

public class QueueItem
{
    public string Reference { get; set; } = string.Empty;
    public int Attempts { get; set; } = 0;
}
