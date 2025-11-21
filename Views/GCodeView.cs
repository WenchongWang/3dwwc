using System;
using System.Windows.Forms;

namespace Lens3DWinForms.Views
{
    public partial class GCodeView : UserControl
    {
        public GCodeView()
        {
            InitializeComponent();
            SetupUI();
        }

        private void SetupUI()
        {
            this.Dock = DockStyle.Fill;
            
            var label = new Label
            {
                Text = "G代码视图",
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
            // GCodeView
            // 
            this.Name = "GCodeView";
            this.Size = new System.Drawing.Size(500, 400);
            this.ResumeLayout(false);
        }
    }
}