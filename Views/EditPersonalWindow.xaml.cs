using System.Windows;
using Finly.ViewModels;

namespace Finly.Views
{
    public partial class EditPersonalWindow : Window
    {
        private readonly EditPersonalViewModel _vm;

        public EditPersonalWindow(int userId)
        {
            InitializeComponent();
            _vm = new EditPersonalViewModel(userId);
            DataContext = _vm;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.Save())
            {
                DialogResult = true;
                Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
