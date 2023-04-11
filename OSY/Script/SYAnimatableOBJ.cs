using Cysharp.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.UI;

namespace JSchool.Modules.Common.OSY
{
    public class SYAnimatableOBJ : MonoBehaviour
    {
        private float originScale;

        public Tweener currentAnimation;

        protected virtual void Awake()
        {
            originScale = transform.localScale.x;
        }

        public Tweener SizeUpDown(float duration = 1, int count = 1, float size = 1.2f)
        {
            AnimationCancle();
            return currentAnimation = transform.DOScale(transform.localScale.x * size, duration / 2)
                .SetLoops(2 * count, LoopType.Yoyo);
        }

        public void SizeUpDown(float time)
        {
            AnimationCancle();
            currentAnimation = transform.DOScale(1.2f, time).SetLoops(2, LoopType.Yoyo);
        }

        public void AnimationHurry()
        {
            if (currentAnimation != null && !currentAnimation.IsComplete())
                currentAnimation.timeScale = 2;
        }

        public async void PlayClickAni()
        {
            AnimationCancle();
            var currentScale = transform.localScale.x;
            currentAnimation = transform.DOPunchScale(-Vector2.one * 0.2f, 0.5f, 1, 0.5f);
            await currentAnimation;
            transform.localScale = Vector3.one * currentScale;
        }

        public void AnimationCancle()
        {
            if (currentAnimation != null && !currentAnimation.IsComplete())
                currentAnimation.Complete();
        }

        public virtual TweenerCore<Color, Color, ColorOptions> DoAlpha(float val, float sec)
        {
            var image = GetComponent<Image>();
            if (image)
                return image.DOFade(val, sec);
            else
                return GetComponent<SpriteRenderer>().DOFade(val, sec);
        }

        public void PlayWrongAni() => WrongAni();

        public virtual Tweener WrongAni() => transform.DOShakePosition(0.5f, Vector3.left * 20).SetEase(Ease.Linear);
    }
}