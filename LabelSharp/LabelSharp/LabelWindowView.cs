using System.Drawing;
using System.Windows.Forms;

namespace LabelSharp
{
    partial class LabelWindowView : Form
    {
        private static LabelWindowView _instance = new LabelWindowView();

        private LabelWindowViewModel _viewModel;
        public LabelWindowViewModel ViewModel
        {
            get => _viewModel;
        }

        public static string ClassName
        {
            get => _instance.txtName.Text;
            set => _instance.txtName.Text = value;
        }

        private LabelWindowView()
        {
            InitializeComponent();
            _viewModel = new LabelWindowViewModel(this);
        }

        public static bool Show(Point? location = null)
        {
            // Before display
            _instance.lstOtherName.SelectedIndex = -1;
            _instance.txtName.SelectAll();
            if (location != null)
            {
                _instance.StartPosition = FormStartPosition.Manual;
                _instance.Location = (Point)location;
            }

            // Display
            _instance.ShowDialog();

            // After display
            if (!_instance.ViewModel.IsCancel)
            {
                if (!_instance.lstOtherName.Items.Contains(ClassName))
                    _instance.lstOtherName.Items.Add(ClassName);

                return false;
            }

            return true;
        }
    }
}
