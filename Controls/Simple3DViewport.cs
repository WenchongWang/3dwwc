using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using netDxf.Entities;

namespace Lens3DWinForms.Controls
{
    public class Simple3DViewport : Control
    {
        private List<EntityObject> entities = new List<EntityObject>();
        private List<Line> lines = new List<Line>(); // 线段列表，用于点击检测
        private int selectedLineIndex = -1; // 选中的线段索引
        private float rotationX = 20f;
        private float rotationY = -30f;
        private System.Drawing.Point lastMousePos;
        private bool isMouseDown = false;
        private bool isRightMouseDown = false;
        private float zoom = 1.0f;
        private float panX = 0f;
        private float panY = 0f;
        
        // 模型边界和缩放
        private netDxf.Vector3 modelCenter = netDxf.Vector3.Zero;
        private float modelScale = 1.0f;
        
        // 事件：线段被选中
        public event Action<int> LineSelected;
        
        public Simple3DViewport()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.Black;
            this.DoubleBuffered = true;
            this.Paint += Simple3DViewport_Paint;
            this.MouseDown += Simple3DViewport_MouseDown;
            this.MouseMove += Simple3DViewport_MouseMove;
            this.MouseUp += Simple3DViewport_MouseUp;
            this.MouseWheel += Simple3DViewport_MouseWheel;
        }

        public void LoadEntities(List<EntityObject> dxfEntities)
        {
            entities = dxfEntities ?? new List<EntityObject>();
            lines = entities.OfType<Line>().ToList(); // 提取所有线段
            selectedLineIndex = -1;
            ComputeModelBounds();
            
            // 重置视图
            rotationX = 20f;
            rotationY = -30f;
            zoom = 1.0f;
            panX = 0f;
            panY = 0f;
            
            Invalidate();
        }
        
        public void HighlightLine(int lineIndex)
        {
            if (lineIndex >= 0 && lineIndex < lines.Count)
            {
                selectedLineIndex = lineIndex;
                Invalidate();
            }
            else
            {
                selectedLineIndex = -1;
                Invalidate();
            }
        }

        private void ComputeModelBounds()
        {
            if (entities.Count == 0)
            {
                modelCenter = netDxf.Vector3.Zero;
                modelScale = 1.0f;
                return;
            }

            double minX = double.PositiveInfinity, minY = double.PositiveInfinity, minZ = double.PositiveInfinity;
            double maxX = double.NegativeInfinity, maxY = double.NegativeInfinity, maxZ = double.NegativeInfinity;

            void accumulate(double x, double y, double z)
            {
                if (x < minX) minX = x; if (x > maxX) maxX = x;
                if (y < minY) minY = y; if (y > maxY) maxY = y;
                if (z < minZ) minZ = z; if (z > maxZ) maxZ = z;
            }

            foreach (var entity in entities)
            {
                switch (entity)
                {
                    case Line ln:
                        accumulate(ln.StartPoint.X, ln.StartPoint.Y, ln.StartPoint.Z);
                        accumulate(ln.EndPoint.X, ln.EndPoint.Y, ln.EndPoint.Z);
                        break;
                    case Polyline3D p:
                        foreach (var v in p.Vertexes) accumulate(v.X, v.Y, v.Z);
                        break;
                    case Polyline2D p2d:
                        foreach (var v in p2d.Vertexes)
                        {
                            var pp = v.Position;
                            accumulate(pp.X, pp.Y, 0.0);
                        }
                        break;
                    case Circle c:
                        accumulate(c.Center.X - c.Radius, c.Center.Y - c.Radius, c.Center.Z);
                        accumulate(c.Center.X + c.Radius, c.Center.Y + c.Radius, c.Center.Z);
                        break;
                    case Arc a:
                        accumulate(a.Center.X - a.Radius, a.Center.Y - a.Radius, a.Center.Z);
                        accumulate(a.Center.X + a.Radius, a.Center.Y + a.Radius, a.Center.Z);
                        break;
                    case Ellipse el:
                        accumulate(el.Center.X - el.MajorAxis, el.Center.Y - el.MinorAxis, el.Center.Z);
                        accumulate(el.Center.X + el.MajorAxis, el.Center.Y + el.MinorAxis, el.Center.Z);
                        break;
                }
            }

            var sizeX = maxX - minX; var sizeY = maxY - minY; var sizeZ = maxZ - minZ;
            var maxSize = Math.Max(sizeX, Math.Max(sizeY, sizeZ));
            if (maxSize <= 0.0001) maxSize = 1.0;
            
            // 计算缩放，使模型适合显示
            float targetSize = Math.Min(this.Width, this.Height) * 0.4f;
            modelScale = (float)(targetSize / maxSize);
            if (modelScale < 0.01f) modelScale = 0.01f;
            if (modelScale > 10f) modelScale = 10f;
            
            modelCenter = new netDxf.Vector3(
                (float)((minX + maxX) / 2.0), 
                (float)((minY + maxY) / 2.0), 
                (float)((minZ + maxZ) / 2.0));
        }

