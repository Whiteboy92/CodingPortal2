using CodingPortal2.Database;
using CodingPortal2.DbModels;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
namespace CodingPortal2.PlagiarismDetection;

public class FetchHttpData
{
    private readonly ApplicationDbContext dbContext;
    
    public FetchHttpData(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }
    
    public async Task ProcessPlagiarismDataAsync(string url)
    {
        using var client = new HttpClient();
        HttpResponseMessage response = await client.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            string htmlContent = await response.Content.ReadAsStringAsync();
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(htmlContent);

            Dictionary<string, List<string>> fileSimilarities = new Dictionary<string, List<string>>();

            foreach (var row in document.DocumentNode.SelectNodes("//table//tr"))
            {
                var columns = row.SelectNodes("td");
                if (columns is { Count: 3 })
                {
                    string file1 = columns[0].InnerText.Trim();
                    string file2 = columns[1].InnerText.Trim();

                    string[] parts1 = file1.Split(" ");
                    string file1Name = parts1[0];
                    string file1Percentage = parts1[1];

                    string[] parts2 = file2.Split(" ");
                    string file2Name = parts2[0];
                    string file2Percentage = parts2[1];

                    string percentageValueString1 = file1Percentage.Replace("(", "").Replace(")", "").Replace("%", "").Trim();
                    string percentageValueString2 = file2Percentage.Replace("(", "").Replace(")", "").Replace("%", "").Trim();

                    if (double.TryParse(percentageValueString1, out double percentageValue1) && percentageValue1 > 60)
                    {
                        if (!fileSimilarities.ContainsKey(file1Name))
                            fileSimilarities[file1Name] = new List<string>();

                        fileSimilarities[file1Name].Add($"{file2Name} {percentageValueString1}");
                    }

                    if (double.TryParse(percentageValueString2, out double percentageValue2) && percentageValue2 > 60)
                    {
                        if (!fileSimilarities.ContainsKey(file2Name))
                            fileSimilarities[file2Name] = new List<string>();

                        fileSimilarities[file2Name].Add($"{file1Name} {percentageValueString2}");
                    }
                }
            }
            
            SavePlagiarismDataToDatabase(fileSimilarities);
        }
        else
        {
            Console.WriteLine($"Failed to retrieve data. Status code: {response.StatusCode}");
        }
    }

    private void SavePlagiarismDataToDatabase(Dictionary<string, List<string>> fileSimilarities)
    {
        foreach (var baseFile in fileSimilarities.Keys)
        {
            string[] baseFileParts = baseFile.Split('.');
            if (baseFileParts.Length != 2 || !int.TryParse(baseFileParts[0], out int baseFileUasId))
            {
                Console.WriteLine($"Invalid file format for base file: {baseFile}");
                continue;
            }

            var userAssignmentSolution = dbContext.UserAssignmentSolutions
                .FirstOrDefault(uas => uas.UserAssignmentSolutionId == baseFileUasId);

            if (userAssignmentSolution == null)
            {
                Console.WriteLine($"UserAssignmentSolution not found for file: {baseFile}");
                continue;
            }

            // Retrieve the existing Plagiarism entity associated with the UserAssignmentSolution
            var plagiarism = dbContext.Plagiarisms.Include(plagiarism => plagiarism.PlagiarismEntries)
                .FirstOrDefault(plagiarism1 => plagiarism1.UserSolutionId == userAssignmentSolution.UserAssignmentSolutionId);

            foreach (var similarFileData in fileSimilarities[baseFile])
            {
                // Parse the data for each similar file
                string[] parts = similarFileData.Split(" ");
                string similarFileName = parts[0];

                string[] similarFileParts = similarFileName.Split('.');
                if (similarFileParts.Length != 2 || !int.TryParse(similarFileParts[0], out int similarFileUasId))
                {
                    Console.WriteLine($"Invalid file format for similar file: {similarFileName}");
                    continue;
                }

                string percentageString = parts[1].Trim('(', ')');

                var similarFileSolution = dbContext.UserAssignmentSolutions
                    .FirstOrDefault(uas => uas.UserAssignmentSolutionId == similarFileUasId);

                if (similarFileSolution == null)
                {
                    Console.WriteLine($"UserAssignmentSolution not found for file: {similarFileName}");
                    continue;
                }

                var similarityPercentage = double.Parse(percentageString);
                
                var existingEntry = plagiarism!.PlagiarismEntries
                    .FirstOrDefault(entry => entry.PlagiarisedSolutionId == similarFileSolution.UserAssignmentSolutionId);

                if (existingEntry == null)
                {
                    var plagiarismEntry = new PlagiarismEntry
                    {
                        PlagiarisedSolutionId = similarFileSolution.UserAssignmentSolutionId,
                        Percentage = similarityPercentage,
                        PlagiarisedSolution = similarFileSolution
                    };
                    
                    plagiarism.PlagiarismEntries.Add(plagiarismEntry);
                }
                else
                {
                    existingEntry.Percentage = similarityPercentage;
                }
            }
        }
        
        dbContext.SaveChanges();
    }

}
