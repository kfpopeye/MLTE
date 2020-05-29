using System;
using System.Collections.Generic;
using System.Diagnostics;

using log4net;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace MLTE
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class mlte_command : IExternalCommand
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(mlte_command));
        private IDictionary<string, bool> hardware = null;
        private IList<string> tokens = null;

        public Autodesk.Revit.UI.Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
#if !DEBUG
            if (!UserIsEntitled(commandData))
                return Result.Failed;
#endif

            global::WindowClasses.FormulaWindow winFormula = null;
            global::WindowClasses.TextEditorWindow winText = null;

            try
            {
                if (commandData.Application.ActiveUIDocument.Document.IsFamilyDocument)
                {
                    winFormula = new global::WindowClasses.FormulaWindow(commandData.Application.ActiveUIDocument.Document);
                    System.Windows.Interop.WindowInteropHelper x = new System.Windows.Interop.WindowInteropHelper(winFormula);
                    x.Owner = Process.GetCurrentProcess().MainWindowHandle;
                    winFormula.ShowDialog();
                }
                else
                {
                    if (useKnockKnockData(commandData))
                        winText = new global::WindowClasses.TextEditorWindow(commandData.Application.ActiveUIDocument, hardware, tokens);
                    else
                        winText = new global::WindowClasses.TextEditorWindow(commandData.Application.ActiveUIDocument);
                    System.Windows.Interop.WindowInteropHelper x = new System.Windows.Interop.WindowInteropHelper(winText);
                    x.Owner = Process.GetCurrentProcess().MainWindowHandle;
                    winText.ShowDialog();
                }
                return Result.Succeeded;
            }
            catch (Exception err)
            {
                _log.Error("MLTE", err);

                Autodesk.Revit.UI.TaskDialog td = new TaskDialog("Unexpected Error");
                td.MainInstruction = "MLTE has encountered an error and cannot complete.";
                td.MainContent = "The developer is no longer updating this app.";
                td.ExpandedContent = err.ToString();
                //td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Send bug report.");
                td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                TaskDialogResult tdr = td.Show();

                //if (tdr == TaskDialogResult.CommandLink1)
                //{
                //    pkhCommon.Email.SendErrorMessage(commandData.Application.Application.VersionName, commandData.Application.Application.VersionBuild, err, this.GetType().Assembly.GetName());
                //}
                if (winText != null)
                    winText.Close();
                if (winFormula != null)
                    winFormula.Close();

                return Result.Failed;
            }
        }

        private bool UserIsEntitled(ExternalCommandData commandData)
        {
            return true;

            //if (Properties.Settings.Default.EntCheck.AddDays(7) > System.DateTime.Today)
            //    return true;

            //string _baseApiUrl = @"https://apps.exchange.autodesk.com/";
            //string _appId = Constants.APP_STORE_ID;

            //UIApplication uiApp = commandData.Application;
            //Autodesk.Revit.ApplicationServices.Application rvtApp = commandData.Application.Application; 

            ////Check to see if the user is logged in.
            //if (!Autodesk.Revit.ApplicationServices.Application.IsLoggedIn)
            //{
            //    TaskDialog td = new TaskDialog(Constants.GROUP_NAME);
            //    td.MainInstruction = "Please login to Autodesk 360 first.";
            //    td.MainContent = "This application must check if you are authorized to use it. Login to Autodesk 360 using the same account you used to purchase this app. An internet connection is required.";
            //    td.Show();
            //    return false;
            //} 

            ////Get the user id, and check entitlement
            //string userId = rvtApp.LoginUserId;
            //bool isAuthorized = pkhCommon.EntitlementHelper.Entitlement(_appId, userId, _baseApiUrl);

            //if (!isAuthorized)
            // {
            //    TaskDialog td = new TaskDialog(Constants.GROUP_NAME);
            //    td.MainInstruction = "You are not authorized to use this app.";
            //    td.MainContent = "Make sure you login into Autodesk 360 with the same account you used to buy this app. If the app was purchased under a company account, contact your IT department to allow you access.";
            //    td.Show();
            //    return false;
            //}
            //else
            //{
            //    Properties.Settings.Default.EntCheck = System.DateTime.Today;
            //    Properties.Settings.Default.Save();
            //}

            //return isAuthorized;
        }

        private bool useKnockKnockData(ExternalCommandData commandData)
        {
            try
            {
                //check if there is a selected element and it is a door
                IEnumerator<ElementId> elen = commandData.Application.ActiveUIDocument.Selection.GetElementIds().GetEnumerator();
                elen.Reset();
                elen.MoveNext();
                Element el = commandData.Application.ActiveUIDocument.Document.GetElement(elen.Current);
                if (el != null && el.Category.Id.IntegerValue != (int)BuiltInCategory.OST_Doors)
                    return false;

                //try to get the door hardware schema
                KnockKnock.Schema_Handler sh = new KnockKnock.Schema_Handler();
                Autodesk.Revit.DB.ExtensibleStorage.Schema theschema = sh.getSchema();
                Autodesk.Revit.DB.ExtensibleStorage.Entity ent = commandData.Application.ActiveUIDocument.Document.ProjectInformation.GetEntity(theschema);
                if (!ent.IsValid())
                    return false;

                hardware = ent.Get<IDictionary<string, bool>>(theschema.GetField(KnockKnock.Schema_Handler.C_Hardware));
                tokens = ent.Get<IList<string>>(theschema.GetField(KnockKnock.Schema_Handler.C_Tokens));
                return true;
            }
            //catch any exceptions and ignore as they do not affect app function
            catch(Exception)
            { }
            return false;
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class aboutbox : IExternalCommand
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(aboutbox));
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                pkhCommon.Windows.About_Box ab = new pkhCommon.Windows.About_Box(this.GetType().Assembly);
                System.Windows.Interop.WindowInteropHelper x = new System.Windows.Interop.WindowInteropHelper(ab);
                x.Owner = Process.GetCurrentProcess().MainWindowHandle;
                ab.ShowDialog();
            }
            catch (Exception err)
            {
                _log.Error("About", err);
                Autodesk.Revit.UI.TaskDialog td = new TaskDialog("Unexpected Error");
                td.MainInstruction = "MLTE has encountered an error and cannot complete.";
                td.MainContent = "The developer is no longer updating this app.";
                td.ExpandedContent = err.ToString();
                //td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Send bug report.");
                td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                TaskDialogResult tdr = td.Show();

                //if (tdr == TaskDialogResult.CommandLink1)
                //{
                //    pkhCommon.Email.SendErrorMessage(commandData.Application.Application.VersionName, commandData.Application.Application.VersionBuild, err, this.GetType().Assembly.GetName());
                //}
            }

            return Result.Succeeded;
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class mlte_help : IExternalCommand
    {
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            MLTE.mlte_help.Launch();
            return Result.Succeeded;
        }
    }

    public class command_AvailableCheck : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication appdata, CategorySet selectCatagories)
        {
            if (appdata.Application.Documents.Size == 0)
                return false;

            if (appdata.ActiveUIDocument.Document.IsFamilyDocument)
                return true;

            if (appdata.ActiveUIDocument.Selection.GetElementIds().Count == 1)
                return true;

            return false;
        }
    }

    public class subcommand_AvailableCheck : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication appdata, CategorySet selectCatagories)
        {
            return true;
        }
    }
}