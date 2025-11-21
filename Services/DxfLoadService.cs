using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using netDxf;
using netDxf.Entities;

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
    }
}