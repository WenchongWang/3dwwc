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
        
        // 绘制相关
        public enum DrawMode { None, Line, Circle, Arc }
        private DrawMode currentDrawMode = DrawMode.None;
        private System.Drawing.Point? drawStartPoint = null;
        private System.Drawing.Point? drawCurrentPoint = null;
        private bool isDrawing = false;
        private bool autoSnapToEndpoints = false; // 自动吸附到端点
        private netDxf.Vector3? snappedStartPoint = null; // 吸附后的起点
        
        // 事件：线段被选中
        public event Action<int> LineSelected;
        // 事件：新实体被添加
        public event Action<EntityObject> EntityAdded;
        
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
            
            // 绘制预览（如果正在绘制）
            if (currentDrawMode != DrawMode.None && isDrawing && drawStartPoint.HasValue && drawCurrentPoint.HasValue)
            {
                DrawPreview(g, centerX, centerY);
            }
            
            // 显示绘制提示
            if (currentDrawMode != DrawMode.None)
            {
                string hint = "";
                switch (currentDrawMode)
                {
                    case DrawMode.Line:
                        hint = isDrawing ? "点击确定终点" : "点击确定起点";
                        break;
                    case DrawMode.Circle:
                        hint = isDrawing ? "点击确定半径" : "点击确定圆心";
                        break;
                    case DrawMode.Arc:
                        hint = isDrawing ? "点击确定半径和角度" : "点击确定圆心（将创建180度圆弧）";
                        break;
                }
                
                using (var font = new Font("Arial", 10))
                using (var brush = new SolidBrush(Color.Yellow))
                {
                    g.DrawString($"绘制模式: {hint} (右键取消)", font, brush, new PointF(10, this.Height - 25));
                }
            }
        }

        /// <summary>
        /// 绘制预览
        /// </summary>
        private void DrawPreview(Graphics g, int centerX, int centerY)
        {
            if (!drawStartPoint.HasValue || !drawCurrentPoint.HasValue) return;

            // 使用吸附后的起点（如果存在），否则使用屏幕坐标转换
            var start3D = snappedStartPoint ?? ScreenToWorld3D(drawStartPoint.Value);
            var end3D = ScreenToWorld3D(drawCurrentPoint.Value);

            using (var previewPen = new Pen(Color.Cyan, 2f))
            {
                previewPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;

                switch (currentDrawMode)
                {
                    case DrawMode.Line:
                        var start2D = ProjectTo2D(start3D, centerX, centerY);
                        var end2D = ProjectTo2D(end3D, centerX, centerY);
                        g.DrawLine(previewPen, start2D, end2D);
                        
                        // 如果使用了吸附，在吸附点绘制一个标记
                        if (snappedStartPoint.HasValue)
                        {
                            using (var snapBrush = new SolidBrush(Color.Yellow))
                            {
                                g.FillEllipse(snapBrush, start2D.X - 5, start2D.Y - 5, 10, 10);
                            }
                        }
                        break;

                    case DrawMode.Circle:
                        double radius = Math.Sqrt(
                            Math.Pow(end3D.X - start3D.X, 2) +
                            Math.Pow(end3D.Y - start3D.Y, 2) +
                            Math.Pow(end3D.Z - start3D.Z, 2));
                        if (radius > 0.001)
                        {
                            var center2D = ProjectTo2D(start3D, centerX, centerY);
                            float screenRadius = (float)(radius * modelScale * zoom);
                            g.DrawEllipse(previewPen, center2D.X - screenRadius, center2D.Y - screenRadius,
                                screenRadius * 2, screenRadius * 2);
                        }
                        break;

                    case DrawMode.Arc:
                        double arcRadius = Math.Sqrt(
                            Math.Pow(end3D.X - start3D.X, 2) +
                            Math.Pow(end3D.Y - start3D.Y, 2) +
                            Math.Pow(end3D.Z - start3D.Z, 2));
                        if (arcRadius > 0.001)
                        {
                            var center2D = ProjectTo2D(start3D, centerX, centerY);
                            float screenRadius = (float)(arcRadius * modelScale * zoom);
                            double angle = Math.Atan2(end3D.Y - start3D.Y, end3D.X - start3D.X) * 180.0 / Math.PI;
                            RectangleF rect = new RectangleF(center2D.X - screenRadius, center2D.Y - screenRadius,
                                screenRadius * 2, screenRadius * 2);
                            g.DrawArc(previewPen, rect, (float)angle, 180f);
                        }
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
                // 绘制模式优先
                if (currentDrawMode != DrawMode.None)
                {
                    if (!isDrawing)
                    {
                        // 开始绘制
                        isDrawing = true;
                        drawStartPoint = e.Location;
                        drawCurrentPoint = e.Location;
                        
                        // 如果是绘制直线且启用了自动吸附，查找最近的端点
                        if (currentDrawMode == DrawMode.Line && autoSnapToEndpoints)
                        {
                            var nearestPoint = FindNearestEndpoint(e.Location);
                            if (nearestPoint.HasValue)
                            {
                                snappedStartPoint = nearestPoint.Value;
                                // 更新drawStartPoint为吸附点的屏幕坐标（用于显示）
                                int centerX = this.Width / 2;
                                int centerY = this.Height / 2;
                                var screenPos = ProjectTo2D(nearestPoint.Value, centerX, centerY);
                                drawStartPoint = new System.Drawing.Point((int)screenPos.X, (int)screenPos.Y);
                            }
                            else
                            {
                                snappedStartPoint = null;
                            }
                        }
                        else
                        {
                            snappedStartPoint = null;
                        }
                        
                        Invalidate();
                    }
                    else
                    {
                        // 完成绘制（第二次点击）
                        CompleteDrawing(e.Location);
                    }
                    return;
                }
                
                // 非绘制模式：检查是否点击了线段（不拖拽的情况下）
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
                // 右键取消绘制
                if (currentDrawMode != DrawMode.None && isDrawing)
                {
                    isDrawing = false;
                    drawStartPoint = null;
                    drawCurrentPoint = null;
                    Invalidate();
                }
                else
                {
                    isRightMouseDown = true;
                    lastMousePos = e.Location;
                }
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
            // 绘制模式：更新预览
            if (currentDrawMode != DrawMode.None && isDrawing && drawStartPoint.HasValue)
            {
                drawCurrentPoint = e.Location;
                Invalidate();
                return;
            }
            
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

        /// <summary>
        /// 完成绘制
        /// </summary>
        private void CompleteDrawing(System.Drawing.Point endPoint)
        {
            if (!drawStartPoint.HasValue) return;

            // 使用吸附后的起点（如果存在），否则使用屏幕坐标转换
            var start3D = snappedStartPoint ?? ScreenToWorld3D(drawStartPoint.Value);
            var end3D = ScreenToWorld3D(endPoint);

            EntityObject newEntity = null;

            switch (currentDrawMode)
            {
                case DrawMode.Line:
                    newEntity = new Line(start3D, end3D);
                    break;

                case DrawMode.Circle:
                    double radius = Math.Sqrt(
                        Math.Pow(end3D.X - start3D.X, 2) +
                        Math.Pow(end3D.Y - start3D.Y, 2) +
                        Math.Pow(end3D.Z - start3D.Z, 2));
                    if (radius > 0.001)
                    {
                        newEntity = new Circle(start3D, radius);
                    }
                    break;

                case DrawMode.Arc:
                    // 圆弧：起点为中心，终点确定半径，需要第三个点确定角度
                    // 简化处理：创建180度圆弧
                    double arcRadius = Math.Sqrt(
                        Math.Pow(end3D.X - start3D.X, 2) +
                        Math.Pow(end3D.Y - start3D.Y, 2) +
                        Math.Pow(end3D.Z - start3D.Z, 2));
                    if (arcRadius > 0.001)
                    {
                        // 计算角度
                        double angle = Math.Atan2(end3D.Y - start3D.Y, end3D.X - start3D.X) * 180.0 / Math.PI;
                        newEntity = new Arc(start3D, arcRadius, angle, angle + 180.0);
                    }
                    break;
            }

            if (newEntity != null)
            {
                AddEntity(newEntity);
            }

            // 重置绘制状态
            isDrawing = false;
            drawStartPoint = null;
            drawCurrentPoint = null;
            snappedStartPoint = null;
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

        /// <summary>
        /// 设置绘制模式
        /// </summary>
        public void SetDrawMode(DrawMode mode)
        {
            if (currentDrawMode != mode)
            {
                currentDrawMode = mode;
                isDrawing = false;
                drawStartPoint = null;
                drawCurrentPoint = null;
                snappedStartPoint = null; // 清除吸附点
                Invalidate();
            }
        }

        /// <summary>
        /// 获取当前绘制模式
        /// </summary>
        public DrawMode GetDrawMode()
        {
            return currentDrawMode;
        }

        /// <summary>
        /// 设置是否启用自动吸附到端点
        /// </summary>
        public void SetAutoSnapToEndpoints(bool enabled)
        {
            autoSnapToEndpoints = enabled;
        }

        /// <summary>
        /// 查找最近的端点（在屏幕坐标的容差范围内）
        /// </summary>
        private netDxf.Vector3? FindNearestEndpoint(System.Drawing.Point screenPoint, float tolerance = 10f)
        {
            if (entities.Count == 0) return null;

            int centerX = this.Width / 2;
            int centerY = this.Height / 2;
            double minDistance = double.PositiveInfinity;
            netDxf.Vector3? nearestPoint = null;

            // 收集所有端点
            var endpoints = new List<netDxf.Vector3>();
            foreach (var entity in entities)
            {
                switch (entity)
                {
                    case Line line:
                        endpoints.Add(line.StartPoint);
                        endpoints.Add(line.EndPoint);
                        break;
                    case Polyline3D poly:
                        foreach (var v in poly.Vertexes)
                            endpoints.Add(new netDxf.Vector3(v.X, v.Y, v.Z));
                        break;
                    case Polyline2D poly2d:
                        foreach (var v in poly2d.Vertexes)
                        {
                            var pos = v.Position;
                            endpoints.Add(new netDxf.Vector3(pos.X, pos.Y, 0));
                        }
                        break;
                    case Circle circle:
                        endpoints.Add(circle.Center);
                        break;
                    case Arc arc:
                        endpoints.Add(arc.Center);
                        break;
                }
            }

            // 查找最近的端点
            foreach (var endpoint in endpoints)
            {
                PointF screenPos = ProjectTo2D(endpoint, centerX, centerY);
                float dx = screenPoint.X - screenPos.X;
                float dy = screenPoint.Y - screenPos.Y;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                if (distance < tolerance && distance < minDistance)
                {
                    minDistance = distance;
                    nearestPoint = endpoint;
                }
            }

            return nearestPoint;
        }

        /// <summary>
        /// 通过坐标输入设置直线终点
        /// </summary>
        public bool SetLineEndPointByCoordinates(double x, double y, double z)
        {
            if (currentDrawMode != DrawMode.Line || !isDrawing || !drawStartPoint.HasValue)
                return false;

            var endPoint3D = new netDxf.Vector3(x, y, z);
            var startPoint3D = snappedStartPoint ?? ScreenToWorld3D(drawStartPoint.Value);

            var newLine = new Line(startPoint3D, endPoint3D);
            AddEntity(newLine);

            // 重置绘制状态
            isDrawing = false;
            drawStartPoint = null;
            drawCurrentPoint = null;
            snappedStartPoint = null;

            return true;
        }

        /// <summary>
        /// 将屏幕坐标转换为3D世界坐标（在XY平面上，Z=0）
        /// </summary>
        private netDxf.Vector3 ScreenToWorld3D(System.Drawing.Point screenPoint)
        {
            int centerX = this.Width / 2;
            int centerY = this.Height / 2;
            
            // 屏幕坐标相对于中心
            float screenX = screenPoint.X - centerX - panX;
            float screenY = -(screenPoint.Y - centerY - panY); // 反转Y轴
            
            // 反向旋转
            float radX = -rotationX * (float)Math.PI / 180f;
            float radY = -rotationY * (float)Math.PI / 180f;
            
            // 先绕X轴反向旋转
            float z = screenY * (float)Math.Sin(radX);
            float y = screenY * (float)Math.Cos(radX);
            
            // 再绕Y轴反向旋转
            float x = screenX * (float)Math.Cos(radY) - z * (float)Math.Sin(radY);
            z = screenX * (float)Math.Sin(radY) + z * (float)Math.Cos(radY);
            
            // 反向缩放和居中
            if (modelScale * zoom > 0.0001f)
            {
                x = x / (modelScale * zoom) + (float)modelCenter.X;
                y = y / (modelScale * zoom) + (float)modelCenter.Y;
                z = z / (modelScale * zoom) + (float)modelCenter.Z;
            }
            
            // 在XY平面上绘制（Z=0）
            return new netDxf.Vector3(x, y, 0);
        }

        /// <summary>
        /// 添加实体到列表
        /// </summary>
        public void AddEntity(EntityObject entity)
        {
            if (entity != null)
            {
                entities.Add(entity);
                if (entity is Line line)
                {
                    lines.Add(line);
                }
                ComputeModelBounds();
                Invalidate();
                EntityAdded?.Invoke(entity);
            }
        }

        /// <summary>
        /// 获取所有实体
        /// </summary>
        public List<EntityObject> GetEntities()
        {
            return new List<EntityObject>(entities);
        }
    }
}
