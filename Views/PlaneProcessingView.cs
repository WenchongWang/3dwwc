using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using netDxf.Entities;
using Lens3DWinForms.Models;

namespace Lens3DWinForms.Views
{
    public partial class PlaneProcessingView : UserControl
    {
        private List<PlaneData> planes;
        private RichTextBox resultTextBox;
        private Button processButton;
        
        public PlaneProcessingView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Size = new System.Drawing.Size(500, 600);
            
            // Process button
            processButton = new Button
            {
                Text = "处理平面",
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(120, 25)
            };
            processButton.Click += ProcessButton_Click;
            
            // Result text box
            resultTextBox = new RichTextBox
            {
                Location = new System.Drawing.Point(10, 40),
                Size = new System.Drawing.Size(480, 550),
                Font = new System.Drawing.Font("Consolas", 9f),
                BackColor = System.Drawing.Color.Black,
                ForeColor = System.Drawing.Color.Lime,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Both
            };
            
            this.Controls.Add(processButton);
            this.Controls.Add(resultTextBox);
        }

        public void LoadPlanes(List<PlaneData> planeData)
        {
            planes = planeData;
        }

        private void ProcessButton_Click(object sender, EventArgs e)
        {
            if (planes == null || planes.Count == 0)
            {
                MessageBox.Show("没有可处理的平面数据", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var processedLines = ProcessPlanesToLines();
            DisplayProcessedLines(processedLines);
        }

        private List<Line> ProcessPlanesToLines()
        {
            var processedLines = new List<Line>();
            
            foreach (var plane in planes)
            {
                // 删除第三个点，用前两个点创建线段
                if (plane.Points.Count >= 2)
                {
                    // 取前两个点创建线段
                    var point1 = plane.Points[0];
                    var point2 = plane.Points[1];
                    
                    var line = new Line(point1, point2)
                    {
                        Color = plane.Line1.Color,
                        Layer = plane.Line1.Layer
                    };
                    
                    processedLines.Add(line);
                }
            }
            
            return processedLines;
        }

        private void DisplayProcessedLines(List<Line> processedLines)
        {
            resultTextBox.Clear();
            resultTextBox.AppendText("=== 平面处理结果 ===\n\n");
            
            for (int i = 0; i < processedLines.Count; i++)
            {
                var line = processedLines[i];
                resultTextBox.AppendText($"[{i + 1}] 线段: {i + 1}\n");
                resultTextBox.AppendText($"   起点: ({line.StartPoint.X:F2}, {line.StartPoint.Y:F2}, {line.StartPoint.Z:F2})\n");
                resultTextBox.AppendText($"   终点: ({line.EndPoint.X:F2}, {line.EndPoint.Y:F2}, {line.EndPoint.Z:F2})\n");
                resultTextBox.AppendText("\n");
            }
            
            resultTextBox.AppendText("=== 统计信息 ===\n");
            resultTextBox.AppendText($"总线段数: {processedLines.Count}\n");
            resultTextBox.AppendText($"原始平面数: {planes?.Count ?? 0}\n");
        }
    }
}