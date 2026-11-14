using Secretariat.Web.Data;

namespace Secretariat.Web.Pages;

public static class Ui
{
    public static string StatusBadge(EnrollmentStatus status) => status switch
    {
        EnrollmentStatus.Enrolled => "text-bg-success",
        EnrollmentStatus.Suspended => "text-bg-danger",
        _ => "text-bg-secondary",
    };

    public static string Local(DateTime? utc) =>
        utc is null ? "—" : DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm");
}
