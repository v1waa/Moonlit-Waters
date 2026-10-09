using System.Windows;

namespace Moonlit_Waters.Windows;

public class DialogWindow : Window
{
    public Task<bool> ShowDialogAsync(Window owner)
    {
        Owner = owner;
        return Task.FromResult(ShowDialog() == true);
    }

    public void Close(bool result) => DialogResult = result;
}
