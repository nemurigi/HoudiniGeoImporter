/**
 * Houdini Geo File Importer for Unity
 *
 * Copyright 2015 by Waldo Bronchart <wbronchart@gmail.com>
 * Exporter added in 2021 by Roy Theunissen <roy.theunissen@live.nl>
 * Licensed under GNU General Public License 3.0 or later.
 * Some rights reserved. See COPYING, AUTHORS.
 */

using UnityEngine;
using UnityEditor;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;

namespace NmrgLibrary.HoudiniGeoImporter
{
    public static class HoudiniGeoExtensions
    {
        public const string PositionAttributeName = "P";
        public const string NormalAttributeName = "N";
        public const string UpAttributeName = "up";
        public const string RotationAttributeName = "orient";
        
        public static readonly string[] GroupFieldNames = { "groups", "grouping" };
        
        internal static Mesh CreateMesh(this HoudiniGeo geo)
        {
            var mesh = new Mesh();
            if (geo.polyPrimitives.Count > 0)
            {
                geo.ToUnityMesh(mesh);
            }

            return mesh;
        }
        
        public static List<string> GetAttributeNames(this HoudiniGeo geo)
        {
            return geo.attributes
                .Where(attrib => attrib.owner == HoudiniGeoAttributeOwner.Point || attrib.owner == HoudiniGeoAttributeOwner.Vertex)
                .Select(attrib => attrib.name)
                .ToList();
        }

