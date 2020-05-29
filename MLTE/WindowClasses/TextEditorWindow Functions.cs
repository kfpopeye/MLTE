using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

namespace WindowClasses
{
    public partial class TextEditorWindow : Window
    {
        private void SaveParameter(MLTE.ParameterItems param_item)
        {
            if (param_item == null)
                return;

            Parameter param = null;
            Transaction trans = new Transaction(ActiveUIDocument.Document);
            trans.Start("MLTE>" + param_item.Name);

            if (param_item.P_Type == "Instance")
                param = TheElement.LookupParameter(param_item.Name);
            else
            {
                TaskDialog td = new TaskDialog("Warning");
                td.MainInstruction = "This will affect all the elements of this type. Are you sure you want to do this?";
                td.CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No;
                TaskDialogResult res = td.Show();
                if (res == TaskDialogResult.No)
                {
                    trans.RollBack();
                    ResetButton_Click(null, null);
                    return;
                }
                ElementType ElType = ActiveUIDocument.Document.GetElement(TheElement.GetTypeId()) as ElementType;
                param = ElType.LookupParameter(param_item.Name);
            }
            try
            {
                bool res = true;
                if (param.StorageType == StorageType.String)
                {
                    res = param.Set(param_item.Value);
                    if (res)
                    {
                        trans.Commit();
                        param_item.Value = param.AsString();
                    }
                }
                else
                {
                    res = param.SetValueString(param_item.Value);
                    if (res)
                    {
                        trans.Commit();
                        param_item.Value = param.AsValueString();
                    }
                }

                if (!res)
                {
                    TaskDialog td = new TaskDialog("MLTE Error");
                    td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                    td.MainInstruction = "An invalid value was entered. Please try again.";
                    td.ExpandedContent = "Parameter : " + param_item.Name + Environment.NewLine +
                        "Value : " + param_item.Value;
                    td.Show();
                    trans.RollBack();
                    ResetButton_Click(null, null);
                }
            }

            catch (Exception err)
            {
                TaskDialog td = new TaskDialog("MLTE Error");
                td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                td.MainInstruction = "An exception was caught updating parameter : " + param_item.Name;
                td.ExpandedContent = err.ToString();
                td.Show();
                trans.RollBack();
                ResetButton_Click(null, null);
            }
        }

        private void AddParameters(ParameterSet parms, string p_type)
        {
            foreach (Parameter p in parms)
            {
                if (!p.IsReadOnly &&
                    p.StorageType != StorageType.ElementId &&
                    p.Definition.ParameterType != ParameterType.YesNo &&
                    p.Definition.ParameterType != ParameterType.Invalid &&
                    p.Definition.Name != LabelUtils.GetLabelFor(BuiltInParameter.UNIFORMAT_CODE))
                {
                    bool is_tokenized = false;
                    if (hardware != null && hardware.ContainsKey(p.Definition.Name))
                        is_tokenized = hardware[p.Definition.Name];

                    string v = null;
                    if (p.StorageType == StorageType.String)
                        v = p.AsString();
                    else
                        v = p.AsValueString();
                    MLTE.ParameterItems pi = new MLTE.ParameterItems(
                        p.Id.IntegerValue,
                        p.Definition.Name,
                        LabelUtils.GetLabelFor(p.Definition.ParameterGroup),
                        v,
                        p_type,
                        is_tokenized);
                    pi.TokenList = TokenList;
                    ParametersCollection.Add(pi);
                }
            }
        }

        private static void GetVisualChildren<T>(DependencyObject current, Collection<T> children) where T : DependencyObject
        {
            if (current != null)
            {
                if (current.GetType() == typeof(T))
                {
                    children.Add((T)current);
                }

                for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(current); i++)
                {
                    GetVisualChildren<T>(System.Windows.Media.VisualTreeHelper.GetChild(current, i), children);
                }
            }
        }

        public static Collection<T> GetVisualChildren<T>(DependencyObject current) where T : DependencyObject
        {
            if (current == null)
            {
                return null;
            }

            Collection<T> children = new Collection<T>();
            GetVisualChildren<T>(current, children);
            return children;
        }

        private T FindParent<T>(DependencyObject child)where T : DependencyObject
        {
            //get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            T parent = parentObject as T;
            if (parent != null)
                return parent;
            else
                return FindParent<T>(parentObject);
        }

