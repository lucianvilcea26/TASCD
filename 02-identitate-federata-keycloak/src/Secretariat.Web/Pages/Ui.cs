using Secretariat.Web.Data;

namespace Secretariat.Web.Pages;

public static class Ui
{
    public static string StatusBadge(EnrollmentStatus status) => status switch
    {
        EnrollmentStatus.Enrolled => "u-pill u-pill-success",
        EnrollmentStatus.Suspended => "u-pill u-pill-danger",
        _ => "u-pill u-pill-muted",
    };

    public static string Local(DateTime? utc) =>
        utc is null ? "—" : DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm");
}
