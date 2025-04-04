using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security.Policy;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using static System.Net.Mime.MediaTypeNames;

namespace MDocReader
{
    public partial class MainWindow : Window
    {
        private string _mainPath = "Home.md";
        private string _mainFragment = "";
        private string _sidebarPath = "_Sidebar.md";
        private string _footerPath = "_Footer.md";

        private List<string> _history = new List<string>();
        private int _historyIndex = -1;

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
            FileListWebBrowser.Navigating += WebBrowserNavigating;
        }

        private void LoadMarkdownFiles()
        {
            string mainFilePath = _mainPath;
            if (File.Exists(mainFilePath))
            {
                _history.Add(mainFilePath);
                ++_historyIndex;
                string mainMarkdown = File.ReadAllText(mainFilePath);
                MainWebBrowser.NavigateToString(MDHelper.ConvertMarkdownToHtml(mainMarkdown, _mainFragment));
            }

            string sidebarFilePath = _sidebarPath;
            if (File.Exists(sidebarFilePath))
            {
                string sidebarMarkdown = File.ReadAllText(sidebarFilePath);
                if (!string.IsNullOrWhiteSpace(sidebarMarkdown))
                {
                    SidebarWebBrowser.NavigateToString(MDHelper.ConvertMarkdownToHtml(sidebarMarkdown));
                    SidebarWebBrowser.Visibility = Visibility.Visible;
                    SidebarGridSplitter.Visibility = Visibility.Visible;
                }
            }

            string footerFilePath = _footerPath;
            if (File.Exists(footerFilePath))
            {
                string footerMarkdown = File.ReadAllText(footerFilePath);
                if (!string.IsNullOrWhiteSpace(footerMarkdown))
                {
                    FooterWebBrowser.NavigateToString(MDHelper.ConvertMarkdownToHtml(footerMarkdown));
                    FooterWebBrowser.Visibility = Visibility.Visible;
                    FooterGridSplitter.Visibility = Visibility.Visible;
                }
            }

            ShowFileList();
        }

        private void WebBrowserNavigating(object sender, NavigatingCancelEventArgs e)
        {
            if (e.Uri == null)
            {
                return;
            }
            e.Cancel = true;
            string url = Uri.UnescapeDataString(e.Uri.AbsolutePath);
            string urlWithMD = $"{e.Uri.AbsolutePath}.md";
            if (!string.IsNullOrEmpty(urlWithMD) && File.Exists(urlWithMD))
            {
                if (_historyIndex < _history.Count - 1)
                {
                    _history.RemoveRange(_historyIndex + 1, _history.Count - (_historyIndex + 1));
                }

                _history.Add(urlWithMD);
                ++_historyIndex;
                string mainMarkdown = File.ReadAllText(urlWithMD);
                if (url != "Home")
                {
                    mainMarkdown = $"# {url}\n{mainMarkdown}";
                }
                _mainPath = urlWithMD;
                _mainFragment = e.Uri.Fragment;
                MainWebBrowser.NavigateToString(MDHelper.ConvertMarkdownToHtml(mainMarkdown, e.Uri.Fragment));
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
            FileListGridSplitter.Background = new SolidColorBrush(ThemeHelper.ParseRgbString(ThemeHelper.GridSplitterBackColor));
            SidebarGridSplitter.BorderBrush = new SolidColorBrush(ThemeHelper.ParseRgbString(ThemeHelper.GridSplitterBorder));
            FooterGridSplitter.BorderBrush = new SolidColorBrush(ThemeHelper.ParseRgbString(ThemeHelper.GridSplitterBorder));
            FileListGridSplitter.BorderBrush = new SolidColorBrush(ThemeHelper.ParseRgbString(ThemeHelper.GridSplitterBorder));

            ToolBar.Background = new SolidColorBrush(ThemeHelper.ParseRgbString(ThemeHelper.BackgroundColorToolBar));
            BackBtn.Background = new ImageBrush(new BitmapImage(ThemeHelper.BackBtnUri));
            ForwardBtn.Background = new ImageBrush(new BitmapImage(ThemeHelper.ForwardBtnUri));
            FileListBtn.Background = new ImageBrush(new BitmapImage(ThemeHelper.FileListBtnUri));
            ChangeThemeBtn.Background = new ImageBrush(new BitmapImage(ThemeHelper.ChangeThemeBtnUri));
        }

        private void FileListBtnClick(object sender, RoutedEventArgs e)
        {
            FileListWebBrowser.Visibility = FileListWebBrowser.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            FileListGridSplitter.Visibility = FileListWebBrowser.Visibility;

            ShowFileList();
        }

        private void ShowFileList()
        {
            if (FileListWebBrowser.Visibility == Visibility.Visible)
            {
                FileListWebBrowser.NavigateToString(MDHelper.ConvertMarkdownToHtml(MDHelper.GetMarkdownFileNames()));
            }
        }

        private void ChangeTheme(object sender, RoutedEventArgs e)
        {
            ChangeTheme();
        }

        private void BackBtnClick(object sender, RoutedEventArgs e)
        {
            if (_historyIndex - 1 < 0)
            {
                return;
            }
            _mainPath = _history[--_historyIndex];
            string mainMarkdown = File.ReadAllText(_mainPath);
            string url = Path.GetFileNameWithoutExtension(_mainPath);
            if (url != "Home")
            {
                mainMarkdown = $"# {url}\n{mainMarkdown}";
            }
            MainWebBrowser.NavigateToString(MDHelper.ConvertMarkdownToHtml(mainMarkdown, _mainFragment));
        }

        private void ForwardBtnClick(object sender, RoutedEventArgs e)
        {
            if (_historyIndex + 1 > _history.Count - 1)
            {
                return;
            }
            _mainPath = _history[++_historyIndex];
            string mainMarkdown = File.ReadAllText(_mainPath);
            string url = Path.GetFileNameWithoutExtension(_mainPath);
            if (url != "Home")
            {
                mainMarkdown = $"# {url}\n{mainMarkdown}";
            }
            MainWebBrowser.NavigateToString(MDHelper.ConvertMarkdownToHtml(mainMarkdown, _mainFragment));
        }
    }
}