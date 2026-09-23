using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using rail;
using Sickhead.Engine.Util;
using StardewModdingAPI.Framework;
using StardewValley;
using StardewValley.Audio;
using StardewValley.Extensions;
using StardewValley.GameData;

namespace StardewModdingAPI.Mobile.Facade;

[HarmonyPatch]
public static class StardewAudioMethods
{

    internal static readonly FieldInfo _categories_Field = AccessTools.Field(typeof(AudioEngine), "_categories");

    internal static readonly MethodInfo IAudioEngine_GetCategoryIndex_MethodInfo
        = AccessTools.Method(typeof(StardewAudioMethods), nameof(IAudioEngine_GetCategoryIndex));

    internal static int AudioEngine_GetCategoryIndex(this AudioEngine audioEngine, string name)
    {
        var _categories = _categories_Field.GetValue(audioEngine) as AudioCategory[];
        for (int i = 0; i < _categories.Length; i++)
        {
            if (_categories[i].Name == name)
            {
                return i;
            }
        }
        return -1;
    }
    internal static int AudioEngineWrapper_GetCategoryIndex(this AudioEngineWrapper audioEngineWrapper, string name)
    {
        try
        {
            var engine = audioEngineWrapper.Engine;
            int index = AudioEngine_GetCategoryIndex(engine, name);
            return index;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        return -1;
    }
    public static int IAudioEngine_GetCategoryIndex(this IAudioEngine obj, string name)
    {
        switch (obj)
        {
            case AudioEngineWrapper audioEngineWrapper:
                return audioEngineWrapper.AudioEngineWrapper_GetCategoryIndex(name);

            case DummyAudioEngine dummyAudioEngine:
                return -1;

            default:
                //called method on base type of object
                var GetCategoryIndex_Method = AccessTools.Method(obj.GetType(), "GetCategoryIndex");
                return (int)GetCategoryIndex_Method.Invoke(obj, [name]);
        }
    }


    #region SoundBank
    static readonly FieldInfo soundBank_FieldInfo = AccessTools.Field(typeof(SoundBankWrapper), "soundBank");
    static void SoundBankWrapper_AddCue(this SoundBankWrapper soundBankWrapper, CueDefinition cue)
    {
        soundBankWrapper.GetSoundBank().AddCue(cue);
    }
    public static SoundBank GetSoundBank(this SoundBankWrapper soundBankWrapper)
        => soundBank_FieldInfo.GetValue(soundBankWrapper) as SoundBank;

    public readonly static MethodInfo ISoundBank_AddCue_MethodInfo
        = AccessTools.Method(typeof(StardewAudioMethods), nameof(ISoundBank_AddCue));
    static void ISoundBank_AddCue(this ISoundBank obj, CueDefinition cue)
    {
        switch (obj)
        {
            case SoundBankWrapper soundBankWrapper:
                SoundBankWrapper_AddCue(soundBankWrapper, cue);
                break;

            default:
                //called method on base type of object
                AccessTools.Method(obj.GetType(), "AddCue").Invoke(obj, [cue]);
                break;
        }
    }

    // Fixed patch
    // ISoundBank.Exist(string name)
    public const string ISoundBank_Exists_MethodFullName
        = "System.Boolean StardewValley.ISoundBank::Exists(System.String)";
    public static readonly MethodInfo ISoundBank_Exists_MethodInfo
        = AccessTools.Method(typeof(StardewAudioMethods), nameof(ISoundBank_Exists));
    static bool ISoundBank_Exists(this ISoundBank thisObject, string name)
    {
        switch (thisObject)
        {
            case SoundBankWrapper soundBankWrapper:
                var soundBank_FieldValue = soundBankWrapper.GetSoundBank();
                return soundBank_FieldValue.Exists(name);

            case DummySoundBank dummy:
                return true;

            default:
                //called method on base type of object
                var result = AccessTools.Method(thisObject.GetType(), "Exist").Invoke(thisObject, [name]);
                return (bool)result;
        }
    }

    #endregion


    #region ICue Rewriter

    //TODO
    //SoundHelper PlayLocal it don't use cue.Volume
    static readonly Dictionary<ICue, float> holder_Volume = new();
    internal const string get_Volume_FullName = "System.Single StardewValley.ICue::get_Volume()";
    internal readonly static MethodInfo ICue_get_Volume_MethodInfo = AccessTools.Method(typeof(StardewAudioMethods), nameof(ICue_get_Volume));
    internal static float ICue_get_Volume(this ICue icue)
    {
        switch (icue)
        {
            case CueWrapper cue:
            case DummyCue dummy:
                if (holder_Volume.TryGetValue(icue, out float volume) is false)
                {
                    //create new default value
                    holder_Volume[icue] = volume = 1f;
                }

                return volume;

            default:
                //get Volume on current type
                return (float)AccessTools.Property(icue.GetType(), "Volume").GetValue(icue);
        }
    }

    internal const string set_Volume_FullName = "System.Void StardewValley.ICue::set_Volume(System.Single)";
    internal readonly static MethodInfo ICue_set_Volume_MethodInfo = AccessTools.Method(typeof(StardewAudioMethods), nameof(ICue_set_Volume));
    internal static void ICue_set_Volume(this ICue icue, float newValue)
    {
        switch (icue)
        {
            case DummyCue dummy:
            case CueWrapper cueWrapper:
                holder_Volume[icue] = newValue;
                return;

            default:
                //other class
                AccessTools.Property(icue.GetType(), "Volume").SetValue(icue, newValue);
                break;
        }
    }


    internal const string get_Pitch_FullName = "System.Single StardewValley.ICue::get_Pitch()";
    internal readonly static MethodInfo Get_Pitch_ProxyMethodInfo = AccessTools.Method(typeof(StardewAudioMethods), nameof(Get_Pitch_Proxy));
    readonly static Dictionary<ICue, float> holder_Pitch = new();
    internal static float Get_Pitch_Proxy(this ICue icue)
    {
        switch (icue)
        {
            case DummyCue dummy:
                //src code in PC it return 0f fake value
                return 0f;

            case CueWrapper cueWrapper:

                if (holder_Pitch.TryGetValue(icue, out float resultFromCueWrapper) is false)
                {
                    //create holder object with value
                    resultFromCueWrapper = holder_Pitch[icue] = 0f;
                }

                return resultFromCueWrapper;

            default:
                //get value from current type
                var pitchProperty = AccessTools.Property(icue.GetType(), "Pitch");
                float result = (float)pitchProperty.GetValue(icue);
                return result;
        }
    }

    internal const string set_Pitch_FullName = "System.Void StardewValley.ICue::set_Pitch(System.Single)";
    internal readonly static MethodInfo Set_Pitch_ProxyMethodInfo = AccessTools.Method(typeof(StardewAudioMethods), nameof(Set_Pitch_Proxy));
    internal static void Set_Pitch_Proxy(this ICue icue, float newValue)
    {
        switch (icue)
        {
            case DummyCue dummy:
            case CueWrapper cueWrapper:
                holder_Pitch[icue] = newValue;
                return;

            default:
                //set value Pitch on current type
                AccessTools.Property(icue.GetType(), "Pitch").SetValue(icue, newValue);
                break;
        }
    }


    internal const string get_IsPitchBeingControlledByRPC_FullName
        = "System.Boolean StardewValley.ICue::get_IsPitchBeingControlledByRPC()";
    internal readonly static MethodInfo Get_IsPitchBeingControlledByRPC_MethodInfo
        = AccessTools.Method(typeof(StardewAudioMethods), nameof(Get_IsPitchBeingControlledByRPC_Proxy));
    static bool Get_IsPitchBeingControlledByRPC_Proxy(this ICue icue)
    {
        //TODO
        return false;
    }

    #endregion
}
