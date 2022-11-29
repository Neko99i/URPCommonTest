using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Timeline;
public class PostProcessBehaviour : PlayableBehaviour
{
    [HideInInspector]
    public Volume volume;
    public VolumeProfile profile;
    public int layer;
    public AnimationCurve weightCurve = AnimationCurve.Linear(0, 0, 1, 1);

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        base.OnBehaviourPlay(playable, info);
    }
    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        if (volume)
        {
            GameObject.DestroyImmediate(volume.gameObject);
        }
    }

    public void ChangeWeight(float time)
    {
        if (volume == null)
            return;

        volume.weight = weightCurve.Evaluate(time);
    }
}
