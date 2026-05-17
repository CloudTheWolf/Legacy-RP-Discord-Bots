using DSharpPlus.Entities;

namespace DOC.Module
{
    internal class Options
    {
        /// <summary>
        /// Gets or sets the Guild ID
        /// </summary>
        public static ulong GuildId { get; set; }

        /// <summary>
        /// Gets or sets the Rest API URL
        /// </summary>
        public static string RestApiUrl { get; set; } = "https://rest.opfw.net/c3";

        /// <summary>
        /// Gets or sets OP-FW Week Zero EPOCH Timestamp
        /// 1609113600 = Monday, 28 December 2020 00:00:00
        /// </summary>
        public static int WeekZero { get; set; } = 1609113600;
        
        /// <summary>
        /// Gets or sets the API URL for Non-Rest API Calls
        /// </summary>
        public static string ApiUrl { get; set; }

        /// <summary>
        /// Gets or sets the API Key for authentication with <see cref="RestApiUrl"/>
        /// </summary>
        public static string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the channel within <see cref="GuildId"/> to post On Duty Messages
        /// </summary>
        public static ulong OnDutyChannel { get; set; }

        /// <summary>
        /// Gets or sets the last message ID for the On Duty Message
        /// </summary>
        public static ulong OnDutyMessage { get; set; } = ulong.MinValue;

        /// <summary>
        /// Gets or sets the last message <see cref="DateTime"/>
        /// </summary>
        public static DateTime LastMessage { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Role Id for DOC Members 
        /// </summary>
        public static ulong DocRoleId {  get; set; }
        public static string LogPrefix { get; set; }
    }
}