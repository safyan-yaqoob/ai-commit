using System.Net.Http.Json;
using System.Text.Json;
using LibGit2Sharp;
class Program
{
    private enum CommitType
    {
        feat,
        fix,
        chore,
        refactor,
        style,
        test,
        docs,
        perf
    }

    private static string GetValidCommitType(string message)
    {
        if (string.IsNullOrEmpty(message)) return CommitType.feat.ToString();

        // Try to extract the type from the message (text before the colon)
        var typeString = message.Split(':')[0].Trim().ToLower();
        
        // Check if it's a valid commit type
        if (Enum.TryParse<CommitType>(typeString, out var commitType))
        {
            return typeString;
        }

        // If type is not valid, return feat as default
        return CommitType.feat.ToString();
    }

    private static readonly string OllamaApiEndpoint = "http://localhost:11434";
    
    private static readonly HttpClient httpClient = new HttpClient()
    {
        Timeout = TimeSpan.FromMinutes(10)
    };

    static async Task Main(string[] args)
    {
        try
        {
            var repoPath = "D:/DIP"; // my current repo
            var changes = GetStagedChanges(repoPath);
            if (!changes.Any())
            {
                Console.WriteLine("No staged changes found.");
                return;
            }

            try
            {
                var testResponse = await httpClient.GetAsync($"{OllamaApiEndpoint}/api/tags");
                if (!testResponse.IsSuccessStatusCode)
                {
                    throw new Exception("Ollama service is not responding correctly");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to Ollama service: {ex.Message}");
                return;
            }

            string commitMessage = await GenerateCommitMessage(changes);
            //await CreateCommit(repoPath, commitMessage);
            Console.WriteLine($"Successfully created commit with message: {commitMessage}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
    private static IEnumerable<string> GetStagedChanges(string repoPath)
    {
        using var repo = new Repository(repoPath);
        var changes = new List<string>();

        var patch = repo.Diff.Compare<Patch>(repo.Head.Tip.Tree, DiffTargets.Index);

        foreach (var change in patch)
        {
            changes.Add($"File: {change.Path}");
            changes.Add($"Change kind: {change.Status}");

            // Truncate long diffs for readability
            var diffText = change.Patch;
            if (diffText.Length > 800)
            {
                diffText = diffText.Substring(0, 800) + "\n[Diff truncated...]";
            }

            changes.Add("Diff:");
            changes.Add(diffText);
            changes.Add("-------------------");
        }

        return changes;
    }


    private static async Task<string> GenerateCommitMessage(IEnumerable<string> changes)
    {
        try
        {
            var requestBody = new
            {
                model = "llama2:7b-chat",
                prompt =
                    "Write a concise Git commit message describing the overall intent of the following staged code changes.\n" +
                    "Do not include file names or paths in the message.\n" +
                    "Strictly limit the message to a maximum of 20 words.\n" +
                    "Format: <type>: <description>\n" +
                    $"Valid types: {string.Join(", ", Enum.GetNames<CommitType>())}\n\n" +
                    $"Changes:\n{string.Join("\n", changes)}",
                stream = false
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

            var response = await httpClient.PostAsJsonAsync($"{OllamaApiEndpoint}/api/generate", requestBody, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"API request failed with status {response.StatusCode}: {errorContent}");
            }
            
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            var message = result.GetProperty("response").GetString();
            
            if (!string.IsNullOrEmpty(message))
            {
                message = message.Trim();
                if (!message.Contains(":"))
                {
                    // No type specified, treat entire message as description with default type
                    return $"{CommitType.feat}: {message}";
                }

                // Split message into type and description
                var parts = message.Split(':', 2);
                if (parts.Length != 2)
                {
                    return $"{CommitType.feat}: {message}";
                }

                // Use the model's type if it's valid, otherwise analyze the changes
                var modelType = parts[0].Trim().ToLower();
                var finalType = Enum.TryParse<CommitType>(modelType, out var parsedType) 
                    ? modelType  // Use model's type if valid
                    : DetermineCommitType(changes).ToString();  // Fallback to analysis
                
                var description = parts[1].Trim();
                if (string.IsNullOrWhiteSpace(description))
                {
                    description = message;
                }

                return $"{finalType}: {description}";
            }

            throw new Exception("No valid commit message generated");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to generate commit message: {ex.Message}");
            return $"{CommitType.feat}: update files";
        }
    }
    
    private static async Task CreateCommit(string repoPath, string commitMessage)
    {
        using var repo = new Repository(repoPath);
        var author = new Signature("CommitMessageAgent", "ai-commit-agent@example.com", DateTimeOffset.Now);
        var committer = author;
        await Task.Run(() => repo.Commit(commitMessage, author, committer));
    }

    private static CommitType DetermineCommitType(IEnumerable<string> changes)
    {
        var changeText = string.Join("\n", changes).ToLower();
        
        // Look for specific patterns to determine commit type
        if (changeText.Contains("test") || changeText.Contains("/tests/") || changeText.Contains("spec.") || changeText.Contains(".test."))
            return CommitType.test;
        
        if (changeText.Contains("fix") || changeText.Contains("bug") || changeText.Contains("issue") || changeText.Contains("error") || changeText.Contains("crash"))
            return CommitType.fix;
            
        if (changeText.Contains("refactor") || changeText.Contains("restructure") || changeText.Contains("cleanup") || changeText.Contains("clean up"))
            return CommitType.refactor;
            
        if (changeText.Contains("style") || changeText.Contains("format") || changeText.Contains("lint") || changeText.Contains("prettier"))
            return CommitType.style;
            
        if (changeText.Contains("perf") || changeText.Contains("performance") || changeText.Contains("optimize") || changeText.Contains("speed up"))
            return CommitType.perf;
            
        if (changeText.Contains("docs") || changeText.Contains("documentation") || changeText.Contains("readme") || changeText.Contains("comment"))
            return CommitType.docs;
            
        if (changeText.Contains("chore") || changeText.Contains("build") || changeText.Contains("ci") || changeText.Contains("config"))
            return CommitType.chore;
            
        // Default to feat for new additions/changes
        return CommitType.feat;
    }
}