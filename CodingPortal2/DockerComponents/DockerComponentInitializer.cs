using System.Diagnostics;
using System.Text.RegularExpressions;
using CodingPortal2.Database;
using CodingPortal2.DatabaseEnums;
using CodingPortal2.DbModels;
using Docker.DotNet;
using Docker.DotNet.Models;
namespace CodingPortal2.DockerComponents;

public class DockerComponentInitializer
{
    private readonly ApplicationDbContext dbContext;
    
    public DockerComponentInitializer(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }
    
    public async Task<string> CompileUserCodeInContainer(string userCode, string userCodeNoFormat, ProgrammingLanguage programmingLanguage, int assignmentId, int userId)
    {
        using var dockerClient = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();
            
        try
        {
            var testsFolderPath = dbContext.Assignments.FirstOrDefault(assignment => assignment.AssignmentId == assignmentId)?.PathToTests;
            var imageName = DockerImageCreation.GetDockerImageForLanguage(programmingLanguage);
            var fileExtension =  DockerImageCreation.GetFileExtension(programmingLanguage);

            await DockerImageCreation.CreateDockerImage(dockerClient, imageName);

            userCodeNoFormat = userCodeNoFormat.Replace("&lt;", "<");
            userCodeNoFormat = userCodeNoFormat.Replace("&gt;", ">");
            userCodeNoFormat = userCodeNoFormat.Replace("&nbsp;", " ");
            userCodeNoFormat = userCodeNoFormat.Replace("&amp;", "&");
            
            if (programmingLanguage == ProgrammingLanguage.Cpp)
            {
                userCodeNoFormat = AddNewlineAfterIncludes(userCodeNoFormat);
            }
            
            var containerWithUserCodeAndTests = await CreateContainerWithUserCodeAndTests(userCodeNoFormat, imageName, fileExtension,
                testsFolderPath!, dockerClient, programmingLanguage);
            await dockerClient.Containers.StartContainerAsync(containerWithUserCodeAndTests.ID, new ContainerStartParameters());
            
            await Task.Delay(12000);
            
            //-------------------
            
            var copyTestCount = $"docker cp {containerWithUserCodeAndTests.ID}:/myApp/TestCount.txt TestCount.txt";
            var copyTestCountProcessStartInfo = new ProcessStartInfo("cmd", $"/c {copyTestCount}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            
            using var copyTestCountProcess = new Process();
            copyTestCountProcess.StartInfo = copyTestCountProcessStartInfo;
            copyTestCountProcess.Start();
            await copyTestCountProcess.WaitForExitAsync();
            
            var totalTests = await File.ReadAllTextAsync("TestCount.txt");
            
            //-------------------
                        
            var copyCompilationError = $"docker cp {containerWithUserCodeAndTests.ID}:/myApp/CompilationError.txt CompilationError.txt";
            var copyCompilationErrorProcessStartInfo = new ProcessStartInfo("cmd", $"/c {copyCompilationError}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            
            using var copyCompilationErrorProcess = new Process();
            copyCompilationErrorProcess.StartInfo = copyCompilationErrorProcessStartInfo;
            copyCompilationErrorProcess.Start();
            await copyCompilationErrorProcess.WaitForExitAsync();
            
            var compilationError = await File.ReadAllTextAsync("CompilationError.txt");
            
            //-------------------
            
            var copyTestsPassedCount = $"docker cp {containerWithUserCodeAndTests.ID}:/myApp/TotalTestsPassed.txt TotalTestsPassed.txt";
            var copyTestsPassedCountProcessStartInfo = new ProcessStartInfo("cmd", $"/c {copyTestsPassedCount}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            
            using var copyOutputProcess = new Process();
            copyOutputProcess.StartInfo = copyTestsPassedCountProcessStartInfo;
            copyOutputProcess.Start();
            await copyOutputProcess.WaitForExitAsync();
            
            var testsOutputString = await File.ReadAllTextAsync("TotalTestsPassed.txt");
            
            /*//-------------------
            
            var copyUserCode = $"docker cp {containerWithUserCodeAndTests.ID}:/myApp/UserCode.cpp UserCode.cpp";
            var copyUserCodeProcessStartInfo = new ProcessStartInfo("cmd", $"/c {copyUserCode}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            
            using var copyOutputProcess3 = new Process();
            copyOutputProcess3.StartInfo = copyUserCodeProcessStartInfo;
            copyOutputProcess3.Start();
            await copyOutputProcess3.WaitForExitAsync();
            
            var userCodeOut = await File.ReadAllTextAsync("UserCode.cpp");
            
            *///-------------------
            
            // wait for copy of files
            await Task.Delay(4000);

            var containerList = await dockerClient.Containers.ListContainersAsync(new ContainersListParameters());
            var container = containerList.FirstOrDefault(containerListResponse => containerListResponse.ID == containerWithUserCodeAndTests.ID);
            if (container != null)
            {
                await dockerClient.Containers.KillContainerAsync(containerWithUserCodeAndTests.ID, new ContainerKillParameters());
                await dockerClient.Containers.RemoveContainerAsync(containerWithUserCodeAndTests.ID, new ContainerRemoveParameters());
                return "compilation output: compilation was terminated because it was running too long!.";
            }
            
            await dockerClient.Containers.RemoveContainerAsync(containerWithUserCodeAndTests.ID, new ContainerRemoveParameters());
            
            /*if (!string.IsNullOrWhiteSpace(errorString))
            {
                testsOutputString = $"No tests executed. {testCount}";
                await SaveUserSolutionToDb(userCode, assignmentId, userId, testsOutputString, testCount);

                return "Compilation failed";
            }*/
            
            await SaveUserSolutionToDb(userCode, userCodeNoFormat, assignmentId, userId, testsOutputString, totalTests);
            
            return $"Compilation output: {compilationError} {DockerTestOutputHelper.FormatTestsOutputForUser(testsOutputString, totalTests)}";
        }
        catch (Exception ex)
        {
            return $"Compilation failed: {ex.Message} Inner Exception: {ex.InnerException?.Message}";
        }
    }
    
    private static string AddNewlineAfterIncludes(string code)
    {
        // Use regular expression to find include directives
        string pattern = @"(#include\s*<.*?>|#include\s*\""[^\""]*\"")";
        
        // Use Regex.Replace to add a newline after each include directive
        string modifiedCode = Regex.Replace(code, pattern, "$1\n");
        
        return modifiedCode;
    }
    
    private async Task SaveUserSolutionToDb(string userCode, string userCodeNoFormat, int assignmentId, int userId, string testsOutputString, string totalTests)
    {
        var userAssignmentSolution = new UserAssignmentSolution
        {
            UserId = userId,
            AssignmentId = assignmentId,
            NoFormatSolution = userCodeNoFormat,
            Solution = userCode,
            UploadDateTime = DateTimeOffset.Now,
            TestPassed = DockerTestOutputHelper.GetPassedTestsCount(testsOutputString),
            TotalTests = DockerTestOutputHelper.GetTotalTestsCount(totalTests),
        };
        
        var plagiarism = new Plagiarism
        {
            UserAssignmentSolution = userAssignmentSolution,
        };
    
        userAssignmentSolution.Plagiarism = plagiarism;

        await dbContext.UserAssignmentSolutions.AddAsync(userAssignmentSolution);
        await dbContext.SaveChangesAsync();


        plagiarism.UserSolutionId = userAssignmentSolution.UserAssignmentSolutionId;
        dbContext.Plagiarisms.Update(plagiarism);
        
        await dbContext.SaveChangesAsync();
    }

    private static async Task<CreateContainerResponse> CreateContainerWithUserCodeAndTests(string userCodeNoFormat, string imageName, string fileExtension,
        string examinationFileFolderPath, DockerClient dockerClient, ProgrammingLanguage language)
    {
        var assignmentExaminationFile = Directory.GetFiles(examinationFileFolderPath, "*.out");
        var examinationFileAsString = string.Join("\n", assignmentExaminationFile.Select(File.ReadAllText));
        
        var containerCreateParams = new CreateContainerParameters
        {
            Image = imageName,
            WorkingDir = "/myApp",
            Cmd = new List<string>
            {
                "bash",
                "-c",
                $"apt-get update && " + // Conditional command for installing g++
                "apt-get install -y cgroup-tools --no-install-recommends && " + // Install cgroup-tools
                $"echo '{examinationFileAsString.Replace("\r\n", "\n")}' > ExaminationFile.sh && " +
                $"echo '{DockerImageCreation.GenerateProjectFileContent()}' > ProjectFile.csproj && " +
                $"chmod 777 ExaminationFile.sh && " +
                $"touch TotalTestsPassed.txt && " + // Create TotalTestsPassed.txt
                $"./ExaminationFile.sh > TestCount.txt && " +
                $"touch CompilationError.txt && " +
                $"chmod 777 TotalTestsPassed.txt && " +
                $"echo '{userCodeNoFormat}' > {DockerImageCreation.GetFileName(language)}.{fileExtension} && " +
                $"{DockerImageCreation.GetCompilationCommand(language)}" + // Compile user code
                $"num_tests=$(./ExaminationFile.sh) && " +
                "for ((i = 0; i < num_tests; i++)); do " +
                $"    MemoryLimitInMb=$(./ExaminationFile.sh P $i); " +
                $"    MemoryLimitInBytes=$((MemoryLimitInMb * 1024 * 1024)); " + // Convert MB to bytes
                $"    TimeLimitInMs=$(./ExaminationFile.sh T $i); " +
                $"    ./ExaminationFile.sh R $i > correctOutput$i.txt; " +
                $"    ./ExaminationFile.sh $i > inputForUserCode.txt; " + //get input for user
                $"    cgcreate -g memory:/mygroup$i && " + // Create memory control group for each test
                $"    chmod -R 777 /sys/fs/cgroup/memory/mygroup$i/* && " + // Add full permissions to memory cgroup
                $"    cgset -r memory.limit_in_bytes=$MemoryLimitInBytes /mygroup$i || true && " + // Set memory limit (with error handling)
                $"    cgcreate -g cpu:/timegroup$i && " + // Create CPU control group for each test
                $"    chmod -R 777 /sys/fs/cgroup/cpu/timegroup$i/* && " + // Add full permissions to CPU cgroup
                $"    cgset -r cpu.cfs_period_us=1000000 /timegroup$i && " + // Set CPU period to 1 second
                $"    cgset -r cpu.cfs_quota_us=$((TimeLimitInMs * 1000)) /timegroup$i || true && " + // Set CPU quota to time limit in milliseconds (with error handling)
                $"    {DockerImageCreation.GetExecutionCommand(language)} " + // Execute compiled user code
                $"    if cmp -s userCodeOutput$i.txt correctOutput$i.txt; then " + // Compare user output with correct output
                $"        echo 'Passed' >> TotalTestsPassed.txt; " + // Write "Passed" to TotalTestsPassed.txt if outputs match
                $"    else " + // If outputs don't match
                $"        echo 'Failed' >> TotalTestsPassed.txt; " + // Write "Failed" to TotalTestsPassed.txt
                $"    fi; " +
                $"    cgdelete -g memory,cpu:/mygroup$i:/timegroup$i; " + // Remove memory and CPU control groups after each test
                "done"
            },
            Tty = true,
            OpenStdin = true,
            StdinOnce = true,
            HostConfig = new HostConfig
            {
                AutoRemove = false,
                Memory = 1024 * 1024 * 1024, // Total memory limit for the container (1GB)
                CPUShares = 512,
                CPUCount = 8,
                Privileged = true, // Set container to run in privileged mode
            }
        };

        var container = await dockerClient.Containers.CreateContainerAsync(containerCreateParams);
        return container;
    }
}
