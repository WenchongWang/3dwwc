using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using netDxf.Entities;

namespace Lens3DWinForms.Forms
{
    public partial class LineListForm : Form
    {
        private ListView lineListView;
        private CheckBox useReorderedCheckBox;
        private List<Line> originalLines = new();
        private List<Line>? reorderedLines;
        private int selectedLineIndex = -1;
        
        public event Action<int> LineSelected;
        
        public LineListForm()
        {
            InitializeComponent();
            SetupUI();
        }
        
        private void InitializeComponent()
        {
            this.Text = "线段列表";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.Manual;
        }
        
        private void SetupUI()
        {
            useReorderedCheckBox = new CheckBox
            {
                Text = "使用排序结果",
                Location = new System.Drawing.Point(10, 10),
                AutoSize = true
            };
            useReorderedCheckBox.CheckedChanged += UseReorderedCheckBox_CheckedChanged;

            lineListView = new ListView
            {
                Location = new System.Drawing.Point(10, 40),
                Size = new Size(560, 400),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Consolas", 9f)
            };
            
            // 添加列
            lineListView.Columns.Add("序号", 60);
            lineListView.Columns.Add("Handle", 80);
            lineListView.Columns.Add("起点 X", 100);
            lineListView.Columns.Add("起点 Y", 100);
            lineListView.Columns.Add("起点 Z", 100);
            lineListView.Columns.Add("终点 X", 100);
            lineListView.Columns.Add("终点 Y", 100);
            lineListView.Columns.Add("终点 Z", 100);
            
            lineListView.SelectedIndexChanged += LineListView_SelectedIndexChanged;
            
            this.Controls.Add(useReorderedCheckBox);
            this.Controls.Add(lineListView);
        }
        
        public void LoadLines(List<EntityObject> entities)
        {
            originalLines = entities.OfType<Line>().ToList();
            reorderedLines = null;
            useReorderedCheckBox.Checked = false;
            RefreshLineList();
        }

        public void UpdateReorderedLines(List<Line> rearrangedLines)
        {
            reorderedLines = rearrangedLines?.ToList();
            if (useReorderedCheckBox.Checked)
            {
                RefreshLineList();
            }
        }
        
        public void HighlightLine(int originalIndex)
        {
            if (originalIndex < 0)
                return;

            int targetItemIndex = -1;
            for (int i = 0; i < lineListView.Items.Count; i++)
            {
                if (lineListView.Items[i].Tag is int tagIndex && tagIndex == originalIndex)
                {
                    targetItemIndex = i;
                    break;
                }
            }

            if (targetItemIndex == -1) return;

            // 清除之前的高亮
            if (selectedLineIndex >= 0 && selectedLineIndex < lineListView.Items.Count)
            {
                lineListView.Items[selectedLineIndex].BackColor = Color.White;
                lineListView.Items[selectedLineIndex].ForeColor = Color.Black;
            }
            
            selectedLineIndex = targetItemIndex;
            var item = lineListView.Items[selectedLineIndex];
            item.Selected = true;
            item.EnsureVisible();
            item.BackColor = Color.Yellow;
            item.ForeColor = Color.Black;
        }
        
        private void LineListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lineListView.SelectedItems.Count > 0)
            {
                var selectedItem = lineListView.SelectedItems[0];
                if (selectedItem.Tag is int index)
                {
                    // 通知主窗体选中了哪条线段
                    LineSelected?.Invoke(index);
                }
            }
        }
        
        public int GetLineCount()
        {
            return GetCurrentLines()?.Count ?? 0;
        }
        
        public Line GetLine(int index)
        {
            var current = GetCurrentLines();
            if (current != null && index >= 0 && index < current.Count)
                return current[index];
            return null;
        }

        private void UseReorderedCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (useReorderedCheckBox.Checked && (reorderedLines == null || reorderedLines.Count == 0))
            {
                MessageBox.Show("尚无排序后的线段数据，请先在“线段重排”视图中重新排列。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                useReorderedCheckBox.Checked = false;
                return;
            }

            RefreshLineList();
        }

        private void RefreshLineList()
        {
            lineListView.Items.Clear();

            var source = GetCurrentLines();
            if (source == null) return;

            for (int i = 0; i < source.Count; i++)
            {
                var line = source[i];
                int originalIndex = originalLines.IndexOf(line);

                var item = new ListViewItem((i + 1).ToString());
                item.SubItems.Add(line.Handle);
                item.SubItems.Add(line.StartPoint.X.ToString("F2"));
                item.SubItems.Add(line.StartPoint.Y.ToString("F2"));
                item.SubItems.Add(line.StartPoint.Z.ToString("F2"));
                item.SubItems.Add(line.EndPoint.X.ToString("F2"));
                item.SubItems.Add(line.EndPoint.Y.ToString("F2"));
                item.SubItems.Add(line.EndPoint.Z.ToString("F2"));
                item.Tag = originalIndex >= 0 ? originalIndex : i; // 存储原始索引

                lineListView.Items.Add(item);
            }

            selectedLineIndex = -1;
        }

        private List<Line>? GetCurrentLines()
        {
            if (useReorderedCheckBox.Checked)
            {
                return reorderedLines;
            }

            return originalLines;
        }
    }
}

