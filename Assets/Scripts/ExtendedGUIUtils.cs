using System;
using UnityEngine;

namespace WinterCrestal.Utils
{
    public static class ExtendedGUIUtils
    {
        public static bool Dropdown<T>(string label, bool isShowing, ref T selectedOption, out bool hasChosen) where T : Enum
        {
            GUILayout.BeginHorizontal();

            hasChosen = false;
            if (GUILayout.Button(label))
                isShowing = !isShowing;

            if (isShowing)
            {
                var list = Enum.GetNames(typeof(T));
                GUILayout.BeginVertical();
                for (int i = 0; i < list.Length; i++)
                {
                    if (GUILayout.Button(list[i]))
                    {
                        selectedOption = (T)Enum.ToObject(typeof(T), i);
                        isShowing = false;
                        hasChosen = true;
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
            return isShowing;
        }

    }

}
