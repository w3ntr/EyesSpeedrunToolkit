using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EyesSpeedrunToolkit
{
    [HarmonyPatch]
    public static class BlockClickThrough_Patch
    {
        // Этот метод говорит Harmony: "Возьми первый попавшийся метод с именем Raycast"
        // Это предотвращает ошибку AmbiguousMatchException
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return AccessTools.FirstMethod(typeof(GraphicRaycaster), m => m.Name == "Raycast");
        }

        [HarmonyPrefix]
        public static bool Prefix()
        {
            // Если меню открыто, возвращаем false. Это ОТМЕНЯЕТ выполнение оригинального метода Raycast,
            // и игра "не видит" клик мыши.
            return !HUDManager.IsMenuOpen;
        }
    }
}