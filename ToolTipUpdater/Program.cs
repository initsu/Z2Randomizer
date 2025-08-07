// See https://aka.ms/new-console-template for more information
using System.Security;
using System.Text;
using System.Text.RegularExpressions;


string viewDirectory = @"O:\source\Z2Randomizer\CrossPlatformUI\Views\Tabs";
string[] files = Directory.GetFiles(viewDirectory, "*.axaml");
string resourceFile = @"O:\source\Z2Randomizer\CrossPlatformUI\Lang\Resources.resx";
string wikiDirectory = @"O:\source\Z2Randomizer\ToolTipUpdater\Z2Randomizer.wiki";

// (?<=\\|/)      ← positive look-behind: ensure we start just after a slash or backslash
// ([^\\\/]+?)    ← capture one or more chars that are not slash/backslash, as few as possible
// (?=View        ← positive look-ahead: next must be "View"
//     (?:\..+)?$ ← optionally followed by a dot+ext, then end of string
// )  
Regex tabNameRegex = new Regex(@"(?<=\\|/)([^\\\/]+?)(?=View(?:\..+)?$)");

Regex markdownHeaderRegex = new Regex(@"^(#{1,6})\s+(.*?)\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

foreach (string file in files)
{
    Console.WriteLine($"Processing: {file}");

    var m = tabNameRegex.Match(file);
    if (m.Success)
    {
        string viewName = m.Groups[1].Value;
        string wikiFile = $"{wikiDirectory}\\{viewName}-Configuration-Reference.md";
        Console.WriteLine($"Input: {file} Tab: \"{viewName}\" Wiki: \"{wikiFile}\"");
        UpdateXmlTooltips(file, resourceFile, wikiFile);
    }
    else
    {
        Console.WriteLine($"Unrecognized view axaml {file}");
    }
}
UpdateXmlTooltips(@$"{viewDirectory}\SpritePreviewView.axaml", resourceFile, $@"{wikiDirectory}\Customize-Configuration-Reference.md");

static string WordWrapAndStyle(string text, int maxLineLength)
{
    var outputLines = new List<string>();
    var inputLines = text.Split(["\r\n", "\n"], StringSplitOptions.None);

    foreach (var inputLine in inputLines)
    {
        // Detect leading whitespace (indentation)
        var indentMatch = Regex.Match(inputLine, @"^([\s-]*)");
        bool bullet = indentMatch.Value.Contains("-");
        string indent = new string(' ', indentMatch.Length + (bullet ? 2 : 0));

        // Trim leading whitespace for word splitting
        var trimmedLine = inputLine.TrimStart(['-', ' ', '\t']);

        // If the trimmed line is empty, preserve the blank line
        if (trimmedLine.Length == 0)
        {
            outputLines.Add(string.Empty);
            continue;
        }

        var words = trimmedLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var line = new StringBuilder(indent);
        if (bullet && indent.Length > 1)
        {
            var bulletChar = indent.Length % 4 == 0 ? '•' : '◦';
            line[indent.Length - 2] = bulletChar;
        }
        int currentLength = indent.Length;

        foreach (var word in words)
        {
            if (currentLength + word.Length + 1 > maxLineLength)
            {
                outputLines.Add(line.ToString());
                line.Clear();
                line.Append(indent);
                currentLength = indent.Length;
            }

            if (currentLength > indent.Length)
            {
                line.Append(' ');
                currentLength++;
            }

            line.Append(word);
            currentLength += word.Length;
        }

        if (line.Length > 0)
            outputLines.Add(line.ToString());
    }

    return string.Join(Environment.NewLine, outputLines);
}

string? GetMarkdownSectionText(string markdownContent, string headerToFind)
{
    var headerPattern = markdownHeaderRegex;
    var matches = headerPattern.Matches(markdownContent);

    int startIndex = -1;
    int endIndex = markdownContent.Length;

    for (int i = 0; i < matches.Count; i++)
    {
        string headerText = matches[i].Groups[2].Value.Trim();

        /* No sub headers:
        if (string.Equals(headerText, headerToFind))
        {
            startIndex = matches[i].Index + matches[i].Length;
            if (i + 1 < matches.Count)
            {
                endIndex = matches[i + 1].Index;
            }
            break;
        }*/
        if (string.Equals(headerText, headerToFind, StringComparison.OrdinalIgnoreCase))
        {
            startIndex = matches[i].Index + matches[i].Length;

            // Find the next header of same or higher level
            for (int j = i + 1; j < matches.Count; j++)
            {
                if (matches[j].Groups[1].Length <= matches[i].Groups[1].Length)
                {
                    endIndex = matches[j].Index;
                    break;
                }
            }

            break;
        }
    }

    if (startIndex == -1)
    {
        return null; // Header not found
    }

    return markdownContent.Substring(startIndex, endIndex - startIndex).Trim();
}

void UpdateXmlTooltips(string xmlFilePath, string resourceFilePath, string wikiFilePath)
{
    string viewXml = File.ReadAllText(xmlFilePath);
    string resourceXml = File.ReadAllText(resourceFilePath);
    string wikiMarkdown;
    try
    {
        wikiMarkdown = File.ReadAllText(wikiFilePath);
    }
    catch(FileNotFoundException)
    {
        Console.WriteLine($"No wiki file found {wikiFilePath}");
        return;
    }

    // Match multi-line CheckBox tags (opening tag only)
    //string checkboxPattern1 = @"<CheckBox\b[^>]*?(\/>)";  // match up to the end of the start tag
    //var checkboxRegex1 = new Regex(checkboxPattern1, RegexOptions.Singleline | RegexOptions.IgnoreCase);

    var elementRegex = new Regex(@"<(CheckBox|ComboBox|StackPanel)\b[^>]*>(.*?)</\1>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
    var elementLabelRegex = new Regex(@"^[^>]*(?:Content|assists:ComboBoxAssist.Label)\s*=\s*""([^""]+)""", RegexOptions.Singleline);
    var textBlockRegex = new Regex(@"[^<]*<TextBlock\b.*?>(.*?)</TextBlock>", RegexOptions.Singleline);

    bool modified = false;
    string updatedViewXml = viewXml;
    string updatedResourceXml = resourceXml;

    int resourceInsertPos = GetXmlDataInsertPos(updatedResourceXml);

    int position = 0;
    while (position < updatedViewXml.Length)
    {
        var elementMatch = elementRegex.Match(updatedViewXml, position);
        if (!elementMatch.Success) { break; }
        position = elementMatch.Index + 1;

        string elementBlock = elementMatch.Value;
        string elementInner = elementMatch.Groups[2].Value;

        string elementLabel;
        var elementLabelMatch = elementLabelRegex.Match(elementBlock);
        if (elementLabelMatch.Success)
        {
            elementLabel = elementLabelMatch.Groups[1].Value;
        }
        else
        {
            var textBlockMatch = textBlockRegex.Match(elementInner);
            if (!textBlockMatch.Success)
            {
                Console.WriteLine("CheckBox with no content?");
                Console.WriteLine($"Block: {elementBlock}");
                continue;
            }
            elementLabel = textBlockMatch.Groups[1].Value;
        }
        // Clean up label
        elementLabel = Regex.Replace(elementLabel, @"<[^>]*>", " ");
        elementLabel = elementLabel.Trim();

        var stringId = Regex.Replace(elementLabel, @"[\s()/\.'+-]", "") + "ToolTip";

        string? descUnescaped = GetMarkdownSectionText(wikiMarkdown, elementLabel);
        if (descUnescaped == null)
        {
            Console.WriteLine($"No ToolTip found for {elementLabel}");
            continue;
        }

        // Ignore options line from Wiki (as you can see the options in the UI)
        descUnescaped = Regex.Replace(descUnescaped, @"^Options:.+", "");
        /*
        var wikiLinkRegex = new Regex(@"(?<=(?:^|\n|[.?!]\s))[^.\n]*?\[\[([^[\]]+)\]\].*$", RegexOptions.Multiline);
        descUnescaped = wikiLinkRegex.Replace(descUnescaped, match =>
        {
            var wikiPage = match.Groups[1].Value.Replace(" ", "-");
            string embedWikiFile = $"{wikiDirectory}\\{wikiPage}.md";
            string embedWikiMarkdown;
            try
            {
                embedWikiMarkdown = File.ReadAllText(embedWikiFile);
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine($"No Wiki file found {embedWikiFile}");
                return "(Error including reference)";
            }
            return "\n" + embedWikiMarkdown;
        });*/

        // Replace wiki links
        descUnescaped = Regex.Replace(descUnescaped, @"\[[^\]]+\]+", "the Wiki");
        descUnescaped = descUnescaped.Trim();

        // Line wrap so we don't get really wide tooltips
        string wrappedDesc = WordWrapAndStyle(descUnescaped, 90);
        // Make string XML-safe
        string escapedDesc = SecurityElement.Escape(wrappedDesc);
        // Style headers
        //escapedDesc = markdownHeaderRegex.Replace(escapedDesc, "<Run FontWeight=\"Bold\" Text=\"$2\" />");
        escapedDesc = markdownHeaderRegex.Replace(escapedDesc, "$2:");
        // Handle line breaks
        //escapedDesc = escapedDesc.Replace("\n", "<LineBreak/>");
        //escapedDesc = escapedDesc.Replace("\r", "");

        var newResourceStr = $"\t<data name=\"{stringId}\" xml:space=\"preserve\">\r\n<value>{escapedDesc}</value>\r\n\t</data>";
        // If entry already exists, replace its content
        var resStartTag = $"[\t ]*<\\s*data\\s+name=\"{Regex.Escape(stringId)}\"[^>]*>";
        var xmlMatch = Regex.Match(updatedResourceXml, resStartTag, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (xmlMatch.Success)
        {
            var regex = new Regex(resStartTag + @".*?<\s*\/\s*data\s*>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var maybeUpdatedString = regex.Replace(updatedResourceXml, newResourceStr, 1, xmlMatch.Index);
            if (maybeUpdatedString != updatedResourceXml)
            {
                updatedResourceXml = maybeUpdatedString;
                modified = true;
                Console.WriteLine("Updating resource file tooltip");
            }
            resourceInsertPos = xmlMatch.Index + newResourceStr.Length; // set new insertion point
        }
        else
        {
            // Inject new resource entry
            string insertion = $"{newResourceStr}\n";
            updatedResourceXml = updatedResourceXml.Insert(resourceInsertPos, insertion);
            modified = true;
            Console.WriteLine("Inserting tooltip");
            resourceInsertPos += insertion.Length;
        }

        var newViewStr = $"<ToolTip.Tip><TextBlock Text=\"{{x:Static lang:Resources.{stringId}}}\"/></ToolTip.Tip>";
        // If <ToolTip.Tip> already exists, replace its content
        string updatedBlock;
        if (Regex.IsMatch(elementInner, @"<\s*ToolTip\.Tip\s*>", RegexOptions.Singleline | RegexOptions.IgnoreCase))
        {
            string updatedInner = Regex.Replace(elementInner,
                @"<\s*ToolTip\.Tip\s*>.*?<\s*\/\s*ToolTip\.Tip\s*>",
                newViewStr,
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

            updatedBlock = elementBlock.Replace(elementInner, updatedInner);
            modified = true;
            Console.WriteLine("Updating tooltip");
        }
        else
        {
            // Inject new <ToolTip.Tip> element after the opening tag
            string insertion = $"{newViewStr}\n";
            int insertPos = elementBlock.IndexOf('>') + 1;
            updatedBlock = elementBlock.Insert(insertPos, insertion);
            modified = true;
            Console.WriteLine("Inserting tooltip");
        }
        updatedViewXml = updatedViewXml.Replace(elementBlock, updatedBlock);
    }

    if (modified)
    {
        File.WriteAllText(xmlFilePath, updatedViewXml);
        Console.WriteLine("File updated: " + xmlFilePath);
        File.WriteAllText(resourceFilePath, updatedResourceXml);
        Console.WriteLine("File updated: " + resourceFilePath);
    }
    else
    {
        Console.WriteLine("No changes made: " + xmlFilePath);
    }
}

int GetXmlDataInsertPos(string data)
{
    var regex = new Regex(@"</data>(?![\s\S]*</data>)");
    var match = regex.Match(data);
    if (match.Success)
    {
        return match.Index + match.Length;
    }
    var regex2 = new Regex(@"</root>(?![\s\S]*</root>)");
    var match2 = regex.Match(data);
    if (match2.Success)
    {
        return match2.Index + match2.Length;
    }
    return data.Length;
}
