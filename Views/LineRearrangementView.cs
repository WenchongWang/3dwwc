using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using netDxf.Entities;

namespace Lens3DWinForms.Views
{
    public partial class LineRearrangementView : UserControl
    {
        private List<Line> lines;
        private SplitContainer resultSplitContainer;
        private ListView resultListView;
        private RichTextBox resultTextBox;
        private List<Line> currentRearrangedLines = new();
        private readonly string rearrangeOutputDirectory;
        private Button rearrangeButton;
        private ComboBox startLineComboBox;
        public event Action<int> LineSelected;

        public event Action<List<Line>> LinesRearranged;
        
        public LineRearrangementView()
        {
            InitializeComponent();
            rearrangeOutputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LineRearrangeLogs");
            if (!System.IO.Directory.Exists(rearrangeOutputDirectory))
            {
                System.IO.Directory.CreateDirectory(rearrangeOutputDirectory);
            }
        }

        private void InitializeComponent()
        {
            this.Size = new System.Drawing.Size(500, 600);
            
            // Start line selection
            var startLabel = new Label
            {
                Text = "选择起始线段:",
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(120, 20)
            };
            
            startLineComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(130, 10),
                Size = new System.Drawing.Size(500, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            
            // Rearrange button
            rearrangeButton = new Button
            {
                Text = "重新排列线段",
                Location = new System.Drawing.Point(400, 50),
                Size = new System.Drawing.Size(120, 25)
            };
            rearrangeButton.Click += RearrangeButton_Click;
            
            resultSplitContainer = new SplitContainer
            {
                Location = new System.Drawing.Point(10, 80),
                Size = new System.Drawing.Size(480, 510),
                Orientation = Orientation.Horizontal,
                SplitterDistance = 280
            };

            resultListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            resultListView.Columns.Add("序号", 60);
            resultListView.Columns.Add("原始索引", 80);
            resultListView.Columns.Add("Handle", 100);
            resultListView.Columns.Add("起点", 120);
            resultListView.Columns.Add("终点", 120);
            resultListView.Columns.Add("到下一线段", 120);
            resultListView.SelectedIndexChanged += ResultListView_SelectedIndexChanged;

            resultTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Consolas", 9f),
                BackColor = System.Drawing.Color.Black,
                ForeColor = System.Drawing.Color.Lime,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Both
            };

            resultSplitContainer.Panel1.Controls.Add(resultListView);
            resultSplitContainer.Panel2.Controls.Add(resultTextBox);

            this.Controls.Add(startLabel);
            this.Controls.Add(startLineComboBox);
            this.Controls.Add(rearrangeButton);
            this.Controls.Add(resultSplitContainer);
        }

        public void LoadLines(List<EntityObject> entities)
        {
            lines = entities.OfType<Line>().ToList();
            
            startLineComboBox.Items.Clear();
            for (int i = 0; i < lines.Count; i++)
            {
                startLineComboBox.Items.Add($"线段 {i + 1}: ({lines[i].StartPoint.X:F2}, {lines[i].StartPoint.Y:F2}, {lines[i].StartPoint.Z:F2}) -> ({lines[i].EndPoint.X:F2}, {lines[i].EndPoint.Y:F2}, {lines[i].EndPoint.Z:F2})");
            }
            
            if (startLineComboBox.Items.Count > 0)
                startLineComboBox.SelectedIndex = 0;
        }

