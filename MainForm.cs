using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows.Forms;
using Lens3DWinForms.Services;
using Lens3DWinForms.Views;
using Lens3DWinForms.Controls;
using Lens3DWinForms.Forms;
using netDxf.Entities;

namespace Lens3DWinForms
{
    public partial class MainForm : Form
    {
        private Simple3DViewport viewportPanel;
        private Button openButton;
        private Button saveButton;
        private Button showLineListButton;
        private Button openStpFileButton;
        private Button frontViewButton;
        private Button backViewButton;
        private Button leftViewButton;
        private Button rightViewButton;
        private Button topViewButton;
        private Button bottomViewButton;
        private Button drawLineButton;
        private Button drawCircleButton;
        private Button drawArcButton;
        private Button cancelDrawButton;
        private CheckBox autoSnapCheckBox;
        private Button inputEndPointButton;
        private TabControl tabControl;
        private TabPage workSequenceTab;
        private TabPage workSequenceTabPage;
        private TabPage gCodeTab;
        private TabPage dataDisplayTab;
        private TabPage lineRearrangementTab;
        private TabPage planeOptimizationTab;
        private TabPage planeProcessingTab;
        private RichTextBox dataDisplayTextBox;
        private DxfLoadService dxfLoadService;
        private string currentDxfFilePath = string.Empty; // 当前打开的DXF文件路径
        private LineRearrangementView lineRearrangementView;
        private PlaneOptimizationView planeOptimizationView;
        private PlaneProcessingView planeProcessingView;
        private LineListForm lineListForm;
        private List<EntityObject> currentEntities = new();
        
        public MainForm()
        {
            InitializeComponent();
            dxfLoadService = new DxfLoadService();
            
            // Initialize UI components to avoid nullability warnings
            viewportPanel = new Simple3DViewport();
            openButton = new Button();
            saveButton = new Button();
            showLineListButton = new Button();
            openStpFileButton = new Button();
            frontViewButton = new Button();
            backViewButton = new Button();
            leftViewButton = new Button();
            rightViewButton = new Button();
            topViewButton = new Button();
            bottomViewButton = new Button();
            drawLineButton = new Button();
            drawCircleButton = new Button();
            drawArcButton = new Button();
            cancelDrawButton = new Button();
            autoSnapCheckBox = new CheckBox();
            inputEndPointButton = new Button();
            tabControl = new TabControl();
            lineListForm = new LineListForm();
            workSequenceTab = new TabPage();
            gCodeTab = new TabPage();
            dataDisplayTab = new TabPage();
            lineRearrangementTab = new TabPage();
            planeOptimizationTab = new TabPage();
            dataDisplayTextBox = new RichTextBox();
            lineRearrangementView = new LineRearrangementView();
            lineRearrangementView.LinesRearranged += LineRearrangementView_LinesRearranged;
            planeOptimizationView = new PlaneOptimizationView();
            planeProcessingView = new PlaneProcessingView();
            
            SetupUI();
        }

        private void SetupUI()
        {
            // Main form setup
            this.Text = "LENS3D - WinForms";
            this.Size = new System.Drawing.Size(1400, 800);
            this.BackColor = System.Drawing.Color.White;

            // Create layout panels
            var mainSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = this.Width - 550
            };

            // Left panel for 3D view
            var leftPanel = new Panel
            {
                Dock = DockStyle.Fill
            };

            // Button container panel
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 200
            };

