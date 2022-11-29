using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class PostProcessMixerBehaviour : PlayableBehaviour
{
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        int inputCount = playable.GetInputCount();

        for (int i = 0; i < inputCount; i++)
        {
            var playableInput = (ScriptPlayable<PostProcessBehaviour>)playable.GetInput(i);
            PostProcessBehaviour input = playableInput.GetBehaviour();

            float inputWeight = playable.GetInputWeight(i);

            if (Mathf.Approximately(inputWeight,0))
            {
                continue;
            }
            float normalizeTime = (float)(playableInput.GetTime() / playableInput.GetDuration());
            input.ChangeWeight(normalizeTime);
        }
    }
}
