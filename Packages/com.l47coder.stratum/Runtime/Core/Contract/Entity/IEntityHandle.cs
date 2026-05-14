using UnityEngine;

namespace Stratum
{
    public interface IEntityHandle
    {
        public GameObject GameObject { get; internal set; }
        public Transform Transform { get; internal set; }
    }
}
