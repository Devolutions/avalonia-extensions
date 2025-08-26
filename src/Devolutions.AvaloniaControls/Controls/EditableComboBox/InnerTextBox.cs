namespace Devolutions.AvaloniaControls.Controls;

using Avalonia.Controls;
using Avalonia.Input;

public partial class EditableComboBox
{
    public class InnerTextBox : TextBox
    {
        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);
            this.SelectAll();
        }

        public void TriggerOnKeyDown(KeyEventArgs e)
        {
            this.OnKeyDown(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Handled) return;

            base.OnKeyDown(e);

            if (e.Key == Key.Up || e.Key == Key.Down) e.Handled = false;
        }
    }
}