using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MDocReader
{
    public enum Theme { Light, Dark }
    internal static class ThemeHelper
    {
        public static Theme Theme { get; set; } = Theme.Light;

        public static string BackgroundColorToolBar
        {
            get
            {
                if (Theme == Theme.Dark)
                {
                    return "rgb(67, 67, 67)";
                }
                else
                {
                    return "rgb(245, 245, 245)";
                }
            }
        }

        public static string BackgroundColorText
        {
            get
            {
                if (Theme == Theme.Dark)
                {
                    return "rgb(57, 57, 57)";
                }
                else
                {
                    return "rgb(255, 255, 255)";
                }
            }
        }

        public static string ForegroundColorText
        {
            get
            {
                if (Theme == Theme.Dark)
                {
                    return "rgb(255, 255, 255)";
                }
                else
                {
                    return "rgb(0, 0, 0)";
                }
            }
        }

        public static string PreBackgroundColorText
        {
            get
            {
                if (Theme == Theme.Dark)
                {
                    return "rgb(45, 45, 45)";
                }
                else
                {
                    return "rgb(252, 252, 252)";
                }
            }
        }

        public static string CodeBackgroundColor
        {
            get
            {
                if (Theme == Theme.Dark)
                {
                    return "rgb(45, 45, 45)";
                }
                else
                {
                    return "rgb(242, 242, 242)";
                }
            }
        }

        public static string PreEvenBackgroundColorText
        {
            get
            {
                if (Theme == Theme.Dark)
                {
                    return "rgb(56, 56, 56)";
                }
                else
                {
                    return "rgb(247, 247, 247)";
                }
            }
        }

        public static string PreForegroundColorText
        {
            get
            {
                if (Theme == Theme.Dark)
                {
                    return "rgb(248, 248, 242)";
                }
                else
                {
                    return "rgb(0, 0, 0)";
                }
            }
        }

        public static string ForegroundColorLink
        {
            get
            {
                if (Theme == Theme.Dark)
                {
                    return "rgb(0, 167, 255)";
                }
                else
                {
                    return "rgb(0, 0, 255)";
                }
            }
        }

        public static string TableHeaderBackgroundColorText
        {
            get
            {
                if (Theme == Theme.Dark)
                {
                    return "rgb(68, 68, 68)";
                }
                else
                {
                    return "rgb(230, 230, 230)";
                }
            }
        }

        public static string TableHoverBackgroundColorText
        {
            get
            {
                if (Theme == Theme.Dark)
                {
                    return "rgb(85, 85, 85)";
                }
                else
                {
                    return "rgb(200, 200, 200)";
                }
            }
        }

        public static string ScrollbarStyleText
        {
            get
            {
                if (Theme == Theme.Dark)
                {
                    return @"
                                scrollbar-base-color: #222;
                                scrollbar-3dlight-color: #222;
                                scrollbar-highlight-color: #222;
                                scrollbar-track-color: #3e3e42;
                                scrollbar-arrow-color: #111;
                                scrollbar-shadow-color: #222;
                                scrollbar-dark-shadow-color: #222; ";
                }
                else
                {
                    return "";
                }
            }
        }

        public static string GridSplitterBackColor
        {
            get
            {
                if (Theme == Theme.Dark)
                {
                    return "rgb(85, 85, 85)";
                }
                else
                {
                    return "rgb(255, 255, 255)";
                }
            }
        }

        public static string GridSplitterBorder
        {
            get
            {
                if (Theme == Theme.Dark)
                {
                    return "rgb(105, 105, 105)";
                }
                else
                {
                    return "rgb(169, 169, 169)";
                }
            }
        }

        public static Uri FileListBtnUri
        {
            get
            {
                if (Theme == Theme.Dark)
                {
                    return new Uri(@"pack://application:,,,/Resource/list-view-white.png", UriKind.RelativeOrAbsolute);
                }
                else
                {
                    return new Uri(@"pack://application:,,,/Resource/list-view-black.png", UriKind.RelativeOrAbsolute);
                }
            }
        }

        public static Uri ChangeThemeBtnUri
        {
            get
            {
                if (Theme == Theme.Dark)
                {
                    return new Uri(@"pack://application:,,,/Resource/theme-white.png", UriKind.RelativeOrAbsolute);
                }
                else
                {
                    return new Uri(@"pack://application:,,,/Resource/theme-black.png", UriKind.RelativeOrAbsolute);
                }
            }
        }

        public static Color ParseRgbString(string rgb)
        {
            string values = rgb.Replace("rgb(", "").Replace(")", "");

            byte[] rgbValues = values.Split(',').Select(v => byte.Parse(v.Trim())).ToArray();

            return Color.FromRgb(rgbValues[0], rgbValues[1], rgbValues[2]);
        }
    }
}
