namespace QuickSign.Application.Contracts.Permissions;

/// <summary>
/// Static string constants defining RBAC permission names across the QuickSign platform.
/// </summary>
public static class QuickSignPermissions
{
    public const string GroupName = "QuickSign";

    public static class Certificates
    {
        public const string Default = GroupName + ".Certificates";
        public const string Issue = Default + ".Issue";
        public const string Revoke = Default + ".Revoke";
        public const string ManageAll = Default + ".ManageAll";
    }

    public static class ApiKeys
    {
        public const string Default = GroupName + ".ApiKeys";
        public const string Create = Default + ".Create";
        public const string Delete = Default + ".Delete";
    }

    public static class Documents
    {
        public const string Default = GroupName + ".Documents";
        public const string View = Default + ".View";
        public const string ViewAll = Default + ".ViewAll";
    }

    public static class Statistics
    {
        public const string Default = GroupName + ".Statistics";
        public const string ViewAdminOverview = Default + ".ViewAdminOverview";
    }
}
