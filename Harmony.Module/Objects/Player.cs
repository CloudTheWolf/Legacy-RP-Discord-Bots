using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony.Module.Converters;
using Newtonsoft.Json;

namespace Harmony.Module.Objects
{
    internal class Player
    {
        public string PlayerName;
        public int Flags;
        public int Source;
        public string LicenseIdentifier;
        [JsonConverter(typeof(CharacterConverter))]
        public Character Character;
    }
}