        private static void ToUnityMesh(this HoudiniGeo geo, Mesh mesh)
        {
            // polyPrimitivesが0ならreturn
            if (geo.polyPrimitives.Count == 0)
            {
                Debug.LogError("Cannot convert HoudiniGeo to Mesh because geo has no PolyPrimitives");
                return;
            }

            mesh.name = geo.name;
            
            int[] indices = geo.polyPrimitives.SelectMany(p => p.indices).ToArray();
            int vertexCount = indices.Length;
            if (vertexCount > 65000)
            {
                throw new Exception(string.Format("Vertex count ({0}) exceeds limit of {1}!", geo.vertexCount, 65000));
            }

            // Check if position attribute exists
            HoudiniGeoAttribute posAttr = null;
            if (!geo.TryGetAttribute(geo.POS_ATTR_NAME, HoudiniGeoAttributeType.Float, out posAttr))
            {
                Debug.LogWarning("HoudiniGEO has no Position attribute on points or vertices");
            }
            
            GetAttrib(geo, geo.POS_ATTR_NAME, out posAttr, out Vector3[] posAttrValues);
            GetAttrib(geo, geo.UV1_ATTR_NAME, out HoudiniGeoAttribute uvAttr, out Vector4[] uvAttrValues);
            GetAttrib(geo, geo.UV1_ATTR_NAME, out HoudiniGeoAttribute uv2Attr, out Vector4[] uv2AttrValues);
            GetAttrib(geo, geo.UV1_ATTR_NAME, out HoudiniGeoAttribute uv3Attr, out Vector4[] uv3AttrValues);
            GetAttrib(geo, geo.UV1_ATTR_NAME, out HoudiniGeoAttribute uv4Attr, out Vector4[] uv4AttrValues);
            GetAttrib(geo, geo.UV1_ATTR_NAME, out HoudiniGeoAttribute uv5Attr, out Vector4[] uv5AttrValues);
            GetAttrib(geo, geo.UV1_ATTR_NAME, out HoudiniGeoAttribute uv6Attr, out Vector4[] uv6AttrValues);
            GetAttrib(geo, geo.UV1_ATTR_NAME, out HoudiniGeoAttribute uv7Attr, out Vector4[] uv7AttrValues);
            GetAttrib(geo, geo.UV1_ATTR_NAME, out HoudiniGeoAttribute uv8Attr, out Vector4[] uv8AttrValues);
            GetAttrib(geo, geo.NORMAL_ATTR_NAME, out HoudiniGeoAttribute normalAttr, out Vector3[] normalAttrValues);
            GetAttrib(geo, geo.TANGENT_ATTR_NAME, out HoudiniGeoAttribute tangentAttr, out Vector3[] tangentAttrValues);
            GetAttrib(geo, geo.MATERIAL_ATTR_NAME, out HoudiniGeoAttribute materialAttr, out string[] materialAttributeValues);
            GetAttrib(geo, geo.COLOR_ATTR_NAME, out HoudiniGeoAttribute colorAttr, out Color[] colorAttrValues);
            GetAttrib(geo, geo.ALPHA_ATTR_NAME, out HoudiniGeoAttribute alphaAttr, out float[] alphaAttrValues);
            
            if (colorAttr != null && alphaAttr != null && colorAttrValues.Length == alphaAttrValues.Length)
            {
                for (int i=0; i<colorAttrValues.Length; i++)
                {
                    colorAttrValues[i].a = alphaAttrValues[i];
                }
            }
            
            // Create our mesh attribute buffers
            var submeshInfo = new Dictionary<string, List<int>>();
            var positions = new Vector3[vertexCount];
            var uvs = new Vector4[vertexCount]; // unity doesn't like it when meshes have no uvs
            var normals = (normalAttr != null) ? new Vector3[vertexCount] : null;
            var colors = (colorAttr != null) ? new Color[vertexCount] : null;
            var tangents = (tangentAttr != null) ? new Vector4[vertexCount] : null;
            var uvs2 = (uv2Attr != null) ? new Vector4[vertexCount] : null;
            var uvs3 = (uv3Attr != null) ? new Vector4[vertexCount] : null;
            var uvs4 = (uv4Attr != null) ? new Vector4[vertexCount] : null;
            var uvs5 = (uv5Attr != null) ? new Vector4[vertexCount] : null;
            var uvs6 = (uv6Attr != null) ? new Vector4[vertexCount] : null;
            var uvs7 = (uv7Attr != null) ? new Vector4[vertexCount] : null;
            var uvs8 = (uv8Attr != null) ? new Vector4[vertexCount] : null;
            
            // AttributeをVertex Bufferの配列に変換する (Vertex/Point Attributes)
            int[] vertToPoint = geo.pointRefs.ToArray();
            Dictionary<int, int> vertIndexGlobalToLocal = new Dictionary<int, int>();
            for (int i=0; i<vertexCount; ++i)
            {
                int vertIndex = indices[i];
                int pointIndex = vertToPoint[vertIndex];
                vertIndexGlobalToLocal.Add(vertIndex, i);
                
                positions[i] = AttribToBuffer(posAttr, posAttrValues, vertIndex, pointIndex);
                uvs[i] = AttribToBuffer(uvAttr, uvAttrValues, vertIndex, pointIndex);
                
                if (normalAttr != null) normals[i] = AttribToBuffer(normalAttr, normalAttrValues, vertIndex, pointIndex);
                if (tangentAttr != null) tangents[i] = AttribToBuffer(tangentAttr, tangentAttrValues, vertIndex, pointIndex);
                if (colorAttr != null) colors[i] = AttribToBuffer(colorAttr, colorAttrValues, vertIndex, pointIndex);
                if (uv2Attr != null) uvs2[i] = AttribToBuffer(uv2Attr, uv2AttrValues, vertIndex, pointIndex);
                if (uv3Attr != null) uvs3[i] = AttribToBuffer(uv3Attr, uv3AttrValues, vertIndex, pointIndex);
                if (uv4Attr != null) uvs4[i] = AttribToBuffer(uv4Attr, uv4AttrValues, vertIndex, pointIndex);
                if (uv5Attr != null) uvs5[i] = AttribToBuffer(uv5Attr, uv5AttrValues, vertIndex, pointIndex);
                if (uv6Attr != null) uvs6[i] = AttribToBuffer(uv6Attr, uv6AttrValues, vertIndex, pointIndex);
                if (uv7Attr != null) uvs7[i] = AttribToBuffer(uv7Attr, uv7AttrValues, vertIndex, pointIndex);
                if (uv8Attr != null) uvs8[i] = AttribToBuffer(uv8Attr, uv8AttrValues, vertIndex, pointIndex);
            }

            // AttributeをVertex Bufferの配列に変換する (Primitive Attributes)
            // Material Atttributeごとにサブメッシュを作成
            foreach (var polyPrim in geo.polyPrimitives)
            {
                // Normals
                if (normalAttr != null && normalAttr.owner == HoudiniGeoAttributeOwner.Primitive)
                {
                    foreach (var vertIndex in polyPrim.indices)
                    {
                        int localVertIndex = vertIndexGlobalToLocal[vertIndex];
                        normals[localVertIndex] = normalAttrValues[polyPrim.id];
                    }
                }

                // Colors
                if (colorAttr != null && colorAttr.owner == HoudiniGeoAttributeOwner.Primitive)
                {
                    foreach (var vertIndex in polyPrim.indices)
                    {
                        int localVertIndex = vertIndexGlobalToLocal[vertIndex];
                        colors[localVertIndex] = colorAttrValues[polyPrim.id];
                    }
                }

                // Add face to submesh based on material attribute
                var materialName = (materialAttr == null) ? geo.DEFAULT_MATERIAL_NAME : materialAttributeValues[polyPrim.id];
                if (!submeshInfo.ContainsKey(materialName))
                {
                    submeshInfo.Add(materialName, new List<int>());
                }
                submeshInfo[materialName].AddRange(polyPrim.triangles);
            }
            
            // meshのBufferにアタッチ
            mesh.vertices = positions;
            mesh.subMeshCount = submeshInfo.Count;
            mesh.normals = normals;
            mesh.colors = colors;
            mesh.tangents = tangents;
            mesh.SetUVs(0, uvs);
            mesh.SetUVs(1, uvs2);
            mesh.SetUVs(2, uvs3);
            mesh.SetUVs(3, uvs4);
            mesh.SetUVs(4, uvs5);
            mesh.SetUVs(5, uvs6);
            mesh.SetUVs(6, uvs7);
            mesh.SetUVs(7, uvs8);
            
            // Set submesh indexbuffers
            int submeshIndex = 0;
            foreach (var item in submeshInfo)
            {
                // Skip empty submeshes
                if (item.Value.Count == 0)
                    continue;
                
                // Set the indices for the submesh (Reversed by default because axis coordinates Z flipped)
                IEnumerable<int> submeshIndices = item.Value;
                if (!geo.importSettings.reverseWinding)
                {
                    submeshIndices = submeshIndices.Reverse();
                }
                mesh.SetIndices(submeshIndices.ToArray(), MeshTopology.Triangles, submeshIndex);
                submeshIndex++;
            }

            // Calculate any missing buffers
            mesh.ConvertToUnityCoordinates();
            mesh.RecalculateBounds();
            if (normalAttr == null)
            {
                mesh.RecalculateNormals();
            }
        }

