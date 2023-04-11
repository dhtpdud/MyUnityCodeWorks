using JSchool.PJLab;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace JSchool.Modules.Common.OSY
{
    public class SYComponentConverter : MonoBehaviour
    {
        public enum Type
        {
            SpriteRenderer,
            Image,
            SkeletonAnimation,
            SkeletonGraphic
        }
        public GameObject[] targets;
        public Type convertTo;

        public void ConvertAll()
        {
            for (var i = 0; i < targets.Length; i++)
                Convert(targets[i]);
        }

        public void Convert(GameObject target)
        {
            Component targetComponent = null;
            Component convertedComponent = null;
            switch (convertTo)
            {
                case Type.SpriteRenderer:
                    targetComponent = target.GetComponent<Image>();
                    if (!targetComponent) return;
                    convertedComponent = PJUtil.GetOrAddComponent<SpriteRenderer>(target.gameObject);
                    ((SpriteRenderer)convertedComponent).sprite = ((Image)targetComponent).sprite;
                    DestroyImmediate(targetComponent);
                    DestroyImmediate(target.GetComponent<RectTransform>());
                    break;
                case Type.Image:
                    target.AddComponent<RectTransform>();
                    targetComponent = target.GetComponent<SpriteRenderer>();
                    if (!targetComponent) return;
                    convertedComponent = PJUtil.GetOrAddComponent<Image>(target.gameObject);
                    ((Image)convertedComponent).sprite = ((SpriteRenderer)targetComponent).sprite;
                    DestroyImmediate(targetComponent);
                    break;


                case Type.SkeletonAnimation:
                    targetComponent = target.GetComponent<SkeletonGraphic>();
                    if (!targetComponent) return;
                    convertedComponent = PJUtil.GetOrAddComponent<SkeletonAnimation>(target.gameObject);
                    ((SkeletonAnimation)convertedComponent).skeletonDataAsset =
                        ((SkeletonGraphic)targetComponent).skeletonDataAsset;

                    DestroyImmediate(targetComponent);
                    DestroyImmediate(target.GetComponent<RectTransform>());
                    break;
                case Type.SkeletonGraphic:
                    target.AddComponent<RectTransform>();
                    ConvertToSkeletonGraphic(target, targetComponent, convertedComponent);
                    break;
            }
        }

        void ConvertToSkeletonGraphic(GameObject target, Component targetComponent,
            Component convertedComponent)
        {
            targetComponent = target.GetComponent<SkeletonAnimation>();
            if (!targetComponent) return;
            convertedComponent = PJUtil.GetOrAddComponent<SkeletonGraphic>(target.gameObject);

            ((SkeletonGraphic)convertedComponent).skeletonDataAsset =
                ((SkeletonAnimation)targetComponent).skeletonDataAsset;

            ((SkeletonGraphic)convertedComponent).initialSkinName =
                ((SkeletonAnimation)targetComponent).initialSkinName;

            //Thread.Sleep(100); 
            //((SkeletonGraphic)convertedComponent).MatchRectTransformWithBounds();
            DestroyImmediate(targetComponent);
            DestroyImmediate(target.GetComponent<MeshRenderer>());
            DestroyImmediate(target.GetComponent<MeshFilter>());
        }
    }
}