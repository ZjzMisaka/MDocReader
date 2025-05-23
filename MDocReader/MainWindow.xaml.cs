using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Xml.Linq;

namespace MDocReader
{
    public partial class MainWindow : Window
    {
        private string _mainPath = ".\\Home.md";
        private string _mainFragment = "";
        private string _sidebarPath = ".\\_Sidebar.md";
        private string _footerPath = ".\\_Footer.md";

        private List<History> _history = new List<History>();
        private int _historyIndex = -1;

        private double _fileListWebBrowserWidth = 250;

        public MainWindow()
        {
            InitializeComponent();

            if (ExeResourceManager.GetPersistedFilesList() != null)
            {
                MDHelper.Persistenced = true;
                this.Title = Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule.FileName);
                SaveThemeBtn.Visibility = Visibility.Collapsed;
            }
            else
            {
                string currentDirectory = Directory.GetCurrentDirectory();
                string folderName = Path.GetFileName(currentDirectory);
                this.Title = folderName;
            }

            SetBrowserFeatureControl();
            LoadMarkdownFiles();
            MainWebBrowser.Navigating += WebBrowserNavigating;
            SidebarWebBrowser.Navigating += WebBrowserNavigating;
            FooterWebBrowser.Navigating += WebBrowserNavigating;
            FileListWebBrowser.Navigating += WebBrowserNavigating;
        }

        private void AddHistory(History history)
        {
            History last = _history.LastOrDefault();
            if (last != default && last.Path == history.Path && last.Fragment == history.Fragment)
            {
                return;
            }
            _history.Add(history);
            ++_historyIndex;
        }

