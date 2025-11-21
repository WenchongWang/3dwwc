//using OpenTK;
//using OpenTK.Graphics;
//using OpenTK.Graphics.OpenGL;
//using OpenTK.Mathematics;
//using OpenTK.Windowing.Common;
//using System;
//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data;
//using System.Drawing;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows.Forms;
//using System;


//namespace OpenTKExample
//{
//       public partial class Form3 : Form
//    {
//        private float angle = 0.0f;

//        public Form3(int width, int height, string title)
//            : base(width, height, GraphicsMode.Default, title)
//        {
//            // 设置窗口的基本属性（可选）  
//            this.VSync = VSyncMode.On; // 启用垂直同步  
//            this.Title = title;
//        }

//        protected override void OnLoad(EventArgs e)
//        {
//            // 初始化OpenGL设置（例如，清除颜色、启用深度测试等）  
//            GL.ClearColor(Color4.CornflowerBlue);
//            GL.Enable(EnableCap.DepthTest);
//        }

//        protected override void OnRenderFrame(FrameEventArgs e)
//        {
//            // 清除屏幕和深度缓冲区  
//            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

//            // 保存OpenGL状态  
//            GL.PushMatrix();

//            // 应用旋转  
//            GL.Rotate(angle, 0, 0, 1);

//            // 绘制一个三角形（假设使用GLUT的投影和模型视图矩阵）  
//            GL.Begin(PrimitiveType.Triangles);
//            GL.Color3(1, 0, 0); // 红色  
//            GL.Vertex2(-0.5f, -0.5f);
//            GL.Color3(0, 1, 0); // 绿色  
//            GL.Vertex2(0.5f, -0.5f);
//            GL.Color3(0, 0, 1); // 蓝色  
//            GL.Vertex2(0.0f, 0.5f);
//            GL.End();

//            // 恢复OpenGL状态  
//            GL.PopMatrix();

//            // 交换前后缓冲区  
//           // this.SwapBuffers();

//            // 更新旋转角度  
//            angle += 1.0f;
//            if (angle > 360) angle = 0.0f;

//            // 处理标题栏的帧率显示  
//           // this.Title = $"{this.Width} x {this.Height} - {e.Time.ToString("F3")} ms/frame";
//        }

//        protected override void OnUpdateFrame(FrameEventArgs e)
//        {
//            // 这里可以添加每帧更新的逻辑（如果需要的话）  
//            // 例如，处理用户输入或更新游戏状态  
//        }

//        protected override void OnUnload(EventArgs e)
//        {
//            // 清理资源（如果需要的话）  
//        }

//        [STAThread]
//        static void Main()
//        {
//            using (GameWindow game = new(800, 600, "OpenTK Example"))
//            {
//                game.Run(30.0); // 30 FPS  
//            }
//        }
//    }
//}