            // Right panel for tabs
            var rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Width = 550
            };

            // Open button
            openButton = new Button
            {
                Text = "打开",
                Size = new System.Drawing.Size(75, 30),
                Location = new System.Drawing.Point(10, 10)
            };
            openButton.Click += OpenButton_Click;
            
            // Save button
            saveButton = new Button
            {
                Text = "保存",
                Size = new System.Drawing.Size(75, 30),
                Location = new System.Drawing.Point(95, 10),
                Enabled = false
            };
            saveButton.Click += SaveButton_Click;
            
            // Show line list button
            showLineListButton = new Button
            {
                Text = "线段列表",
                Size = new System.Drawing.Size(75, 30),
                Location = new System.Drawing.Point(180, 10),
                Enabled = false
            };
            showLineListButton.Click += ShowLineListButton_Click;
            
            // Open STP file button
            openStpFileButton = new Button
            {
                Text = "STP文件",
                Size = new System.Drawing.Size(75, 30),
                Location = new System.Drawing.Point(265, 10)
            };
            openStpFileButton.Click += OpenStpFileButton_Click;

            // 视图切换按钮 - 第一行
            frontViewButton = new Button
            {
                Text = "前视图",
                Size = new System.Drawing.Size(75, 30),
                Location = new System.Drawing.Point(10, 50)
            };
            frontViewButton.Click += (s, e) => viewportPanel.SetStandardView("Front");

            leftViewButton = new Button
            {
                Text = "左视图",
                Size = new System.Drawing.Size(75, 30),
                Location = new System.Drawing.Point(95, 50)
            };
            leftViewButton.Click += (s, e) => viewportPanel.SetStandardView("Left");

            rightViewButton = new Button
            {
                Text = "右视图",
                Size = new System.Drawing.Size(75, 30),
                Location = new System.Drawing.Point(180, 50)
            };
            rightViewButton.Click += (s, e) => viewportPanel.SetStandardView("Right");

            // 视图切换按钮 - 第二行
            backViewButton = new Button
            {
                Text = "后视图",
                Size = new System.Drawing.Size(75, 30),
                Location = new System.Drawing.Point(10, 90)
            };
            backViewButton.Click += (s, e) => viewportPanel.SetStandardView("Back");

            topViewButton = new Button
            {
                Text = "上视图",
                Size = new System.Drawing.Size(75, 30),
                Location = new System.Drawing.Point(95, 90)
            };
            topViewButton.Click += (s, e) => viewportPanel.SetStandardView("Top");

            bottomViewButton = new Button
            {
                Text = "下视图",
                Size = new System.Drawing.Size(75, 30),
                Location = new System.Drawing.Point(180, 90)
            };
            bottomViewButton.Click += (s, e) => viewportPanel.SetStandardView("Bottom");

            // 绘制工具按钮 - 第三行
            drawLineButton = new Button
            {
                Text = "绘制直线",
                Size = new System.Drawing.Size(75, 30),
                Location = new System.Drawing.Point(10, 130)
            };
            drawLineButton.Click += (s, e) => 
            {
                viewportPanel.SetDrawMode(Simple3DViewport.DrawMode.Line);
                UpdateDrawButtonStates(drawLineButton);
            };

            drawCircleButton = new Button
            {
                Text = "绘制圆",
                Size = new System.Drawing.Size(75, 30),
                Location = new System.Drawing.Point(95, 130)
            };
            drawCircleButton.Click += (s, e) => 
            {
                viewportPanel.SetDrawMode(Simple3DViewport.DrawMode.Circle);
                UpdateDrawButtonStates(drawCircleButton);
            };

            drawArcButton = new Button
            {
                Text = "绘制圆弧",
                Size = new System.Drawing.Size(75, 30),
                Location = new System.Drawing.Point(180, 130)
            };
            drawArcButton.Click += (s, e) => 
            {
                viewportPanel.SetDrawMode(Simple3DViewport.DrawMode.Arc);
                UpdateDrawButtonStates(drawArcButton);
            };

            cancelDrawButton = new Button
            {
                Text = "取消绘制",
                Size = new System.Drawing.Size(75, 30),
                Location = new System.Drawing.Point(265, 130),
                Enabled = false
            };
            cancelDrawButton.Click += (s, e) => 
            {
                viewportPanel.SetDrawMode(Simple3DViewport.DrawMode.None);
                UpdateDrawButtonStates(null);
            };

            // 自动吸附到端点选项
            autoSnapCheckBox = new CheckBox
            {
                Text = "自动吸附到端点",
                Size = new System.Drawing.Size(120, 25),
                Location = new System.Drawing.Point(10, 165),
                AutoSize = true
            };
            autoSnapCheckBox.CheckedChanged += (s, e) => 
            {
                viewportPanel.SetAutoSnapToEndpoints(autoSnapCheckBox.Checked);
            };

            // 输入终点坐标按钮
            inputEndPointButton = new Button
            {
                Text = "输入终点坐标",
                Size = new System.Drawing.Size(100, 30),
                Location = new System.Drawing.Point(140, 160),
                Enabled = false
            };
            inputEndPointButton.Click += InputEndPointButton_Click;

            // 3D Viewport Panel - 使用 Simple3DViewport (GDI+ 渲染，更稳定)
            viewportPanel = new Simple3DViewport
            {
                Dock = DockStyle.Fill
            };
            
            // 连接视口的线段选中事件
            viewportPanel.LineSelected += ViewportPanel_LineSelected;
            
            // 连接视口的新实体添加事件
            viewportPanel.EntityAdded += ViewportPanel_EntityAdded;
            
            // 连接线段列表窗体的选中事件
            lineListForm.LineSelected += LineListForm_LineSelected;
            lineListForm.LinesOrderChanged += LineListForm_LinesOrderChanged;

            // Tab control for right panel
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // Work sequence tab
            workSequenceTab = new TabPage("工作顺序");
            var workSequenceView = new WorkSequenceView();
            workSequenceTab.Controls.Add(workSequenceView);


            //w
            workSequenceTabPage=new TabPage("工作顺序2");



            // G-code tab
            gCodeTab = new TabPage("G代码");
            var gCodeView = new GCodeView();
            gCodeTab.Controls.Add(gCodeView);

            // Data display tab
            dataDisplayTab = new TabPage("数据展示");
            dataDisplayTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Consolas", 10f),
                BackColor = System.Drawing.Color.Black,
                ForeColor = System.Drawing.Color.Lime,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Both
            };
            dataDisplayTab.Controls.Add(dataDisplayTextBox);

            // Line rearrangement tab
            lineRearrangementTab = new TabPage("线段重排");
            lineRearrangementView = new LineRearrangementView();
            lineRearrangementView.LinesRearranged += LineRearrangementView_LinesRearranged;
            lineRearrangementTab.Controls.Add(lineRearrangementView);

            // Plane optimization tab
            planeOptimizationTab = new TabPage("平面优化");
            planeOptimizationView = new PlaneOptimizationView();
            planeOptimizationTab.Controls.Add(planeOptimizationView);

            // Plane processing tab
            planeProcessingTab = new TabPage("平面处理");
            planeProcessingView = new PlaneProcessingView();
            planeProcessingTab.Controls.Add(planeProcessingView);





            tabControl.TabPages.Add(workSequenceTab);
            tabControl.TabPages.Add(gCodeTab);
            tabControl.TabPages.Add(dataDisplayTab);
            tabControl.TabPages.Add(lineRearrangementTab);
            tabControl.TabPages.Add(planeOptimizationTab);
            tabControl.TabPages.Add(planeProcessingTab);

            tabControl.TabPages.Add(workSequenceTabPage);
            // Add buttons to button panel
            buttonPanel.Controls.Add(openButton);
            buttonPanel.Controls.Add(saveButton);
            buttonPanel.Controls.Add(showLineListButton);
            buttonPanel.Controls.Add(openStpFileButton);
            buttonPanel.Controls.Add(frontViewButton);
            buttonPanel.Controls.Add(backViewButton);
            buttonPanel.Controls.Add(leftViewButton);
            buttonPanel.Controls.Add(rightViewButton);
            buttonPanel.Controls.Add(topViewButton);
            buttonPanel.Controls.Add(bottomViewButton);
            buttonPanel.Controls.Add(drawLineButton);
            buttonPanel.Controls.Add(drawCircleButton);
            buttonPanel.Controls.Add(drawArcButton);
            buttonPanel.Controls.Add(cancelDrawButton);
            buttonPanel.Controls.Add(autoSnapCheckBox);
            buttonPanel.Controls.Add(inputEndPointButton);
            
            // Add controls to left panel
            leftPanel.Controls.Add(viewportPanel);
            leftPanel.Controls.Add(buttonPanel);
            
            rightPanel.Controls.Add(tabControl);

            mainSplitContainer.Panel1.Controls.Add(leftPanel);
            mainSplitContainer.Panel2.Controls.Add(rightPanel);

            this.Controls.Add(mainSplitContainer);
        }

        private void OpenButton_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "DXF Files (*.dxf)|*.dxf|All Files (*.*)|*.*";
                
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;
                    LoadDxfFile(filePath);
                }
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            try
            {
                // 获取当前实体列表（包括原有的和新绘制的）
                var allEntities = (currentEntities != null && currentEntities.Count > 0)
                    ? new List<EntityObject>(currentEntities)
                    : viewportPanel.GetEntities();
                
                if (allEntities == null || allEntities.Count == 0)
                {
                    MessageBox.Show("没有可保存的实体", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string saveFilePath = currentDxfFilePath;
                
                // 如果没有当前文件路径，或者用户想另存为，显示保存对话框
                if (string.IsNullOrEmpty(saveFilePath))
                {
                    using (var saveFileDialog = new SaveFileDialog())
                    {
                        saveFileDialog.Filter = "DXF Files (*.dxf)|*.dxf|All Files (*.*)|*.*";
                        saveFileDialog.DefaultExt = "dxf";
                        saveFileDialog.FileName = "drawing.dxf";
                        
                        if (saveFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            saveFilePath = saveFileDialog.FileName;
                        }
                        else
                        {
                            return; // 用户取消了保存
                        }
                    }
                }
                else
                {
                    // 如果已有文件路径，询问是否保存到原文件或另存为
                    var result = MessageBox.Show(
                        $"是否保存到原文件？\n原文件: {saveFilePath}\n\n点击'是'保存到原文件，点击'否'另存为", 
                        "保存DXF文件", 
                        MessageBoxButtons.YesNoCancel, 
                        MessageBoxIcon.Question);
                    
                    if (result == DialogResult.Cancel)
                    {
                        return;
                    }
                    else if (result == DialogResult.No)
                    {
                        // 另存为
                        using (var saveFileDialog = new SaveFileDialog())
                        {
                            saveFileDialog.Filter = "DXF Files (*.dxf)|*.dxf|All Files (*.*)|*.*";
                            saveFileDialog.DefaultExt = "dxf";
                            saveFileDialog.FileName = System.IO.Path.GetFileNameWithoutExtension(saveFilePath) + "_modified.dxf";
                            
                            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                            {
                                saveFilePath = saveFileDialog.FileName;
                            }
                            else
                            {
                                return; // 用户取消了保存
                            }
                        }
                    }
                }

                // 保存DXF文件
                bool success = dxfLoadService.SaveDxfEntities(allEntities, saveFilePath);
                
                if (success)
                {
                    currentDxfFilePath = saveFilePath; // 更新当前文件路径
                    MessageBox.Show($"成功保存 DXF 文件\n文件路径: {saveFilePath}\n实体数量: {allEntities.Count}", 
                        "保存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存 DXF 文件时出错: {ex.Message}\n\n堆栈跟踪:\n{ex.StackTrace}", 
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadDxfFile(string filePath)
        {
            try
            {
                // 保存当前文件路径
                currentDxfFilePath = filePath;
                
                // Use the DXF loading service for 3D rendering
                var entities = dxfLoadService.LoadDxfEntitiesFor3D(filePath);
                currentEntities = entities.ToList();
                
                // Pass entities to viewport for rendering
                viewportPanel.LoadEntities(currentEntities);
                
                // Load lines to line list form
                lineListForm.LoadLines(currentEntities);
                showLineListButton.Enabled = true;
                saveButton.Enabled = true; // 启用保存按钮
                
                // Display parsed data in the text box
                DisplayParsedData(currentEntities, filePath);
                
                // Pass line entities to rearrangement view
                lineRearrangementView.LoadLines(currentEntities);
                
                // Pass line entities to plane optimization view
                planeOptimizationView.LoadLines(currentEntities);
                
                // Get optimized planes and pass to plane processing view
                var optimizedPlanes = planeOptimizationView.GetOptimizedPlanes();
                planeProcessingView.LoadPlanes(optimizedPlanes);

                // Switch to data display tab
                tabControl.SelectedTab = dataDisplayTab;
                
                MessageBox.Show($"成功加载 DXF 文件，包含 {entities.Count} 个实体", "成功", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading DXF file: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void DisplayParsedData(List<EntityObject> entities, string filePath)
        {
            dataDisplayTextBox.Clear();
            
            // Display file information
            dataDisplayTextBox.AppendText($"=== DXF 文件解析结果 ===\n");
            dataDisplayTextBox.AppendText($"文件路径: {filePath}\n");
            dataDisplayTextBox.AppendText($"实体总数: {entities.Count}\n\n");
            
            // Count entities by type
            var entityCounts = new Dictionary<string, int>();
            foreach (var entity in entities)
            {
                string typeName = entity.GetType().Name;
                if (entityCounts.ContainsKey(typeName))
                    entityCounts[typeName]++;
                else
                    entityCounts[typeName] = 1;
            }
            
            // Display entity type counts
            dataDisplayTextBox.AppendText("=== 实体类型统计 ===\n");
            foreach (var kvp in entityCounts)
            {
                dataDisplayTextBox.AppendText($"{kvp.Key}: {kvp.Value}\n");
            }
            dataDisplayTextBox.AppendText("\n");
            
            // Display detailed entity information
            dataDisplayTextBox.AppendText("=== 实体详细信息 ===\n");
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                dataDisplayTextBox.AppendText($"[{i + 1}] {entity.GetType().Name}: {entity.Handle}\n");
                
                // Display specific properties based on entity type
                switch (entity)
                {
                    case netDxf.Entities.Line line:
                        dataDisplayTextBox.AppendText($"   起点: ({line.StartPoint.X:F2}, {line.StartPoint.Y:F2}, {line.StartPoint.Z:F2})\n");
                        dataDisplayTextBox.AppendText($"   终点: ({line.EndPoint.X:F2}, {line.EndPoint.Y:F2}, {line.EndPoint.Z:F2})\n");
                        break;
                        
                    case netDxf.Entities.Circle circle:
                        dataDisplayTextBox.AppendText($"   圆心: ({circle.Center.X:F2}, {circle.Center.Y:F2}, {circle.Center.Z:F2})\n");
                        dataDisplayTextBox.AppendText($"   半径: {circle.Radius:F2}\n");
                        break;
                        
                    case netDxf.Entities.Arc arc:
                        dataDisplayTextBox.AppendText($"   圆心: ({arc.Center.X:F2}, {arc.Center.Y:F2}, {arc.Center.Z:F2})\n");
                        dataDisplayTextBox.AppendText($"   半径: {arc.Radius:F2}\n");
                        dataDisplayTextBox.AppendText($"   起始角: {arc.StartAngle:F2}°\n");
                        dataDisplayTextBox.AppendText($"   终止角: {arc.EndAngle:F2}°\n");
                        break;
                        
                    case netDxf.Entities.Polyline2D poly2d:
                        dataDisplayTextBox.AppendText($"   顶点数: {poly2d.Vertexes.Count}\n");
                        if (poly2d.Vertexes.Count > 0)
                        {
                            dataDisplayTextBox.AppendText($"   第一个顶点: ({poly2d.Vertexes[0].Position.X:F2}, {poly2d.Vertexes[0].Position.Y:F2})\n");
                            dataDisplayTextBox.AppendText($"   最后一个顶点: ({poly2d.Vertexes[poly2d.Vertexes.Count - 1].Position.X:F2}, {poly2d.Vertexes[poly2d.Vertexes.Count - 1].Position.Y:F2})\n");
                        }
                        break;
                        
                    case netDxf.Entities.Polyline3D poly3d:
                        dataDisplayTextBox.AppendText($"   顶点数: {poly3d.Vertexes.Count}\n");
                        if (poly3d.Vertexes.Count > 0)
                        {
                            dataDisplayTextBox.AppendText($"   第一个顶点: ({poly3d.Vertexes[0].X:F2}, {poly3d.Vertexes[0].Y:F2}, {poly3d.Vertexes[0].Z:F2})\n");
                            dataDisplayTextBox.AppendText($"   最后一个顶点: ({poly3d.Vertexes[poly3d.Vertexes.Count - 1].X:F2}, {poly3d.Vertexes[poly3d.Vertexes.Count - 1].Y:F2}, {poly3d.Vertexes[poly3d.Vertexes.Count - 1].Z:F2})\n");
                        }
                        break;
                        
                    case netDxf.Entities.Ellipse ellipse:
                        dataDisplayTextBox.AppendText($"   圆心: ({ellipse.Center.X:F2}, {ellipse.Center.Y:F2}, {ellipse.Center.Z:F2})\n");
                        dataDisplayTextBox.AppendText($"   长轴: {ellipse.MajorAxis:F2}\n");
                        dataDisplayTextBox.AppendText($"   短轴: {ellipse.MinorAxis:F2}\n");
                        dataDisplayTextBox.AppendText($"   起始角: {ellipse.StartAngle:F2}°\n");
                        dataDisplayTextBox.AppendText($"   终止角: {ellipse.EndAngle:F2}°\n");
                        break;
                        
                    case netDxf.Entities.Insert insert:
                        dataDisplayTextBox.AppendText($"   插入点: ({insert.Position.X:F2}, {insert.Position.Y:F2}, {insert.Position.Z:F2})\n");
                        dataDisplayTextBox.AppendText($"   块名: {insert.Block?.Name ?? "无"}\n");
                        dataDisplayTextBox.AppendText($"   缩放: X={insert.Scale.X:F2}, Y={insert.Scale.Y:F2}, Z={insert.Scale.Z:F2}\n");
                        dataDisplayTextBox.AppendText($"   旋转: {insert.Rotation:F2}°\n");
                        break;
                        
                    default:
                        dataDisplayTextBox.AppendText($"   类型: {entity.GetType().Name}\n");
                        dataDisplayTextBox.AppendText($"   图层: {entity.Layer.Name}\n");
                        dataDisplayTextBox.AppendText($"   颜色: {entity.Color.Index}\n");
                        break;
                }
                dataDisplayTextBox.AppendText("\n");
            }
        }

        private void ShowLineListButton_Click(object sender, EventArgs e)
        {
            if (lineListForm != null)
            {
                lineListForm.Show();


                lineListForm.TopLevel = false;
                lineListForm.Dock = DockStyle.Fill;
                lineListForm.FormBorderStyle = FormBorderStyle.None;
                //dForm.Show();
                //this.tabControl8.TabPages[2].Controls.Add(dForm);
                lineListForm.Show();

                //  lineListForm.
                //  lineListForm.BringToFront();
                workSequenceTabPage.Controls.Add(lineListForm);
                lineListForm.Show();
                // tabControl.TabPages[0].Controls.Add(lineListForm);
            }
        }
        
        private void OpenStpFileButton_Click(object sender, EventArgs e)
        {
            var stpForm = new StpFileForm();
            stpForm.Show();
        }
        
        private void ViewportPanel_LineSelected(int lineIndex)
        {
            // 视口中选中线段时，高亮列表中的对应项
            if (lineListForm != null && lineListForm.Visible)
            {
                lineListForm.HighlightLine(lineIndex);
            }
        }
        
        private void LineListForm_LineSelected(int lineIndex)
        {
            // 列表中选中线段时，高亮视口中的对应线段
            if (viewportPanel != null)
            {
                viewportPanel.HighlightLine(lineIndex);
            }
        }

        private void LineRearrangementView_LinesRearranged(List<Line> reorderedLines)
        {
            if (reorderedLines == null || reorderedLines.Count == 0)
                return;

            lineListForm?.UpdateReorderedLines(reorderedLines);
        }

        private void LineListForm_LinesOrderChanged(List<Line> reorderedLines)
        {
            if (reorderedLines == null || reorderedLines.Count == 0 || currentEntities == null || currentEntities.Count == 0)
                return;

            var newEntities = new List<EntityObject>(currentEntities.Count);
            int lineIndex = 0;

            foreach (var entity in currentEntities)
            {
                if (entity is Line && lineIndex < reorderedLines.Count)
                {
                    newEntities.Add(reorderedLines[lineIndex]);
                    lineIndex++;
                }
                else
                {
                    newEntities.Add(entity);
                }
            }

            currentEntities = newEntities;
            viewportPanel.UpdateEntitiesOrder(currentEntities);

            // 更新其它视图
            var entityCopy = new List<EntityObject>(currentEntities);
            lineRearrangementView.LoadLines(entityCopy);
            planeOptimizationView.LoadLines(entityCopy);
            var optimizedPlanes = planeOptimizationView.GetOptimizedPlanes();
            planeProcessingView.LoadPlanes(optimizedPlanes);

            // 数据展示更新
            DisplayParsedData(entityCopy, string.IsNullOrEmpty(currentDxfFilePath) ? "手动重排" : currentDxfFilePath);
        }

        private void ViewportPanel_EntityAdded(netDxf.Entities.EntityObject entity)
        {
            // 当新实体被添加时，更新相关视图
            if (entity != null)
            {
                // 更新线段列表
                var allEntities = viewportPanel.GetEntities();
                currentEntities = allEntities;
                lineListForm?.LoadLines(allEntities);
                
                // 更新线段重排视图
                lineRearrangementView?.LoadLines(allEntities);
                
                // 更新平面优化视图
                planeOptimizationView?.LoadLines(allEntities);
                
                // 更新数据展示
                DisplayParsedData(allEntities, "手动绘制");
                
                // 切换到数据展示标签页
                tabControl.SelectedTab = dataDisplayTab;
            }
        }

        private void UpdateDrawButtonStates(Button activeButton)
        {
            // 重置所有按钮状态
            drawLineButton.BackColor = SystemColors.Control;
            drawCircleButton.BackColor = SystemColors.Control;
            drawArcButton.BackColor = SystemColors.Control;
            
            // 高亮激活的按钮
            if (activeButton != null)
            {
                activeButton.BackColor = Color.LightGreen;
                cancelDrawButton.Enabled = true;
                
                // 只有绘制直线模式才启用输入终点按钮
                inputEndPointButton.Enabled = (activeButton == drawLineButton);
            }
            else
            {
                cancelDrawButton.Enabled = false;
                inputEndPointButton.Enabled = false;
            }
        }

        private void InputEndPointButton_Click(object sender, EventArgs e)
        {
            // 检查是否正在绘制直线
            if (viewportPanel.GetDrawMode() != Simple3DViewport.DrawMode.Line)
            {
                MessageBox.Show("请先进入绘制直线模式", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 创建输入对话框
            using (var inputForm = new Form())
            {
                inputForm.Text = "输入终点坐标";
                inputForm.Size = new System.Drawing.Size(300, 180);
                inputForm.StartPosition = FormStartPosition.CenterParent;
                inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                inputForm.MaximizeBox = false;
                inputForm.MinimizeBox = false;

                var labelX = new Label { Text = "X:", Location = new System.Drawing.Point(20, 20), Size = new System.Drawing.Size(30, 20) };
                var textBoxX = new TextBox { Location = new System.Drawing.Point(60, 18), Size = new System.Drawing.Size(200, 20) };

                var labelY = new Label { Text = "Y:", Location = new System.Drawing.Point(20, 50), Size = new System.Drawing.Size(30, 20) };
                var textBoxY = new TextBox { Location = new System.Drawing.Point(60, 48), Size = new System.Drawing.Size(200, 20) };

                var labelZ = new Label { Text = "Z:", Location = new System.Drawing.Point(20, 80), Size = new System.Drawing.Size(30, 20) };
                var textBoxZ = new TextBox { Location = new System.Drawing.Point(60, 78), Size = new System.Drawing.Size(200, 20) };
                textBoxZ.Text = "0"; // 默认Z为0

                var okButton = new Button { Text = "确定", DialogResult = DialogResult.OK, Location = new System.Drawing.Point(100, 110), Size = new System.Drawing.Size(75, 30) };
                var cancelButton = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Location = new System.Drawing.Point(185, 110), Size = new System.Drawing.Size(75, 30) };

                inputForm.Controls.AddRange(new Control[] { labelX, textBoxX, labelY, textBoxY, labelZ, textBoxZ, okButton, cancelButton });
                inputForm.AcceptButton = okButton;
                inputForm.CancelButton = cancelButton;

                if (inputForm.ShowDialog(this) == DialogResult.OK)
                {
                    if (double.TryParse(textBoxX.Text, out double x) &&
                        double.TryParse(textBoxY.Text, out double y) &&
                        double.TryParse(textBoxZ.Text, out double z))
                    {
                        bool success = viewportPanel.SetLineEndPointByCoordinates(x, y, z);
                        if (success)
                        {
                            // 绘制完成，退出绘制模式
                            viewportPanel.SetDrawMode(Simple3DViewport.DrawMode.None);
                            UpdateDrawButtonStates(null);
                        }
                        else
                        {
                            MessageBox.Show("无法设置终点坐标，请确保已开始绘制直线", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    else
                    {
                        MessageBox.Show("请输入有效的数字", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void InitializeComponent()
        {
            // Windows Forms designer initialization
            this.SuspendLayout();
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Name = "MainForm";
            this.ResumeLayout(false);
        }
    }
}