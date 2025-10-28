using System.Windows.Controls;

namespace Finly.Pages
{
    public partial class ImportSyncPage : UserControl
    {
        private readonly string? _section;

        public ImportSyncPage()
        {
            InitializeComponent();
        }

        public ImportSyncPage(string? sectionKey) : this()
        {
            _section = sectionKey;
        }
    }
}
