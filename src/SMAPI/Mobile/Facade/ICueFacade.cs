using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewModdingAPI.Framework.ModLoading.Rewriters;
using StardewValley;

namespace StardewModdingAPI.Mobile.Facade;

internal interface ICueFacade : ICue, IRewriteFacade
{
    float Pitch { get; set; }
    float Volume { get; set; }
    bool IsPitchBeingControlledByRPC { get; }
}
