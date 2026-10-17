using System.Text.Json;

namespace 崴正.Models;

public sealed class HistoryOperationReceipt
{
    public Guid OperationId { get; set; }
    public int ProjectId { get; set; }
    public string ActionType { get; set; } = "";
    public string ResultJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
}
