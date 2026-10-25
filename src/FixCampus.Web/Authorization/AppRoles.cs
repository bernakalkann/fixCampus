namespace FixCampus.Web.Authorization;

public static class AppRoles
{
    public const string Student = "Ogrenci";
    public const string Administrator = "Yonetici";
}

public static class AppPolicies
{
    public const string StudentAccess = "OgrenciErisimi";
    public const string AdministratorAccess = "YoneticiErisimi";
}