        private static void ConvertToUnityCoordinates(this Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            for (int i=0; i<vertices.Length; i++)
            {
                vertices[i].z *= -1;
            }
            mesh.vertices = vertices;

            Vector3[] normals = mesh.normals;
            for (int i=0; i<normals.Length; i++)
            {
                normals[i].z *= -1;
            }
            mesh.normals = normals;
            
            Vector4[] tangents = mesh.tangents;
            for (int i=0; i<tangents.Length; i++)
            {
                tangents[i].z *= -1;
            }
            mesh.tangents = tangents;
        }
        
        public static bool HasAttribute(this HoudiniGeo geo, string attrName, HoudiniGeoAttributeOwner owner)
        {
            if (owner == HoudiniGeoAttributeOwner.Any)
            {
                return geo.attributes.Any(a => a.name == attrName);
            }

            return geo.attributes.Any(a => a.owner == owner && a.name == attrName);
        }
        
        public static bool TryGetAttribute(this HoudiniGeo geo, string attrName, out HoudiniGeoAttribute attr)
        {
            attr = geo.attributes.FirstOrDefault(a => a.name == attrName);
            return (attr != null);
        }
        
        public static bool TryGetAttribute(this HoudiniGeo geo, string attrName, HoudiniGeoAttributeType type, out HoudiniGeoAttribute attr)
        {
            attr = geo.attributes.FirstOrDefault(a => a.type == type && a.name == attrName);
            return (attr != null);
        }

