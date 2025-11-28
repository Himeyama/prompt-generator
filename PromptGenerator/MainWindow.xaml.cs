using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using ModelContextProtocol.Protocol;


namespace PromptGenerator;
public sealed partial class MainWindow : Window
{
    PromptUIGenerator uiGenerator;
    PGMcpClient mcpClient;
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        Title = AppTitleTextBlock.Text;

        Status.statusBar = StatusBar;
        Status.dispatcherQueue = DispatcherQueue;
        // ZoomIn.KeyboardAcceleratorTextOverride = ZoomInText.Text;
        // ZoomOut.KeyboardAcceleratorTextOverride = ZoomOutText.Text;

        uiGenerator = new PromptUIGenerator();
        uiGenerator.Initialize(UIPanel, Charactor, Prompt);

        mcpClient = new();
        mcpClient.CreateClient();
    }

    void AutoSave_Toggled(object sender, RoutedEventArgs e)
    {
        try{
            // EnableAutoSave.Visibility = AutoSave.IsOn ? Visibility.Visible : Visibility.Collapsed;
            // DisableAutoSave.Visibility = AutoSave.IsOn ? Visibility.Collapsed : Visibility.Visible;
        }catch(Exception ex){
            Status.AddMessage(ex.Message);
        }
    }

    void Base64ToImage(string base64Text, string path)
    {
        byte[] imageBytes = Convert.FromBase64String(base64Text);
        File.WriteAllBytes(path, imageBytes);
        Status.AddMessage($"Image saved to: {path}");
    }

    async void Run(object sender, RoutedEventArgs e)
    {
        GenerateButton.IsEnabled = false;

        string imagePath = CreateSavePath(Prompt.Text);
        string jsonResult = await mcpClient.Run(Prompt.Text);

        try
        {
            // Parse the main JSON string
            using JsonDocument doc = JsonDocument.Parse(jsonResult);
            JsonElement root = doc.RootElement; // Expects an array

            // Check if it's an array and has at least one element
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                JsonElement firstItem = root[0]; // Get the first item in the array

                if (firstItem.TryGetProperty("text", out JsonElement textElement) && textElement.ValueKind == JsonValueKind.String)
                {
                    string innerJsonString = textElement.GetString();
                    using JsonDocument innerDoc = JsonDocument.Parse(innerJsonString);
                    JsonElement innerRoot = innerDoc.RootElement;

                    if (innerRoot.TryGetProperty("success", out JsonElement successElement) && successElement.ValueKind == JsonValueKind.True)
                    {
                        if (innerRoot.TryGetProperty("output", out JsonElement outputElement) && outputElement.ValueKind == JsonValueKind.String)
                        {
                            Base64ToImage(outputElement.GetString(), imagePath);
                        }
                        else
                        {
                            Status.AddMessage("Inner JSON missing 'output' property or it's not a string.");
                        }
                    }
                    else
                    {
                        Status.AddMessage("Operation failed: Inner JSON 'success' property is false or missing.");
                    }
                }
                else
                {
                    Status.AddMessage("First item in array missing 'text' property or it's not a string.");
                }
            }
            else
            {
                Status.AddMessage("JSON result is empty or not an array as expected.");
            }
        }
        catch (JsonException ex)
        {
            Status.AddMessage($"Failed to parse JSON result: {ex.Message}");
            return;
        }
        // Load the image from the file path and set it as the source for the Image control.
        OutputImage.Source = new BitmapImage(new Uri(imagePath));
        GenerateButton.IsEnabled = true;
    }

    string CreateSavePath(string promptName)
    {
        string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string baseDirectory = Path.Combine(homeDirectory, "prompt-generator", "images");
        
        if (!Directory.Exists(baseDirectory))
            Directory.CreateDirectory(baseDirectory);
        
        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        string sanitizedPromptName = Regex.Replace(promptName, @"[^a-zA-Z0-9]+", "_");
        sanitizedPromptName = Regex.Replace(sanitizedPromptName, @"_+", "_");
        sanitizedPromptName = sanitizedPromptName.Trim('_');
        string fileName = $"{timestamp}-{sanitizedPromptName}.png";
        return Path.Combine(baseDirectory, fileName);
    }

    async void ClickOpen(object sender, RoutedEventArgs e)
    {
        await FilePicker.Open(this);
    }

    async void ClickSave(object sender, RoutedEventArgs e)
    {
        await FilePicker.Save(this, "Save");
    }

    async void ClickSaveAs(object sender, RoutedEventArgs e)
    {
        await FilePicker.Save(this, "Save as");
    }

    void ClickZoomIn(object sender, RoutedEventArgs e)
    {
        Status.AddMessage($"Zoom In");
    }

    void ClickZoomOut(object sender, RoutedEventArgs e)
    {
        Status.AddMessage($"Zoom out");
    }

    void ClickRestoreDefaultZoom(object sender, RoutedEventArgs e)
    {
        Status.AddMessage($"Restore default zoom");
    }

    async void ClickAbout(object sender, RoutedEventArgs e)
    {
        await Dialog.Show(Content, "This app is an example app for Windows App SDK!", "About");
        Status.AddMessage($"Thank you for using this app!");
    }

    void ClickExit(object sender, RoutedEventArgs e)
    {
        Close();
    }
}