namespace 崴正.Models;

public class AuditLogQueryResult
{
    public string? Search { get; set; }

    public string? Module { get; set; }

    public string? ActionType { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 25;

    public int TotalCount { get; set; }

    public int TotalPages { get; set; } = 1;

    public List<string> Modules { get; set; } = new();

    public List<string> ActionTypes { get; set; } = new();

    public List<AuditLogListItem> Items { get; set; } = new();
}

public class AuditLogListItem
{
    public long LogId { get; set; }

    public string Module { get; set; } = string.Empty;

    public string ActionType { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public int? ProjectId { get; set; }

    public string? ProjectName { get; set; }

    public string Description { get; set; } = string.Empty;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime CreatedAt { get; set; }
}
