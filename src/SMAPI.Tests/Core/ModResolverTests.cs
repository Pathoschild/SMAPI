using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Framework.ModLoading;
using StardewModdingAPI.Toolkit;
using StardewModdingAPI.Toolkit.Framework.ModData;
using StardewModdingAPI.Toolkit.Framework.UpdateData;
using StardewModdingAPI.Toolkit.Serialization.Models;
using StardewModdingAPI.Toolkit.Utilities.PathLookups;
using SemanticVersion = StardewModdingAPI.SemanticVersion;

namespace SMAPI.Tests.Core;

/// <summary>Unit tests for <see cref="ModResolver"/>.</summary>
[TestFixture]
public class ModResolverTests
{
    /*********
    ** Unit tests
    *********/
    /****
    ** ReadManifests
    ****/
    [Test(Description = "Assert that the resolver correctly returns an empty list if there are no mods installed.")]
    public void ReadBasicManifest_NoMods_ReturnsEmptyList()
    {
        // arrange
        string rootFolder = this.GetTempFolderPath();
        Directory.CreateDirectory(rootFolder);

        // act
        IModMetadata[] mods = new ModResolver().ReadManifests(new ModToolkit(), rootFolder, new ModDatabase(), useCaseInsensitiveFilePaths: true).ToArray();

        // assert
        Assert.AreEqual(0, mods.Length, 0, $"Expected to find zero manifests, found {mods.Length} instead.");

        // cleanup
        Directory.Delete(rootFolder, recursive: true);
    }

