using Coherence.Editor;
using UnityEditor;

namespace Dissonance.Integrations.Coherence.Editor
{
    [CustomEditor(typeof(CoherenceSyncVoice))]
    internal class CoherenceSyncVoiceEditor : BaseEditor
    {
        protected override void OnGUI()
        {
            DrawPropertiesExcluding(serializedObject, "m_Script");
        }
    }
}

