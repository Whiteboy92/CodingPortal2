using System.Text;
using CodingPortal2.Database;
using CodingPortal2.DatabaseEnums;
using CodingPortal2.DockerComponents;
using Docker.DotNet;
using Docker.DotNet.Models;
namespace CodingPortal2.PlagiarismDetection;

public class DockerInitializationPlagiarism
{
    private readonly ApplicationDbContext dbContext;
    
    public DockerInitializationPlagiarism(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    private const int RunningContainerTimeMs = 30000;

    public async Task<string?> DetectPlagiarismsForAssignment(ProgrammingLanguage language, int assignmentId)
    {
        using var dockerClient = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();

        try
        {
            await DockerImageCreationPlagiarism.CreateDockerImage(dockerClient);
            string url = "";
            
            var plagiarismContainer = await CreateContainerToRunAntiPlagiarism(dockerClient, language, assignmentId);
            await dockerClient.Containers.StartContainerAsync(plagiarismContainer.ID, new ContainerStartParameters());

            // Capture the output of the Moss plagiarism check
            var logsStream = await dockerClient.Containers.GetContainerLogsAsync(
                plagiarismContainer.ID,
                new ContainerLogsParameters
                {
                    ShowStdout = true,
                    Follow = true
                });

            using var reader = new StreamReader(logsStream);
            StringBuilder containerLogs = new StringBuilder();

            while (!reader.EndOfStream)
            {
                string? logLine = await reader.ReadLineAsync();

                if (logLine != null && logLine.StartsWith("http"))
                {
                    url = logLine;
                }

                containerLogs.AppendLine(logLine);
            }

            await Task.Delay(RunningContainerTimeMs);

            var containerList = await dockerClient.Containers.ListContainersAsync(new ContainersListParameters());
            var container = containerList.FirstOrDefault(containerListResponse => containerListResponse.ID == plagiarismContainer.ID);
            if (container != null)
            {
                await dockerClient.Containers.KillContainerAsync(plagiarismContainer.ID, new ContainerKillParameters());
                await dockerClient.Containers.RemoveContainerAsync(plagiarismContainer.ID, new ContainerRemoveParameters());
                return "compilation output: Container was killed because it was running too long!.";
            }
            
            await dockerClient.Containers.RemoveContainerAsync(plagiarismContainer.ID, new ContainerRemoveParameters());

            return url;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return e.Message;
        }
    }

    private async Task<CreateContainerResponse> CreateContainerToRunAntiPlagiarism(DockerClient dockerClient, ProgrammingLanguage language, int assignmentId)
    {
        string executionCommand = GenerateMossExecutionCommand(language, assignmentId);
        string extension = DockerImageCreation.GetFileExtension(language);
        string pearlScriptContent = "#!/usr/bin/perl\nuse IO::Socket;\n$server = 'moss.stanford.edu';\n$port = '7690';\n$noreq = \"Request not sent.\";\n$usage = \"usage: moss [-x] [-l language] [-d] [-b basefile1] ... [-b basefilen] [-m #] [-c \\\"string\\\"] file1 file2 file3 ...\";\n\n#\n# The userid is used to authenticate your queries to the server; don't change it!\n#\n$userid=967155773;\n\n#\n# Process the command line options.  This is done in a non-standard\n# way to allow multiple -b's.\n#\n$opt_l = \"c\";   # default language is c\n$opt_m = 10;\n$opt_d = 0;\n$opt_x = 0;\n$opt_c = \"\";\n$opt_n = 250;\n$bindex = 0;    # this becomes non-zero if we have any base files\n\nwhile (@ARGV && ($_ = $ARGV[0]) =~ /^-(.)(.*)/) {\n    ($first,$rest) = ($1,$2);   \n\n    shift(@ARGV);\n    if ($first eq \"d\") {\n        $opt_d = 1;\n        next;\n    }\n    if ($first eq \"b\") {\n        if($rest eq '') {\n            die \"No argument for option -b.\\n\" unless @ARGV;\n            $rest = shift(@ARGV);\n        }\n        $opt_b[$bindex++] = $rest;\n        next;\n    }\n    if ($first eq \"l\") {\n        if ($rest eq '') {\n            die \"No argument for option -l.\\n\" unless @ARGV;\n            $rest = shift(@ARGV);\n        }\n        $opt_l = $rest;\n        next;\n    }\n    if ($first eq \"m\") {\n        if($rest eq '') {\n            die \"No argument for option -m.\\n\" unless @ARGV;\n            $rest = shift(@ARGV);\n        }\n        $opt_m = $rest;\n        next;\n    }\n    if ($first eq \"c\") {\n        if($rest eq '') {\n            die \"No argument for option -c.\\n\" unless @ARGV;\n            $rest = shift(@ARGV);\n        }\n        $opt_c = $rest;\n        next;\n    }\n    if ($first eq \"n\") {\n        if($rest eq '') {\n            die \"No argument for option -n.\\n\" unless @ARGV;\n            $rest = shift(@ARGV);\n        }\n        $opt_n = $rest;\n        next;\n    }\n    if ($first eq \"x\") {\n        $opt_x = 1;\n        next;\n    }\n    #\n    # Override the name of the server.  This is used for testing this script.\n    #\n    if ($first eq \"s\") {\n        $server = shift(@ARGV);\n        next;\n    }\n    #\n    # Override the port.  This is used for testing this script.\n    #\n    if ($first eq \"p\") {\n        $port = shift(@ARGV);\n        next;\n    }\n    die \"Unrecognized option -$first.  $usage\\n\";\n}\n\n#\n# Check a bunch of things first to ensure that the\n# script will be able to run to completion.\n#\n\n#\n# Make sure all the argument files exist and are readable.\n#\nprint \"Checking files . . . \\n\";\n$i = 0;\nwhile($i < $bindex)\n{\n    die \"Base file $opt_b[$i] does not exist. $noreq\\n\" unless -e \"$opt_b[$i]\";\n    die \"Base file $opt_b[$i] is not readable. $noreq\\n\" unless -r \"$opt_b[$i]\";\n    die \"Base file $opt_b is not a text file. $noreq\\n\" unless -T \"$opt_b[$i]\";\n    $i++;\n}\nforeach $file (@ARGV)\n{\n    die \"File $file does not exist. $noreq\\n\" unless -e \"$file\";\n    die \"File $file is not readable. $noreq\\n\" unless -r \"$file\";\n    die \"File $file is not a text file. $noreq\\n\" unless -T \"$file\";\n}\n\nif (\"@ARGV\" eq '') {\n    die \"No files submitted.\\n $usage\";\n}\nprint \"OK\\n\";\n\n#\n# Now the real processing begins.\n#\n\n\n$sock = new IO::Socket::INET (\n                                  PeerAddr => $server,\n                                  PeerPort => $port,\n                                  Proto => 'tcp',\n                                 );\ndie \"Could not connect to server $server: $!\\n\" unless $sock;\n$sock->autoflush(1);\n\nsub read_from_server {\n    $msg = <$sock>;\n    print $msg;\n}\n\nsub upload_file {\n    local ($file, $id, $lang) = @_;\n#\n# The stat function does not seem to give correct filesizes on windows, so\n# we compute the size here via brute force.\n#\n    open(F,$file);\n    $size = 0;\n    while (<F>) {\n        $size += length($_);\n    }\n    close(F);\n\n    print \"Uploading $file ...\";\n    open(F,$file);\n    $file =~s/\\s/\\_/g;    # replace blanks in filename with underscores\n    print $sock \"file $id $lang $size $file\\n\";\n    while (<F>) {\n        print $sock $_;\n    }\n    close(F);\n    print \"done.\\n\";\n}\n\n\nprint $sock \"moss $userid\\n\";      # authenticate user\nprint $sock \"directory $opt_d\\n\";\nprint $sock \"X $opt_x\\n\";\nprint $sock \"maxmatches $opt_m\\n\";\nprint $sock \"show $opt_n\\n\";\n\n#\n# confirm that we have a supported languages\n#\nprint $sock \"language $opt_l\\n\";\n$msg = <$sock>;\nchop($msg);\nif ($msg eq \"no\") {\n    print $sock \"end\\n\";\n    die \"Unrecognized language $opt_l.\";\n}\n\n\n# upload any base files\n$i = 0;\nwhile($i < $bindex) {\n    &upload_file($opt_b[$i++],0,$opt_l);\n}\n\n$setid = 1;\nforeach $file (@ARGV) {\n    &upload_file($file,$setid++,$opt_l);\n}\n\nprint $sock \"query 0 $opt_c\\n\";\nprint \"Query submitted.  Waiting for the server's response.\\n\";\n&read_from_server();\nprint $sock \"end\\n\";\nclose($sock);\nEOF";
        string cmdString = "";
        
        var solutions = dbContext.UserAssignmentSolutions
            .Where(userAssignmentSolution => userAssignmentSolution.AssignmentId == assignmentId)
            .Select(userAssignmentSolution => new { userAssignmentSolution.Solution, userAssignmentSolution.UserAssignmentSolutionId })
            .ToList();
        
        cmdString += $"apt-get update && apt-get install -y perl && (cat <<'EOF' > moss.pl\n{pearlScriptContent}\n) && chmod 777 moss.pl && ";
        
        foreach (var solution in solutions)
        {
            var fileName = $"{solution.UserAssignmentSolutionId}";
            string noFormattingSolution = await DockerTestOutputHelper.RemoveHtmlFormatting(solution.Solution);
            noFormattingSolution += "\nEOF\n";
            string command = $"cat <<'EOF' > {fileName}.{extension}\n{noFormattingSolution}\n";
            cmdString += command;
        }
        
        cmdString += executionCommand;
        cmdString = cmdString.TrimEnd('&', '&', ' ');
        
        var containerCreateParams = new CreateContainerParameters
        {
            Image = "ubuntu:20.04",
            WorkingDir = "/AntiPlagiarism",
            Cmd = new List<string>
            {
                "bash",
                "-c",
                cmdString,
            },
            Tty = true,
            OpenStdin = true,
            StdinOnce = true,
            HostConfig = new HostConfig
            {
                AutoRemove = false, 
                Memory = 1024 * 1024 * 1024,
                CPUShares = 1024,
                CPUCount = 8,
            },
        };
        
        var container = await dockerClient.Containers.CreateContainerAsync(containerCreateParams);
        return container;
    }
    
    private string GenerateMossExecutionCommand(ProgrammingLanguage language, int assignmentId)
    {
        string fileExtension = DockerImageCreation.GetFileExtension(language);
        string mossLanguage = DockerImageCreation.GetMossLanguage(language);

        List<string> fileNames = new List<string>();

        foreach (var userAssignmentSolution in dbContext.UserAssignmentSolutions
            .Where(solution => solution.AssignmentId == assignmentId))
        {
            var file = $" {userAssignmentSolution.UserAssignmentSolutionId}.{fileExtension}";
            fileNames.Add(file);
        }

        string mossCommand = $"./moss.pl -l {mossLanguage}";
        foreach (string fileName in fileNames)
        {
            mossCommand += $"{fileName}";
        }

        return mossCommand;
    }
}