        private void RearrangeButton_Click(object sender, EventArgs e)
        {
            if (startLineComboBox.SelectedIndex == -1 || lines == null || lines.Count == 0)
            {
                MessageBox.Show("请先选择起始线段", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int startIndex = startLineComboBox.SelectedIndex;
            var rearrangedLines = RearrangeLines(startIndex);
            
            DisplayRearrangedLines(rearrangedLines);
            SaveRearrangedLinesToFile(rearrangedLines);
            LinesRearranged?.Invoke(rearrangedLines);
        }

        private List<Line> RearrangeLines(int startIndex)
        {
            if (lines == null || lines.Count == 0)
                return new List<Line>();

            var result = new List<Line>();
            
            // 循环排列逻辑：从startIndex开始，按顺序循环到末尾，然后从头到startIndex-1
            // 例如：如果有4个线段(0,1,2,3)，从索引2开始
            // 结果应该是：2, 3, 0, 1
            
            // 1. 从startIndex到末尾
            for (int i = startIndex; i < lines.Count; i++)
            {
                result.Add(lines[i]);
            }
            
            // 2. 从0到startIndex-1
            for (int i = 0; i < startIndex; i++)
            {
                result.Add(lines[i]);
            }
            
            return result;
        }

        private double CalculateDistance(netDxf.Vector3 point1, netDxf.Vector3 point2)
        {
            double dx = point1.X - point2.X;
            double dy = point1.Y - point2.Y;
            double dz = point1.Z - point2.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        private void DisplayRearrangedLines(List<Line> rearrangedLines)
        {
            currentRearrangedLines = rearrangedLines;

            resultListView.Items.Clear();
            resultTextBox.Clear();
            resultTextBox.AppendText("=== 线段循环排列结果 ===\n\n");

            int startIndex = startLineComboBox.SelectedIndex;
            resultTextBox.AppendText($"起始线段索引: {startIndex + 1}\n");
            resultTextBox.AppendText($"排列方式: 从线段 {startIndex + 1} 开始，按原始顺序循环排列\n\n");

            for (int i = 0; i < rearrangedLines.Count; i++)
            {
                var line = rearrangedLines[i];
                int originalIndex = lines.IndexOf(line);

                double distanceToNext;
                if (i < rearrangedLines.Count - 1)
                {
                    distanceToNext = CalculateDistance(line.EndPoint, rearrangedLines[i + 1].StartPoint);
                }
                else
                {
                    distanceToNext = CalculateDistance(line.EndPoint, rearrangedLines[0].StartPoint);
                }

                var item = new ListViewItem((i + 1).ToString());
                item.SubItems.Add((originalIndex + 1).ToString());
                item.SubItems.Add(line.Handle);
                item.SubItems.Add($"({line.StartPoint.X:F2},{line.StartPoint.Y:F2},{line.StartPoint.Z:F2})");
                item.SubItems.Add($"({line.EndPoint.X:F2},{line.EndPoint.Y:F2},{line.EndPoint.Z:F2})");
                item.SubItems.Add($"{distanceToNext:F4}");
                item.Tag = originalIndex;
                resultListView.Items.Add(item);

                resultTextBox.AppendText($"[{i + 1}] 原始索引: {originalIndex + 1}\n");
                resultTextBox.AppendText($"   线段Handle: {line.Handle}\n");
                resultTextBox.AppendText($"   起点: ({line.StartPoint.X:F2}, {line.StartPoint.Y:F2}, {line.StartPoint.Z:F2})\n");
                resultTextBox.AppendText($"   终点: ({line.EndPoint.X:F2}, {line.EndPoint.Y:F2}, {line.EndPoint.Z:F2})\n");
                resultTextBox.AppendText($"   到下一线段距离: {distanceToNext:F4}{(i == rearrangedLines.Count - 1 ? " (闭环)" : string.Empty)}\n\n");
            }

            resultTextBox.AppendText($"=== 统计信息 ===\n");
            resultTextBox.AppendText($"总线段数: {rearrangedLines.Count}\n");
            resultTextBox.AppendText($"原始线段数: {lines?.Count ?? 0}\n\n");

            resultTextBox.AppendText("=== 排列说明 ===\n");
            resultTextBox.AppendText("这是一个循环排列，从选定的起始线段开始，\n");
            resultTextBox.AppendText("按照原始顺序循环到末尾，然后从头继续到起始线段之前。\n");
            resultTextBox.AppendText("例如：如果有4个线段(1,2,3,4)，从线段2开始，\n");
            resultTextBox.AppendText("排列结果为：2, 3, 4, 1\n");
        }

        private void ResultListView_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (resultListView.SelectedItems.Count == 0) return;
            var selectedItem = resultListView.SelectedItems[0];
            if (selectedItem.Tag is int originalIndex)
            {
                LineSelected?.Invoke(originalIndex);
            }
        }

        private void SaveRearrangedLinesToFile(List<Line> rearrangedLines)
        {
            try
            {
                if (rearrangedLines == null || rearrangedLines.Count == 0)
                    return;

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"LineRearrange_{timestamp}.txt";
                string filePath = Path.Combine(rearrangeOutputDirectory, fileName);

                using (var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
                {
                    for (int i = 0; i < rearrangedLines.Count; i++)
                    {
                        var line = rearrangedLines[i];
                        int originalIndex = lines.IndexOf(line) + 1;
                        string startPoint = $"{line.StartPoint.X:F2},{line.StartPoint.Y:F2},{line.StartPoint.Z:F2}";
                        string endPoint = $"{line.EndPoint.X:F2},{line.EndPoint.Y:F2},{line.EndPoint.Z:F2}";

                        writer.WriteLine($"\t{i + 1}\tLine{originalIndex}\t\t起始点:{startPoint},终止点:{endPoint}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存重排结果文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}