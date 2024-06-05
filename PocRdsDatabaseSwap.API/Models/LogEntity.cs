namespace PocRdsDatabaseSwap.API.Models;

public class LogEntity
{
    public Guid ID { get; set; }
    public DateTime LogDate { get; set; }
    public string Body { get; set; }
}