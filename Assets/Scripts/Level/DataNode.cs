using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy.Level
{
    // Data for each pattern node imported from the editor
    [Serializable]
    public class DataNode
    {
        public int id;
        public string name;
        public string region;
        public string type;
        public bool terminal;
        public int entranceCount;
        public List<int> connections;
        public Vector2 editorPosition;

        public bool visited = false;
    }
    
    // Data for the pattern dictionary
    [Serializable]
    public class PatternData
    {
        public string name;
        public List<DataNode> data;
    }
}
