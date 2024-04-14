using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Extensions.UniTaskExternal
{
    public static class AnimatorAsyncExtensions
    {
        public static UniTask WaitAnimationComplete(this Animator animator, int layer = 0)
        {
            return WaitAnimationCore(animator, cancellationToken: animator.GetCancellationTokenOnDestroy(),
                layer: layer);
        }

        public static UniTask WaitAnimationComplete(this Animator animator, int animationName, float progress = 1f,
            int layer = 0)
        {
            return WaitAnimationCore(animator, animationName, progress, layer,
                animator.GetCancellationTokenOnDestroy());
        }

        public static UniTask WaitAnimationComplete(this Animator animator, string animationName, float progress = 1f,
            int layer = 0)
        {
            return WaitAnimationCore(animator, Animator.StringToHash(animationName), progress, layer,
                animator.GetCancellationTokenOnDestroy());
        }

        public static UniTask WaitAnimationComplete(this Animator animator,
            CancellationToken cancellationToken, int layer = 0)
        {
            return WaitAnimationCore(animator, cancellationToken: cancellationToken, layer: layer);
        }

        public static UniTask WaitAnimationComplete(this Animator animator, int animationName,
            CancellationToken cancellationToken, float progress = 1f,
            int layer = 0)
        {
            return WaitAnimationCore(animator, animationName, progress, layer, cancellationToken);
        }

        public static UniTask WaitAnimationComplete(this Animator animator, string animationName,
            CancellationToken cancellationToken,
            int layer = 0, float progress = 1f)
        {
            return WaitAnimationCore(animator, Animator.StringToHash(animationName), progress, layer,
                cancellationToken);
        }

        private static async UniTask WaitAnimationCore(this Animator animator,
            int animationName = -1,
            float progress = 1f, int layer = 0,
            CancellationToken cancellationToken = default)
        {
            if (animator == null)
                throw new ArgumentNullException(nameof(animator));

            if (!animator.enabled)
                return;

            await UniTask.WaitUntil(() =>
            {
                var state = animator.GetCurrentAnimatorStateInfo(layer);

                return (state.shortNameHash == animationName || animationName == -1) &&
                       state.normalizedTime >= progress;
            }, cancellationToken: cancellationToken);
        }
    }
}
