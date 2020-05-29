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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Collections.ObjectModel;
using System.ComponentModel;
using AurelienRibon.Ui.SyntaxHighlightBox;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

namespace WindowClasses
{
    /// <summary>
    /// Interaction logic for FormulaWindow.xaml
    /// </summary>
    public partial class FormulaWindow : Window
    {
        private Document ActiveDocument = null;
        private string OldFormula = string.Empty;
        private string OldValue = string.Empty;
        private ObservableCollection<MLTE.ParameterItems> ParametersCollection = null;
        private CollectionView plist_view = null;
        private int selectedItem = -1;

        public FormulaWindow(Document doc)
        {
            InitializeComponent();

            ActiveDocument = doc;
            ParametersCollection = new ObservableCollection<MLTE.ParameterItems>();

            InitializeParameters();
        }

        private void InitializeParameters()
        {
            ParametersCollection.Clear();

            StringBuilder ParameterNames = new StringBuilder();
            FamilyManager famMan = ActiveDocument.FamilyManager;
            foreach (FamilyParameter p in famMan.Parameters)
            {
                if (!p.IsReadOnly &&
                    p.StorageType != StorageType.ElementId &&
                    p.Definition.ParameterType != ParameterType.YesNo &&
                    p.Id.IntegerValue != (int)BuiltInParameter.RENDER_RPC_FILENAME &&
                    p.Id.IntegerValue != (int)BuiltInParameter.RENDER_RPC_PROPERTIES &&
                    p.Definition.ParameterType != ParameterType.Invalid &&
                    p.Definition.Name != LabelUtils.GetLabelFor(BuiltInParameter.UNIFORMAT_CODE)
                    )
                {
                    string v = null;
                     if (p.StorageType == StorageType.String)
                        v = famMan.CurrentType.AsString(p);
                    else
                        v = famMan.CurrentType.AsValueString(p);
                    ParametersCollection.Add(new MLTE.ParameterItems(
                        p.Id.IntegerValue,
                        p.Definition.Name,
                        p.Formula,
                        LabelUtils.GetLabelFor(p.Definition.ParameterGroup),
                        v,
                        p.IsInstance ? "Instance" : "Type"));
                    ParameterNames.Append(p.Definition.Name + " ");
                }
            }
            parameterListView.ItemsSource = ParametersCollection;

            plist_view = (CollectionView)CollectionViewSource.GetDefaultView(parameterListView.ItemsSource);
            plist_view.Filter = FilterResults;
            plist_view.SortDescriptions.Clear();
            plist_view.SortDescriptions.Add(new SortDescription("Group", ListSortDirection.Ascending));
            plist_view.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            PropertyGroupDescription groupDescription = new PropertyGroupDescription("Group");
            plist_view.GroupDescriptions.Clear();
            plist_view.GroupDescriptions.Add(groupDescription);

            HighlighterManager.Instance.AddWordRule(ParameterNames.ToString());
            shbox.CurrentHighlighter = HighlighterManager.Instance.Highlighters["VHDL"];

            this.Title = "M.L.T.E - " + famMan.CurrentType.Name;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (parameterListView.SelectedItem == null)
                return;

            selectedItem = parameterListView.SelectedIndex;
            FamilyManager fm = ActiveDocument.FamilyManager;
            MLTE.ParameterItems param_item = parameterListView.SelectedItem as MLTE.ParameterItems;
            FamilyParameter param = fm.get_Parameter(param_item.Name);
            Transaction trans = new Transaction(ActiveDocument);
            trans.Start("MLTE>" + param_item.Name);

            ComboBoxItem cbi = cbValorForm.SelectedItem as ComboBoxItem;
            if ((string)cbi.Content == "Formula")
            {
                string formula = shbox.Text.Replace("\t", "").Replace(Environment.NewLine, " "); //strips tabs and newlines
                try
                {
                    string s = FormulaManager.Validate(param.Id, ActiveDocument, param_item.Formula);
                    if (!string.IsNullOrEmpty(s))
                    {
                        TaskDialog.Show("Result", s);
                        trans.RollBack();
                        return;
                    }
                }
                catch(Autodesk.Revit.Exceptions.ApplicationException)
                {
                }
                param_item.Formula = formula;

                if (param.CanAssignFormula)
                {
                    try
                    {
                        if (param_item.Formula == string.Empty)
                            fm.SetFormula(param, null);
                        else
                            fm.SetFormula(param, param_item.Formula);
                        trans.Commit();
                        param_item.Value = fm.CurrentType.AsValueString(param);
                    }
                    catch (Autodesk.Revit.Exceptions.ArgumentException)
                    {
                        TaskDialog td = new TaskDialog("MLTE Error");
                        td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                        td.MainInstruction = "An invalid formula was entered. Please try again.";
                        td.ExpandedContent = "Parameter : " + param_item.Name + Environment.NewLine +
                            "Formula : " + param_item.Formula;
                        td.Show();
                        trans.RollBack();
                        ResetButton_Click(null, null);
                    }
                }
            }
            else
            {
                if (param.IsDeterminedByFormula)
                {
                    TaskDialog.Show("MLTE - Warning", "The value of this parameter is determined by a formula. Remove the formula first");
                    trans.RollBack();
                }
                else
                {
                    try
                    {
                        if (param.StorageType == StorageType.String)
                        {
                            fm.Set(param, valbox.Text);
                            trans.Commit();
                            param_item.Value = fm.CurrentType.AsString(param);
                        }
                        else
                        {
                            fm.SetValueString(param, valbox.Text);
                            trans.Commit();
                            param_item.Value = fm.CurrentType.AsValueString(param);
                        }
                    }
                    catch (Autodesk.Revit.Exceptions.ArgumentException)
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
            }
            InitializeParameters();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            MLTE.ParameterItems param = parameterListView.SelectedItem as MLTE.ParameterItems;
            if (param != null)
            {
                ComboBoxItem cbi = cbValorForm.SelectedItem as ComboBoxItem;
                if ((string)cbi.Content == "Formula")
                {
                    param.Formula = OldFormula;
                    param.NotifyPropertyChanged("Formula");
                }
                else
                {
                    param.Value = OldValue;
                    param.NotifyPropertyChanged("Value");
                }
            }
        }

        void listView1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView lv = sender as ListView;
            MLTE.ParameterItems lvi = lv.SelectedItem as MLTE.ParameterItems;

            if (lvi != null)
            {
                OldFormula = lvi.Formula;
                OldValue = lvi.Value;

                if (lvi.Formula != null)
                {
                    cbValorForm.SelectedIndex = 1;
                    comboBox1_SelectionChanged(null, null);
                }
                else if (lvi.Value != null)
                {
                    cbValorForm.SelectedIndex = 0;
                    comboBox1_SelectionChanged(null, null);
                }
            }
        }

