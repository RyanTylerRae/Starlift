using UnityEngine;
using UnityEngine.Rendering;

namespace Haze.Runtime
{
    public class HazeBufferHistoryItem : CameraHistoryItem
    {
        private int _id;
        private RenderTextureDescriptor _descriptor;
        private Hash128 _descKey;

        public override void OnCreate(BufferedRTHandleSystem owner, uint typeId)
        {
            base.OnCreate(owner, typeId);
            _id = MakeId(0);
        }

        public override void Reset()
        {
            ReleaseHistoryFrameRT(_id);
        }

        public RTHandle CurrentTexture => GetCurrentFrameRT(_id);
        public RTHandle PreviousTexture => GetPreviousFrameRT(_id);

        public void Update(RenderTextureDescriptor textureDescriptor)
        {
            if (_descKey != Hash128.Compute(ref textureDescriptor))
            {
                ReleaseHistoryFrameRT(_id);
            }

            if (CurrentTexture != null)
            {
                return;
            }

            AllocHistoryFrameRT(_id, 2, ref textureDescriptor, "HazeBufferHistory");

            _descriptor = textureDescriptor;
            _descKey = Hash128.Compute(ref textureDescriptor);
        }
    }
}
