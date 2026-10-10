using Il2Cpp;
using Il2CppBehaviorDesigner.Runtime.Tasks.Unity.UnityGameObject;
using Il2CppBishopGames;
using Il2CppBishopGames.UI;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppPixelCrushers;
using Il2CppTape2Tape.Customization.UI;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TapeToTapeMods.Patches;

[HarmonyLib.HarmonyPatch(typeof(SettingsMenu), nameof(SettingsMenu.BehaviourAwake))]
public class TestNewSettingsTab
{
    static void Postfix(ref SettingsMenu __instance)
    {
        Transform tabContainer = __instance.transform.FindChild("ScaledContainer/Canvas/MainContent/Tabs");
        Transform audioTab = __instance.gameObject.transform.FindChild("ScaledContainer/Canvas/MainContent/Tabs/Toggle_Audio");

        if (audioTab != null)
        {
            GameObject newTab = Object.Instantiate(audioTab.gameObject, tabContainer);
            newTab.transform.SetSiblingIndex(7);

            UnselectableToggle newTabToggle = newTab.GetComponent<UnselectableToggle>();
            
            var animatedBtn = newTab.GetComponent<UnselectableAnimatedButton>();
            animatedBtn.OnClick.RemoveAllListeners();
            
            animatedBtn.OnClick.AddListener<UnselectableButtonBase>(new Action<UnselectableButtonBase>((t) =>
            {
                Melon<Core>.Logger.Msg("Clicked new Tab");
            }));
            
            newTab.name = "Toggle_Audio_New";
            
            var tabBtnTMP = newTab.GetComponentInChildren<TextMeshProUGUI>();
            tabBtnTMP.SetText("New Tab");
        }
    }
}