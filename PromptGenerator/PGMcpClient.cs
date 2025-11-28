using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ModelContextProtocol.Client;
using System.Linq;
using ModelContextProtocol.Protocol;
using System.Threading.Tasks;


namespace PromptGenerator;

public class PGMcpClient
{
    public McpClient client;
    public string command = "";
    public string[] args = [];
    public string errorMessage = "";
    public bool initializedSuccessfully { get; private set; } = false;

    public PGMcpClient()
    {
        string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string baseDirectory = Path.Combine(homeDirectory, "prompt-generator");

        if (!Directory.Exists(baseDirectory))
            Directory.CreateDirectory(baseDirectory);

        string configPath = Path.Combine(
            baseDirectory,
            "mcp_config.json"
        );
        
        if (!File.Exists(configPath))
        {
            string defaultJson = @"{""mcpServers"":{}}";
            File.WriteAllText(configPath, defaultJson);
        }

        string json = File.ReadAllText(configPath);
        JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        if (!root.TryGetProperty("mcpServers", out JsonElement mcpServers)){
            errorMessage = "設定ファイルに 'mcpServers' プロパティが見つかりません。";
            return;
        }

        JsonElement genimageElement; // Declare genimageElement once
        if (!mcpServers.TryGetProperty("genimage", out genimageElement)){
            errorMessage = "設定ファイルに 'mcpServers.genimage' プロパティが見つかりません。";
            return;
        }

        command = genimageElement.GetProperty("command").GetString();
        args = [.. genimageElement.GetProperty("args")
                                .EnumerateArray()
                                .Select(a => a.GetString())];
        
        initializedSuccessfully = true;
    }

    public async void CreateClient(){
        if(initializedSuccessfully){
            client = await McpClient.CreateAsync(
                new StdioClientTransport(new()
                {
                    Command = command,
                    Arguments = args,
                    Name = "genimage",
                })
            );
        }
    }

    public async Task<string> Run(string prompt)
    {
        if(initializedSuccessfully && client is not null)
        {
            CallToolResult result = await client.CallToolAsync("generate_image", new Dictionary<string, object>
            {
                { "prompt", prompt }
            });
            return JsonSerializer.Serialize(result.Content);
        }
        return null;
    }
}
