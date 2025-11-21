using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using netDxf.Entities;
using Lens3DWinForms.Models;

namespace Lens3DWinForms.Views
{
    public partial class PlaneOptimizationView : UserControl
    {
        private List<Line> lines;
        private RichTextBox resultTextBox;
        private Button optimizeButton;
        
        public PlaneOptimizationView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Size = new System.Drawing.Size(500, 600);
            
            // Optimize button
            optimizeButton = new Button
            {
                Text = "优化平面",
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(120, 25)
            };
            optimizeButton.Click += OptimizeButton_Click;
            
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
            
            this.Controls.Add(optimizeButton);
            this.Controls.Add(resultTextBox);
        }

        public void LoadLines(List<EntityObject> entities)
        {
            lines = entities.OfType<Line>().ToList();
        }

        public List<PlaneData> GetOptimizedPlanes()
        {
            if (lines == null || lines.Count == 0)
            {
                return new List<PlaneData>();
            }
            
            return OptimizeLinesToPlanes();
        }

        private void OptimizeButton_Click(object sender, EventArgs e)
        {
            if (lines == null || lines.Count == 0)
            {
                MessageBox.Show("没有可优化的线段数据", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var optimizedPlanes = OptimizeLinesToPlanes();
            DisplayOptimizedPlanes(optimizedPlanes);
        }

        private List<PlaneData> OptimizeLinesToPlanes()
        {
            var planes = new List<PlaneData>();
            
            // 按照原始线段顺序处理所有相邻线段
            // 遍历所有相邻的线段对
            for (int i = 0; i < lines.Count - 1; i++)
            {
                var currentLine = lines[i];
                var nextLine = lines[i + 1];
                
                // 检查是否相邻且共享端点
                if (AreLinesAdjacent(currentLine, nextLine))
                {
                    var plane = CreatePlaneFromLines(currentLine, nextLine);
                    if (plane != null)
                    {
                        planes.Add(plane);
                        i++; // 跳过下一个线段，因为已经处理了
                    }
                }
            }
            
            return planes;
        }

        private bool AreLinesAdjacent(Line line1, Line line2)
        {
            // 检查线段是否相邻（共享端点）
            double tolerance = 0.001;
            
            return PointsEqual(line1.EndPoint, line2.StartPoint, tolerance) ||
                   PointsEqual(line1.EndPoint, line2.EndPoint, tolerance) ||
                   PointsEqual(line1.StartPoint, line2.StartPoint, tolerance) ||
                   PointsEqual(line1.StartPoint, line2.EndPoint, tolerance);
        }

        private bool PointsEqual(netDxf.Vector3 p1, netDxf.Vector3 p2, double tolerance)
        {
            return Math.Abs(p1.X - p2.X) < tolerance &&
                   Math.Abs(p1.Y - p2.Y) < tolerance &&
                   Math.Abs(p1.Z - p2.Z) < tolerance;
        }

        private PlaneData CreatePlaneFromLines(Line line1, Line line2)
        {
            // 获取所有不重复的点（4个点优化为3个点）
            var points = new List<netDxf.Vector3>
            {
                line1.StartPoint,
                line1.EndPoint,
                line2.StartPoint,
                line2.EndPoint
            };
            
            // 去除重复点
            var uniquePoints = points.Distinct(new Vector3Comparer()).ToList();
            
            if (uniquePoints.Count == 3)
            {
                return new PlaneData
                {
                    Points = uniquePoints,
                    Line1 = line1,
                    Line2 = line2,
                    Area = CalculateTriangleArea(uniquePoints[0], uniquePoints[1], uniquePoints[2])
                };
            }
            
            return null;
        }

        private double CalculateTriangleArea(netDxf.Vector3 p1, netDxf.Vector3 p2, netDxf.Vector3 p3)
        {
            // 计算三角形面积
            double a = CalculateDistance(p1, p2);
            double b = CalculateDistance(p2, p3);
            double c = CalculateDistance(p3, p1);
            
            double s = (a + b + c) / 2;
            return Math.Sqrt(s * (s - a) * (s - b) * (s - c));
        }

        private double CalculateDistance(netDxf.Vector3 p1, netDxf.Vector3 p2)
        {
            double dx = p1.X - p2.X;
            double dy = p1.Y - p2.Y;
            double dz = p1.Z - p2.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        private void DisplayOptimizedPlanes(List<PlaneData> planes)
        {
            resultTextBox.Clear();
            resultTextBox.AppendText("=== 平面优化结果 ===\n\n");
            
            resultTextBox.AppendText($"原始线段数: {lines.Count}\n");
            resultTextBox.AppendText($"优化后的平面数: {planes.Count}\n\n");
            
            for (int i = 0; i < planes.Count; i++)
            {
                var plane = planes[i];
                resultTextBox.AppendText($"平面 {i + 1}:\n");
                resultTextBox.AppendText($"  组成线段: {(plane.Line1.Handle)} 和 {(plane.Line2.Handle) }\n");
                resultTextBox.AppendText($"  面积: {plane.Area:F4}\n");
                
                resultTextBox.AppendText($"  顶点1: ({plane.Points[0].X:F2}, {plane.Points[0].Y:F2}, {plane.Points[0].Z:F2})\n");
                resultTextBox.AppendText($"  顶点2: ({plane.Points[1].X:F2}, {plane.Points[1].Y:F2}, {plane.Points[1].Z:F2})\n");
                resultTextBox.AppendText($"  顶点3: ({plane.Points[2].X:F2}, {plane.Points[2].Y:F2}, {plane.Points[2].Z:F2})\n");
                
                resultTextBox.AppendText("\n");
            }
            
            if (planes.Count == 0)
            {
                resultTextBox.AppendText("未找到可以优化的相邻线段\n");
            }
        }

        private class Vector3Comparer : IEqualityComparer<netDxf.Vector3>
        {
            private const double Tolerance = 0.001;
            
            public bool Equals(netDxf.Vector3 v1, netDxf.Vector3 v2)
            {
                return Math.Abs(v1.X - v2.X) < Tolerance &&
                       Math.Abs(v1.Y - v2.Y) < Tolerance &&
                       Math.Abs(v1.Z - v2.Z) < Tolerance;
            }
            
            public int GetHashCode(netDxf.Vector3 obj)
            {
                return obj.X.GetHashCode() ^ obj.Y.GetHashCode() ^ obj.Z.GetHashCode();
            }
        }
    }


}