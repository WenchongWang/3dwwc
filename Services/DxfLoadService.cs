using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using netDxf;
using netDxf.Entities;
using netDxf.Tables;

namespace Lens3DWinForms.Services
{
    public class DxfLoadService
    {
        private OrderedDictionary graphics = new OrderedDictionary();
        private OrderedDictionary geometryModels = new OrderedDictionary();
        
        public OrderedDictionary GetGraphics => graphics;
        public OrderedDictionary GetGeometryModels => geometryModels;

        public List<string> LoadDxfEntities(string filePath)
        {
            var dxf = DxfDocument.Load(filePath);
            graphics.Clear();
            geometryModels.Clear();
            
            List<string> entityInfo = new List<string>();
            
            foreach (EntityObject entity in dxf.Entities.All)
            {
                if (entity.Color.Index == 163) // Same filter condition as original
                {
                    try
                    {
                        // Store entity information
                        string key = $"{entity.GetType().Name}_{entity.Handle}";
                        graphics.Add(key, entity);
                        
                        // Add to info list for display
                        entityInfo.Add($"{entity.GetType().Name}: {entity.Handle}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing entity: {ex.Message}");
                    }
                }
            }
            
            return entityInfo;
        }

        public List<EntityObject> LoadDxfEntitiesFor3D(string filePath)
        {
            var dxf = DxfDocument.Load(filePath);
            graphics.Clear();
            geometryModels.Clear();
            
            List<EntityObject> entities = new List<EntityObject>();
            
            foreach (EntityObject entity in dxf.Entities.All)
            {
                try
                {
                    string key = $"{entity.GetType().Name}_{entity.Handle}";
                    graphics.Add(key, entity);
                    entities.Add(entity);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing entity: {ex.Message}");
                }
            }
            
            return entities;
        }

        private netDxf.Entities.Line OrderLine(netDxf.Entities.Line line)
        {
            // Simplified ordering logic - can be enhanced as needed
            return line;
        }