    [Test(Description = "Assert that the resolver correctly returns a failed metadata if there's an empty mod folder.")]
    public void ReadBasicManifest_EmptyModFolder_ReturnsFailedManifest()
    {
        // arrange
        string rootFolder = this.GetTempFolderPath();
        string modFolder = Path.Combine(rootFolder, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(modFolder);

        // act
        IModMetadata[] mods = new ModResolver().ReadManifests(new ModToolkit(), rootFolder, new ModDatabase(), useCaseInsensitiveFilePaths: true).ToArray();
        IModMetadata? mod = mods.FirstOrDefault();

        // assert
        Assert.AreEqual(1, mods.Length, 0, $"Expected to find one manifest, found {mods.Length} instead.");
        Assert.AreEqual(ModMetadataStatus.Failed, mod!.Status, "The mod metadata was not marked failed.");
        Assert.IsNotNull(mod.Error, "The mod metadata did not have an error message set.");

        // cleanup
        Directory.Delete(rootFolder, recursive: true);
    }

    [Test(Description = "Assert that the resolver correctly reads manifest data from a randomized file.")]
    public void ReadBasicManifest_CanReadFile()
    {
        // create manifest data
        IDictionary<string, object> originalDependency = new Dictionary<string, object>
        {
            [nameof(IManifestDependency.UniqueID)] = Sample.String()
        };
        IDictionary<string, object> original = new Dictionary<string, object>
        {
            [nameof(IManifest.Name)] = Sample.String(),
            [nameof(IManifest.Author)] = Sample.String(),
            [nameof(IManifest.Version)] = new SemanticVersion(Sample.Int(), Sample.Int(), Sample.Int(), Sample.String()),
            [nameof(IManifest.Description)] = Sample.String(),
            [nameof(IManifest.UniqueID)] = $"{Sample.String()}.{Sample.String()}",
            [nameof(IManifest.EntryDll)] = $"{Sample.String()}.dll",
            [nameof(IManifest.MinimumApiVersion)] = $"{Sample.Int()}.{Sample.Int()}.{Sample.Int()}-{Sample.String()}",
            [nameof(IManifest.Dependencies)] = new[] { originalDependency },
            ["ExtraString"] = Sample.String(),
            ["ExtraInt"] = Sample.Int()
        };

        // write to filesystem
        string rootFolder = this.GetTempFolderPath();
        string modFolder = Path.Combine(rootFolder, Guid.NewGuid().ToString("N"));
        string filename = Path.Combine(modFolder, "manifest.json");
        Directory.CreateDirectory(modFolder);
        File.WriteAllText(filename, JsonConvert.SerializeObject(original));

        // act
        IModMetadata[] mods = new ModResolver().ReadManifests(new ModToolkit(), rootFolder, new ModDatabase(), useCaseInsensitiveFilePaths: true).ToArray();
        IModMetadata? mod = mods.FirstOrDefault();

        // assert
        Assert.AreEqual(1, mods.Length, 0, "Expected to find one manifest.");
        Assert.IsNotNull(mod, "The loaded manifest shouldn't be null.");
        Assert.AreEqual(null, mod!.DataRecord, "The data record should be null since we didn't provide one.");
        Assert.AreEqual(modFolder, mod.DirectoryPath, "The directory path doesn't match.");
        Assert.AreEqual(null, mod.Error, "The error should be null since parsing should have succeeded.");
        Assert.AreEqual(ModMetadataStatus.Found, mod.Status, "The status doesn't match.");

        Assert.AreEqual(original[nameof(IManifest.Name)], mod.DisplayName, "The display name should use the manifest name.");
        Assert.AreEqual(original[nameof(IManifest.Name)], mod.Manifest.Name, "The manifest's name doesn't match.");
        Assert.AreEqual(original[nameof(IManifest.Author)], mod.Manifest.Author, "The manifest's author doesn't match.");
        Assert.AreEqual(original[nameof(IManifest.Description)], mod.Manifest.Description, "The manifest's description doesn't match.");
        Assert.AreEqual(original[nameof(IManifest.EntryDll)], mod.Manifest.EntryDll, "The manifest's entry DLL doesn't match.");
        Assert.AreEqual(original[nameof(IManifest.MinimumApiVersion)], mod.Manifest.MinimumApiVersion?.ToString(), "The manifest's minimum API version doesn't match.");
        Assert.AreEqual(original[nameof(IManifest.Version)]?.ToString(), mod.Manifest.Version?.ToString(), "The manifest's version doesn't match.");

        Assert.IsNotNull(mod.Manifest.ExtraFields, "The extra fields should not be null.");
        Assert.AreEqual(2, mod.Manifest.ExtraFields.Count, "The extra fields should contain two values.");
        Assert.AreEqual(original["ExtraString"], mod.Manifest.ExtraFields["ExtraString"], "The manifest's extra fields should contain an 'ExtraString' value.");
        Assert.AreEqual(original["ExtraInt"], mod.Manifest.ExtraFields["ExtraInt"], "The manifest's extra fields should contain an 'ExtraInt' value.");

        Assert.IsNotNull(mod.Manifest.Dependencies, "The dependencies field should not be null.");
        Assert.AreEqual(1, mod.Manifest.Dependencies.Length, "The dependencies field should contain one value.");
        Assert.AreEqual(originalDependency[nameof(IManifestDependency.UniqueID)], mod.Manifest.Dependencies[0].UniqueID, "The first dependency's unique ID doesn't match.");

        // cleanup
        Directory.Delete(rootFolder, recursive: true);
    }

    /****
    ** ValidateManifests
    ****/
    [Test(Description = "Assert that validation doesn't fail if there are no mods installed.")]
    public void ValidateManifests_NoMods_DoesNothing()
    {
        new ModResolver().ValidateManifests(Array.Empty<ModMetadata>(), apiVersion: new SemanticVersion("1.0"), getUpdateUrl: _ => null, getFileLookup: this.GetFileLookup, validateFilesExist: false);
    }

    [Test(Description = "Assert that validation skips manifests that have already failed without calling any other properties.")]
    public void ValidateManifests_Skips_Failed()
    {
        // arrange
        Mock<IModMetadata> mock = this.GetMetadata("Mod A");
        mock.Setup(p => p.Status).Returns(ModMetadataStatus.Failed);

        // act
        new ModResolver().ValidateManifests(new[] { mock.Object }, apiVersion: new SemanticVersion("1.0"), getUpdateUrl: _ => null, getFileLookup: this.GetFileLookup, validateFilesExist: false);

        // assert
        mock.VerifyGet(p => p.Status, Times.Once, "The validation did not check the manifest status.");
    }

    [Test(Description = "Assert that validation fails if the mod has 'assume broken' status.")]
    public void ValidateManifests_ModStatus_AssumeBroken_Fails()
    {
        // arrange
        Mock<IModMetadata> mock = this.GetMetadata("Mod A", Array.Empty<string>(), allowStatusChange: true);
        mock.Setup(p => p.DataRecord).Returns(() => new ModDataRecordVersionedFields(this.GetModDataRecord())
        {
            Status = ModStatus.AssumeBroken
        });

        // act
        new ModResolver().ValidateManifests(new[] { mock.Object }, apiVersion: new SemanticVersion("1.0"), getUpdateUrl: _ => null, getFileLookup: this.GetFileLookup, validateFilesExist: false);

        // assert
        mock.Verify(p => p.SetStatus(ModMetadataStatus.Failed, It.IsAny<ModFailReason>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once, "The validation did not fail the metadata.");
    }

    [Test(Description = "Assert that validation fails when the minimum API version is higher than the current SMAPI version.")]
    public void ValidateManifests_MinimumApiVersion_Fails()
    {
        // arrange
        Mock<IModMetadata> mock = this.GetMetadata("Mod A", Array.Empty<string>(), allowStatusChange: true);
        mock.Setup(p => p.Manifest).Returns(this.GetManifest(minimumApiVersion: "1.1"));

        // act
        new ModResolver().ValidateManifests(new[] { mock.Object }, apiVersion: new SemanticVersion("1.0"), getUpdateUrl: _ => null, getFileLookup: this.GetFileLookup, validateFilesExist: false);

        // assert
        mock.Verify(p => p.SetStatus(ModMetadataStatus.Failed, It.IsAny<ModFailReason>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once, "The validation did not fail the metadata.");
    }

    [Test(Description = "Assert that validation fails when the manifest references a DLL that does not exist.")]
    public void ValidateManifests_MissingEntryDLL_Fails()
    {
        // arrange
        string directoryPath = this.GetTempFolderPath();
        Mock<IModMetadata> mock = this.GetMetadata(this.GetManifest(id: "Mod A", version: "1.0", entryDll: "Missing.dll"), allowStatusChange: true, directoryPath: directoryPath);
        Directory.CreateDirectory(directoryPath);

        // act
        new ModResolver().ValidateManifests(new[] { mock.Object }, apiVersion: new SemanticVersion("1.0"), getUpdateUrl: _ => null, getFileLookup: this.GetFileLookup);

        // assert
        mock.Verify(p => p.SetStatus(ModMetadataStatus.Failed, It.IsAny<ModFailReason>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once, "The validation did not fail the metadata.");

        // cleanup
        Directory.Delete(directoryPath);
    }

    [Test(Description = "Assert that validation fails when multiple mods have the same unique ID.")]
    public void ValidateManifests_DuplicateUniqueID_Fails()
    {
        // arrange
        Mock<IModMetadata> modA = this.GetMetadata("Mod A", Array.Empty<string>(), allowStatusChange: true);
        Mock<IModMetadata> modB = this.GetMetadata(this.GetManifest(id: "Mod A", name: "Mod B", version: "1.0"), allowStatusChange: true);

        // act
        new ModResolver().ValidateManifests(new[] { modA.Object, modB.Object }, apiVersion: new SemanticVersion("1.0"), getUpdateUrl: _ => null, getFileLookup: this.GetFileLookup, validateFilesExist: false);

        // assert
        modA.Verify(p => p.SetStatus(ModMetadataStatus.Failed, ModFailReason.Duplicate, It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce, "The validation did not fail the first mod with a unique ID.");
        modB.Verify(p => p.SetStatus(ModMetadataStatus.Failed, ModFailReason.Duplicate, It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce, "The validation did not fail the second mod with a unique ID.");
    }

    [Test(Description = "Assert that validation fails when the manifest references a DLL that does not exist.")]
    public void ValidateManifests_Valid_Passes()
    {
        // set up manifest
        IManifest manifest = this.GetManifest();

        // create DLL
        string modFolder = Path.Combine(this.GetTempFolderPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(modFolder);
        File.WriteAllText(Path.Combine(modFolder, manifest.EntryDll!), "");

        // arrange
        Mock<IModMetadata> mock = new(MockBehavior.Strict);
        mock.Setup(p => p.Status).Returns(ModMetadataStatus.Found);
        mock.Setup(p => p.DataRecord).Returns(this.GetModDataRecordVersionedFields());
        mock.Setup(p => p.Manifest).Returns(manifest);
        mock.Setup(p => p.DirectoryPath).Returns(modFolder);

        // act
        new ModResolver().ValidateManifests(new[] { mock.Object }, apiVersion: new SemanticVersion("1.0"), getUpdateUrl: _ => null, getFileLookup: this.GetFileLookup);

        // assert
        // if Moq doesn't throw a method-not-setup exception, the validation didn't override the status.

        // cleanup
        Directory.Delete(modFolder, recursive: true);
    }

    /****
    ** ProcessDependencies
    ****/
    [Test(Description = "Assert that processing dependencies doesn't fail if there are no mods installed.")]
    public void ProcessDependencies_NoMods_DoesNothing()
    {
        // act
        IModMetadata[] mods = new ModResolver().ProcessDependencies(Array.Empty<IModMetadata>(), new ModDatabase()).ToArray();

        // assert
        Assert.AreEqual(0, mods.Length, 0, "Expected to get an empty list of mods.");
    }

    [Test(Description = "Assert that processing dependencies doesn't change the order if there are no mod dependencies.")]
    public void ProcessDependencies_NoDependencies_DoesNothing()
    {
        // arrange
        // A B C
        Mock<IModMetadata> modA = this.GetMetadata("Mod A");
        Mock<IModMetadata> modB = this.GetMetadata("Mod B");
        Mock<IModMetadata> modC = this.GetMetadata("Mod C");

        // act
        IModMetadata[] mods = new ModResolver().ProcessDependencies(new[] { modA.Object, modB.Object, modC.Object }, new ModDatabase()).ToArray();

        // assert
        Assert.AreEqual(3, mods.Length, 0, "Expected to get the same number of mods input.");
        Assert.AreSame(modA.Object, mods[0], "The load order unexpectedly changed with no dependencies.");
        Assert.AreSame(modB.Object, mods[1], "The load order unexpectedly changed with no dependencies.");
        Assert.AreSame(modC.Object, mods[2], "The load order unexpectedly changed with no dependencies.");
    }

    [Test(Description = "Assert that processing dependencies skips mods that have already failed without calling any other properties.")]
    public void ProcessDependencies_Skips_Failed()
    {
        // arrange
        Mock<IModMetadata> mock = new(MockBehavior.Strict);
        mock.Setup(p => p.Status).Returns(ModMetadataStatus.Failed);

        // act
        new ModResolver().ProcessDependencies(new[] { mock.Object }, new ModDatabase());

        // assert
        mock.VerifyGet(p => p.Status, Times.Once, "The validation did not check the manifest status.");
    }

    [Test(Description = "Assert that simple dependencies are reordered correctly.")]
    public void ProcessDependencies_Reorders_SimpleDependencies()
    {
        // arrange
        // A ◀── B
        // ▲     ▲
        // │     │
        // └─ C ─┘
        Mock<IModMetadata> modA = this.GetMetadata("Mod A");
        Mock<IModMetadata> modB = this.GetMetadata("Mod B", dependencies: new[] { "Mod A" });
        Mock<IModMetadata> modC = this.GetMetadata("Mod C", dependencies: new[] { "Mod A", "Mod B" });

        // act
        IModMetadata[] mods = new ModResolver().ProcessDependencies(new[] { modC.Object, modA.Object, modB.Object }, new ModDatabase()).ToArray();

        // assert
        Assert.AreEqual(3, mods.Length, 0, "Expected to get the same number of mods input.");
        Assert.AreSame(modA.Object, mods[0], "The load order is incorrect: mod A should be first since the other mods depend on it.");
        Assert.AreSame(modB.Object, mods[1], "The load order is incorrect: mod B should be second since it needs mod A, and is needed by mod C.");
        Assert.AreSame(modC.Object, mods[2], "The load order is incorrect: mod C should be third since it needs both mod A and mod B.");
    }

    [Test(Description = "Assert that simple dependency chains are reordered correctly.")]
    public void ProcessDependencies_Reorders_DependencyChain()
    {
        // arrange
        // A ◀── B ◀── C ◀── D
        Mock<IModMetadata> modA = this.GetMetadata("Mod A");
        Mock<IModMetadata> modB = this.GetMetadata("Mod B", dependencies: new[] { "Mod A" });
        Mock<IModMetadata> modC = this.GetMetadata("Mod C", dependencies: new[] { "Mod B" });
        Mock<IModMetadata> modD = this.GetMetadata("Mod D", dependencies: new[] { "Mod C" });

        // act
        IModMetadata[] mods = new ModResolver().ProcessDependencies(new[] { modC.Object, modA.Object, modB.Object, modD.Object }, new ModDatabase()).ToArray();

        // assert
        Assert.AreEqual(4, mods.Length, 0, "Expected to get the same number of mods input.");
        Assert.AreSame(modA.Object, mods[0], "The load order is incorrect: mod A should be first since it's needed by mod B.");
        Assert.AreSame(modB.Object, mods[1], "The load order is incorrect: mod B should be second since it needs mod A, and is needed by mod C.");
        Assert.AreSame(modC.Object, mods[2], "The load order is incorrect: mod C should be third since it needs mod B, and is needed by mod D.");
        Assert.AreSame(modD.Object, mods[3], "The load order is incorrect: mod D should be fourth since it needs mod C.");
    }

    [Test(Description = "Assert that overlapping dependency chains are reordered correctly.")]
    public void ProcessDependencies_Reorders_OverlappingDependencyChain()
    {
        // arrange
        // A ◀── B ◀── C ◀── D
        //       ▲     ▲
        //       │     │
        //       E ◀── F
        Mock<IModMetadata> modA = this.GetMetadata("Mod A");
        Mock<IModMetadata> modB = this.GetMetadata("Mod B", dependencies: new[] { "Mod A" });
        Mock<IModMetadata> modC = this.GetMetadata("Mod C", dependencies: new[] { "Mod B" });
        Mock<IModMetadata> modD = this.GetMetadata("Mod D", dependencies: new[] { "Mod C" });
        Mock<IModMetadata> modE = this.GetMetadata("Mod E", dependencies: new[] { "Mod B" });
        Mock<IModMetadata> modF = this.GetMetadata("Mod F", dependencies: new[] { "Mod C", "Mod E" });

        // act
        IModMetadata[] mods = new ModResolver().ProcessDependencies(new[] { modC.Object, modA.Object, modB.Object, modD.Object, modF.Object, modE.Object }, new ModDatabase()).ToArray();

        // assert
        Assert.AreEqual(6, mods.Length, 0, "Expected to get the same number of mods input.");
        Assert.AreSame(modA.Object, mods[0], "The load order is incorrect: mod A should be first since it's needed by mod B.");
        Assert.AreSame(modB.Object, mods[1], "The load order is incorrect: mod B should be second since it needs mod A, and is needed by mod C.");
        Assert.AreSame(modC.Object, mods[2], "The load order is incorrect: mod C should be third since it needs mod B, and is needed by mod D.");
        Assert.AreSame(modD.Object, mods[3], "The load order is incorrect: mod D should be fourth since it needs mod C.");
        Assert.AreSame(modE.Object, mods[4], "The load order is incorrect: mod E should be fifth since it needs mod B, but is specified after C which also needs mod B.");
        Assert.AreSame(modF.Object, mods[5], "The load order is incorrect: mod F should be last since it needs mods E and C.");
    }

    [Test(Description = "Assert that mods with circular dependency chains are skipped, but any other mods are loaded in the correct order.")]
    public void ProcessDependencies_Skips_CircularDependentMods()
    {
        // arrange
        // A ◀── B ◀── C ──▶ D
        //             ▲     │
        //             │     ▼
        //             └──── E
        Mock<IModMetadata> modA = this.GetMetadata("Mod A");
        Mock<IModMetadata> modB = this.GetMetadata("Mod B", dependencies: new[] { "Mod A" });
        Mock<IModMetadata> modC = this.GetMetadata("Mod C", dependencies: new[] { "Mod B", "Mod D" }, allowStatusChange: true);
        Mock<IModMetadata> modD = this.GetMetadata("Mod D", dependencies: new[] { "Mod E" }, allowStatusChange: true);
        Mock<IModMetadata> modE = this.GetMetadata("Mod E", dependencies: new[] { "Mod C" }, allowStatusChange: true);

        // act
        IModMetadata[] mods = new ModResolver().ProcessDependencies(new[] { modC.Object, modA.Object, modB.Object, modD.Object, modE.Object }, new ModDatabase()).ToArray();

        // assert
        Assert.AreEqual(5, mods.Length, 0, "Expected to get the same number of mods input.");
        Assert.AreSame(modA.Object, mods[0], "The load order is incorrect: mod A should be first since it's needed by mod B.");
        Assert.AreSame(modB.Object, mods[1], "The load order is incorrect: mod B should be second since it needs mod A.");
        modC.Verify(p => p.SetStatus(ModMetadataStatus.Failed, It.IsAny<ModFailReason>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once, "Mod C was expected to fail since it's part of a dependency loop.");
        modD.Verify(p => p.SetStatus(ModMetadataStatus.Failed, It.IsAny<ModFailReason>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once, "Mod D was expected to fail since it's part of a dependency loop.");
        modE.Verify(p => p.SetStatus(ModMetadataStatus.Failed, It.IsAny<ModFailReason>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once, "Mod E was expected to fail since it's part of a dependency loop.");
    }

    [Test(Description = "Assert that dependencies are sorted correctly even if some of the mods failed during metadata loading.")]
    public void ProcessDependencies_WithSomeFailedMods_Succeeds()
    {
        // arrange
        // A ◀── B ◀── C   D (failed)
        Mock<IModMetadata> modA = this.GetMetadata("Mod A");
        Mock<IModMetadata> modB = this.GetMetadata("Mod B", dependencies: new[] { "Mod A" });
        Mock<IModMetadata> modC = this.GetMetadata("Mod C", dependencies: new[] { "Mod B" }, allowStatusChange: true);
        Mock<IModMetadata> modD = new(MockBehavior.Strict);
        modD.Setup(p => p.Manifest).Returns<IManifest>(null);
        modD.Setup(p => p.Status).Returns(ModMetadataStatus.Failed);

        // act
        IModMetadata[] mods = new ModResolver().ProcessDependencies(new[] { modC.Object, modA.Object, modB.Object, modD.Object }, new ModDatabase()).ToArray();

        // assert
        Assert.AreEqual(4, mods.Length, 0, "Expected to get the same number of mods input.");
        Assert.AreSame(modD.Object, mods[0], "The load order is incorrect: mod D should be first since it was already failed.");
        Assert.AreSame(modA.Object, mods[1], "The load order is incorrect: mod A should be second since it's needed by mod B.");
        Assert.AreSame(modB.Object, mods[2], "The load order is incorrect: mod B should be third since it needs mod A, and is needed by mod C.");
        Assert.AreSame(modC.Object, mods[3], "The load order is incorrect: mod C should be fourth since it needs mod B, and is needed by mod D.");
    }

    [Test(Description = "Assert that dependencies are failed if they don't meet the minimum version.")]
    public void ProcessDependencies_WithMinVersions_FailsIfNotMet()
    {
        // arrange
        // A 1.0 ◀── B (need A 1.1)
        Mock<IModMetadata> modA = this.GetMetadata(this.GetManifest(id: "Mod A", version: "1.0"));
        Mock<IModMetadata> modB = this.GetMetadata(this.GetManifest(id: "Mod B", version: "1.0", dependencies: new IManifestDependency[] { new ManifestDependency("Mod A", "1.1") }), allowStatusChange: true);

        // act
        IModMetadata[] mods = new ModResolver().ProcessDependencies(new[] { modA.Object, modB.Object }, new ModDatabase()).ToArray();

        // assert
        Assert.AreEqual(2, mods.Length, 0, "Expected to get the same number of mods input.");
        modB.Verify(p => p.SetStatus(ModMetadataStatus.Failed, It.IsAny<ModFailReason>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once, "Mod B unexpectedly didn't fail even though it needs a newer version of Mod A.");
    }

    [Test(Description = "Assert that dependencies are accepted if they meet the minimum version.")]
    public void ProcessDependencies_WithMinVersions_SucceedsIfMet()
    {
        // arrange
        // A 1.0 ◀── B (need A 1.0-beta)
        Mock<IModMetadata> modA = this.GetMetadata(this.GetManifest(id: "Mod A", version: "1.0"));
        Mock<IModMetadata> modB = this.GetMetadata(this.GetManifest(id: "Mod B", version: "1.0", dependencies: new IManifestDependency[] { new ManifestDependency("Mod A", "1.0-beta") }), allowStatusChange: false);

        // act
        IModMetadata[] mods = new ModResolver().ProcessDependencies(new[] { modA.Object, modB.Object }, new ModDatabase()).ToArray();

        // assert
        Assert.AreEqual(2, mods.Length, 0, "Expected to get the same number of mods input.");
        Assert.AreSame(modA.Object, mods[0], "The load order is incorrect: mod A should be first since it's needed by mod B.");
        Assert.AreSame(modB.Object, mods[1], "The load order is incorrect: mod B should be second since it needs mod A.");
    }

    [Test(Description = "Assert that optional dependencies are sorted correctly if present.")]
    public void ProcessDependencies_IfOptional()
    {
        // arrange
        // A ◀── B
        Mock<IModMetadata> modA = this.GetMetadata(this.GetManifest(id: "Mod A", version: "1.0"));
        Mock<IModMetadata> modB = this.GetMetadata(this.GetManifest(id: "Mod B", version: "1.0", dependencies: new IManifestDependency[] { new ManifestDependency("Mod A", "1.0", required: false) }), allowStatusChange: false);

        // act
        IModMetadata[] mods = new ModResolver().ProcessDependencies(new[] { modB.Object, modA.Object }, new ModDatabase()).ToArray();

        // assert
        Assert.AreEqual(2, mods.Length, 0, "Expected to get the same number of mods input.");
        Assert.AreSame(modA.Object, mods[0], "The load order is incorrect: mod A should be first since it's needed by mod B.");
        Assert.AreSame(modB.Object, mods[1], "The load order is incorrect: mod B should be second since it needs mod A.");
    }

    [Test(Description = "Assert that optional dependencies are accepted if they're missing.")]
    public void ProcessDependencies_IfOptional_SucceedsIfMissing()
    {
        // arrange
        // A ◀── B where A doesn't exist
        Mock<IModMetadata> modB = this.GetMetadata(this.GetManifest(id: "Mod B", version: "1.0", dependencies: new IManifestDependency[] { new ManifestDependency("Mod A", "1.0", required: false) }), allowStatusChange: false);

        // act
        IModMetadata[] mods = new ModResolver().ProcessDependencies(new[] { modB.Object }, new ModDatabase()).ToArray();

        // assert
        Assert.AreEqual(1, mods.Length, 0, "Expected to get the same number of mods input.");
        Assert.AreSame(modB.Object, mods[0], "The load order is incorrect: mod B should be first since it's the only mod.");
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get a generated folder path in the temp folder. This folder isn't created automatically.</summary>
    private string GetTempFolderPath()
    {
        return Path.Combine(Path.GetTempPath(), "smapi-unit-tests", Guid.NewGuid().ToString("N"));
    }

    /// <summary>Get a file lookup for a given directory.</summary>
    /// <param name="rootDirectory">The full path to the directory.</param>
    private IFileLookup GetFileLookup(string rootDirectory)
    {
        return MinimalFileLookup.GetCachedFor(rootDirectory);
    }

    /// <summary>Get a randomized basic manifest.</summary>
    /// <param name="id">The <see cref="IManifest.UniqueID"/> value, or <c>null</c> for a generated value.</param>
    /// <param name="name">The <see cref="IManifest.Name"/> value, or <c>null</c> for a generated value.</param>
    /// <param name="version">The <see cref="IManifest.Version"/> value, or <c>null</c> for a generated value.</param>
    /// <param name="entryDll">The <see cref="IManifest.EntryDll"/> value, or <c>null</c> for a generated value.</param>
    /// <param name="contentPackForID">The <see cref="IManifest.ContentPackFor"/> value.</param>
    /// <param name="minimumApiVersion">The <see cref="IManifest.MinimumApiVersion"/> value.</param>
    /// <param name="dependencies">The <see cref="IManifest.Dependencies"/> value.</param>
    private Manifest GetManifest(string? id = null, string? name = null, string? version = null, string? entryDll = null, string? contentPackForID = null, string? minimumApiVersion = null, IManifestDependency[]? dependencies = null)
    {
        return new Manifest(
            uniqueId: id ?? $"{Sample.String()}.{Sample.String()}",
            name: name ?? id ?? Sample.String(),
            author: Sample.String(),
            description: Sample.String(),
            version: version != null ? new SemanticVersion(version) : new SemanticVersion(Sample.Int(), Sample.Int(), Sample.Int(), Sample.String()),
            entryDll: entryDll ?? $"{Sample.String()}.dll",
            contentPackFor: contentPackForID != null ? new ManifestContentPackFor(contentPackForID, null) : null,
            minimumApiVersion: minimumApiVersion != null ? new SemanticVersion(minimumApiVersion) : null,
            dependencies: dependencies ?? Array.Empty<IManifestDependency>(),
            updateKeys: Array.Empty<string>()
        );
    }

    /// <summary>Get a randomized basic manifest.</summary>
    /// <param name="uniqueID">The mod's name and unique ID.</param>
    private Mock<IModMetadata> GetMetadata(string uniqueID)
    {
        return this.GetMetadata(this.GetManifest(uniqueID, "1.0"));
    }

    /// <summary>Get a randomized basic manifest.</summary>
    /// <param name="uniqueID">The mod's name and unique ID.</param>
    /// <param name="dependencies">The dependencies this mod requires.</param>
    /// <param name="allowStatusChange">Whether the code being tested is allowed to change the mod status.</param>
    private Mock<IModMetadata> GetMetadata(string uniqueID, string[] dependencies, bool allowStatusChange = false)
    {
        IManifest manifest = this.GetManifest(id: uniqueID, version: "1.0", dependencies: dependencies.Select(dependencyID => (IManifestDependency)new ManifestDependency(dependencyID, null as ISemanticVersion)).ToArray());
        return this.GetMetadata(manifest, allowStatusChange);
    }

    /// <summary>Get a randomized basic manifest.</summary>
    /// <param name="manifest">The mod manifest.</param>
    /// <param name="allowStatusChange">Whether the code being tested is allowed to change the mod status.</param>
    /// <param name="directoryPath">The directory path the mod metadata should be pointed at, or <c>null</c> to generate a fake path.</param>
    private Mock<IModMetadata> GetMetadata(IManifest manifest, bool allowStatusChange = false, string? directoryPath = null)
    {
        directoryPath ??= this.GetTempFolderPath();

        Mock<IModMetadata> mod = new(MockBehavior.Strict);
        mod.Setup(p => p.DataRecord).Returns(this.GetModDataRecordVersionedFields());
        mod.Setup(p => p.Status).Returns(ModMetadataStatus.Found);
        mod.Setup(p => p.DisplayName).Returns(manifest.UniqueID);
        mod.Setup(p => p.DirectoryPath).Returns(directoryPath);
        mod.Setup(p => p.Manifest).Returns(manifest);
        mod.Setup(p => p.HasID(It.IsAny<string>())).Returns((string id) => manifest.UniqueID == id);
        mod.Setup(p => p.GetUpdateKeys(It.IsAny<bool>())).Returns(Enumerable.Empty<UpdateKey>());
        mod.Setup(p => p.GetRelativePathWithRoot()).Returns(directoryPath);
        if (allowStatusChange)
        {
            mod
                .Setup(p => p.SetStatus(It.IsAny<ModMetadataStatus>(), It.IsAny<ModFailReason>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<ModMetadataStatus, ModFailReason, string, string>((status, failReason, message, errorDetails) => Console.WriteLine($"<{manifest.UniqueID} changed status: [{status}] {message}\n{failReason}\n{errorDetails}"))
                .Returns(mod.Object);
        }
        return mod;
    }

    /// <summary>Generate a default mod data record.</summary>
    private ModDataRecord GetModDataRecord()
    {
        return new("Default Display Name", new ModDataModel("Sample ID", null, ModWarning.None));
    }

    /// <summary>Generate a default mod data versioned fields instance.</summary>
    private ModDataRecordVersionedFields GetModDataRecordVersionedFields()
    {
        return new ModDataRecordVersionedFields(this.GetModDataRecord());
    }
}