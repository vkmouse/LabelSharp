using System.Windows.Forms;

namespace LabelSharp
{
    public partial class LabelSharpView : Form
    {
        private LabelSharpViewModel viewModel;

        public LabelSharpView()
        {
            InitializeComponent();
            viewModel = new LabelSharpViewModel(this);
        }
    }
}
