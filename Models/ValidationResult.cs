namespace EasyNSIS.Models;

/// <summary>
/// バリデーション結果
/// </summary>
public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<ValidationError> Errors { get; } = [];

    public void AddError(string field, string message)
    {
        Errors.Add(new ValidationError(field, message));
    }

    public void Merge(ValidationResult other)
    {
        Errors.AddRange(other.Errors);
    }
}

public record ValidationError(string Field, string Message);