        public static bool TryGetAttribute(this HoudiniGeo geo, string attrName, HoudiniGeoAttributeOwner owner, out HoudiniGeoAttribute attr)
        {
            if (owner == HoudiniGeoAttributeOwner.Any)
            {
                attr = geo.attributes.FirstOrDefault(a => a.name == attrName);
            }
            else
            {
                attr = geo.attributes.FirstOrDefault(a => a.owner == owner && a.name == attrName);
            }

            return (attr != null);
        }
        
        public static bool TryGetAttribute(this HoudiniGeo geo, string attrName, HoudiniGeoAttributeType type,
                                           HoudiniGeoAttributeOwner owner, out HoudiniGeoAttribute attr)
        {
            if (owner == HoudiniGeoAttributeOwner.Any)
            {
                attr = geo.attributes.FirstOrDefault(a => a.type == type && a.name == attrName);
            }
            else
            {
                attr = geo.attributes.FirstOrDefault(a => a.owner == owner && a.type == type && a.name == attrName);
            }
            return (attr != null);
        }
        
                private static void GetAttrib(HoudiniGeo geo, string uvAttribName, out HoudiniGeoAttribute attr, out Vector4[] attrValues)
        {
            attr = null;
            attrValues = null;
            if (geo.TryGetAttribute(uvAttribName, HoudiniGeoAttributeType.Float, out attr))
            {
                attr.GetValues(out attrValues);
            }
        }
        
        private static void GetAttrib(HoudiniGeo geo, string uvAttribName, out HoudiniGeoAttribute attr, out Vector3[] attrValues)
        {
            attr = null;
            attrValues = null;
            if (geo.TryGetAttribute(uvAttribName, HoudiniGeoAttributeType.Float, out attr))
            {
                attr.GetValues(out attrValues);
            }
        }
        
        private static void GetAttrib(HoudiniGeo geo, string uvAttribName, out HoudiniGeoAttribute attr, out string[] attrValues)
        {
            attr = null;
            attrValues = null;
            if (geo.TryGetAttribute(uvAttribName, HoudiniGeoAttributeType.Float, out attr))
            {
                attr.GetValues(out attrValues);
            }
        }
        
        private static void GetAttrib(HoudiniGeo geo, string uvAttribName, out HoudiniGeoAttribute attr, out float[] attrValues)
        {
            attr = null;
            attrValues = null;
            if (geo.TryGetAttribute(uvAttribName, HoudiniGeoAttributeType.Float, out attr))
            {
                attr.GetValues(out attrValues);
            }
        }
        
        private static void GetAttrib(HoudiniGeo geo, string uvAttribName, out HoudiniGeoAttribute attr, out Color[] attrValues)
        {
            attr = null;
            attrValues = null;
            if (geo.TryGetAttribute(uvAttribName, HoudiniGeoAttributeType.Float, out attr))
            {
                attr.GetValues(out attrValues);
            }
        }

        private static T AttribToBuffer<T>(HoudiniGeoAttribute attrib, T[] attrValues, int vertIndex, int pointIndex)
        {
            if (attrib == null)
            {
                return default(T);
            }
            switch (attrib.owner)
            {
                case HoudiniGeoAttributeOwner.Vertex:
                    return attrValues[vertIndex];
                case HoudiniGeoAttributeOwner.Point:
                    return attrValues[pointIndex];
            }
            return default(T);
        }
        
        private static void GetValues(this HoudiniGeoAttribute attr, out float[] values)
        {
            // if (!attr.ValidateForGetValues<float>(HoudiniGeoAttributeType.Float, 1))
            // {
            //     values = new float[0];
            //     return;
            // }

            float[] rawValues = attr.floatValues.ToArray();
            values = new float[rawValues.Length / attr.tupleSize];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = rawValues[i * attr.tupleSize + 0];
            }
        }
        
