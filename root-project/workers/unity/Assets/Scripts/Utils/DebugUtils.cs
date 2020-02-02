using System.IO;
using UnityEngine;

namespace AdvancedGears
{
    static class DebugUtils
    {
        public static void LogFormatColor(UnityEngine.Color col, string fmt, params object[] args)
        {
            fmt = "<color=#" + ColorUtility.ToHtmlStringRGBA(col) + ">" + fmt + "</color>";
            Debug.LogFormat(fmt, args);
        }
    }
}
