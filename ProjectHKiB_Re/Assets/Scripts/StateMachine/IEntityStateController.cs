using UnityEngine;

public interface IEntityStateController
{
    public AnimationController AnimationController { get; set; }

    public void PlayStateAnimation(string animationName)
    {
        if (AnimationController)
            AnimationController.Play(animationName);
        else
            Debug.LogWarning("Warning: animationController missing!!!");
    }
}