using UnityEngine;
using UnityEditor;
using Froggi.AudioKit;

namespace Froggi.AudioKit.Editors
{
    public static class AudioKitHeader
    {
        private static readonly Color headerColor = new Color(0.2f, 0.8f, 0.4f);
        private static readonly Color textColor = Color.white;
        private static Texture2D headerTexture;

        static AudioKitHeader()
        {
            headerTexture = new Texture2D(1, 1);
            headerTexture.SetPixel(0, 0, headerColor);
            headerTexture.Apply();
        }

        public static void DrawHeader()
        {
            Rect headerRect = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(headerRect, headerTexture);

            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 18;
            headerStyle.normal.textColor = textColor;
            headerStyle.alignment = TextAnchor.MiddleCenter;

            GUI.Label(headerRect, "AudioKit by Froggi.Dev", headerStyle);

            EditorGUILayout.Space(10);
        }

        public static void DrawHeader(string subtitle)
        {
            DrawHeader();

            GUIStyle subtitleStyle = new GUIStyle(EditorStyles.label);
            subtitleStyle.fontSize = 12;
            subtitleStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            subtitleStyle.alignment = TextAnchor.MiddleCenter;

            Rect subtitleRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
            GUI.Label(subtitleRect, subtitle, subtitleStyle);

            EditorGUILayout.Space(5);
        }
    }
}