        private static void GetValues(this HoudiniGeoAttribute attr, out Vector2[] values)
        {
            // if (!attr.ValidateForGetValues<Vector2>(HoudiniGeoAttributeType.Float, 2))
            // {
            //     values = new Vector2[0];
            //     return;
            // }
            
            var size = attr.tupleSize;
            float[] rawValues = attr.floatValues.ToArray();
            values = new Vector2[rawValues.Length / attr.tupleSize];
            for (int i=0; i<values.Length; i++)
            {
                values[i].x = size < 1 ? 0.0f : rawValues[i * attr.tupleSize + 0];
                values[i].y = size < 2 ? 0.0f : rawValues[i * attr.tupleSize + 1];
            }
        }

        private static void GetValues(this HoudiniGeoAttribute attr, out Vector3[] values)
        {
            // if (!attr.ValidateForGetValues<Vector3>(HoudiniGeoAttributeType.Float, 3))
            // {
            //     values = new Vector3[0];
            //     return;
            // }

            var size = attr.tupleSize;
            float[] rawValues = attr.floatValues.ToArray();
            values = new Vector3[rawValues.Length / attr.tupleSize];
            for (int i=0; i<values.Length; i++)
            {
                values[i].x = size < 1 ? 0.0f : rawValues[i * size + 0];
                values[i].y = size < 2 ? 0.0f : rawValues[i * size + 1];
                values[i].z = size < 3 ? 0.0f : rawValues[i * size + 2];
            }
        }
        
        private static void GetValues(this HoudiniGeoAttribute attr, out Vector4[] values)
        {
            // if (!attr.ValidateForGetValues<Vector4>(HoudiniGeoAttributeType.Float, 4))
            // {
            //     values = new Vector4[0];
            //     return;
            // }
            
            int size = attr.tupleSize;
            float[] rawValues = attr.floatValues.ToArray();
            values = new Vector4[rawValues.Length / attr.tupleSize];
            for (int i=0; i<values.Length; i++)
            {
                values[i].x = size < 1 ? 0.0f : rawValues[i * size + 0];
                values[i].y = size < 2 ? 0.0f : rawValues[i * size + 1];
                values[i].z = size < 3 ? 0.0f : rawValues[i * size + 2];
                values[i].w = size < 4 ? 0.0f : rawValues[i * size + 3];
            }
        }
        
        private static void GetValues(this HoudiniGeoAttribute attr, out Color[] values)
        {
            // if (!attr.ValidateForGetValues<Color>(HoudiniGeoAttributeType.Float, 3))
            // {
            //     values = new Color[0];
            //     return;
            // }
            
            int size = attr.tupleSize;
            float[] rawValues = attr.floatValues.ToArray();
            values = new Color[rawValues.Length / attr.tupleSize];
            for (int i=0; i<values.Length; i++)
            {
                values[i].r = size < 1 ? 0.0f : rawValues[i * attr.tupleSize + 0];
                values[i].g = size < 2 ? 0.0f : rawValues[i * attr.tupleSize + 1];
                values[i].b = size < 3 ? 0.0f : rawValues[i * attr.tupleSize + 2];
                values[i].a = size < 4 ? 0.0f : rawValues[i * attr.tupleSize + 3];
            }
        }

        private static void GetValues(this HoudiniGeoAttribute attr, out int[] values)
        {
            if (!attr.ValidateForGetValues<int>(HoudiniGeoAttributeType.Integer, 1))
            {
                values = new int[0];
                return;
            }
            
            values = attr.intValues.ToArray();
        }
        
        private static void GetValues(this HoudiniGeoAttribute attr, out string[] values)
        {
            if (!attr.ValidateForGetValues<string>(HoudiniGeoAttributeType.String, 1))
            {
                values = new string[0];
                return;
            }
            
            values = attr.stringValues.ToArray();
        }
        
