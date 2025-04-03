using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Markdig.Extensions.AutoIdentifiers;
using Markdig;
using System.IO;

namespace MDocReader
{
    internal class MDHelper
    {
        internal static string GetMarkdownFileNames()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            string[] markdownFiles = Directory.GetFiles(currentDirectory, "*.md");
            string result = "";
            foreach (string filePath in markdownFiles)
            {
                string fileName = Path.GetFileName(filePath);
                if (fileName == "_Sidebar.md" || fileName == "_Footer.md")
                {
                    continue;
                }
                result += $"- [{fileName}]({Path.GetFileNameWithoutExtension(filePath)})\n";
            }

            return result;
        }

        internal static string ConvertMarkdownToHtml(string markdown, string fragment = null)
        {
            string scrollScript = string.Empty;
            if (!string.IsNullOrEmpty(fragment))
            {
                fragment = Uri.UnescapeDataString(fragment);
                scrollScript = $@"
<script>
    (function() {{
        // Wait for the DOM to be fully loaded
        document.addEventListener('DOMContentLoaded', function() {{
            // Extract the fragment and remove the leading '#' if it exists
            var targetId = '{fragment}'.charAt(0) === '#' ? '{fragment}'.substring(1) : '{fragment}';
            
            // Find the target element by ID
            var targetElement = document.getElementById(targetId);
            
            // If the element exists, scroll to it
            if (targetElement) {{
                targetElement.scrollIntoView({{
                    behavior: 'smooth', // Smooth scrolling (ignored in older IE versions)
                    block: 'start'     // Align to the top of the element
                }});
            }}
        }});
    }})();
</script>";
            }

            var pipeline = new MarkdownPipelineBuilder().UseAutoIdentifiers(AutoIdentifierOptions.GitHub).UseAdvancedExtensions().Build();
            string htmlContent = Markdown.ToHtml(markdown, pipeline);

            return $@"
{GetHtmlStart()}
<body>
    {htmlContent}
    {scrollScript}
</body>
</html>";
        }

        internal static string GetHtmlStart()
        {
            return $@"<!DOCTYPE html>
                    <html lang=""en"">
                    <head>
                        <meta charset=""UTF-8"">
                        <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                        <title>Document</title>
                        <style>
                            html {{
                                overflow: auto;
                                {ThemeHelper.ScrollbarStyleText}
                            }}

                            body {{
                                background-color: {ThemeHelper.BackgroundColorText};
                                color: {ThemeHelper.ForegroundColorText};
                            }}

                            a {{
                                text-decoration: none;
                                color: {ThemeHelper.ForegroundColorLink};
                            }}

                            a:visited {{
                                color: {ThemeHelper.ForegroundColorLink};
                            }}

                            p, ul, ol {{
                                margin-top: 0.5em;
                                margin-bottom: 0.5em;
                            }}

                            code {{
                                background-color: {ThemeHelper.CodeBackgroundColor};
                                color: {ThemeHelper.PreForegroundColorText};
                                padding: 0.2em 0.4em;
                                border-radius: 4px;
                                font-family: Consolas, ""Courier New"", monospace;
                                font-size: 0.95em;
                                display: inline-block;
                                white-space: pre;
                                vertical-align: middle;
                                overflow: hidden;
                            }}

                            pre code {{
                                display: block;
                                padding: 1em;
                                overflow-x: auto;
                            }}

                            pre {{
                                background-color: {ThemeHelper.PreBackgroundColorText};
                                color: {ThemeHelper.PreForegroundColorText};
                                padding: 0.2em;
                                border-radius: 4px;
                                font-family: Consolas, ""Courier New"", monospace;
                                font-size: 0.95em;
                                overflow: auto;
                            }}

                            table {{
                                width: 100%;
                                border-collapse: collapse;
                                margin: 1em 0;
                                background-color: {ThemeHelper.PreBackgroundColorText};
                                color: {ThemeHelper.PreForegroundColorText};
                                font-family: Arial, sans-serif;
                                font-size: 0.95em;
                            }}

                            th, td {{
                                padding: 0.6em 0.8em;
                                border: 1px solid #444;
                                text-align: left;
                            }}

                            th {{
                                background-color: {ThemeHelper.TableHeaderBackgroundColorText};
                                font-weight: bold;
                            }}

                            tr:nth-child(even) {{
                                background-color: {ThemeHelper.PreEvenBackgroundColorText};
                            }}

                            tr:hover {{
                                background-color: {ThemeHelper.TableHoverBackgroundColorText};
                            }}
                        </style>

                    </head>";
        }
    }
}
