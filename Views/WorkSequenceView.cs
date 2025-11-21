using System;
using System.Windows.Forms;

namespace Lens3DWinForms.Views
{
    public partial class WorkSequenceView : UserControl
    {
        public WorkSequenceView()
        {
            InitializeComponent();
            SetupUI();
        }

        private void SetupUI()
        {
            this.Dock = DockStyle.Fill;
            
            var label = new Label
            {
                Text = "工作顺序视图",
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Font = new System.Drawing.Font("Microsoft Sans Serif", 16f)
            };
            
            this.Controls.Add(label);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // WorkSequenceView
            // 
            this.Name = "WorkSequenceView";
            this.Size = new System.Drawing.Size(500, 400);
            this.ResumeLayout(false);
        }
    }
}