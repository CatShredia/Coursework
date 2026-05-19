using CatshrediasNews.Client.Resources;
using Microsoft.Extensions.Localization;

namespace CatshrediasNews.Client.Services;

public static class LocalizationExtensions
{
    public static string ArticleStatus(this IStringLocalizer<SharedResources> l, string status) =>
        status switch
        {
            "Draft"         => l["Status_Draft"],
            "PendingReview" => l["Status_Pending"],
            "Published"     => l["Status_Published"],
            "Rejected"      => l["Status_Rejected"],
            _               => status
        };

    public static string RoleName(this IStringLocalizer<SharedResources> l, string role) =>
        role switch
        {
            "Admin"     => l["Role_Admin"],
            "Moderator" => l["Role_Moderator"],
            "Publicist" => l["Role_Publicist"],
            "User"      => l["Role_User"],
            _           => role
        };
}
