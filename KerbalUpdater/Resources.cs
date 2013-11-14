using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KerbalUpdater
{
    static class Resources
    {
        public static GUIStyle TABLE_HEAD_STYLE { get; private set; }
        public static GUIStyle TABLE_ROW_STYLE { get; private set; }
        public static GUIStyle TABLE_BODY_STYLE { get; private set; }
        public static GUIStyle ACTION_BUTTON_STYLE { get; private set; }
        public static GUIStyle URL_STYLE { get; private set; }
        public static GUIStyle TOGGLE_STYLE { get; private set; }
        private static bool Initialized = false;
        public static void Initialize()
        {
            if (Initialized)
            {
                return;
            }

            GUI.skin = HighLogic.Skin;

            TABLE_HEAD_STYLE = new GUIStyle();
            TABLE_HEAD_STYLE.fontStyle = FontStyle.Bold;
            TABLE_HEAD_STYLE.normal.textColor = Color.white;

            TABLE_BODY_STYLE = new GUIStyle(GUI.skin.box);
            TABLE_BODY_STYLE.padding = new RectOffset(0, 0, 0, 0);

            TABLE_ROW_STYLE = new GUIStyle(GUI.skin.box);
            TABLE_ROW_STYLE.padding = new RectOffset(0, 12, 10, 4);

            ACTION_BUTTON_STYLE = new GUIStyle(GUI.skin.button);
            ACTION_BUTTON_STYLE.fixedHeight = 28;

            URL_STYLE = new GUIStyle(GUI.skin.label);
            URL_STYLE.normal.textColor = Color.yellow;

            TOGGLE_STYLE = new GUIStyle(GUI.skin.toggle);
            TOGGLE_STYLE.padding = new RectOffset(0, 0, 0, 0);
            TOGGLE_STYLE.normal.textColor = GUI.skin.label.normal.textColor;
        }
    }
}
