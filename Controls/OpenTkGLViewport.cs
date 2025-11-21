using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using netDxf.Entities;
using OpenTK.WinForms;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Lens3DWinForms.Controls
{
    public class OpenTkGLViewport : GLControl
    {
        private List<EntityObject> entities = new List<EntityObject>();
        private float rotationX = 20f;
        private float rotationY = -30f;
        private float zoom = 1.0f;
        private System.Drawing.Point lastMousePos;
        private bool isMouseDown = false;
        private Vector3 modelCenter = Vector3.Zero;
        private float modelScale = 1.0f;
        private bool isRightMouseDown = false;
        private float panX = 0f;
        private float panY = 0f;

        public OpenTkGLViewport() : base()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.LightBlue;

            Load += OnLoad;
            Paint += OnPaint;
            Resize += OnResize;
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            MouseWheel += OnMouseWheel;
        }

        public void LoadEntities(List<EntityObject> dxfEntities)
        {
            entities = dxfEntities ?? new List<EntityObject>();
            ComputeModelBounds();
            
            // 重置相机位置和缩放
            rotationX = 20f;
            rotationY = -30f;
            zoom = 1.0f;
            panX = 0f;
            panY = 0f;
            
            // 强制刷新视口
            if (IsHandleCreated)
            {
                MakeCurrent();
                Invalidate();
                Update();
            }
        }

        private void OnLoad(object? sender, EventArgs e)
        {
            MakeCurrent();
            GL.ClearColor(0f, 0f, 0f, 1f);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.LineWidth(2f);
            GL.Enable(EnableCap.LineSmooth);
            GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);
            SetupProjection();

           // GL.ClearColor(Color4.CornflowerBlue);
            GL.Enable(EnableCap.DepthTest);


        }

        private void OnResize(object? sender, EventArgs e)
        {
            if (Width <= 0 || Height <= 0) return;
            MakeCurrent();
            GL.Viewport(0, 0, Width, Height);
            SetupProjection();
            Invalidate();
        }

        private void SetupProjection()
        {
            float aspect = Math.Max(Width, 1) / (float)Math.Max(Height, 1);
            Matrix4 proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45f), aspect, 0.1f, 10000f);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref proj);
        }

        private void OnPaint(object? sender, PaintEventArgs e)
        {
            MakeCurrent();
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // 设置投影矩阵
            SetupProjection();

            // 设置模型视图矩阵
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();

            // 相机位置 - 根据模型缩放调整距离
            // 基础距离，根据缩放调整
            float baseDistance = 30f;
            if (modelScale > 0)
            {
                // 模型缩放后的大小约为 15 单位，相机距离应该能看清
                // 使用固定的合理距离，让缩放后的模型在视野中心
                baseDistance = 30f;
            }
            GL.Translate(0f, 0f, -baseDistance * (1f / zoom));
            
            // 旋转
            GL.Rotate(rotationX, 1f, 0f, 0f);
            GL.Rotate(rotationY, 0f, 1f, 0f);
            
            // 平移
            GL.Translate(panX, panY, 0f);
            
            // 先绘制坐标轴（在世界坐标系中，不受模型变换影响）
            GL.PushMatrix();
            RenderCoordinateAxes();
            GL.PopMatrix();
            
            // 缩放和居中（仅对模型应用）
            GL.PushMatrix();
            GL.Scale(modelScale, modelScale, modelScale);
            GL.Translate(-modelCenter.X, -modelCenter.Y, -modelCenter.Z);
            
            // 绘制实体
            RenderEntities();
            GL.PopMatrix();
            
            SwapBuffers();
        }

        private void RenderGrid()
        {
            GL.Color3(0.2f, 0.2f, 0.2f);
            GL.Begin(PrimitiveType.Lines);
            for (int i = -50; i <= 50; i += 5)
            {
                GL.Vertex3(i, 0, -50);
                GL.Vertex3(i, 0, 50);
                GL.Vertex3(-50, 0, i);
                GL.Vertex3(50, 0, i);
            }
            GL.End();
        }

        private void RenderCoordinateAxes()
        {
            // 根据模型大小调整坐标轴长度，确保可见
            float axisLength = 50f; // 固定长度，在世界坐标系中
            
            GL.LineWidth(2f);
            
            // X轴 - 红色
            GL.Color3(1.0f, 0.0f, 0.0f);
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex3(0f, 0f, 0f);
            GL.Vertex3(axisLength, 0f, 0f);
            GL.End();

            // Y轴 - 绿色
            GL.Color3(0.0f, 1.0f, 0.0f);
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex3(0f, 0f, 0f);
            GL.Vertex3(0f, axisLength, 0f);
            GL.End();

            // Z轴 - 蓝色
            GL.Color3(0.0f, 0.0f, 1.0f);
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex3(0f, 0f, 0f);
            GL.Vertex3(0f, 0f, axisLength);
            GL.End();









        }

        private void RenderEntities()
        {
            if (entities.Count == 0) return;
            
            // 使用更明显的颜色和线宽
            GL.LineWidth(2f);
            GL.Color3(1.0f, 1.0f, 0.0f); // 黄色，更明显

            foreach (var entity in entities)
            {
                switch (entity)
                {
                    case Line line:
                        GL.Begin(PrimitiveType.Lines);
                        GL.Vertex3((float)line.StartPoint.X, (float)line.StartPoint.Y, (float)line.StartPoint.Z);
                        GL.Vertex3((float)line.EndPoint.X, (float)line.EndPoint.Y, (float)line.EndPoint.Z);
                        GL.End();
                        break;

                    case Polyline3D poly:
                        if (poly.Vertexes.Count < 2) break;
                        GL.Begin(PrimitiveType.LineStrip);
                        foreach (var v in poly.Vertexes)
                        {
                            GL.Vertex3((float)v.X, (float)v.Y, (float)v.Z);
                        }
                        GL.End();
                        break;

                    case Polyline2D poly2d:
                        if (poly2d.Vertexes.Count < 2) break;
                        GL.Begin(PrimitiveType.LineStrip);
                        foreach (var v in poly2d.Vertexes)
                        {
                            var p = v.Position;
                            GL.Vertex3((float)p.X, (float)p.Y, 0f);
                        }
                        GL.End();
                        break;

                    

                    case Circle c:
                        GL.Begin(PrimitiveType.LineLoop);
                        const int segments = 64;
                        for (int i = 0; i < segments; i++)
                        {
                            double ang = i * (2.0 * Math.PI / segments);
                            float x = (float)(c.Center.X + c.Radius * Math.Cos(ang));
                            float y = (float)(c.Center.Y + c.Radius * Math.Sin(ang));
                            float z = (float)c.Center.Z;
                            GL.Vertex3(x, y, z);
                        }
                        GL.End();
                        break;

                    case netDxf.Entities.Arc a:
                        GL.Begin(PrimitiveType.LineStrip);
                        const int arcSegments = 64;
                        double start = a.StartAngle * Math.PI / 180.0;
                        double end = a.EndAngle * Math.PI / 180.0;
                        double total = end - start;
                        if (total < 0) total += 2.0 * Math.PI;
                        for (int i = 0; i <= arcSegments; i++)
                        {
                            double ang = start + total * i / arcSegments;
                            float x = (float)(a.Center.X + a.Radius * Math.Cos(ang));
                            float y = (float)(a.Center.Y + a.Radius * Math.Sin(ang));
                            float z = (float)a.Center.Z;
                            GL.Vertex3(x, y, z);
                        }
                        GL.End();
                        break;

                    case netDxf.Entities.Ellipse el:
                        GL.Begin(PrimitiveType.LineLoop);
                        const int ellSegments = 96;
                        for (int i = 0; i < ellSegments; i++)
                        {
                            double t = i * (2.0 * Math.PI / ellSegments);
                            var p = el.Center + new netDxf.Vector3(
                                el.MajorAxis * Math.Cos(t),
                                el.MinorAxis * Math.Sin(t),
                                0.0);
                            GL.Vertex3((float)p.X, (float)p.Y, (float)p.Z);
                        }
                        GL.End();
                        break;

                    case netDxf.Entities.Insert ins:
                        if (ins.Block == null) break;
                        GL.PushMatrix();
                        GL.Translate((float)ins.Position.X, (float)ins.Position.Y, (float)ins.Position.Z);
                        foreach (var be in ins.Block.Entities)
                        {
                            // render block entities relative to insert position
                            switch (be)
                            {
                                case Line bln:
                                    GL.Begin(PrimitiveType.Lines);
                                    GL.Vertex3((float)bln.StartPoint.X, (float)bln.StartPoint.Y, (float)bln.StartPoint.Z);
                                    GL.Vertex3((float)bln.EndPoint.X, (float)bln.EndPoint.Y, (float)bln.EndPoint.Z);
                                    GL.End();
                                    break;
                               
                                case Polyline2D bp2d:
                                    if (bp2d.Vertexes.Count < 2) break;
                                    GL.Begin(PrimitiveType.LineStrip);
                                    foreach (var v in bp2d.Vertexes)
                                    {
                                        var p2 = v.Position;
                                        GL.Vertex3((float)p2.X, (float)p2.Y, 0f);
                                    }
                                    GL.End();
                                    break;
                                case Circle bc:
                                    GL.Begin(PrimitiveType.LineLoop);
                                    for (int i = 0; i < 64; i++)
                                    {
                                        double ang = i * (2.0 * Math.PI / 64);
                                        float x = (float)(bc.Center.X + bc.Radius * Math.Cos(ang));
                                        float y = (float)(bc.Center.Y + bc.Radius * Math.Sin(ang));
                                        float z = (float)bc.Center.Z;
                                        GL.Vertex3(x, y, z);
                                    }
                                    GL.End();
                                    break;
                            }
                        }
                        GL.PopMatrix();
                        break;
                }
            }
        }

        private void ComputeModelBounds()
        {
            if (entities.Count == 0)
            {
                modelCenter = Vector3.Zero;
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
            
            // 调整缩放：确保模型在视野内，但不要过度缩小
            // 目标是将模型缩放到大约 10-20 单位大小
            float targetSize = 15f;
            modelScale = (float)(targetSize / maxSize);
            
            // 限制缩放范围，避免模型太小或太大
            if (modelScale < 0.01f) modelScale = 0.01f;
            if (modelScale > 10f) modelScale = 10f;
            
            modelCenter = new Vector3((float)((minX + maxX) / 2.0), (float)((minY + maxY) / 2.0), (float)((minZ + maxZ) / 2.0));
        }

        private void OnMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { isMouseDown = true; lastMousePos = e.Location; }
            if (e.Button == MouseButtons.Right) { isRightMouseDown = true; lastMousePos = e.Location; }
        }

        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            int dx = e.X - lastMousePos.X;
            int dy = e.Y - lastMousePos.Y;
            if (isMouseDown)
            {
                rotationY += dx * 0.5f;
                rotationX += dy * 0.5f;
            }
            if (isRightMouseDown)
            {
                panX += dx * 0.01f;
                panY -= dy * 0.01f;
            }
            lastMousePos = e.Location;
            Invalidate();
        }

        private void OnMouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) isMouseDown = false;
            if (e.Button == MouseButtons.Right) isRightMouseDown = false;
        }

        private void OnMouseWheel(object? sender, MouseEventArgs e)
        {
            float factor = 1.0f + (e.Delta > 0 ? 0.1f : -0.1f);
            zoom *= factor;
            zoom = Math.Max(0.2f, Math.Min(zoom, 5.0f));
            Invalidate();
        }
    }
}