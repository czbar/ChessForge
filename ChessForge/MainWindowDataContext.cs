using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ChessForge
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // Default size of the font for the buttons in the NAG StackPanel.
        private readonly double NAG_BUTTON_FONT_SIZE = 12;

        // Default height of the NAG StackPanel.
        private readonly double NAG_PANEL_HEIGHT = 22;

        // Default width of the icons in the NAG StackPanel.
        private readonly double NAG_ICON_WIDTH = 18;

        // Default width of the comment icons in the NAG StackPanel.
        private readonly double NAG_COMMENT_ICON_WIDTH = 30;

        // Default height of the icons in the NAG StackPanel.
        private readonly double NAG_ICON_HEIGHT = 18;

        // Default increment for the main window height when the font size is increased/decreased.
        private readonly double NAG_ROW_INCREMENT = 6;

        // Default increment for the font size of the buttons in the NAG StackPanel when the "Large Menu Font" option is toggled.
        private readonly double NAG_FONT_SIZE_INCREMENT = 4;

        // Default increment for the width of the icons in the NAG StackPanel when the "Large Menu Font" option is toggled.
        private readonly double NAG_ICON_WIDTH_INCREMENT = 4;

        // Default increment for the height of the icons in the NAG StackPanel when the "Large Menu Font" option is toggled.
        private readonly double NAG_ICON_HEIGHT_INCREMENT = 4;

        // Default increment for the width of the comment icons in the NAG StackPanel when the "Large Menu Font" option is toggled.
        private readonly double NAG_COMMENT_ICON_WIDTH_INCREMENT = 12;



        // Backing field for the NagButtonFontSize property.
        private double _nagButtonFontSize = 12;

        // Backing field for the NagPanelHeight property.
        private double _nagPanelHeight = 48;

        // Backing field for the NagIconWidth property.
        private double _nagIconWidth = 18;

        // Backing field for the NagCommentIconWidth property.
        private double _nagCommentIconWidth = 30;

        // Backing field for the NagIconHeight property.
        private double _nagIconHeight = 18;

        // Backing field for the MainWindowHeight property.
        private double _mainWindowHeight = 600;

        // Backing field for the WindowHeightIncrement property.
        private double _windowHeightIncrement = 20;

        // Backing field for the NagFontSizeIncrement property.
        private double _nagFontSizeIncrement = 4;

        /// <summary>
        /// NagButtonFontSize is the font size for the buttons in the NAG StackPanel. 
        /// It is bound to the FontSize property of the buttons in the XAML.
        /// </summary>
        public double NagButtonFontSize
        {
            get => _nagButtonFontSize;
            set
            {
                if (_nagButtonFontSize != value)
                {
                    _nagButtonFontSize = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// NagPanelHeight is the height of the StackPanel that contains the NAG buttons.
        /// It is bound to the Height property of the Stack Panels in the XAML.
        /// </summary>
        public double NagPanelHeight
        {
            get => _nagPanelHeight;
            set
            {
                if (_nagPanelHeight != value)
                {
                    _nagPanelHeight = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// NagIconWidth is the width of the icons in the NAG StackPanel. 
        /// It is bound to the Width property of the Image controls in the XAML.
        /// </summary>
        public double NagIconWidth
        {
            get => _nagIconWidth;
            set
            {
                if (_nagIconWidth != value)
                {
                    _nagIconWidth = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// NagCommentIconWidth is the width of the comment icons in the NAG StackPanel. 
        /// It is bound to the Width property of the Image controls for comments in the XAML.
        /// </summary>
        public double NagCommentIconWidth
        {
            get => _nagCommentIconWidth;
            set
            {
                if (_nagCommentIconWidth != value)
                {
                    _nagCommentIconWidth = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// NagIconHeight is the height of the icons in the NAG StackPanel. 
        /// It is bound to the Height property of the Image controls in the XAML.
        /// </summary>
        public double NagIconHeight
        {
            get => _nagIconHeight;
            set
            {
                if (_nagIconHeight != value)
                {
                    _nagIconHeight = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// MainWindowHeight is the height of the main view (above the StackPanel). 
        /// It is bound to the Height property of the main view in the XAML.
        /// </summary>
        public double MainWindowHeight
        {
            get => _mainWindowHeight;
            set
            {
                if (_mainWindowHeight != value)
                {
                    _mainWindowHeight = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// WindowHeightIncrement is the increment for the main window height when the font size is increased/decreased. 
        /// It is used to adjust the height of the main view when the "Large Menu Font" option is toggled.
        /// </summary>
        public double WindowHeightIncrement
        {
            get => _windowHeightIncrement;
            set => _windowHeightIncrement = value;
        }

        /// <summary>
        /// NagFontSizeIncrement is the increment for the font size of the buttons in the NAG StackPanel when the "Large Menu Font" option is toggled. 
        /// It is used to adjust the font size of the buttons when the option is toggled.
        /// </summary>
        public double NagFontSizeIncrement
        {
            get => _nagFontSizeIncrement;
            set => _nagFontSizeIncrement = value;
        }

        /// <summary>
        /// Initializes the data context for the main window and sets the sizes of the NAG buttons based on the configuration.
        /// </summary>
        public void InitializeDataContext()
        {
            DataContext = this;
            ResizeNagStackPanels();
        }

        /// <summary>
        /// Sets the sizes of the NAG buttons and the height of the main view based on the "Large Menu Font" option in the configuration. If the option is enabled, it increases the font size of the buttons and adjusts the height of the main view accordingly. If the option is disabled, it resets the font size to the default value.
        /// </summary>
        public void ResizeNagStackPanels()
        {
            bool b = Configuration.LargeMenuFont;

            double bottomMargin = b ? LayoutUtils.TAB_ITEM_BOTTOM_MARGIN + NAG_ROW_INCREMENT : LayoutUtils.TAB_ITEM_BOTTOM_MARGIN;

            LayoutUtils.AdjustTabItemBottomMargin(UiRtbStudyTreeView, bottomMargin);
            LayoutUtils.AdjustTabItemBottomMargin(UiRtbModelGamesView, bottomMargin);
            LayoutUtils.AdjustTabItemBottomMargin(UiRtbExercisesView, bottomMargin);

            UiSpGameNagPanel.Height = b ? NAG_PANEL_HEIGHT + NAG_ROW_INCREMENT : NAG_PANEL_HEIGHT;
            UiSpStudyNagPanel.Height = b ? NAG_PANEL_HEIGHT + NAG_ROW_INCREMENT : NAG_PANEL_HEIGHT;
            UiSpExercNagPanel.Height = b ? NAG_PANEL_HEIGHT + NAG_ROW_INCREMENT : NAG_PANEL_HEIGHT;

            NagButtonFontSize = b ? NAG_BUTTON_FONT_SIZE + NAG_FONT_SIZE_INCREMENT : NAG_BUTTON_FONT_SIZE;
            NagIconWidth = b ? NAG_ICON_WIDTH + NAG_ICON_WIDTH_INCREMENT : NAG_ICON_WIDTH;
            NagIconHeight = b ? NAG_ICON_HEIGHT + NAG_ICON_HEIGHT_INCREMENT : NAG_ICON_HEIGHT;

            NagCommentIconWidth = b ? NAG_COMMENT_ICON_WIDTH + NAG_COMMENT_ICON_WIDTH_INCREMENT : NAG_COMMENT_ICON_WIDTH;
        }

        /// <summary>
        /// Event handler for the PropertyChanged event. It is raised when a property value changes and is used to notify the UI to update the bound properties.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// PropertyChanged event invocator. It raises the PropertyChanged event for the specified property name. The CallerMemberName attribute is used to automatically get the name of the calling property, so it can be called without parameters from the property setters.
        /// </summary>
        /// <param name="propertyName"></param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
