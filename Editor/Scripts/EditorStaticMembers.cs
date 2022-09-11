using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class EditorStaticMembers
{
    private static readonly string texturesPath = "Textures/";

    private static readonly string transparentCheckerName = "transparent_checker";
    public static Texture2D transparentChecker;

    static EditorStaticMembers()
    {
        transparentChecker = Resources.Load<Texture2D>(texturesPath + transparentCheckerName);
    }

    public static string CreateUniqueChannelName(SerializedProperty channelsList, string inputName)
    {
        string uniqueName = inputName;
        int number = 2;
        while (true)
        {
            bool match = false;
            for(int i = 0; i < channelsList.arraySize; i++)
            {
                if(channelsList.GetArrayElementAtIndex(i).FindPropertyRelative("_name").stringValue == uniqueName)
                {
                    match = true;
                    uniqueName = inputName + " (" + number + ")";
                    break;
                }
            }
            if (!match)
                return uniqueName;
            number++;
        }
    }
    
    /// <summary>
    /// Creates a unique name but skips one index because that is the one being checked for
    /// </summary>
    /// <param name="channelsList"></param>
    /// <param name="skipIndex"></param>
    /// <param name="inputName"></param>
    /// <returns></returns>
    public static string CreateUniqueChannelName(SerializedProperty channelsList, int skipIndex, string inputName)
    {
        string uniqueName = inputName;
        int number = 2;
        while (true)
        {
            bool match = false;
            for (int i = 0; i < channelsList.arraySize; i++)
            {
                if (i == skipIndex) continue;
                if (channelsList.GetArrayElementAtIndex(i).FindPropertyRelative("_name").stringValue == uniqueName)
                {
                    match = true;
                    uniqueName = inputName + " (" + number + ")";
                    break;
                }
            }
            if (!match) return uniqueName;
            number++;
        }
    }
}
