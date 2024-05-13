using CodingPortal2.DatabaseEnums;
using Docker.DotNet;
using Docker.DotNet.Models;
namespace CodingPortal2.DockerComponents;

public static class DockerImageCreation
{
    public static async Task CreateDockerImage(DockerClient client, string imageName)
    {
        try
        {
            var images = await client.Images.ListImagesAsync(new ImagesListParameters());

            var existingImage = images.FirstOrDefault(image => image.RepoTags != null && image.RepoTags.Contains(imageName));

            if (existingImage == null)
            {
                await client.Images.CreateImageAsync(
                    new ImagesCreateParameters
                    {
                        FromImage = imageName,
                    },
                    new AuthConfig(), 
                    new Progress<JSONMessage>());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to create Docker image '{imageName}': {ex.Message}");
            throw;
        }
    }
 
    public static string GetDockerImageForLanguage(ProgrammingLanguage language)
    {
        return language switch
        {
            ProgrammingLanguage.Cpp => "gcc:latest",
            ProgrammingLanguage.Java => "adoptopenjdk:latest",
            ProgrammingLanguage.Csharp => "mcr.microsoft.com/dotnet/sdk:8.0",
            _ => throw new NotSupportedException($"Language {language} is not supported.")
        };
    }

    public static string GetFileExtension(ProgrammingLanguage language)
    {
        return language switch
        {
            ProgrammingLanguage.Cpp => "cpp",
            ProgrammingLanguage.Java => "java",
            ProgrammingLanguage.Csharp => "cs",
            _ => throw new NotSupportedException($"Language {language} is not supported.")
        };
    }

    public static string GetCompilationCommand(ProgrammingLanguage language)
    {
        return language switch
        {
            ProgrammingLanguage.Cpp => "g++ -o UserCode UserCode.cpp 2> CompilationError.txt && ",
            ProgrammingLanguage.Java => "javac -d . Main.java 2> CompilationError.txt  && ",
            ProgrammingLanguage.Csharp => "dotnet build --configuration Release ProjectFile.csproj 2> CompilationError.txt && ",
            _ => "",
        };
    }

    
    public static string GenerateProjectFileContent()
    {
        return @"<Project Sdk=""Microsoft.NET.Sdk"">
                 <PropertyGroup>
                   <OutputType>Exe</OutputType>
                   <TargetFramework>net8.0</TargetFramework>
                   <ImplicitUsings>enable</ImplicitUsings>
                   <Nullable>enable</Nullable>
                 </PropertyGroup>
              </Project>";
    }
    
    public static string GetMossLanguage(ProgrammingLanguage language)
    {
        return language switch
        {
            ProgrammingLanguage.Cpp => "cc",
            ProgrammingLanguage.Java => "java",
            ProgrammingLanguage.Csharp => "csharp",
            _ => "",
        };
    }
    
    public static string GetExecutionCommand(ProgrammingLanguage language)
    {
        return language switch
        {
            ProgrammingLanguage.Cpp => "./UserCode < inputForUserCode.txt > userCodeOutput$i.txt; ",
            ProgrammingLanguage.Java => "java Main < inputForUserCode.txt > userCodeOutput$i.txt; ",
            ProgrammingLanguage.Csharp => "dotnet /myApp/bin/Release/net8.0/ProjectFile.dll < inputForUserCode.txt > userCodeOutput$i.txt; ",
            _ => ""
        };
    }
    
    public static string GetFileName(ProgrammingLanguage language)
    {
        return language switch
        {
            ProgrammingLanguage.Cpp => "UserCode",
            ProgrammingLanguage.Java => "Main",
            ProgrammingLanguage.Csharp => "UserCode",
            _ => ""
        };
    }
}