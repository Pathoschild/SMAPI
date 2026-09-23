using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace PackSMAPIZip;

internal class SMAPIAndroidBuildTool
{
    AssemblyDefinition? assembly = null;
    ModuleDefinition mainModule;
    TypeDefinition SMAPIAndroidBuild_TypeDef;
    public SMAPIAndroidBuildTool(string smapiDllFilePath)
    {
        this.assembly = AssemblyDefinition.ReadAssembly(smapiDllFilePath);
        this.mainModule = this.assembly.MainModule;
        this.SMAPIAndroidBuild_TypeDef = this.FindType("StardewModdingAPI.Mobile.SMAPIAndroidBuild");
    }
    private TypeDefinition FindType(string fname)
        => this.mainModule.Types.SingleOrDefault(t => t.FullName == fname);

    public string GetSMAPIVersion()
    {
        var constantsType = this.mainModule.Types.Single(t => t.FullName == "StardewModdingAPI.EarlyConstants");
        var RawApiVersionForAndroidField = constantsType.Fields.Single(p => p.Name == "RawApiVersionForAndroid");
        return RawApiVersionForAndroidField.Constant as string;
    }

    public object ReadConstant(TypeDefinition constantsType, string fieldName)
    {
        return constantsType.Fields.Single(p => p.Name == fieldName).Constant;
    }


    public long GetBuildCode()
    {
        return long.Parse(this.ReadConstant(this.SMAPIAndroidBuild_TypeDef, "BuildCode") as string);
    }
}
