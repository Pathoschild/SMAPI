using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StardewModdingAPI.ModBuildConfig.Analyzer;

/// <summary>Detects implicit conversion from Stardew Valley's <c>Netcode</c> types. These have very unintuitive implicit conversion rules, so mod authors should always explicitly convert the type with appropriate null checks.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NetFieldAnalyzer : DiagnosticAnalyzer
{
    /*********
    ** Fields
    *********/
    /// <summary>The namespace for Stardew Valley's <c>Netcode</c> types.</summary>
    private const string NetcodeNamespace = "Netcode";

    /// <summary>Maps net fields to their equivalent non-net properties where available.</summary>
    private readonly IDictionary<string, string> NetFieldWrapperProperties = new Dictionary<string, string>
    {
        // AnimatedSprite
        ["StardewValley.AnimatedSprite::currentAnimation"] = "CurrentAnimation",
        ["StardewValley.AnimatedSprite::currentFrame"] = "CurrentFrame",
        ["StardewValley.AnimatedSprite::sourceRect"] = "SourceRect",
        ["StardewValley.AnimatedSprite::spriteHeight"] = "SpriteHeight",
        ["StardewValley.AnimatedSprite::spriteWidth"] = "SpriteWidth",

        // Character
        ["StardewValley.Character::currentLocationRef"] = "currentLocation",
        ["StardewValley.Character::facingDirection"] = "FacingDirection",
        ["StardewValley.Character::name"] = "Name",
        ["StardewValley.Character::position"] = "Position",
        ["StardewValley.Character::scale"] = "Scale",
        ["StardewValley.Character::speed"] = "Speed",
        ["StardewValley.Character::sprite"] = "Sprite",

        // Chest
        ["StardewValley.Objects.Chest::tint"] = "Tint",

        // Farmer
        ["StardewValley.Farmer::houseUpgradeLevel"] = "HouseUpgradeLevel",
        ["StardewValley.Farmer::isMale"] = "IsMale",
        ["StardewValley.Farmer::items"] = "Items",
        ["StardewValley.Farmer::magneticRadius"] = "MagneticRadius",
        ["StardewValley.Farmer::stamina"] = "Stamina",
        ["StardewValley.Farmer::uniqueMultiplayerID"] = "UniqueMultiplayerID",
        ["StardewValley.Farmer::usingTool"] = "UsingTool",

        // Forest
        ["StardewValley.Locations.Forest::netTravelingMerchantDay"] = "travelingMerchantDay",
        ["StardewValley.Locations.Forest::netLog"] = "log",

        // FruitTree
        ["StardewValley.TerrainFeatures.FruitTree::greenHouseTileTree"] = "GreenHouseTileTree",
        ["StardewValley.TerrainFeatures.FruitTree::greenHouseTree"] = "GreenHouseTree",

        // GameLocation
        ["StardewValley.GameLocation::isFarm"] = "IsFarm",
        ["StardewValley.GameLocation::isOutdoors"] = "IsOutdoors",
        ["StardewValley.GameLocation::lightLevel"] = "LightLevel",
        ["StardewValley.GameLocation::name"] = "Name",

        // Item
        ["StardewValley.Item::category"] = "Category",
        ["StardewValley.Item::netName"] = "Name",
        ["StardewValley.Item::parentSheetIndex"] = "ParentSheetIndex",
        ["StardewValley.Item::specialVariable"] = "SpecialVariable",

        // Junimo
        ["StardewValley.Characters.Junimo::eventActor"] = "EventActor",

        // LightSource
        ["StardewValley.LightSource::identifier"] = "Identifier",

        // Monster
        ["StardewValley.Monsters.Monster::damageToFarmer"] = "DamageToFarmer",
        ["StardewValley.Monsters.Monster::experienceGained"] = "ExperienceGained",
        ["StardewValley.Monsters.Monster::health"] = "Health",
        ["StardewValley.Monsters.Monster::maxHealth"] = "MaxHealth",
        ["StardewValley.Monsters.Monster::netFocusedOnFarmers"] = "focusedOnFarmers",
        ["StardewValley.Monsters.Monster::netWildernessFarmMonster"] = "wildernessFarmMonster",
        ["StardewValley.Monsters.Monster::slipperiness"] = "Slipperiness",

        // NPC
        ["StardewValley.NPC::age"] = "Age",
        ["StardewValley.NPC::birthday_Day"] = "Birthday_Day",
        ["StardewValley.NPC::birthday_Season"] = "Birthday_Season",
        ["StardewValley.NPC::breather"] = "Breather",
        ["StardewValley.NPC::defaultMap"] = "DefaultMap",
        ["StardewValley.NPC::gender"] = "Gender",
        ["StardewValley.NPC::hideShadow"] = "HideShadow",
        ["StardewValley.NPC::isInvisible"] = "IsInvisible",
        ["StardewValley.NPC::isWalkingTowardPlayer"] = "IsWalkingTowardPlayer",
        ["StardewValley.NPC::manners"] = "Manners",
        ["StardewValley.NPC::optimism"] = "Optimism",
        ["StardewValley.NPC::socialAnxiety"] = "SocialAnxiety",

        // Object
        ["StardewValley.Object::canBeGrabbed"] = "CanBeGrabbed",
        ["StardewValley.Object::canBeSetDown"] = "CanBeSetDown",
        ["StardewValley.Object::edibility"] = "Edibility",
        ["StardewValley.Object::flipped"] = "Flipped",
        ["StardewValley.Object::fragility"] = "Fragility",
        ["StardewValley.Object::hasBeenPickedUpByFarmer"] = "HasBeenPickedUpByFarmer",
        ["StardewValley.Object::isHoedirt"] = "IsHoeDirt",
        ["StardewValley.Object::isOn"] = "IsOn",
        ["StardewValley.Object::isRecipe"] = "IsRecipe",
        ["StardewValley.Object::isSpawnedObject"] = "IsSpawnedObject",
        ["StardewValley.Object::minutesUntilReady"] = "MinutesUntilReady",
        ["StardewValley.Object::netName"] = "name",
        ["StardewValley.Object::price"] = "Price",
        ["StardewValley.Object::quality"] = "Quality",
        ["StardewValley.Object::stack"] = "Stack",
        ["StardewValley.Object::tileLocation"] = "TileLocation",
        ["StardewValley.Object::type"] = "Type",

        // Projectile
        ["StardewValley.Projectiles.Projectile::ignoreLocationCollision"] = "IgnoreLocationCollision",

        // Tool
        ["StardewValley.Tool::currentParentTileIndex"] = "CurrentParentTileIndex",
        ["StardewValley.Tool::indexOfMenuItemView"] = "IndexOfMenuItemView",
        ["StardewValley.Tool::initialParentTileIndex"] = "InitialParentTileIndex",
        ["StardewValley.Tool::instantUse"] = "InstantUse",
        ["StardewValley.Tool::netName"] = "BaseName",
        ["StardewValley.Tool::stackable"] = "Stackable",
        ["StardewValley.Tool::upgradeLevel"] = "UpgradeLevel"
    };

    /// <summary>The diagnostic info for an implicit net field cast.</summary>
    private readonly DiagnosticDescriptor AvoidImplicitNetFieldCastRule = new(
        id: "AvoidImplicitNetFieldCast",
        title: "Netcode types shouldn't be implicitly converted",
        messageFormat: "This implicitly converts '{0}' from {1} to {2}, but {1} has unintuitive implicit conversion rules. Consider comparing against the actual value instead to avoid bugs. See https://smapi.io/package/avoid-implicit-net-field-cast for details.",
        category: "SMAPI.CommonErrors",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://smapi.io/package/avoid-implicit-net-field-cast"
    );

    /// <summary>The diagnostic info for an avoidable net field access.</summary>
    private readonly DiagnosticDescriptor AvoidNetFieldRule = new(
        id: "AvoidNetField",
        title: "Avoid Netcode types when possible",
        messageFormat: "'{0}' is a {1} field; consider using the {2} property instead. See https://smapi.io/package/avoid-net-field for details.",
        category: "SMAPI.CommonErrors",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://smapi.io/package/avoid-net-field"
    );


    /*********
    ** Accessors
    *********/
    /// <summary>The descriptors for the diagnostics that this analyzer is capable of producing.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public NetFieldAnalyzer()
    {
        this.SupportedDiagnostics = ImmutableArray.CreateRange(new[] { this.AvoidNetFieldRule, this.AvoidImplicitNetFieldCastRule });
    }

    /// <summary>Called once at session start to register actions in the analysis context.</summary>
    /// <param name="context">The analysis context.</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(
            this.AnalyzeMemberAccess,
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxKind.ConditionalAccessExpression
        );
        context.RegisterSyntaxNodeAction(
            this.AnalyzeCast,
            SyntaxKind.CastExpression,
            SyntaxKind.AsExpression
        );
        context.RegisterSyntaxNodeAction(
            this.AnalyzeBinaryComparison,
            SyntaxKind.EqualsExpression,
            SyntaxKind.NotEqualsExpression,
            SyntaxKind.GreaterThanExpression,
            SyntaxKind.GreaterThanOrEqualExpression,
            SyntaxKind.LessThanExpression,
            SyntaxKind.LessThanOrEqualExpression
        );
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Analyze a member access syntax node and add a diagnostic message if applicable.</summary>
    /// <param name="context">The analysis context.</param>
    /// <returns>Returns whether any warnings were added.</returns>
    private void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        this.HandleErrors(context.Node, () =>
        {
            // get member access info
            if (!AnalyzerUtilities.TryGetMemberInfo(context.Node, context.SemanticModel, out ITypeSymbol declaringType, out TypeInfo memberType, out string memberName))
                return;
            if (!this.IsNetType(memberType.Type))
                return;

            // warn: use property wrapper if available
            foreach (ITypeSymbol type in AnalyzerUtilities.GetConcreteTypes(declaringType))
            {
                if (this.NetFieldWrapperProperties.TryGetValue($"{type}::{memberName}", out string suggestedPropertyName))
                {
                    context.ReportDiagnostic(Diagnostic.Create(this.AvoidNetFieldRule, context.Node.GetLocation(), context.Node, memberType.Type.Name, suggestedPropertyName));
                    return;
                }
            }

            // warn: implicit conversion
            if (this.IsInvalidConversion(memberType.Type, memberType.ConvertedType))
                context.ReportDiagnostic(Diagnostic.Create(this.AvoidImplicitNetFieldCastRule, context.Node.GetLocation(), context.Node, memberType.Type.Name, memberType.ConvertedType));
        });
    }

    /// <summary>Analyze an explicit cast or 'x as y' node and add a diagnostic message if applicable.</summary>
    /// <param name="context">The analysis context.</param>
    /// <returns>Returns whether any warnings were added.</returns>
    private void AnalyzeCast(SyntaxNodeAnalysisContext context)
    {
        // NOTE: implicit conversion within the expression is detected by the member access
        // checks. This method is only concerned with the conversion of its final value.
        this.HandleErrors(context.Node, () =>
        {
            if (AnalyzerUtilities.TryGetCastOrAsInfo(context.Node, context.SemanticModel, out ExpressionSyntax fromExpression, out TypeInfo fromType, out TypeInfo toType))
            {
                if (this.IsInvalidConversion(fromType.ConvertedType, toType.Type))
                    context.ReportDiagnostic(Diagnostic.Create(this.AvoidImplicitNetFieldCastRule, context.Node.GetLocation(), fromExpression, fromType.ConvertedType.Name, toType.Type));
            }
        });
    }

    /// <summary>Analyze a binary comparison syntax node and add a diagnostic message if applicable.</summary>
    /// <param name="context">The analysis context.</param>
    /// <returns>Returns whether any warnings were added.</returns>
    private void AnalyzeBinaryComparison(SyntaxNodeAnalysisContext context)
    {
        // NOTE: implicit conversion within an operand is detected by the member access checks.
        // This method is only concerned with the conversion of each side's final value.
        this.HandleErrors(context.Node, () =>
        {
            BinaryExpressionSyntax expression = (BinaryExpressionSyntax)context.Node;
            foreach (var pair in new[] { Tuple.Create(expression.Left, expression.Right), Tuple.Create(expression.Right, expression.Left) })
            {
                // get node info
                ExpressionSyntax curExpression = pair.Item1; // the side of the comparison being examined
                ExpressionSyntax otherExpression = pair.Item2; // the other side
                TypeInfo curType = context.SemanticModel.GetTypeInfo(curExpression);
                TypeInfo otherType = context.SemanticModel.GetTypeInfo(otherExpression);
                if (!this.IsNetType(curType.ConvertedType))
                    continue;

                // warn for comparison to null
                // An expression like `building.indoors != null` will sometimes convert `building.indoors` to NetFieldBase instead of object before comparison. Haven't reproduced this in unit tests yet.
                Optional<object> otherValue = context.SemanticModel.GetConstantValue(otherExpression);
                if (otherValue.HasValue && otherValue.Value == null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(this.AvoidImplicitNetFieldCastRule, context.Node.GetLocation(), curExpression, curType.Type.Name, "null"));
                    break;
                }

                // warn for implicit conversion
                if (!this.IsNetType(otherType.ConvertedType))
                {
                    context.ReportDiagnostic(Diagnostic.Create(this.AvoidImplicitNetFieldCastRule, context.Node.GetLocation(), curExpression, curType.Type.Name, curType.ConvertedType));
                    break;
                }
            }
        });
    }

    /// <summary>Handle exceptions raised while analyzing a node.</summary>
    /// <param name="node">The node being analyzed.</param>
    /// <param name="action">The callback to invoke.</param>
    private void HandleErrors(SyntaxNode node, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed processing expression: '{node}'. Exception details: {ex.ToString().Replace('\r', ' ').Replace('\n', ' ')}");
        }
    }

    /// <summary>Get whether a net field was converted in an error-prone way.</summary>
    /// <param name="fromType">The source type.</param>
    /// <param name="toType">The target type.</param>
    private bool IsInvalidConversion(ITypeSymbol fromType, ITypeSymbol toType)
    {
        // no conversion
        if (!this.IsNetType(fromType) || this.IsNetType(toType))
            return false;

        // conversion to implemented interface is OK
        if (fromType.AllInterfaces.Contains(toType, SymbolEqualityComparer.Default))
            return false;

        // avoid any other conversions
        return true;
    }

    /// <summary>Get whether a type symbol references a <c>Netcode</c> type.</summary>
    /// <param name="typeSymbol">The type symbol.</param>
    private bool IsNetType(ITypeSymbol typeSymbol)
    {
        return typeSymbol?.ContainingNamespace?.Name == NetFieldAnalyzer.NetcodeNamespace;
    }
}
