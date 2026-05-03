using System.Collections.Generic;
using UnityEngine;

namespace Stratum
{
    public class AddressableGroupOrderConfig : ScriptableObject
    {
        [SerializeField] private List<string> _groupGuids = new();
        public List<string> GroupGuids => _groupGuids;
    }
}
