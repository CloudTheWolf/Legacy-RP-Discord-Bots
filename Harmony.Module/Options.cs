using DSharpPlus.Entities;

namespace Harmony.Module
{
    internal class Options
    {
        public static string MySqlHost { get; set; } = null!;

        public static int MySqlPort { get; set; }

        public static string MySqlUsername { get; set; } = null!;

        public static string MySqlPassword { get; set; } = null!;

        public static string MySqlDatabase { get; set; } = null!;

        public static string CompanyName { get; set; } = null!;

        public static DiscordRole ManagerRole { get; set; }

        public static ulong GuildId { get; set; }
        
        public static ulong ManagerRoleId { get; set; }

        public static string ApiUrl { get; set; }
        public static string RestApiUrl { get; set; }
        public static string ApiKey { get; set; }

        public static ulong DutyChannelId { get; set; }
        public static DiscordChannel DutyChannel { get; set; }
    }
}