        private void Simple3DViewport_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Black);
            
            if (entities.Count == 0)
            {
                using (var font = new Font("Arial", 12))
                using (var brush = new SolidBrush(Color.White))
                {
                    g.DrawString("3D 可视化区域 - 加载 DXF 文件以显示", font, brush, new PointF(10, 10));
                }
                return;
            }
            
            // 重新计算边界（如果窗口大小改变）
            if (this.Width > 0 && this.Height > 0)
            {
                ComputeModelBounds();
            }
            
            int centerX = this.Width / 2;
            int centerY = this.Height / 2;
            
            // 绘制坐标轴
            DrawAxes(g, centerX, centerY);
            
            // 绘制实体
            int lineIndex = 0;
            foreach (var entity in entities)
            {
                switch (entity)
                {
                    case Line line:
                        DrawLine(g, line, centerX, centerY, lineIndex);
                        lineIndex++;
                        break;
                    case Polyline3D polyline:
                        DrawPolyline3D(g, polyline, centerX, centerY);
                        break;
                    case Polyline2D polyline2d:
                        DrawPolyline2D(g, polyline2d, centerX, centerY);
                        break;
                    case Circle circle:
                        DrawCircle(g, circle, centerX, centerY);
                        break;
                    case Arc arc:
                        DrawArc(g, arc, centerX, centerY);
                        break;
                    case Ellipse ellipse:
                        DrawEllipse(g, ellipse, centerX, centerY);
                        break;
                }
            }
        }

        private void DrawAxes(Graphics g, int centerX, int centerY)
        {
            float axisLength = 50f;
            
            // X轴 - 红色
            var xEnd = ProjectTo2D(new netDxf.Vector3(axisLength, 0, 0), centerX, centerY);
            using (var pen = new Pen(Color.Red, 2))
            {
                g.DrawLine(pen, centerX, centerY, xEnd.X, xEnd.Y);
            }
            
            // Y轴 - 绿色
            var yEnd = ProjectTo2D(new netDxf.Vector3(0, axisLength, 0), centerX, centerY);
            using (var pen = new Pen(Color.Green, 2))
            {
                g.DrawLine(pen, centerX, centerY, yEnd.X, yEnd.Y);
            }
            
            // Z轴 - 蓝色
            var zEnd = ProjectTo2D(new netDxf.Vector3(0, 0, axisLength), centerX, centerY);
            using (var pen = new Pen(Color.Blue, 2))
            {
                g.DrawLine(pen, centerX, centerY, zEnd.X, zEnd.Y);
            }
        }

        private void DrawLine(Graphics g, Line line, int centerX, int centerY, int lineIndex = -1)
        {
            PointF start = ProjectTo2D(line.StartPoint, centerX, centerY);
            PointF end = ProjectTo2D(line.EndPoint, centerX, centerY);
            
            // 检查是否是被选中的线段
            bool isSelected = (lineIndex >= 0 && lineIndex == selectedLineIndex);
            
            Color lineColor = isSelected ? Color.Red : Color.Yellow;
            float lineWidth = isSelected ? 4f : 2f;
            
            using (var pen = new Pen(lineColor, lineWidth))
            {
                g.DrawLine(pen, start, end);
            }
        }

        private void DrawPolyline3D(Graphics g, Polyline3D polyline, int centerX, int centerY)
        {
            if (polyline.Vertexes.Count < 2)
                return;
            
            PointF[] points = new PointF[polyline.Vertexes.Count];
            for (int i = 0; i < polyline.Vertexes.Count; i++)
            {
                points[i] = ProjectTo2D(polyline.Vertexes[i], centerX, centerY);
            }
            
            using (var pen = new Pen(Color.Cyan, 2))
            {
                g.DrawLines(pen, points);
            }
        }

        private void DrawPolyline2D(Graphics g, Polyline2D polyline, int centerX, int centerY)
        {
            if (polyline.Vertexes.Count < 2)
                return;
            
            PointF[] points = new PointF[polyline.Vertexes.Count];
            for (int i = 0; i < polyline.Vertexes.Count; i++)
            {
                var pos = polyline.Vertexes[i].Position;
                points[i] = ProjectTo2D(new netDxf.Vector3(pos.X, pos.Y, 0), centerX, centerY);
            }
            
            using (var pen = new Pen(Color.Magenta, 2))
            {
                g.DrawLines(pen, points);
            }
        }

        private void DrawCircle(Graphics g, Circle circle, int centerX, int centerY)
        {
            PointF center = ProjectTo2D(circle.Center, centerX, centerY);
            
            // 计算投影后的半径（简化处理）
            float radius = (float)(circle.Radius * modelScale * zoom);
            if (radius < 1f) radius = 1f;
            
            using (var pen = new Pen(Color.Orange, 2))
            {
                g.DrawEllipse(pen, center.X - radius, center.Y - radius, radius * 2, radius * 2);
            }
        }

        private void DrawArc(Graphics g, Arc arc, int centerX, int centerY)
        {
            PointF center = ProjectTo2D(arc.Center, centerX, centerY);
            float radius = (float)(arc.Radius * modelScale * zoom);
            if (radius < 1f) radius = 1f;
            
            float startAngle = (float)(arc.StartAngle * Math.PI / 180.0);
            float sweepAngle = (float)((arc.EndAngle - arc.StartAngle) * Math.PI / 180.0);
            if (sweepAngle < 0) sweepAngle += (float)(2 * Math.PI);
            
            RectangleF rect = new RectangleF(center.X - radius, center.Y - radius, radius * 2, radius * 2);
            
            using (var pen = new Pen(Color.Lime, 2))
            {
                g.DrawArc(pen, rect, (float)(startAngle * 180 / Math.PI), (float)(sweepAngle * 180 / Math.PI));
            }
        }

        private void DrawEllipse(Graphics g, Ellipse ellipse, int centerX, int centerY)
        {
            PointF center = ProjectTo2D(ellipse.Center, centerX, centerY);
            float majorAxis = (float)(ellipse.MajorAxis * modelScale * zoom);
            float minorAxis = (float)(ellipse.MinorAxis * modelScale * zoom);
            
            RectangleF rect = new RectangleF(center.X - majorAxis, center.Y - minorAxis, majorAxis * 2, minorAxis * 2);
            
            using (var pen = new Pen(Color.Pink, 2))
            {
                g.DrawEllipse(pen, rect);
            }
        }

        private PointF ProjectTo2D(netDxf.Vector3 point, int centerX, int centerY)
        {
            // 应用模型变换：先居中，再缩放
            float x = (float)(point.X - modelCenter.X) * modelScale * zoom;
            float y = (float)(point.Y - modelCenter.Y) * modelScale * zoom;
            float z = (float)(point.Z - modelCenter.Z) * modelScale * zoom;
            
            // 应用旋转（简单的3D旋转）
            float radX = rotationX * (float)Math.PI / 180f;
            float radY = rotationY * (float)Math.PI / 180f;
            
            // 绕Y轴旋转
            float tempX = x * (float)Math.Cos(radY) - z * (float)Math.Sin(radY);
            float tempZ = x * (float)Math.Sin(radY) + z * (float)Math.Cos(radY);
            x = tempX;
            z = tempZ;
            
            // 绕X轴旋转
            float tempY = y * (float)Math.Cos(radX) - z * (float)Math.Sin(radX);
            tempZ = y * (float)Math.Sin(radX) + z * (float)Math.Cos(radX);
            y = tempY;
            z = tempZ;
            
            // 正交投影到2D
            float screenX = centerX + x + panX;
            float screenY = centerY - y + panY; // 反转Y轴
            
            return new PointF(screenX, screenY);
        }

        private void Simple3DViewport_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // 检查是否点击了线段（不拖拽的情况下）
                System.Drawing.Point clickPoint = e.Location;
                int clickedLineIndex = FindLineAtPoint(clickPoint);
                
                if (clickedLineIndex >= 0)
                {
                    selectedLineIndex = clickedLineIndex;
                    Invalidate();
                    LineSelected?.Invoke(clickedLineIndex);
                }
                else
                {
                    // 开始拖拽旋转
                    isMouseDown = true;
                    lastMousePos = e.Location;
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                isRightMouseDown = true;
                lastMousePos = e.Location;
            }
        }
        
        private int FindLineAtPoint(System.Drawing.Point point)
        {
            if (lines.Count == 0) return -1;
            
            int centerX = this.Width / 2;
            int centerY = this.Height / 2;
            float hitDistance = 5f; // 点击容差（像素）
            
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                PointF start = ProjectTo2D(line.StartPoint, centerX, centerY);
                PointF end = ProjectTo2D(line.EndPoint, centerX, centerY);
                
                // 计算点到线段的距离
                float distance = PointToLineDistance(point, start, end);
                
                if (distance <= hitDistance)
                {
                    return i;
                }
            }
            
            return -1;
        }
        
        private float PointToLineDistance(System.Drawing.Point point, PointF lineStart, PointF lineEnd)
        {
            float A = point.X - lineStart.X;
            float B = point.Y - lineStart.Y;
            float C = lineEnd.X - lineStart.X;
            float D = lineEnd.Y - lineStart.Y;
            
            float dot = A * C + B * D;
            float lenSq = C * C + D * D;
            float param = lenSq != 0 ? dot / lenSq : -1;
            
            float xx, yy;
            
            if (param < 0)
            {
                xx = lineStart.X;
                yy = lineStart.Y;
            }
            else if (param > 1)
            {
                xx = lineEnd.X;
                yy = lineEnd.Y;
            }
            else
            {
                xx = lineStart.X + param * C;
                yy = lineStart.Y + param * D;
            }
            
            float dx = point.X - xx;
            float dy = point.Y - yy;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private void Simple3DViewport_MouseMove(object sender, MouseEventArgs e)
        {
            int dx = e.X - lastMousePos.X;
            int dy = e.Y - lastMousePos.Y;
            
            // 如果移动距离较大，认为是拖拽操作
            if (isMouseDown && (Math.Abs(dx) > 2 || Math.Abs(dy) > 2))
            {
                rotationY += dx * 0.5f;
                rotationX += dy * 0.5f;
                Invalidate();
            }
            else if (isRightMouseDown)
            {
                panX += dx;
                panY += dy;
                Invalidate();
            }
            
            lastMousePos = e.Location;
        }

        private void Simple3DViewport_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isMouseDown = false;
            }
            else if (e.Button == MouseButtons.Right)
            {
                isRightMouseDown = false;
            }
        }

        private void Simple3DViewport_MouseWheel(object sender, MouseEventArgs e)
        {
            float factor = 1.0f + (e.Delta > 0 ? 0.1f : -0.1f);
            zoom *= factor;
            zoom = Math.Max(0.1f, Math.Min(zoom, 5.0f));
            Invalidate();
        }

        /// <summary>
        /// 设置标准视图角度
        /// </summary>
        /// <param name="viewType">视图类型：Front(前), Back(后), Left(左), Right(右), Top(上), Bottom(下)</param>
        public void SetStandardView(string viewType)
        {
            switch (viewType.ToLower())
            {
                case "front": // 前视图
                    rotationX = 0f;
                    rotationY = 0f;
                    break;
                case "back": // 后视图
                    rotationX = 0f;
                    rotationY = 180f;
                    break;
                case "left": // 左视图
                    rotationX = 0f;
                    rotationY = 90f;
                    break;
                case "right": // 右视图
                    rotationX = 0f;
                    rotationY = -90f;
                    break;
                case "top": // 上视图
                    rotationX = -90f;
                    rotationY = 0f;
                    break;
                case "bottom": // 下视图
                    rotationX = 90f;
                    rotationY = 0f;
                    break;
                default:
                    return;
            }
            
            // 重置平移，保持缩放
            panX = 0f;
            panY = 0f;
            
            Invalidate();
        }
    }
}
