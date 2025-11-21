using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Lens3DWinForms.Controls;
using Lens3DWinForms.Services;
using netDxf.Entities;

namespace Lens3DWinForms.Forms
{
    public partial class StpFileForm : Form
    {
        private Button openStpButton;
        private RichTextBox fileInfoTextBox;
        private Simple3DViewport viewportPanel;
        private SplitContainer mainSplitContainer;
        private string currentFilePath = string.Empty;
        private StpLoadService stpLoadService;
        
        public StpFileForm()
        {
            InitializeComponent();
            SetupUI();
            stpLoadService = new StpLoadService();
        }
        
        private void InitializeComponent()
        {
            this.Text = "STP 文件查看器";
            this.Size = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
        }
        
        private void SetupUI()
        {
            // 主分割容器
            mainSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = this.Width - 400,
                Orientation = Orientation.Vertical
            };
            
            // 左侧面板 - 3D视图和按钮
            var leftPanel = new Panel
            {
                Dock = DockStyle.Fill
            };
            
            // 打开按钮
            openStpButton = new Button
            {
                Text = "打开 STP 文件",
                Size = new Size(120, 30),
                Location = new System.Drawing.Point(10, 10)
            };
            openStpButton.Click += OpenStpButton_Click;
            
            // 3D视口
            viewportPanel = new Simple3DViewport
            {
                Dock = DockStyle.Fill
            };
            
            leftPanel.Controls.Add(openStpButton);
            leftPanel.Controls.Add(viewportPanel);
            
            // 右侧面板 - 文件信息
            var rightPanel = new Panel
            {
                Dock = DockStyle.Fill
            };
            
            // 文件信息文本框
            fileInfoTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9f),
                BackColor = Color.Black,
                ForeColor = Color.Lime,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Both
            };
            
            rightPanel.Controls.Add(fileInfoTextBox);
            
            mainSplitContainer.Panel1.Controls.Add(leftPanel);
            mainSplitContainer.Panel2.Controls.Add(rightPanel);
            
            this.Controls.Add(mainSplitContainer);
        }
        
        private void OpenStpButton_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "STEP Files (*.stp;*.step)|*.stp;*.step|All Files (*.*)|*.*";
                openFileDialog.Title = "选择 STP 文件";
                
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    LoadStpFile(openFileDialog.FileName);
                }
            }
        }
        
        private void LoadStpFile(string filePath)
        {
            try
            {
                currentFilePath = filePath;
                
                // 使用STP加载服务
                var entities = stpLoadService.LoadStpFile(filePath);
                
                // 显示文件信息
                DisplayFileInfo(filePath, entities);
                
                // 在视口中显示
                if (entities.Count > 0)
                {
                    viewportPanel.LoadEntities(entities);
                    MessageBox.Show($"成功加载 STP 文件\n实体总数: {stpLoadService.GetEntityCount()}\n可显示实体: {entities.Count}", 
                        "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"STP文件已解析，但未能提取可显示的几何实体。\n总实体数: {stpLoadService.GetEntityCount()}", 
                        "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载 STP 文件时出错: {ex.Message}\n\n堆栈跟踪:\n{ex.StackTrace}", 
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void DisplayFileInfo(string filePath, List<EntityObject> dxfEntities)
        {
            fileInfoTextBox.Clear();
            
            fileInfoTextBox.AppendText("=== STP 文件信息 ===\n");
            fileInfoTextBox.AppendText($"文件路径: {filePath}\n");
            fileInfoTextBox.AppendText($"文件大小: {new FileInfo(filePath).Length / 1024.0:F2} KB\n\n");
            
            // 获取统计信息
            var stats = stpLoadService.GetEntityStatistics();
            int totalEntities = stpLoadService.GetEntityCount();
            
            fileInfoTextBox.AppendText($"=== 解析统计 ===\n");
            fileInfoTextBox.AppendText($"STP实体总数: {totalEntities}\n");
            fileInfoTextBox.AppendText($"可显示实体数: {dxfEntities.Count}\n\n");
            
            fileInfoTextBox.AppendText("=== STP实体类型统计 ===\n");
            foreach (var kvp in stats.OrderByDescending(x => x.Value))
            {
                fileInfoTextBox.AppendText($"{kvp.Key}: {kvp.Value}\n");
            }
            fileInfoTextBox.AppendText("\n");
            
            // 显示DXF实体统计
            if (dxfEntities.Count > 0)
            {
                fileInfoTextBox.AppendText("=== DXF实体类型统计 ===\n");
                var dxfStats = dxfEntities.GroupBy(e => e.GetType().Name)
                    .ToDictionary(g => g.Key, g => g.Count());
                
                foreach (var kvp in dxfStats.OrderByDescending(x => x.Value))
                {
                    fileInfoTextBox.AppendText($"{kvp.Key}: {kvp.Value}\n");
                }
                fileInfoTextBox.AppendText("\n");
                
                // 显示前20个实体详情
                fileInfoTextBox.AppendText("=== 实体详情（前20个）===\n");
                int displayCount = Math.Min(20, dxfEntities.Count);
                for (int i = 0; i < displayCount; i++)
                {
                    var entity = dxfEntities[i];
                    fileInfoTextBox.AppendText($"[{i + 1}] {entity.GetType().Name}\n");
                    
                    switch (entity)
                    {
                        case Line line:
                            fileInfoTextBox.AppendText($"   起点: ({line.StartPoint.X:F2}, {line.StartPoint.Y:F2}, {line.StartPoint.Z:F2})\n");
                            fileInfoTextBox.AppendText($"   终点: ({line.EndPoint.X:F2}, {line.EndPoint.Y:F2}, {line.EndPoint.Z:F2})\n");
                            break;
                        case Circle circle:
                            fileInfoTextBox.AppendText($"   圆心: ({circle.Center.X:F2}, {circle.Center.Y:F2}, {circle.Center.Z:F2})\n");
                            fileInfoTextBox.AppendText($"   半径: {circle.Radius:F2}\n");
                            break;
                    }
                    fileInfoTextBox.AppendText("\n");
                }
                
                if (dxfEntities.Count > 20)
                {
                    fileInfoTextBox.AppendText($"... 还有 {dxfEntities.Count - 20} 个实体未显示\n");
                }
            }
            else
            {
                fileInfoTextBox.AppendText("注意: 未能从STP文件中提取可显示的几何实体。\n");
                fileInfoTextBox.AppendText("这可能是因为:\n");
                fileInfoTextBox.AppendText("1. STP文件格式复杂，包含高级几何体\n");
                fileInfoTextBox.AppendText("2. 需要更完整的STEP解析器（如OpenCASCADE）\n");
                fileInfoTextBox.AppendText("3. 文件中的几何体使用了当前解析器不支持的格式\n");
            }
        }
    }
}
