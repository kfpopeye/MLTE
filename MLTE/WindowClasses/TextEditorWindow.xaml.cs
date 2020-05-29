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
    /// <summary>
    /// Interaction logic for TextEditorWindow.xaml
    /// </summary>
    public partial class TextEditorWindow : Window
    {
        private UIDocument ActiveUIDocument = null;
        private string OldValue = string.Empty;
        private Element TheElement = null;
        private ObservableCollection<MLTE.ParameterItems> ParametersCollection = null;
        private CollectionView plist_view = null;
        private IDictionary<string, bool> hardware = null;
        private IList<string> TokenList = null;
        private MLTE.ParameterItems currentItem = null;
        private int selectedItem = -1;

        public TextEditorWindow(UIDocument actdoc)
        {
            InitializeComponent();

            ActiveUIDocument = actdoc;
            ParametersCollection = new ObservableCollection<MLTE.ParameterItems>();
            ICollection<ElementId> ids = ActiveUIDocument.Selection.GetElementIds();
            TheElement = ActiveUIDocument.Document.GetElement(ids.ElementAt<ElementId>(0));
            InitializeParameters();
        }

        public TextEditorWindow(UIDocument actdoc, IDictionary<string, bool> hrdwr, IList<string> tkns)
        {
            InitializeComponent();

            hardware = hrdwr;
            TokenList = tkns;
            TokenList.Insert(0, "<none>");
            ActiveUIDocument = actdoc;
            ParametersCollection = new ObservableCollection<MLTE.ParameterItems>();
            ICollection<ElementId> ids = ActiveUIDocument.Selection.GetElementIds();
            TheElement = ActiveUIDocument.Document.GetElement(ids.ElementAt<ElementId>(0));
            InitializeParameters();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (parameterListView.SelectedItem == null)
                return;

            selectedItem = parameterListView.SelectedIndex;
            MLTE.ParameterItems param_item = parameterListView.SelectedItem as MLTE.ParameterItems;
            param_item.Value = shbox.Text;
            this.IsEnabled = false;
            SaveParameter(param_item);
            this.IsEnabled = true;
            InitializeParameters();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            MLTE.ParameterItems param = parameterListView.SelectedItem as MLTE.ParameterItems;
            if (param != null)
            {
                param.Value = OldValue;
            }
        }

        void parameterListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (parameterListView.SelectedItem == null)
                return;

            if (parameterListView.HasItems)
            {
                MLTE.ParameterItems param_item = parameterListView.SelectedItem as MLTE.ParameterItems;
                if (param_item.Value == null)
                    OldValue = string.Empty;
                else
                    OldValue = param_item.Value;
            }
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (cbUpperCase.IsChecked != null)
            {
                if ((bool)cbUpperCase.IsChecked)
                    shbox.CharacterCasing = CharacterCasing.Upper;
                else
                    shbox.CharacterCasing = CharacterCasing.Normal;
                global::MLTE.Properties.Settings.Default.UpperCaseState = (bool)cbUpperCase.IsChecked;
                global::MLTE.Properties.Settings.Default.Save();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AdornerLayer al = AdornerLayer.GetAdornerLayer(splitter);
            al.Add(new DoubleArrowAdorner(splitter));

            if (global::MLTE.Properties.Settings.Default.UpperCaseState)
                shbox.CharacterCasing = CharacterCasing.Upper;
            else
                shbox.CharacterCasing = CharacterCasing.Normal;
        }

        private void expandButton_Click(object sender, RoutedEventArgs e)
        {
            Collection<Expander> collection = GetVisualChildren<Expander>(this.parameterListView);
            foreach (Expander expander in collection)
            {
                expander.IsExpanded = true;
            }
        }

        private void collapseButton_Click(object sender, RoutedEventArgs e)
        {
            Collection<Expander> collection = GetVisualChildren<Expander>(this.parameterListView);
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

            Transaction tr = new Transaction(ActiveUIDocument.Document);
            tr.Start("MLTE>Duplicate");
            ElementType et = DuplicateElement();
            if (et != null)
            {
                TheElement.ChangeTypeId(et.Id);
                tr.Commit();
                InitializeParameters();
            }
            else
                tr.RollBack();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (currentItem != null)
            {
                System.Windows.Controls.ComboBox cb = e.Source as System.Windows.Controls.ComboBox;
                string val = cb.SelectedValue.ToString();
                if (val == "<none>")
                    val = string.Empty;
                currentItem.Value = val;
                SaveParameter(currentItem);
                OldValue = val;
                currentItem = null;
            }
        }

        private void ComboBox_DropDownOpened(object sender, EventArgs e)
        {
            if (parameterListView.HasItems)
            {
                System.Windows.Controls.ComboBox cb = sender as System.Windows.Controls.ComboBox;
                ListViewItem lvi = FindParent<ListViewItem>(cb);
                currentItem = lvi.Content as MLTE.ParameterItems;
                parameterListView.SelectedItem = currentItem;
            }
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
