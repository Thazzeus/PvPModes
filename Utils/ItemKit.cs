using System;
using System.Collections.Generic;
using System.Text;

namespace PvPModes.Utils
{
    public class ItemKit
    {
        public string Name { get; }
        public Dictionary<int, int> PrefabGUIDs { get; }

        public ItemKit(string name, Dictionary<int, int> prefabGuids)
        {
            Name = name;
            PrefabGUIDs = prefabGuids;
        }
    }
}
