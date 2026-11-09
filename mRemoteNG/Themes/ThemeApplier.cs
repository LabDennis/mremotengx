using System.Drawing;
using System.Windows.Forms;

namespace mRemoteNG.Themes
{
    /// <summary>
    /// Walks a control tree and maps the active theme's extended palette onto
    /// standard WinForms controls. mrng* controls own their theming and are skipped.
    /// </summary>
    public static class ThemeApplier
    {
        public static void Apply(Control root)
        {
            ThemeManager manager = ThemeManager.getInstance();
            if (!manager.ActiveAndExtended) return;
            ExtendedColorPalette palette = manager.ActiveTheme.ExtendedPalette;
            ApplyTo(root, palette);
        }

        /// <summary>Applies the theme now and re-applies it on every theme change.</summary>
        public static void Hook(Control root)
        {
            Apply(root);
            ThemeManager.getInstance().ThemeChanged += () => Apply(root);
        }

        private static void ApplyTo(Control control, ExtendedColorPalette palette)
        {
            switch (control)
            {
                case PropertyGrid grid:
                    StylePropertyGrid(grid, palette);
                    break;
                case TreeView treeView:
                    treeView.BackColor = palette.getColor("TreeView_Background");
                    treeView.ForeColor = palette.getColor("TreeView_Foreground");
                    treeView.LineColor = palette.getColor("TreeView_Foreground");
                    break;
                case ListView listView:
                    listView.BackColor = palette.getColor("List_Background");
                    listView.ForeColor = palette.getColor("List_Item_Foreground");
                    break;
                case ListBox listBox:
                    listBox.BackColor = palette.getColor("List_Background");
                    listBox.ForeColor = palette.getColor("List_Item_Foreground");
                    break;
                case ComboBox comboBox:
                    comboBox.BackColor = palette.getColor("ComboBox_Background");
                    comboBox.ForeColor = palette.getColor("ComboBox_Foreground");
                    break;
                case TextBoxBase textBox:
                    textBox.BackColor = palette.getColor("TextBox_Background");
                    textBox.ForeColor = palette.getColor("TextBox_Foreground");
                    break;
                case CheckBox checkBox:
                    checkBox.BackColor = palette.getColor("CheckBox_Background");
                    checkBox.ForeColor = palette.getColor("CheckBox_Text");
                    break;
                case RadioButton radioButton:
                    radioButton.BackColor = palette.getColor("CheckBox_Background");
                    radioButton.ForeColor = palette.getColor("CheckBox_Text");
                    break;
                case ButtonBase button:
                    button.BackColor = palette.getColor("Button_Background");
                    button.ForeColor = palette.getColor("Button_Foreground");
                    break;
                case GroupBox groupBox:
                    groupBox.BackColor = palette.getColor("GroupBox_Backgorund");
                    groupBox.ForeColor = palette.getColor("GroupBox_Foreground");
                    break;
                case Label label:
                    label.BackColor = palette.getColor("Dialog_Background");
                    label.ForeColor = palette.getColor("Dialog_Foreground");
                    break;
                case ProgressBar progressBar:
                    progressBar.BackColor = palette.getColor("ProgressBar_Background");
                    progressBar.ForeColor = palette.getColor("ProgressBar_Fill");
                    break;
                case TabControl _:
                case ScrollableControl _:
                    control.BackColor = palette.getColor("Dialog_Background");
                    control.ForeColor = palette.getColor("Dialog_Foreground");
                    break;
            }

            foreach (Control child in control.Controls)
            {
                // mrng* controls own their theming.
                if (child.GetType().Name.StartsWith("Mrng"))
                    continue;
                ApplyTo(child, palette);
            }
        }

        private static void StylePropertyGrid(PropertyGrid grid, ExtendedColorPalette palette)
        {
            Color dialogBack = palette.getColor("Dialog_Background");
            Color dialogFore = palette.getColor("Dialog_Foreground");
            Color listBack = palette.getColor("List_Background");
            Color listFore = palette.getColor("List_Item_Foreground");

            grid.BackColor = dialogBack;
            grid.LineColor = dialogBack;
            grid.ViewBackColor = listBack;
            grid.ViewForeColor = listFore;
            grid.CategoryForeColor = listFore;
            grid.CategorySplitterColor = listBack;
            grid.HelpBackColor = dialogBack;
            grid.HelpForeColor = dialogFore;
            grid.CommandsBackColor = dialogBack;
            grid.CommandsForeColor = dialogFore;
            grid.DisabledItemForeColor = palette.getColor("List_Item_Disabled_Foreground");
            grid.SelectedItemWithFocusBackColor = palette.getColor("List_Item_Selected_Background");
            grid.SelectedItemWithFocusForeColor = palette.getColor("List_Item_Selected_Foreground");
            // The visual style glyphs render light-theme colors regardless of the palette.
            grid.CanShowVisualStyleGlyphs = false;
        }
    }
}