        /// <summary>
        /// 保存实体列表到DXF文件
        /// </summary>
        /// <param name="entities">要保存的实体列表</param>
        /// <param name="filePath">保存的文件路径</param>
        /// <returns>是否保存成功</returns>
        public bool SaveDxfEntities(List<EntityObject> entities, string filePath)
        {
            try
            {
                // 创建新的DXF文档
                var dxf = new DxfDocument();
                
                // 收集所有需要的Layer和Linetype，确保新文档中有它们
                var layers = new Dictionary<string, Layer>();
                var linetypes = new Dictionary<string, Linetype>();
                
                foreach (var entity in entities)
                {
                    // 收集Layer
                    if (entity.Layer != null && !layers.ContainsKey(entity.Layer.Name))
                    {
                        try
                        {
                            // 查找是否已存在该Layer
                            Layer existingLayer = null;
                            foreach (var layer in dxf.Layers)
                            {
                                if (layer.Name == entity.Layer.Name)
                                {
                                    existingLayer = layer;
                                    break;
                                }
                            }
                            
                            if (existingLayer == null)
                            {
                                var newLayer = new Layer(entity.Layer.Name)
                                {
                                    Color = entity.Layer.Color,
                                    Lineweight = entity.Layer.Lineweight
                                };
                                dxf.Layers.Add(newLayer);
                                layers[entity.Layer.Name] = newLayer;
                            }
                            else
                            {
                                layers[entity.Layer.Name] = existingLayer;
                            }
                        }
                        catch
                        {
                            // 如果获取失败，创建新的
                            var newLayer = new Layer(entity.Layer.Name)
                            {
                                Color = entity.Layer.Color,
                                Lineweight = entity.Layer.Lineweight
                            };
                            dxf.Layers.Add(newLayer);
                            layers[entity.Layer.Name] = newLayer;
                        }
                    }
                    
                    // 收集Linetype
                    if (entity.Linetype != null && !linetypes.ContainsKey(entity.Linetype.Name))
                    {
                        try
                        {
                            // 查找是否已存在该Linetype
                            Linetype existingLinetype = null;
                            foreach (var linetype in dxf.Linetypes)
                            {
                                if (linetype.Name == entity.Linetype.Name)
                                {
                                    existingLinetype = linetype;
                                    break;
                                }
                            }
                            
                            if (existingLinetype == null)
                            {
                                // 创建简单的线型
                                var newLinetype = new Linetype(entity.Linetype.Name);
                                dxf.Linetypes.Add(newLinetype);
                                linetypes[entity.Linetype.Name] = newLinetype;
                            }
                            else
                            {
                                linetypes[entity.Linetype.Name] = existingLinetype;
                            }
                        }
                        catch
                        {
                            // 如果获取失败，创建新的
                            var newLinetype = new Linetype(entity.Linetype.Name);
                            dxf.Linetypes.Add(newLinetype);
                            linetypes[entity.Linetype.Name] = newLinetype;
                        }
                    }
                }
                
                // 添加所有实体到文档
                foreach (var entity in entities)
                {
                    // 克隆实体以避免引用问题
                    EntityObject clonedEntity = CloneEntity(entity, dxf, layers, linetypes);
                    if (clonedEntity != null)
                    {
                        dxf.Entities.Add(clonedEntity);
                    }
                }
                
                // 保存DXF文件
                dxf.Save(filePath);
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving DXF file: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 克隆实体对象
        /// </summary>
        private EntityObject CloneEntity(EntityObject entity, DxfDocument targetDoc, 
            Dictionary<string, Layer> layers, 
            Dictionary<string, Linetype> linetypes)
        {
            try
            {
                EntityObject clonedEntity = null;
                
                switch (entity)
                {
                    case Line line:
                        clonedEntity = new Line(line.StartPoint, line.EndPoint);
                        break;
                    
                    case Circle circle:
                        clonedEntity = new Circle(circle.Center, circle.Radius);
                        break;
                    
                    case Arc arc:
                        clonedEntity = new Arc(arc.Center, arc.Radius, arc.StartAngle, arc.EndAngle);
                        break;
                    
                    case Polyline3D poly3d:
                        var newPoly3d = new Polyline3D();
                        foreach (var vertex in poly3d.Vertexes)
                        {
                            newPoly3d.Vertexes.Add(vertex);
                        }
                        clonedEntity = newPoly3d;
                        break;
                    
                    case Polyline2D poly2d:
                        var newPoly2d = new Polyline2D();
                        foreach (var vertex in poly2d.Vertexes)
                        {
                            newPoly2d.Vertexes.Add(new Polyline2DVertex(vertex.Position));
                        }
                        clonedEntity = newPoly2d;
                        break;
                    
                    case Ellipse ellipse:
                        clonedEntity = new Ellipse(ellipse.Center, ellipse.MajorAxis, ellipse.MinorAxis)
                        {
                            StartAngle = ellipse.StartAngle,
                            EndAngle = ellipse.EndAngle
                        };
                        break;
                    
                    default:
                        // 对于其他类型的实体，返回null（不保存）
                        Console.WriteLine($"Unsupported entity type for cloning: {entity.GetType().Name}");
                        return null;
                }
                
                if (clonedEntity != null)
                {
                    // 设置属性，使用新文档中的Layer和Linetype
                    clonedEntity.Color = entity.Color;
                    clonedEntity.Lineweight = entity.Lineweight;
                    
                    // 设置Layer（使用新文档中的Layer）
                    if (entity.Layer != null && layers.ContainsKey(entity.Layer.Name))
                    {
                        clonedEntity.Layer = layers[entity.Layer.Name];
                    }
                    else if (entity.Layer != null)
                    {
                        // 如果Layer不存在，查找默认Layer（通常是"0"）
                        Layer defaultLayer = null;
                        foreach (var layer in targetDoc.Layers)
                        {
                            if (layer.Name == "0" || layer.Name == entity.Layer.Name)
                            {
                                defaultLayer = layer;
                                break;
                            }
                        }
                        if (defaultLayer != null)
                        {
                            clonedEntity.Layer = defaultLayer;
                        }
                    }
                    
                    // 设置Linetype（使用新文档中的Linetype）
                    if (entity.Linetype != null && linetypes.ContainsKey(entity.Linetype.Name))
                    {
                        clonedEntity.Linetype = linetypes[entity.Linetype.Name];
                    }
                    else if (entity.Linetype != null)
                    {
                        // 如果Linetype不存在，查找默认Linetype（通常是"Continuous"）
                        Linetype defaultLinetype = null;
                        foreach (var linetype in targetDoc.Linetypes)
                        {
                            if (linetype.Name == "Continuous" || linetype.Name == entity.Linetype.Name)
                            {
                                defaultLinetype = linetype;
                                break;
                            }
                        }
                        if (defaultLinetype != null)
                        {
                            clonedEntity.Linetype = defaultLinetype;
                        }
                    }
                }
                
                return clonedEntity;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cloning entity {entity.GetType().Name}: {ex.Message}");
                return null;
            }
        }
    }
}