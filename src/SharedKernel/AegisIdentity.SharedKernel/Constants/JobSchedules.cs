namespace AegisIdentity.SharedKernel.Constants;

public static class JobSchedules
{
    public const string DailyAt3Am    = "0 3 * * *";
    public const string CleanupJobName = "cleanup-expired-refresh-tokens";
}