        private static bool ValidateForGetValues<T>(this HoudiniGeoAttribute attr, HoudiniGeoAttributeType expectedType, 
                                                    int expectedMinTupleSize)
        {
            if (attr.type != expectedType)
            {
                Debug.LogError(string.Format("Cannot convert raw values of {0} attribute '{1}' to {2} (type: {3})", 
                                             attr.owner, attr.name, typeof(T).Name, attr.type));
                return false;
            }
            
            if (attr.tupleSize < expectedMinTupleSize)
            {
                Debug.LogError(string.Format("The tuple size of {0} attribute '{1}' too small for conversion to {2}",
                                             attr.owner, attr.name, typeof(T).Name));
                return false;
            }
            
            return true;
        }

        private static bool GetAttributeTypeAndSize(Type valueType, out HoudiniGeoAttributeType type, out int tupleSize)
        {
            type = HoudiniGeoAttributeType.Invalid;
            tupleSize = 0;
            
            if (valueType == typeof(bool))
            {
                type = HoudiniGeoAttributeType.Integer;
                tupleSize = 1;
            }
            else if (valueType == typeof(float))
            {
                type = HoudiniGeoAttributeType.Float;
                tupleSize = 1;
            }
            else if (valueType == typeof(int))
            {
                type = HoudiniGeoAttributeType.Integer;
                tupleSize = 1;
            }
            else if (valueType == typeof(string))
            {
                type = HoudiniGeoAttributeType.String;
                tupleSize = 1;
            }
            if (valueType == typeof(Vector2))
            {
                type = HoudiniGeoAttributeType.Float;
                tupleSize = 2;
            }
            else if (valueType == typeof(Vector3))
            {
                type = HoudiniGeoAttributeType.Float;
                tupleSize = 3;
            }
            else if (valueType == typeof(Vector4))
            {
                type = HoudiniGeoAttributeType.Float;
                tupleSize = 4;
            }
            else if (valueType == typeof(Vector2Int))
            {
                type = HoudiniGeoAttributeType.Integer;
                tupleSize = 2;
            }
            else if (valueType == typeof(Vector3Int))
            {
                type = HoudiniGeoAttributeType.Integer;
                tupleSize = 3;
            }
            else if (valueType == typeof(Quaternion))
            {
                type = HoudiniGeoAttributeType.Float;
                tupleSize = 4;
            }
            else if (valueType == typeof(Color))
            {
                type = HoudiniGeoAttributeType.Float;
                tupleSize = 3;
            }

            return type != HoudiniGeoAttributeType.Invalid;
        }
        
        private static object GetAttributeValue(Type type, HoudiniGeoAttribute attribute, int index)
        {
            if (type == typeof(bool))
                return attribute.intValues[index] == 1;
            if (type == typeof(float))
                return attribute.floatValues[index];
            if (type == typeof(int))
                return attribute.intValues[index];
            if (type == typeof(string))
                return attribute.stringValues[index];
            if (type == typeof(Vector2))
                return new Vector2(attribute.floatValues[index * 2], attribute.floatValues[index * 2 + 1]);
            if (type == typeof(Vector3))
                return new Vector3(attribute.floatValues[index * 3], attribute.floatValues[index * 3 + 1], attribute.floatValues[index * 3 + 2]);
            if (type == typeof(Vector4))
                return new Vector4(attribute.floatValues[index * 4], attribute.floatValues[index * 4 + 1], attribute.floatValues[index * 4 + 2], attribute.floatValues[index * 4 + 3]);
            if (type == typeof(Vector2Int))
                return new Vector2Int(attribute.intValues[index * 2], attribute.intValues[index * 2 + 1]);
            if (type == typeof(Vector3Int))
                return new Vector3Int(attribute.intValues[index * 3], attribute.intValues[index * 3 + 1], attribute.intValues[index * 3 + 2]);
            if (type == typeof(Quaternion))
                return new Quaternion(attribute.floatValues[index * 4], attribute.floatValues[index * 4 + 1], attribute.floatValues[index * 4 + 2], attribute.floatValues[index * 4 + 3]);
            if (type == typeof(Color))
                return new Color(attribute.floatValues[index * 3], attribute.floatValues[index * 3 + 1], attribute.floatValues[index * 3 + 2]);
            
            Debug.LogWarning($"Tried to get value of unrecognized type '{type.Name}'");
            return null;
        }
    }
}
