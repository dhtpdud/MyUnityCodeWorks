using Spine;
using Spine.Unity;
using UnityEngine;
using AnimationState = Spine.AnimationState;

namespace JSchool.Common.Script.BaseFramework.Spine
{
    [RequireComponent(typeof(ISkeletonAnimation))]
    public class MonoSpineController : MonoBehaviour, ISpineController
    {
        public SkeletonAnimation SkeletonAnimation { get; private set; }
        public SkeletonGraphic SkeletonGraphic { get; private set;}
        public ISkeletonAnimation ISkeletonAnimation { get; private set;}
        public IAnimationStateComponent IAnimationStateComponent { get; private set;}
        
        public SkeletonDataAsset SkeletonDataAsset => IsSkeletonGraphic ? SkeletonGraphic.SkeletonDataAsset : SkeletonAnimation.SkeletonDataAsset;
        public Skeleton Skeleton => ISkeletonAnimation.Skeleton;
        public AnimationState AnimationState => IAnimationStateComponent.AnimationState;

        public bool IsSkeletonGraphic { get; protected set;}

        private bool _initialized = false;
        
        protected virtual void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (_initialized)
                return;
            _initialized = false;
            
            if (!TryGetComponent<ISkeletonAnimation>(out var iSkeletonAnimation))
            {
                Debug.LogError("ISkeletonAnimation Component is not attached");
                return;
            }

            if (iSkeletonAnimation is SkeletonAnimation skeletonAnimation)
            {
                SkeletonAnimation = skeletonAnimation;
                ISkeletonAnimation = skeletonAnimation;
                IAnimationStateComponent = skeletonAnimation;
                SkeletonAnimation.Initialize(false);
            }
            else if (iSkeletonAnimation is SkeletonGraphic skeletonGraphic)
            {
                IsSkeletonGraphic = true;
                SkeletonGraphic = skeletonGraphic;
                ISkeletonAnimation = skeletonGraphic;
                IAnimationStateComponent = skeletonGraphic;
                SkeletonGraphic.Initialize(false);
            }
        }
    }
}