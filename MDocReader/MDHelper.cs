using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Markdig.Extensions.AutoIdentifiers;
using Markdig;
using System.IO;
using System.Windows.Documents;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MDocReader
{
    internal class MDHelper
    {
        internal static bool Persistenced { get; set; }

        internal static List<string> GetFilesList()
        {
            if (Persistenced)
            {
                return ExeResourceManager.GetPersistedFilesList();
            }
            else
            {
                string currentDirectory = Directory.GetCurrentDirectory();
                return Directory.GetFiles(currentDirectory, "*.md").ToList();
            }
        }

        internal static List<string> GetImageFilesInCurrentDirectory()
        {
            string currentDirectory = Directory.GetCurrentDirectory();

            string[] imageExtensions = { ".jpeg", ".jpg", ".png", ".gif", ".bmp", ".svg", ".ico" };

            List<string> imageFiles = new List<string>();

            foreach (var file in Directory.EnumerateFiles(currentDirectory, "*.*", SearchOption.AllDirectories))
            {
                if (Array.Exists(imageExtensions, ext => ext.Equals(Path.GetExtension(file), StringComparison.OrdinalIgnoreCase)))
                {
                    imageFiles.Add(GetRelativePath(currentDirectory, file));
                }
            }

            return imageFiles;
        }

        private static string GetRelativePath(string basePath, string fullPath)
        {
            if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                basePath += Path.DirectorySeparatorChar;
            }

            Uri baseUri = new Uri(basePath, UriKind.Absolute);
            Uri fullUri = new Uri(fullPath, UriKind.Absolute);

            Uri relativeUri = baseUri.MakeRelativeUri(fullUri);

            return Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', Path.DirectorySeparatorChar);
        }

        internal static string GetMarkdownFileNames()
        {
            List<string> markdownFiles = GetFilesList();
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
            htmlContent = ProcessImgTags(htmlContent);

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

        private static string ProcessImgTags(string htmlContent)
        {
            if (string.IsNullOrWhiteSpace(htmlContent))
            {
                return htmlContent;
            }

            string imgTagPattern = @"<img\s+[^>]*src=['""]([^'""]+)['""][^>]*>";
            var matches = Regex.Matches(htmlContent, imgTagPattern);

            foreach (Match match in matches)
            {
                string imgTag = match.Value;
                string srcValue = match.Groups[1].Value;
                string srcValueSearch = srcValue;

                if (srcValueSearch.StartsWith("./"))
                {
                    srcValueSearch = srcValueSearch.Substring(2);
                }
                srcValueSearch = srcValueSearch.Replace("/", "\\");

                if (!MDHelper.Persistenced && FileNameExist(srcValue))
                {
                    string fullPath = Path.GetFullPath(srcValue);
                    string newSrc = "file:///" + fullPath.Replace("\\", "/");
                    string updatedImgTag = imgTag.Replace(srcValue, newSrc);

                    htmlContent = htmlContent.Replace(imgTag, updatedImgTag);
                }
                if (MDHelper.Persistenced && FileNameExist(srcValueSearch))
                {
                    string base64String = ExeResourceManager.ReadTextFile(srcValueSearch);
                    string updatedImgTag = imgTag.Replace(srcValue, $"data:image/{Path.GetExtension(srcValue)};base64,{base64String}");
                    htmlContent = htmlContent.Replace(imgTag, updatedImgTag);
                }
            }

            return htmlContent;
        }

        public static bool FileNameExist(string name)
        {
            if (MDHelper.Persistenced)
            {
                return ExeResourceManager.GetPersistedFilesList().Contains(name);
            }
            else
            {
                return File.Exists(name);
            }
        }
    }
}
