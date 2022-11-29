using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

//PostProcessClip 代表的是后处理片段资源本身，用来定义 Clip 支持哪些功能。
//比如我们脚本中定义的混合 Blend；这里的 Blending 代表如果我们将两段 
//Clip 拖到同时间段的话，两段 Clip 会进行融合。
public class PostProcessClip : PlayableAsset, ITimelineClipAsset
{

    public PostProcessBehaviour template = new PostProcessBehaviour();
    public ClipCaps clipCaps
    {
        get
        {
            return ClipCaps.Extrapolation | ClipCaps.Blending;
        }
    }

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<PostProcessBehaviour>.Create(graph, template);
        PostProcessBehaviour clone = playable.GetBehaviour();
        return playable;
    }
}