        private bool FilterResults(object obj)
        {
            //match items here with your TextBox value.. obj is an item from the list
            if (searchTextBox.Text == null || searchTextBox.Text == string.Empty || searchTextBox.Text == "Filter")
                return true;

            MLTE.ParameterItems param = obj as MLTE.ParameterItems;
            if (param.Name != null && param.Name.ToUpper().Contains(searchTextBox.Text.ToUpper()))
                return true;
            if (param.Value != null && param.Value.ToUpper().Contains(searchTextBox.Text.ToUpper()))
                return true;

            return false;
        }

        /// <summary>
        /// Prompts for a name, checks name for duplicate and then suplicates the Element. A transaction must be started before using this function.
        /// </summary>
        /// <returns>ElementType of duplicated element</returns>
        private ElementType DuplicateElement()
        {
            bool nameExists = false;

            //invalid name characters \:{}[]|;<>?`~ or any non-printable characters
            global::pkhCommon.Windows.Input_Box ib = new global::pkhCommon.Windows.Input_Box("MLTE",
                "Enter a name.",
                "Enter a name for the new element. Do not include the characters \\:{}[]|;<>?`~ or any non-printable characters",
                new System.Text.RegularExpressions.Regex(@"[\\:{}[\]|;<>?`~]", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.IgnorePatternWhitespace));
            ib.Owner = this;
            ib.ShowDialog();
            if ((bool)ib.DialogResult && ib.UserInput != string.Empty)
            {
                FamilyInstance fi = TheElement as FamilyInstance;
                if (fi != null)
                {
                    Family f = fi.Symbol.Family;
                    foreach (ElementId fsid in f.GetFamilySymbolIds())
                    {
                        FamilySymbol fs = ActiveUIDocument.Document.GetElement(fsid) as FamilySymbol;
                        if (fs.Name == ib.UserInput)
                            nameExists = true;
                    }
                }
                else
                {
                    //get all element ID's of similar types
                    ElementType et = ActiveUIDocument.Document.GetElement(TheElement.GetTypeId()) as ElementType;
                    ICollection<ElementId> coll = et.GetSimilarTypes();
                    //scan through names for duplicate
                    foreach (ElementId eid in coll)
                    {
                        if (ActiveUIDocument.Document.GetElement(eid).Name == ib.UserInput)
                            nameExists = true;
                    }
                }

                if (nameExists)
                    TaskDialog.Show("Name error", "The name \"" + ib.UserInput + "\" already exists.");
                else
                {
                    ElementType et = ActiveUIDocument.Document.GetElement(TheElement.GetTypeId()) as ElementType;
                    return et.Duplicate(ib.UserInput);
                }
            }

            return null;
        }

        private void InitializeParameters()
        {
            ParametersCollection.Clear();

            //get instance parameters
            AddParameters(TheElement.Parameters, "Instance");
            //get type parameters
            ElementType ItsType = ActiveUIDocument.Document.GetElement(TheElement.GetTypeId()) as ElementType;
            if (ItsType != null)
            {
                AddParameters(ItsType.Parameters, "Type");
                this.Title = "M.L.T.E - " + TheElement.Category.Name + " : " + ItsType.FamilyName + " : " + TheElement.Name;
            }
            else
                this.Title = "M.L.T.E - " + TheElement.Category.Name + " : " + TheElement.Name;

            if (!TheElement.CanHaveTypeAssigned())
                duplicateButton.IsEnabled = false;

            if (ParametersCollection.Count == 0)
            {
                TaskDialog.Show("MLTE", "This element has no parameters.");
            }

            parameterListView.ItemsSource = ParametersCollection;
            plist_view = (CollectionView)CollectionViewSource.GetDefaultView(parameterListView.ItemsSource);
            plist_view.Filter = FilterResults;
            PropertyGroupDescription groupDescription = new PropertyGroupDescription("Group");
            plist_view.GroupDescriptions.Clear();
            plist_view.GroupDescriptions.Add(groupDescription);
            plist_view.SortDescriptions.Add(new SortDescription("Group", ListSortDirection.Ascending));
            plist_view.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }
    }

    [ValueConversion(typeof(string), typeof(string))]
    public class KKComboboxConverter : IValueConverter
    {
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string val = value as string;
            if (val == "<none>")
                val = string.Empty;
            return val;
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string val = value as string;
            if (val == string.Empty || val == null)
                return "<none>";
            return val;
        }
    }
}