using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using StardewModdingAPI.Framework.ModLoading.Rewriters;
using StardewValley;
using StardewValley.Quests;
using StardewValley.SaveMigrations;
using StardewValley.SaveSerialization;
using StardewValley.TerrainFeatures;

namespace StardewModdingAPI.Mobile.Facade;

public class SaveGameFacade : SaveGame, IRewriteFacade
{
    public static void ensureFolderStructureExists()
    {
        string folderName = FilterFileName(Game1.GetSaveGameName()) + "_" + Game1.uniqueIDForThisGame;
        Directory.CreateDirectory(Path.Combine(StardewValley.Program.GetSavesFolder(), folderName));
    }

    // on Android we don't use this,
    // because use XmlSerialier Generator instead of dynamic serialization
    public static XmlSerializer serializer
    {
        get => SaveSerializer.GetSerializer(typeof(SaveGame));
        set { }
    }

    public static XmlSerializer farmerSerializer
    {
        get => SaveSerializer.GetSerializer(typeof(Farmer));
        set { }
    }
}
