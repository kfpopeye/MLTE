using System;
using System.IO;
using System.Windows.Media.Imaging;
using System.Net;
using System.Xml;

using Autodesk;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI.Events;
using log4net;
using log4net.Config;

namespace MLTE
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Journaling(Autodesk.Revit.Attributes.JournalingMode.NoCommandData)]
    public partial class MLTE : IExternalApplication
    {
        internal static ContextualHelp mlte_help = null;
        private static ILog _log = null;

        private void CreateRibbonPanel(UIControlledApplication application)
        {
            // This method is used to create the ribbon panel.
            // which contains the controlled application.

            string AddinPath = Properties.Settings.Default.AddinPath;
            string DLLPath = AddinPath + @"\MLTE.dll";
            RibbonPanel pkhlPanel = application.CreateRibbonPanel("MLTE");

            PushButton kk_Button = pkhlPanel.AddItem(new PushButtonData("mlteButton", "M.L.T.E.", DLLPath, "MLTE.mlte_command")) as PushButton;
            kk_Button.Image = NewBitmapImage(this.GetType().Assembly, "mlte16x16.png");
            kk_Button.LargeImage = NewBitmapImage(this.GetType().Assembly, "mlte.png");
            kk_Button.ToolTip = "Multi Line Text Editor";
            kk_Button.Visible = true;
            kk_Button.AvailabilityClassName = "MLTE.command_AvailableCheck";
            kk_Button.SetContextualHelp(mlte_help);

            // Create a slide out
            pkhlPanel.AddSlideOut();

            PushButtonData about_Button = new PushButtonData("mlteaboutButton", "About", DLLPath, "MLTE.aboutbox");
            about_Button.Image = NewBitmapImage(this.GetType().Assembly, "about16x16.png");
            about_Button.LargeImage = NewBitmapImage(this.GetType().Assembly, "about.png");
            about_Button.ToolTip = "All about MLTE.";
            about_Button.AvailabilityClassName = "MLTE.subcommand_AvailableCheck";

            PushButtonData help_Button = new PushButtonData("mltehelpButton", "Help", DLLPath, "MLTE.mlte_help");
            help_Button.Image = NewBitmapImage(this.GetType().Assembly, "kk_help16x16.png");
            help_Button.LargeImage = NewBitmapImage(this.GetType().Assembly, "kk_help.png");
            help_Button.ToolTip = "Help for using MLTE.";
            help_Button.AvailabilityClassName = "MLTE.subcommand_AvailableCheck";

            pkhlPanel.AddStackedItems(about_Button, help_Button);
        }

        /// <summary>
        /// Load a new icon bitmap from embedded resources.
        /// For the BitmapImage, make sure you reference WindowsBase and PresentationCore, and import the System.Windows.Media.Imaging namespace.
        /// Drag images into Resources folder in solution explorer and set build action to "Embedded Resource"
        /// </summary>
        private BitmapImage NewBitmapImage(System.Reflection.Assembly a, string imageName)
        {
            // to read from an external file:
            //return new BitmapImage( new Uri(
            //  Path.Combine( _imageFolder, imageName ) ) );

            Stream s = a.GetManifestResourceStream("MLTE.Resources." + imageName);
            BitmapImage img = new BitmapImage();

            img.BeginInit();
            img.StreamSource = s;
            img.EndInit();

            return img;
        }

        #region Event Handlers
        public Autodesk.Revit.UI.Result OnShutdown(UIControlledApplication application)
        {
            return Autodesk.Revit.UI.Result.Succeeded;
        }

        public Autodesk.Revit.UI.Result OnStartup(UIControlledApplication application)
        {
            string s = this.GetType().Assembly.Location;
            int x = s.IndexOf(@"\MLTE.dll", StringComparison.CurrentCultureIgnoreCase);
            s = s.Substring(0, x);
            Properties.Settings.Default.AddinPath = s;

            string logConfig = Path.Combine(Properties.Settings.Default.AddinPath, "mlte.log4net.config");
            FileInfo configStream = new FileInfo(logConfig);
            XmlConfigurator.Configure(configStream);
            _log = LogManager.GetLogger(typeof(MLTE));
            _log.InfoFormat("Running version: {0}", this.GetType().Assembly.GetName().Version.ToString());
            _log.InfoFormat("Found myself at: {0}", Properties.Settings.Default.AddinPath);

            mlte_help = new ContextualHelp(
                ContextualHelpType.ChmFile,
                Path.Combine(
                    Directory.GetParent(Properties.Settings.Default.AddinPath).ToString(), //contents directory
                    @"mlte_help.chm"));

            CreateRibbonPanel(application);

            return Autodesk.Revit.UI.Result.Succeeded;
        }
        #endregion
    }
}