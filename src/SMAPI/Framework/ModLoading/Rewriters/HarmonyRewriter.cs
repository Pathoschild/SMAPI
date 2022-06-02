using System;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewModdingAPI.Framework.ModLoading.RewriteFacades;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters;

/// <summary>Detects Harmony references, and rewrites Harmony 1.x assembly references to work with Harmony 2.x.</summary>
internal class HarmonyRewriter : BaseInstructionHandler
{
    /*********
    ** Fields
    *********/
    /// <summary>Whether any Harmony 1.x types were replaced.</summary>
    private bool ReplacedTypes;

    /// <summary>Whether to rewrite Harmony 1.x code.</summary>
    private readonly bool ShouldRewrite;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public HarmonyRewriter(bool shouldRewrite = true)
        : base(defaultPhrase: "Harmony 1.x")
    {
        this.ShouldRewrite = shouldRewrite;
    }

    /// <inheritdoc />
    public override bool Handle(ModuleDefinition module, TypeReference type, Action<TypeReference> replaceWith)
    {
        // detect Harmony
        if (type.Scope is not AssemblyNameReference scope || scope.Name != "0Harmony")
            return false;

        // rewrite Harmony 1.x type to Harmony 2.0 type
        if (this.ShouldRewrite && scope.Version.Major == 1)
        {
            Type targetType = this.GetMappedType(type);
            replaceWith(module.ImportReference(targetType));
            this.OnChanged();
            this.ReplacedTypes = true;
            return true;
        }

        this.MarkFlag(InstructionHandleResult.DetectedGamePatch);
        return false;
    }

    /// <inheritdoc />
    public override bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction)
    {
        if (this.ShouldRewrite)
        {
            // rewrite Harmony 1.x methods to Harmony 2.0
            MethodReference? methodRef = RewriteHelper.AsMethodReference(instruction);
            if (this.TryRewriteMethodsToFacade(module, methodRef))
            {
                this.OnChanged();
                return true;
            }

            // rewrite renamed fields
            FieldReference? fieldRef = RewriteHelper.AsFieldReference(instruction);
            if (fieldRef != null)
            {
                if (fieldRef.DeclaringType.FullName == "HarmonyLib.HarmonyMethod" && fieldRef.Name == "prioritiy")
                {
                    fieldRef.Name = nameof(HarmonyMethod.priority);
                    this.OnChanged();
                }
            }
        }

        return false;
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Update the mod metadata when any Harmony 1.x code is migrated.</summary>
    private void OnChanged()
    {
        this.MarkRewritten();
        this.MarkFlag(InstructionHandleResult.DetectedGamePatch);
    }

    /// <summary>Rewrite methods to use Harmony facades if needed.</summary>
    /// <param name="module">The assembly module containing the method reference.</param>
    /// <param name="methodRef">The method reference to map.</param>
    private bool TryRewriteMethodsToFacade(ModuleDefinition module, MethodReference? methodRef)
    {
        if (!this.ReplacedTypes)
            return false; // not Harmony (or already using Harmony 2.0)

        // get facade type
        Type? toType = methodRef?.DeclaringType.FullName switch
        {
            "HarmonyLib.Harmony" => typeof(HarmonyInstanceFacade),
            "HarmonyLib.AccessTools" => typeof(AccessToolsFacade),
            "HarmonyLib.HarmonyMethod" => typeof(HarmonyMethodFacade),
            _ => null
        };
        if (toType == null)
            return false;

        // map if there's a matching method
        if (RewriteHelper.HasMatchingSignature(toType, methodRef!))
        {
            methodRef!.DeclaringType = module.ImportReference(toType);
            return true;
        }

        return false;
    }

    /// <summary>Get an equivalent Harmony 2.x type.</summary>
    /// <param name="type">The Harmony 1.x type.</param>
    private Type GetMappedType(TypeReference type)
    {
        return type.FullName switch
        {
            "Harmony.HarmonyInstance" => typeof(Harmony),
            "Harmony.ILCopying.ExceptionBlock" => typeof(ExceptionBlock),
            _ => this.GetMappedTypeByConvention(type)
        };
    }

    /// <summary>Get an equivalent Harmony 2.x type using the convention expected for most types.</summary>
    /// <param name="type">The Harmony 1.x type.</param>
    private Type GetMappedTypeByConvention(TypeReference type)
    {
        string fullName = type.FullName.Replace("Harmony.", "HarmonyLib.");
        string targetName = typeof(Harmony).AssemblyQualifiedName!.Replace(typeof(Harmony).FullName!, fullName);
        return Type.GetType(targetName, throwOnError: true)!;
    }
}