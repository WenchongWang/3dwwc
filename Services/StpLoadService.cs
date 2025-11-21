using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using netDxf.Entities;
using netDxf;

namespace Lens3DWinForms.Services
{
    public class StpLoadService
    {
        private Dictionary<string, StpEntity> entities = new Dictionary<string, StpEntity>();
        
        public List<EntityObject> LoadStpFile(string filePath)
        {
            entities.Clear();
            var dxfEntities = new List<EntityObject>();
            
            try
            {
                // 读取STP文件
                var content = File.ReadAllText(filePath, Encoding.UTF8);
                
                // 解析STP文件
                ParseStpFile(content);
                
                // 转换为DXF实体
                dxfEntities = ConvertToDxfEntities();
            }
            catch (Exception ex)
            {
                throw new Exception($"加载STP文件失败: {ex.Message}", ex);
            }
            
            return dxfEntities;
        }
        
        private void ParseStpFile(string content)
        {
            // STP文件格式：ISO 10303-21
            // 格式：#ID = ENTITY_TYPE('', PARAMETERS);
            
            // 匹配实体定义的正则表达式
            var entityPattern = new Regex(@"#(\d+)\s*=\s*([A-Z_]+)\s*\([^)]*\)\s*;", RegexOptions.Multiline);
            
            var matches = entityPattern.Matches(content);
            
            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 3)
                {
                    string id = match.Groups[1].Value;
                    string type = match.Groups[2].Value;
                    string fullMatch = match.Value;
                    
                    entities[id] = new StpEntity
                    {
                        Id = id,
                        Type = type,
                        Content = fullMatch
                    };
                }
            }
        }
        
        private List<EntityObject> ConvertToDxfEntities()
        {
            var result = new List<EntityObject>();
            
            // 解析点
            var points = new Dictionary<string, netDxf.Vector3>();
            foreach (var entity in entities.Values.Where(e => e.Type == "CARTESIAN_POINT"))
            {
                var point = ParseCartesianPoint(entity.Content);
                if (point.HasValue)
                {
                    points[entity.Id] = point.Value;
                }
            }
            
            // 解析线段
            foreach (var entity in entities.Values.Where(e => e.Type == "LINE"))
            {
                var line = ParseLine(entity.Content, points);
                if (line != null)
                {
                    result.Add(line);
                }
            }
            
            // 解析圆
            foreach (var entity in entities.Values.Where(e => e.Type == "CIRCLE"))
            {
                var circle = ParseCircle(entity.Content, points);
                if (circle != null)
                {
                    result.Add(circle);
                }
            }
            
            // 解析边曲线（EDGE_CURVE）
            foreach (var entity in entities.Values.Where(e => e.Type == "EDGE_CURVE"))
            {
                var edges = ParseEdgeCurve(entity.Content, points);
                result.AddRange(edges);
            }
            
            return result;
        }
        
        private netDxf.Vector3? ParseCartesianPoint(string content)
        {
            // 格式：#ID = CARTESIAN_POINT('', (X, Y, Z));
            var coordPattern = new Regex(@"\(([-\d.E]+),\s*([-\d.E]+),\s*([-\d.E]+)\)");
            var match = coordPattern.Match(content);
            
            if (match.Success && match.Groups.Count == 4)
            {
                if (double.TryParse(match.Groups[1].Value, out double x) &&
                    double.TryParse(match.Groups[2].Value, out double y) &&
                    double.TryParse(match.Groups[3].Value, out double z))
                {
                    return new netDxf.Vector3(x, y, z);
                }
            }
            
            return null;
        }
        
        private Line ParseLine(string content, Dictionary<string, netDxf.Vector3> points)
        {
            // 格式：#ID = LINE('', #POINT_ID1, #POINT_ID2);
            var refPattern = new Regex(@"#(\d+)");
            var matches = refPattern.Matches(content);
            
            if (matches.Count >= 2)
            {
                string id1 = matches[0].Groups[1].Value;
                string id2 = matches[1].Groups[1].Value;
                
                // 查找点实体
                var point1 = FindPointById(id1, points);
                var point2 = FindPointById(id2, points);
                
                if (point1.HasValue && point2.HasValue)
                {
                    return new Line(point1.Value, point2.Value);
                }
            }
            
            return null;
        }
        
        private Circle ParseCircle(string content, Dictionary<string, netDxf.Vector3> points)
        {
            // 格式：#ID = CIRCLE('', #CENTER_POINT_ID, RADIUS);
            var refPattern = new Regex(@"#(\d+)");
            var radiusPattern = new Regex(@"([\d.E-]+)\s*\)");
            
            var centerMatch = refPattern.Match(content);
            var radiusMatch = radiusPattern.Match(content);
            
            if (centerMatch.Success && radiusMatch.Success)
            {
                string centerId = centerMatch.Groups[1].Value;
                if (double.TryParse(radiusMatch.Groups[1].Value, out double radius))
                {
                    var center = FindPointById(centerId, points);
                    if (center.HasValue)
                    {
                        return new Circle(center.Value, radius);
                    }
                }
            }
            
            return null;
        }
        
        private List<EntityObject> ParseEdgeCurve(string content, Dictionary<string, netDxf.Vector3> points)
        {
            var result = new List<EntityObject>();
            
            // 简化的边曲线解析
            // 实际STP格式更复杂，这里只做基本处理
            var refPattern = new Regex(@"#(\d+)");
            var matches = refPattern.Matches(content);
            
            // 尝试找到起点和终点
            if (matches.Count >= 2)
            {
                var point1 = FindPointById(matches[0].Groups[1].Value, points);
                var point2 = FindPointById(matches[1].Groups[1].Value, points);
                
                if (point1.HasValue && point2.HasValue)
                {
                    result.Add(new Line(point1.Value, point2.Value));
                }
            }
            
            return result;
        }
        
        private netDxf.Vector3? FindPointById(string id, Dictionary<string, netDxf.Vector3> points)
        {
            // 直接查找
            if (points.ContainsKey(id))
            {
                return points[id];
            }
            
            // 查找引用的点实体
            if (entities.ContainsKey(id))
            {
                var entity = entities[id];
                if (entity.Type == "CARTESIAN_POINT")
                {
                    var point = ParseCartesianPoint(entity.Content);
                    if (point.HasValue)
                    {
                        points[id] = point.Value;
                        return point.Value;
                    }
                }
                else if (entity.Type == "VERTEX_POINT")
                {
                    // 解析顶点点引用
                    var refPattern = new Regex(@"#(\d+)");
                    var match = refPattern.Match(entity.Content);
                    if (match.Success)
                    {
                        return FindPointById(match.Groups[1].Value, points);
                    }
                }
            }
            
            return null;
        }
        
        public Dictionary<string, int> GetEntityStatistics()
        {
            var stats = new Dictionary<string, int>();
            
            foreach (var entity in entities.Values)
            {
                if (stats.ContainsKey(entity.Type))
                {
                    stats[entity.Type]++;
                }
                else
                {
                    stats[entity.Type] = 1;
                }
            }
            
            return stats;
        }
        
        public int GetEntityCount()
        {
            return entities.Count;
        }
    }
    
    public class StpEntity
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}