        void comboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem cbi = cbValorForm.SelectedItem as ComboBoxItem;
            if ((string)cbi.Content == "Value")
            {
                shbox.Visibility = System.Windows.Visibility.Hidden;
                valbox.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                shbox.Visibility = System.Windows.Visibility.Visible;
                valbox.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AdornerLayer al = AdornerLayer.GetAdornerLayer(splitter);
            al.Add(new DoubleArrowAdorner(splitter));
        }

        private void expandButton_Click(object sender, RoutedEventArgs e)
        {
            Collection<Expander> collection = TextEditorWindow.GetVisualChildren<Expander>(this.parameterListView);
            foreach (Expander expander in collection)
            {
                expander.IsExpanded = true;
            }
        }

        private void collapseButton_Click(object sender, RoutedEventArgs e)
        {
            Collection<Expander> collection = TextEditorWindow.GetVisualChildren<Expander>(this.parameterListView);
            foreach (Expander expander in collection)
            {
                expander.IsExpanded = false;
            }
        }

        private void searchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            searchTextBox.Text = string.Empty;
        }

        private void searchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.IsLoaded)
            {
                if (searchTextBox.Text.Length > 2)
                    plist_view.Refresh();
            }
        }

        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            searchTextBox.Text = "Filter";
        }

        private void duplicateButton_Click(object sender, RoutedEventArgs e)
        {
            //invalid name characters \:{}[]|;<>?`~ or any non-printable characters
            global::pkhCommon.Windows.Input_Box ib = new global::pkhCommon.Windows.Input_Box("MLTE",
                "Enter a name.",
                "Enter a name for the new element. Do not include the characters \\:{}[]|;<>?`~ or any non-printable characters",
                new System.Text.RegularExpressions.Regex(@"[\\:{}[\]|;<>?`~]", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.IgnorePatternWhitespace));
            ib.Owner = this;
            ib.ShowDialog();
            if ((bool)ib.DialogResult && ib.UserInput != string.Empty)
            {
                bool nameExists = false;
                foreach (FamilyType ft in ActiveDocument.FamilyManager.Types)
                {
                    if (ft.Name == ib.UserInput)
                        nameExists = true;
                }

                if (nameExists)
                    TaskDialog.Show("Naming Error", "The name \"" + ib.UserInput + "\" already exists.");
                else
                {
                    Transaction tr = new Transaction(ActiveDocument);
                    tr.Start("MLTE>Duplicate");
                    ActiveDocument.FamilyManager.NewType(ib.UserInput);
                    tr.Commit();
                }
            }
            this.Title = "M.L.T.E - " + ActiveDocument.FamilyManager.CurrentType.Name;
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

        private void parameterListView_LayoutUpdated(object sender, EventArgs e)
        {
            if (selectedItem != -1)
            {
                parameterListView.SelectedIndex = selectedItem;
                parameterListView.ScrollIntoView(parameterListView.SelectedItem);
                selectedItem = -1;
            }
        }
    }
}
