using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using System.Text.Json.Serialization;

public class PromptData
{
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class PromptUIGenerator
{
    private StackPanel mainStackPanel;
    private TextBox promptTextBox;
    private Dictionary<string, string> selections = [];
    string charactor { get; set; } = "";

    public void Initialize(StackPanel stackPanel, TextBox Charactor, TextBox prompt)
    {
        mainStackPanel = stackPanel;
        promptTextBox = prompt;
        
        string jsonPath = GetJsonPath();
        if (File.Exists(jsonPath))
        {
            LoadAndGenerateUI(jsonPath);
        }
        else
        {
            // JSONファイルが存在しない場合、空のJSONを作成
            File.WriteAllText(jsonPath, "{}");
        }

        Charactor.TextChanged += (sender, e) =>
        {
            charactor = Charactor.Text;
            UpdatePromptText();
        };
    }

    private string GetJsonPath()
    {
        // 環境変数 PG_PROMPT_PATH をチェック
        string envPath = Environment.GetEnvironmentVariable("PG_PROMPT_PATH");
        if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
        {
            return envPath;
        }

        // ドキュメントフォルダから取得
        string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string baseDirectory = Path.Combine(homeDirectory, "prompt-generator");

        if (!Directory.Exists(baseDirectory))
            Directory.CreateDirectory(baseDirectory);

        string configPath = Path.Combine(
            baseDirectory,
            "prompts.json"
        );
        return configPath;
    }

    private void LoadAndGenerateUI(string jsonPath)
    {
        try
        {
            string jsonContent = File.ReadAllText(jsonPath);
            Dictionary<string, Dictionary<string, List<PromptData>>> data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, List<PromptData>>>>(jsonContent);

            if (data != null)
            {
                foreach (KeyValuePair<string, Dictionary<string, List<PromptData>>> mainCategory in data)
                {
                    CreateMainCategory(mainCategory.Key, mainCategory.Value);
                }
            }
        }
        catch (Exception ex)
        {
            // エラーハンドリング
            TextBlock errorText = new()
            {
                Text = $"JSONファイルの読み込みに失敗しました: {ex.Message}",
                FontSize = 16,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red),
                Margin = new Thickness(0, 0, 0, 16)
            };
            mainStackPanel.Children.Add(errorText);
        }
    }

    private void CreateMainCategory(string categoryName, Dictionary<string, List<PromptData>> subCategories)
    {
        // メインカテゴリーのラベル
        TextBlock mainLabel = new()
        {
            Text = categoryName,
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Margin = mainStackPanel.Children.Count == 0 
                ? new Thickness(0, 0, 0, 16) 
                : new Thickness(0, 24, 0, 16)
        };
        mainStackPanel.Children.Add(mainLabel);

        // サブカテゴリーを2列グリッドで配置
        int columnIndex = 0;
        Grid currentGrid = null;

        foreach (KeyValuePair<string, List<PromptData>> subCategory in subCategories)
        {
            if (columnIndex % 2 == 0)
            {
                // 新しいGridを作成
                currentGrid = new Grid
                {
                    ColumnSpacing = 8,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                currentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                currentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                mainStackPanel.Children.Add(currentGrid);
            }

            StackPanel stackPanel = new();
            Grid.SetColumn(stackPanel, columnIndex % 2);

            // サブカテゴリーのラベル
            TextBlock subLabel = new()
            {
                Text = subCategory.Key,
                FontSize = 18,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            stackPanel.Children.Add(subLabel);

            // ComboBox
            ComboBox comboBox = new()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                PlaceholderText = "選択してください"
            };

            // "選択解除"オプション
            ComboBoxItem noSelectionItem = new()
            {
                Content = "選択解除"
            };
            comboBox.Items.Add(noSelectionItem);

            // データ項目を追加
            foreach (PromptData item in subCategory.Value)
            {
                ComboBoxItem comboItem = new()
                {
                    Content = item.Name,
                    Tag = item.Prompt
                };
                comboBox.Items.Add(comboItem);
            }

            // 選択変更イベント
            string key = $"{categoryName}-{subCategory.Key}";
            comboBox.SelectionChanged += (sender, e) =>
            {
                ComboBoxItem selectedItem = comboBox.SelectedItem as ComboBoxItem;
                if (selectedItem != null && selectedItem.Tag != null)
                {
                    selections[key] = selectedItem.Tag.ToString();
                }
                else
                {
                    selections.Remove(key);
                }
                UpdatePromptText();
            };

            stackPanel.Children.Add(comboBox);
            currentGrid.Children.Add(stackPanel);

            columnIndex++;
        }
    }

    private void UpdatePromptText()
    {
        List<string> prompts = [];

        if (!string.IsNullOrEmpty(charactor))
            prompts.Add(charactor);
        
        foreach (string selection in selections.Values)
            if (!string.IsNullOrEmpty(selection))
                prompts.Add(selection);

        promptTextBox.Text = string.Join(", ", prompts);
    }
}