        private void LoadMarkdownFiles()
        {
            string mainFilePath = _mainPath;
            if (MDHelper.FileNameExist(mainFilePath))
            {
                AddHistory(new History(_mainPath, _mainFragment));
                string mainMarkdown = ReadFile(mainFilePath);
                MainWebBrowser.NavigateToString(MDHelper.ConvertMarkdownToHtml(mainMarkdown, _mainFragment));
            }

            string sidebarFilePath = _sidebarPath;
            if (MDHelper.FileNameExist(sidebarFilePath))
            {
                string sidebarMarkdown = ReadFile(sidebarFilePath);
                if (!string.IsNullOrWhiteSpace(sidebarMarkdown))
                {
                    DocGrid.ColumnDefinitions[2].Width = new GridLength(250, GridUnitType.Pixel);
                    SidebarWebBrowser.NavigateToString(MDHelper.ConvertMarkdownToHtml(sidebarMarkdown));
                    SidebarWebBrowser.Visibility = Visibility.Visible;
                    SidebarGridSplitter.Visibility = Visibility.Visible;
                }
            }

            string footerFilePath = _footerPath;
            if (MDHelper.FileNameExist(footerFilePath))
            {
                string footerMarkdown = ReadFile(footerFilePath);
                if (!string.IsNullOrWhiteSpace(footerMarkdown))
                {
                    DocGrid.RowDefinitions[2].Height = new GridLength(35, GridUnitType.Pixel);
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
            string urlWithMD = $"{url}.md".Replace('/', Path.DirectorySeparatorChar);
            if (!urlWithMD.StartsWith($".{Path.DirectorySeparatorChar}"))
            {
                urlWithMD = $".{Path.DirectorySeparatorChar}{urlWithMD}";
            }
            if (!string.IsNullOrEmpty(urlWithMD) && MDHelper.FileNameExist(urlWithMD))
            {
                if (_historyIndex < _history.Count - 1)
                {
                    _history.RemoveRange(_historyIndex + 1, _history.Count - (_historyIndex + 1));
                }

                string mainMarkdown = ReadFile(urlWithMD);
                if (url != "Home")
                {
                    mainMarkdown = $"# {Path.GetFileName(url)}\n{mainMarkdown}";
                }
                _mainPath = urlWithMD;
                _mainFragment = e.Uri.Fragment;
                AddHistory(new History(_mainPath, _mainFragment));
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
                key?.SetValue(appName, 11001, Microsoft.Win32.RegistryValueKind.DWord);
            }
        }

        private void WindowKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (e.Key == Key.T)
                {
                    ChangeTheme();
                    e.Handled = true;
                }
                else if ((Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) && e.Key == Key.S)
                {
                    Save();
                    e.Handled = true;
                }
                else if (e.Key == Key.Left)
                {
                    Back();
                    e.Handled = true;
                }
                else if (e.Key == Key.Right)
                {
                    Forward();
                    e.Handled = true;
                }
                else if (e.Key == Key.B)
                {
                    SwitchFileList();
                    e.Handled = true;
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
            SaveThemeBtn.Background = new ImageBrush(new BitmapImage(ThemeHelper.SaveBtnUri));
        }

        private void FileListBtnClick(object sender, RoutedEventArgs e)
        {
            SwitchFileList();
        }

        private void SwitchFileList()
        {
            FileListWebBrowser.Visibility = FileListWebBrowser.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            FileListGridSplitter.Visibility = FileListWebBrowser.Visibility;

            if (FileListWebBrowser.Visibility == Visibility.Visible)
            {
                FileListWebBrowserColumn.Width = new GridLength(_fileListWebBrowserWidth);
            }
            else
            {
                _fileListWebBrowserWidth = FileListWebBrowserColumn.Width.Value;
                FileListWebBrowserColumn.Width = new GridLength(0);
            }

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
            Back();
        }

        private void ForwardBtnClick(object sender, RoutedEventArgs e)
        {
            Forward();
        }

        private void Back()
        {
            if (_historyIndex - 1 < 0)
            {
                return;
            }
            --_historyIndex;
            _mainPath = _history[_historyIndex].Path;
            _mainFragment = _history[_historyIndex].Fragment;
            string mainMarkdown = ReadFile(_mainPath);
            string url = Path.GetFileNameWithoutExtension(_mainPath);
            if (url != "Home")
            {
                mainMarkdown = $"# {Path.GetFileName(url)}\n{mainMarkdown}";
            }
            MainWebBrowser.NavigateToString(MDHelper.ConvertMarkdownToHtml(mainMarkdown, _mainFragment));
        }

        private void Forward()
        {
            if (_historyIndex + 1 > _history.Count - 1)
            {
                return;
            }
            ++_historyIndex;
            _mainPath = _history[_historyIndex].Path;
            _mainFragment = _history[_historyIndex].Fragment;
            string mainMarkdown = ReadFile(_mainPath);
            string url = Path.GetFileNameWithoutExtension(_mainPath);
            if (url != "Home")
            {
                mainMarkdown = $"# {Path.GetFileName(url)}\n{mainMarkdown}";
            }
            MainWebBrowser.NavigateToString(MDHelper.ConvertMarkdownToHtml(mainMarkdown, _mainFragment));
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void Save()
        {
            if (MDHelper.Persistenced)
            {
                return;
            }
            Dictionary<string, string> filesToPersist = new Dictionary<string, string>();
            string currentDirectory = Directory.GetCurrentDirectory();
            List<string> fileList = Directory.GetFiles(currentDirectory, "*.md", SearchOption.AllDirectories).ToList();
            foreach (var file in fileList)
            {
                string shortFileName = MDHelper.GetRelativePath(Directory.GetCurrentDirectory(), file);
                filesToPersist.Add(shortFileName, File.ReadAllText(file));
            }
            List<string> imageFiles = MDHelper.GetImageFilesInCurrentDirectory();
            foreach (var file in imageFiles)
            {
                string shortFileName = file;
                byte[] pngBytes = File.ReadAllBytes(shortFileName);
                string base64String = Convert.ToBase64String(pngBytes);
                filesToPersist.Add(shortFileName, base64String);
            }
            ExeResourceManager.PersistTextFiles(filesToPersist);
        }

        private string ReadFile(string name)
        {
            if (MDHelper.Persistenced)
            {
                return ExeResourceManager.ReadTextFile(name);
            }
            else
            {
                return File.ReadAllText(name);
            }
        }
    }
}