namespace EasyNSIS.Models;

/// <summary>
/// ビルド状態
/// </summary>
public enum BuildState
{
    Idle,
    Building,
    Completed,
    Failed,
    Cancelled
}
