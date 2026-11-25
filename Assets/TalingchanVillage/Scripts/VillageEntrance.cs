using UnityEngine;
namespace Talingchan.Village
{
    public class VillageEntrance : MonoBehaviour
    {
        public string label;
        public Vector3 outside;
        public Vector3 inside;
        public Vector3 OutsideWorld=>transform.TransformPoint(outside);
        public Vector3 InsideWorld=>transform.TransformPoint(inside);
    }
}
