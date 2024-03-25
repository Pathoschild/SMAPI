using System.Collections.Generic;
using System.IO;

namespace StardewModdingAPI.ModBuildConfig.Framework
{
    public interface IModFileManager
    {
        /// <summary>Get the files in the mod package.</summary>
        public IDictionary<string, FileInfo> GetFiles();
    }
}
