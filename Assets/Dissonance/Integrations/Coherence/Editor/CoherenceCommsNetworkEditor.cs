using Coherence.Connection;
using UnityEngine;
using UnityEditor;
using Dissonance.Editor;

namespace Dissonance.Integrations.Coherence.Editor
{
    [CustomEditor(typeof(CoherenceCommsNetwork))]
    internal class CoherenceCommsNetworkEditor
        : BaseDissonnanceCommsNetworkEditor<
            CoherenceCommsNetwork,
            CoherenceServer,
            CoherenceClient,
            ClientID,
            Unit,
            Unit
        >
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
        }
    }
}
