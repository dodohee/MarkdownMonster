﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using FontAwesome.WPF;
using MahApps.Metro.Controls;
using MarkdownMonster;
using Westwind.Utilities;

namespace WeblogAddin
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class WebLogForm : MetroWindow
    {
        public WeblogAddinModel Model { get; set;  }


        #region Startup and Shutdown

        public WebLogForm()
        {            
            Model = new WeblogAddinModel()
            {
                ActivePostMetadata = new WeblogPostMetadata(),
                Configuration = WeblogApp.Configuration,
                Window = this
            };

            mmApp.SetTheme(mmApp.Configuration.ApplicationTheme);

            InitializeComponent();
            //mmApp.SetThemeWindowOverride(this);         

            DataContext = Model;

            // Code bindings
            ComboWeblogType.ItemsSource = Enum.GetValues(typeof(WeblogTypes)).Cast<WeblogTypes>();            

            Loaded += WebLogStart_Loaded;
            Closing += WebLogStart_Closing;
        }

        private void WebLogStart_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Model.LoadWebLognames();

            var markdown = Model.AppModel.ActiveEditor.GetMarkdown();
            Model.ActivePostMetadata = Model.Addin.GetPostConfigFromMarkdown(markdown);
        }


        private void WebLogStart_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // save settings
            WeblogApp.Configuration.Write();
        }

        #endregion

        #region Button Handlers
        private async void ButtonPostBlog_Click(object sender, System.Windows.RoutedEventArgs e)
        {            
            // Update the Markdown document first
            string markdown = Model.Addin.SetConfigInMarkdown(Model.ActivePostMetadata);
            Model.AppModel.ActiveEditor.SetMarkdown(markdown);
            
            WeblogApp.Configuration.LastWeblogAccessed = Model.ActivePostMetadata.WeblogName;

            var window = Model.AppModel.Window;

            ShowStatus("Uploading Blog post...");
            SetStatusIcon(FontAwesome.WPF.FontAwesomeIcon.Upload, Colors.Orange, true);                
            
            try
            {
                await Dispatcher.InvokeAsync(()=>
                {
                    Thread.Sleep(2000);
                    // Then send the post - it will re-read the new values
                    if (Model.Addin.SendPost()) { 
                        
                        this.Close();
                    }
                    else
                        window.ShowStatus("Failed to upload blog post.", 5000);
                },System.Windows.Threading.DispatcherPriority.Background);                
            }
            finally
            {
                window.ShowStatus("Blog post uploaded successfully.", 5000);
                SetStatusIcon();
            }
        }

        private void ButtonSaveMeta_Click(object sender, RoutedEventArgs e)
        {
            // Update the Markdown document first
            string markdown = Model.Addin.SetConfigInMarkdown(Model.ActivePostMetadata);
            Model.AppModel.ActiveEditor.SetMarkdown(markdown);            
        }

        private void ButtonNewPost_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string title = Model.NewTitle;
            string weblogName = Model.Configuration.LastWeblogAccessed;

            if (string.IsNullOrEmpty(title))
                return;

            // strip path of invalid characters
            var invalids = Path.GetInvalidFileNameChars();
            string filename = null;
            foreach (char c in invalids)
                filename = title.Replace(c, '-');

            var folder = Path.Combine(WeblogApp.Configuration.PostsFolder, filename);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            var outputFile = Path.Combine(folder, filename + ".md");

            // Create the new post by creating a file with title preset
            string newPostMarkdown = Model.Addin.NewWeblogPost(new WeblogPostMetadata()
            {
                Title = title,
                WeblogName = weblogName
            });
            File.WriteAllText(outputFile, newPostMarkdown);
            Model.AppModel.Window.OpenTab(outputFile);

            mmApp.Configuration.LastFolder = Path.GetDirectoryName(outputFile);

            this.Close();
        }

        #endregion

        #region StatusBar

        public void ShowStatus(string message = null, int milliSeconds = 0)
        {
            if (message == null)
                message = "Ready";

            StatusText.Text = message;

            if (milliSeconds > 0)
            {
                var t = new Timer(new TimerCallback((object win) =>
                {
                    var window = win as MainWindow;
                    window.Dispatcher.Invoke(() => { window.ShowStatus(null, 0); });
                }), this, milliSeconds, Timeout.Infinite);
            }
        }

        /// <summary>
        /// Status the statusbar icon on the left bottom to some indicator
        /// </summary>
        /// <param name="icon"></param>
        /// <param name="color"></param>
        /// <param name="spin"></param>
        public void SetStatusIcon(FontAwesomeIcon icon, Color color, bool spin = false)
        {
            StatusIcon.Icon = icon;
            StatusIcon.Foreground = new SolidColorBrush(color);
            if (spin)
                StatusIcon.SpinDuration = 30;

            StatusIcon.Spin = spin;
        }

        /// <summary>
        /// Resets the Status bar icon on the left to its default green circle
        /// </summary>
        public void SetStatusIcon()
        {
            StatusIcon.Icon = FontAwesomeIcon.Circle;
            StatusIcon.Foreground = new SolidColorBrush(Colors.Green);
            StatusIcon.Spin = false;
            StatusIcon.SpinDuration = 0;
            StatusIcon.StopSpin();
        }
        #endregion

        private void ComboWebLogName_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TextWeblogPassword.Password))
                Model.ActiveWeblogInfo.Password = TextWeblogPassword.Password;


        }
    }
}