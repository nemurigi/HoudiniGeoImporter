using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace NmrgLibrary.HoudiniGeoImporter
{
    [ScriptedImporter(1, ".geo")]
    public class HoudiniGeoImporter : ScriptedImporter
    {
        [SerializeField] private List<string> attribNames;
        [SerializeField] bool reverseWinding = true;

        [SerializeField] private string posAttribName = "P";
        [SerializeField] private string normalAttribName = "N";
        [SerializeField] private string colorAttribName = "Cd";
        [SerializeField] private string uv1AttribName = "uv";
        [SerializeField] private string uv2AttribName = "uv2";
        [SerializeField] private string uv3AttribName = "uv3";
        [SerializeField] private string uv4AttribName = "uv4";
        [SerializeField] private string uv5AttribName = "uv5";
        [SerializeField] private string uv6AttribName = "uv6";
        [SerializeField] private string uv7AttribName = "uv7";
        [SerializeField] private string uv8AttribName = "uv8";
        
        
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var houdiniGeo = HoudiniGeoFileParser.Parse(ctx.assetPath);
            houdiniGeo.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
            houdiniGeo.importSettings.reverseWinding = reverseWinding;
            houdiniGeo.POS_ATTR_NAME    = posAttribName;
            houdiniGeo.NORMAL_ATTR_NAME = normalAttribName;
            houdiniGeo.COLOR_ATTR_NAME  = colorAttribName;
            houdiniGeo.UV1_ATTR_NAME    = uv1AttribName;
            houdiniGeo.UV2_ATTR_NAME    = uv2AttribName;
            houdiniGeo.UV3_ATTR_NAME    = uv3AttribName;
            houdiniGeo.UV4_ATTR_NAME    = uv4AttribName;
            houdiniGeo.UV5_ATTR_NAME    = uv5AttribName;
            houdiniGeo.UV6_ATTR_NAME    = uv6AttribName;
            houdiniGeo.UV7_ATTR_NAME    = uv7AttribName;
            houdiniGeo.UV8_ATTR_NAME    = uv8AttribName;
            
            attribNames = houdiniGeo.GetAttributeNames();
            
            var obj = new GameObject();
            var meshFilter = obj.AddComponent<MeshFilter>();
            var meshRenderer = obj.AddComponent<MeshRenderer>();
            ctx.AddObjectToAsset("obj", obj);

            var mesh = houdiniGeo.CreateMesh();
            meshFilter.mesh = mesh;
            ctx.AddObjectToAsset("mesh", mesh);
            
            var material = new Material(Shader.Find("Standard"));
            meshRenderer.material = material;
            ctx.AddObjectToAsset("material", material);
            
            ctx.SetMainObject(obj);
        }
    }
}