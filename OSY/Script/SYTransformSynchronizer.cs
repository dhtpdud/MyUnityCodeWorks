using UnityEngine;

namespace JSchool.Modules.Common.OSY
{
    public class SYTransformSynchronizer : MonoBehaviour
    {
        public Transform target;

        private void Update()
        {
            transform.position = target.position;
            transform.rotation = target.rotation;
        }
    }
}
