using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Markdig;

namespace MDocReader
{
    public partial class MainWindow : Window
    {
        private string mainPath = "Home.md";
        private string mainFragment = "";
        private string sidebarPath = "_Sidebar.md";
        private string footerPath = "_Footer.md";

        public MainWindow()
        {
            InitializeComponent();

            string currentDirectory = Directory.GetCurrentDirectory();
            string folderName = Path.GetFileName(currentDirectory);
            this.Title = folderName;

            SetBrowserFeatureControl();
            LoadMarkdownFiles();
            MainWebBrowser.Navigating += WebBrowserNavigating;
            SidebarWebBrowser.Navigating += WebBrowserNavigating;
            FooterWebBrowser.Navigating += WebBrowserNavigating;
        }

        private void LoadMarkdownFiles()
        {
            string mainFilePath = mainPath;
            if (File.Exists(mainFilePath))
            {
                string mainMarkdown = File.ReadAllText(mainFilePath);
                MainWebBrowser.NavigateToString(ConvertMarkdownToHtml(mainMarkdown, mainFragment));
            }

            string sidebarFilePath = sidebarPath;
            if (File.Exists(sidebarFilePath))
            {
                string sidebarMarkdown = File.ReadAllText(sidebarFilePath);
                if (!string.IsNullOrWhiteSpace(sidebarMarkdown))
                {
                    SidebarWebBrowser.NavigateToString(ConvertMarkdownToHtml(sidebarMarkdown));
                    SidebarWebBrowser.Visibility = Visibility.Visible;
                    SidebarGridSplitter.Visibility = Visibility.Visible;
                }
            }

            string footerFilePath = footerPath;
            if (File.Exists(footerFilePath))
            {
                string footerMarkdown = File.ReadAllText(footerFilePath);
                if (!string.IsNullOrWhiteSpace(footerMarkdown))
                {
                    FooterWebBrowser.NavigateToString(ConvertMarkdownToHtml(footerMarkdown));
                    FooterWebBrowser.Visibility = Visibility.Visible;
                    FooterGridSplitter.Visibility = Visibility.Visible;
                }
            }
        }

        private void WebBrowserNavigating(object sender, NavigatingCancelEventArgs e)
        {
            if (e.Uri == null)
            {
                return;
            }
            e.Cancel = true;
            string url = e.Uri.AbsolutePath;
            string urlWithMD = $"{e.Uri.AbsolutePath}.md";
            if (!string.IsNullOrEmpty(urlWithMD) && File.Exists(urlWithMD))
            {
                string mainMarkdown = File.ReadAllText(urlWithMD);
                if (url != "Home")
                {
                    mainMarkdown = $"# {url}\n{mainMarkdown}";
                }
                mainPath = urlWithMD;
                mainFragment = e.Uri.Fragment;
                MainWebBrowser.NavigateToString(ConvertMarkdownToHtml(mainMarkdown, e.Uri.Fragment));
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
            }
        }

        private string ConvertMarkdownToHtml(string markdown, string fragment = null)
        {
            string scrollScript = string.Empty;
            if (!string.IsNullOrEmpty(fragment))
            {
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

            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            string htmlContent = Markdown.ToHtml(markdown, pipeline);

            return $@"
{GetHtmlStart()}
<body>
    {htmlContent}
    {scrollScript}
</body>
</html>";
        }

        private string GetHtmlStart()
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

        private void SetBrowserFeatureControl()
        {
            string appName = System.IO.Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
                $@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"))
            {
                key?.SetValue(appName, 11001, Microsoft.Win32.RegistryValueKind.DWord); // 11001 对应 IE11
            }
        }

        private void WindowKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (e.Key == Key.T)
                {
                    ChangeTheme();
                }
            }
        }

        private void ChangeTheme()
        {
            ThemeHelper.Theme = ThemeHelper.Theme == Theme.Light ? Theme.Dark : Theme.Light;
            LoadMarkdownFiles();
            SidebarGridSplitter.Background = new SolidColorBrush(ThemeHelper.ParseRgbString(ThemeHelper.GridSplitterBackColor));
            FooterGridSplitter.Background = new SolidColorBrush(ThemeHelper.ParseRgbString(ThemeHelper.GridSplitterBackColor));
            SidebarGridSplitter.BorderBrush = new SolidColorBrush(ThemeHelper.ParseRgbString(ThemeHelper.GridSplitterBorder));
            FooterGridSplitter.BorderBrush = new SolidColorBrush(ThemeHelper.ParseRgbString(ThemeHelper.GridSplitterBorder));
        }
    }
}