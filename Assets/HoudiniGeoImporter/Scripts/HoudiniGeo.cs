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

namespace NmrgLibrary.HoudiniGeoImporter
{
    public class HoudiniGeoFileInfo
    {
        public DateTime date;
        public float timetocook;
        public string software;
        public string artist;
        public string hostname;
        public float time; // TODO: What is this for? It was missing but I just see it being 0 in GEO files.
        public Bounds bounds;
        public string primcount_summary;
        public string attribute_summary;
        public string group_summary;

        public HoudiniGeoFileInfo Copy()
        {
            return (HoudiniGeoFileInfo)MemberwiseClone();
        }
    }

    public enum HoudiniGeoAttributeType
    {
        Invalid = 0,
        Float,
        Integer,
        String,
    }

    public enum HoudiniGeoAttributeOwner
    {
        Invalid = 0,
        Vertex,
        Point,
        Primitive,
        Detail,
        Any,
    }

    public class HoudiniGeoAttribute
    {
        public string name;
        public HoudiniGeoAttributeType type;
        public HoudiniGeoAttributeOwner owner;
        public int tupleSize;

        public List<float> floatValues = new List<float>();
        public List<int> intValues = new List<int>();
        public List<string> stringValues = new List<string>();
    }
    
    public abstract class HoudiniGeoPrimitive
    {
        public string type;
        public int id;
    }
    
    public class PolyPrimitive : HoudiniGeoPrimitive
    {
        public int[] indices;
        public int[] triangles;

        public PolyPrimitive()
        {
            type = "Poly";
        }
    }
    
    public class BezierCurvePrimitive : HoudiniGeoPrimitive
    {
        public int[] indices;
        public int order;
        public int[] knots;
        
        public BezierCurvePrimitive()
        {
            type = "BezierCurve";
        }
    }
    
    public class NURBCurvePrimitive : HoudiniGeoPrimitive
    {
        public List<int> indices = new List<int>();
        public int order;
        public bool endInterpolation;
        public List<int> knots = new List<int>();
        
        public NURBCurvePrimitive()
        {
            type = "NURBCurve";
        }
    }
    
    public enum HoudiniGeoGroupType
    {
        Invalid = 0,
        Points,
        Primitives,
        Edges,
    }

    public class HoudiniGeoGroup
    {
        public string name;
        public HoudiniGeoGroupType type;

        public HoudiniGeoGroup(string name, HoudiniGeoGroupType type)
        {
            this.name = name;
            this.type = type;
        }
    }

    public class PrimitiveGroup : HoudiniGeoGroup
    {
        public List<int> ids = new List<int>();

        public PrimitiveGroup(string name, List<int> ids = null) : base(name, HoudiniGeoGroupType.Primitives)
        {
            if (ids != null)
                this.ids = ids;
        }
    }
    
    public class PointGroup : HoudiniGeoGroup
    {
        public List<int> ids = new List<int>();
        public List<int> vertIds = new List<int>();

        public PointGroup(string name, List<int> ids = null, List<int> vertIds = null)
            : base(name, HoudiniGeoGroupType.Points)
        {
            if (ids != null)
                this.ids = ids;
            if (vertIds != null)
                this.vertIds = vertIds;
        }
    }
    
    public class EdgeGroup : HoudiniGeoGroup
    {
        public List<KeyValuePair<int, int>> pointPairs = new List<KeyValuePair<int, int>>();

        public EdgeGroup(string name, List<KeyValuePair<int, int>> pointPairs = null)
            : base(name, HoudiniGeoGroupType.Edges)
        {
            if (pointPairs != null)
                this.pointPairs = pointPairs;
        }
    }

    public class HoudiniGeoImportSettings
    {
        public bool reverseWinding;
    }

    public class HoudiniGeo
    {
        public string POS_ATTR_NAME = "P";
        public string NORMAL_ATTR_NAME = "N";
        public string COLOR_ATTR_NAME = "Cd";
        public string ALPHA_ATTR_NAME = "Alpha";
        public string TANGENT_ATTR_NAME = "tangent";
        public string MATERIAL_ATTR_NAME = "shop_materialpath";
        public string DEFAULT_MATERIAL_NAME = "Default";
        public string UV1_ATTR_NAME = "uv";
        public string UV2_ATTR_NAME = "uv2";
        public string UV3_ATTR_NAME = "uv3";
        public string UV4_ATTR_NAME = "uv4";
        public string UV5_ATTR_NAME = "uv5";
        public string UV6_ATTR_NAME = "uv6";
        public string UV7_ATTR_NAME = "uv7";
        public string UV8_ATTR_NAME = "uv8";

        public HoudiniGeoImportSettings importSettings = new HoudiniGeoImportSettings();
        public string name;
        
        public string fileVersion;
        public bool hasIndex;
        public int pointCount;
        public int vertexCount;
        public int primCount;
        public HoudiniGeoFileInfo fileInfo;

        public List<int> pointRefs = new List<int>();
            
        public List<HoudiniGeoAttribute> attributes = new List<HoudiniGeoAttribute>();

        public List<PolyPrimitive> polyPrimitives = new List<PolyPrimitive>();
        public List<BezierCurvePrimitive> bezierCurvePrimitives = new List<BezierCurvePrimitive>();
        public List<NURBCurvePrimitive> nurbCurvePrimitives = new List<NURBCurvePrimitive>();

        public List<PrimitiveGroup> primitiveGroups = new List<PrimitiveGroup>();
        public List<PointGroup> pointGroups = new List<PointGroup>();
        public List<EdgeGroup> edgeGroups = new List<EdgeGroup>();
    }
}
