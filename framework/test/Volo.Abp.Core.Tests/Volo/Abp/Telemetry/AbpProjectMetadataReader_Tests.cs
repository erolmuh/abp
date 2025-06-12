using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Shouldly;
using Volo.Abp.Telemetry.Helpers;
using Xunit;

namespace Volo.Abp.Telemetry;

public class AbpProjectMetadataReader_Tests 
{
    private readonly string _testDirectoryPath;
    public AbpProjectMetadataReader_Tests()
    {
        _testDirectoryPath = Path.Combine(Path.GetTempPath(), "AbpProjectMetadataReaderTests");
        Directory.CreateDirectory(_testDirectoryPath);
    }

    [Fact]
    public void Should_Read_Project_Metadata_Successfully()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var role = "test-role";
        var packagePath = CreateTestPackageFile(projectId, role);
        var assembly = CreateMockAssembly(packagePath);

        // Act
        var metadata = AbpProjectMetadataReader.ReadProjectMetadata(assembly);

        // Assert
        metadata.ShouldNotBeNull();
        metadata.ProjectId.ShouldBe(projectId);
        metadata.Role.ShouldBe(role);
    }

    [Fact]
    public void Should_Return_Null_When_No_Package_File_Found()
    {
        // Arrange
        var assembly = CreateMockAssembly(_testDirectoryPath);

        // Act
        var metadata = AbpProjectMetadataReader.ReadProjectMetadata(assembly);

        // Assert
        metadata.ShouldBeNull();
    }

    private string CreateTestPackageFile(Guid? projectId, [CanBeNull] string role)
    {
        var packagePath = Path.Combine(_testDirectoryPath, "test.abppkg");
        var json = JsonSerializer.Serialize(new
        {
            projectId = projectId.ToString(),
            role = role
        });
        File.WriteAllText(packagePath, json);
        return packagePath;
    }

    private Assembly CreateMockAssembly(string packagePath)
    {
        var assemblyPath = Path.Combine(Path.GetDirectoryName(packagePath), $"{Guid.NewGuid():N}.dll");
        
        File.Copy(Assembly.GetExecutingAssembly().Location, assemblyPath, true);

         var loadContext = new AssemblyLoadContext(name: "TestLoadContext", isCollectible: true);
        var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
        
        return assembly;
    }

    
}