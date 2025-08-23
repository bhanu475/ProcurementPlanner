using System.ComponentModel.DataAnnotations;
using ProcurementPlanner.Core.Models;

namespace ProcurementPlanner.Core.Entities;

public class NotificationTemplate : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public NotificationType Type { get; set; }

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public string RequiredParametersJson { get; set; } = "[]";

    // Helper property to work with required parameters as a list
    public List<string> RequiredParameters
    {
        get => System.Text.Json.JsonSerializer.Deserialize<List<string>>(RequiredParametersJson) ?? new List<string>();
        set => RequiredParametersJson = System.Text.Json.JsonSerializer.Serialize(value);
    }

    // Business logic methods
    public string RenderSubject(Dictionary<string, string> parameters)
    {
        var result = Subject;
        foreach (var param in parameters)
        {
            result = result.Replace($"{{{param.Key}}}", param.Value);
        }
        return result;
    }

    public string RenderBody(Dictionary<string, string> parameters)
    {
        var result = Body;
        foreach (var param in parameters)
        {
            result = result.Replace($"{{{param.Key}}}", param.Value);
        }
        return result;
    }

    public bool ValidateParameters(Dictionary<string, string> parameters)
    {
        var requiredParams = RequiredParameters;
        return requiredParams.All(param => parameters.ContainsKey(param) && !string.IsNullOrEmpty(parameters[param]));
    }
}