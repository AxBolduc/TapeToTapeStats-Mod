using System.Collections;
using System.Runtime.InteropServices;
using HarmonyLib;
using Il2Cpp;
using Il2CppBishopGames.UI;
using Il2CppI2.Loc;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppTape2Tape.Customization.UI;
using Il2CppTMPro;
using MelonLoader;
using TapeToTapeStatsMod.Services;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TapeToTapeStatsMod.Patches;

/// <summary>
/// Feasibility spike: injects Tape to Tape Stats settings into the native settings menu as a dedicated "Stats+"
/// tab, and logs menu internals under [SettingsSpike].
/// </summary>
[HarmonyPatch(typeof(SettingsMenu), nameof(SettingsMenu.BehaviourAwake))]
public static class SettingsMenuSpikePatch
{
    private const string LogPrefix = "[SettingsSpike]";
    private const string SettingsContainerPath = "ScaledContainer/Canvas/MainContent/Options/SettingsContainer";
    private const string TabsPath = "ScaledContainer/Canvas/Tabs";

    private static readonly (string Label, string Url)[] ServerPresets =
    {
        ("Production", "https://api.tapetotape.axbolduc.com"),
        ("Preview", "https://api.test.tapetotape.axbolduc.com"),
        ("Local", "http://localhost:8787"),
    };

    private static MelonLogger.Instance Log => Melon<Core>.Logger;

    private static void Postfix(SettingsMenu __instance)
    {
        try
        {
            Inject(__instance);
        }
        catch (Exception e)
        {
            Log.Error($"{LogPrefix} Injection failed: {e}");
        }
    }

    private static void Inject(SettingsMenu menu)
    {
        LogMenuState(menu, "before injection");

        var settingsContainer = menu.transform.Find(SettingsContainerPath);
        var tabs = menu.transform.Find(TabsPath);

        if (settingsContainer == null || tabs == null)
        {
            Log.Error($"{LogPrefix} Missing SettingsContainer ({settingsContainer != null}) or Tabs ({tabs != null}).");
            return;
        }

        var buttonRowTemplate = settingsContainer.Find("SystemMenu/Container/ButtonWithTitle_Credits");
        var cycleRowTemplate = settingsContainer.Find("GraphicsMenu/Container/Quality");

        if (buttonRowTemplate == null || cycleRowTemplate == null)
        {
            Log.Error($"{LogPrefix} Missing button row ({buttonRowTemplate != null}) or cycle row ({cycleRowTemplate != null}) template.");
            return;
        }

        AddStatsTab(menu, tabs, settingsContainer, buttonRowTemplate.gameObject, cycleRowTemplate.gameObject);

        LogMenuState(menu, "after injection");
    }

    private static void AddStatsTab(
        SettingsMenu menu,
        Transform tabs,
        Transform settingsContainer,
        GameObject buttonRowTemplate,
        GameObject cycleRowTemplate)
    {
        var tabTemplate = tabs.Find("Toggle_Audio");
        var lastTab = tabs.Find("Toggle_Stats");
        var pageTemplate = settingsContainer.Find("AudioMenu");

        if (tabTemplate == null || lastTab == null || pageTemplate == null)
        {
            Log.Error($"{LogPrefix} Missing tab template ({tabTemplate != null}), last tab ({lastTab != null}) or page template ({pageTemplate != null}).");
            return;
        }

        var tab = Clone(tabTemplate.gameObject, tabs, "Toggle_TapeToTapeStats");
        tab.transform.SetSiblingIndex(lastTab.GetSiblingIndex() + 1);
        SetText(tab.transform, "Container/Background/Text (TMP)", "Stats+");

        // Clone the page while inactive so AudioSettingsUI never wakes up on the copy.
        var pageWasActive = pageTemplate.gameObject.activeSelf;
        Log.Msg($"{LogPrefix} AudioMenu activeSelf at BehaviourAwake: {pageWasActive}");
        pageTemplate.gameObject.SetActive(false);
        var page = Object.Instantiate(pageTemplate.gameObject, settingsContainer);
        pageTemplate.gameObject.SetActive(pageWasActive);
        page.name = "TapeToTapeStatsMenu";

        Object.DestroyImmediate(page.GetComponent<AudioSettingsUI>());
        StripLocalization(page);

        var container = page.transform.Find("Container");

        if (container == null)
        {
            Log.Error($"{LogPrefix} Cloned page has no Container.");
            return;
        }

        for (var i = container.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(container.GetChild(i).gameObject);
        }

        AddStatsRows(container, buttonRowTemplate, cycleRowTemplate, "Stats+ tab");

        var subMenus = menu.m_SubMenusInOrder;
        var extended = new Il2CppReferenceArray<SettingsMenu.SubMenu>(subMenus.Length + 1);

        for (var i = 0; i < subMenus.Length; i++)
        {
            extended[i] = subMenus[i];
        }

        var toggle = tab.GetComponent<UnselectableToggle>();
        var subMenu = new SettingsMenu.SubMenu
        {
            Button = toggle,
            Panel = page,
        };
        extended[subMenus.Length] = subMenu;
        menu.m_SubMenusInOrder = extended;

        menu.CacheSubMenuComponents();
        AddToTabGroup(menu.m_TabTogglesParent, toggle);
    }

    // The tab group uses a serialized m_ForceToggles list instead of discovering its children.
    private static void AddToTabGroup(UnselectableToggleGroup group, UnselectableToggle toggle)
    {
        var forced = group.m_ForceToggles;
        var forcedCount = forced?.Length ?? 0;
        var extended = new Il2CppReferenceArray<UnselectableToggle>(forcedCount + 1);

        for (var i = 0; forced != null && i < forcedCount; i++)
        {
            extended[i] = forced[i];
        }

        extended[forcedCount] = toggle;
        group.m_ForceToggles = extended;

        // Before its own Awake the group has no toggles yet and will read the extended list itself.
        var initialized = group.m_Toggles != null && group.m_Toggles.Count > 0;
        Log.Msg($"{LogPrefix} Tab group: {forcedCount} forced toggles, initialized={initialized}");

        if (initialized)
        {
            group.RefreshChildren();
        }
    }

    private static void AddStatsRows(Transform container, GameObject buttonRowTemplate, GameObject cycleRowTemplate, string where)
    {
        var keyRow = Clone(buttonRowTemplate, container, "ButtonWithTitle_StatsApiKey");
        SetText(keyRow.transform, "Title", "Stats API key");
        SetText(keyRow.transform, "Button/Text (TMP)", MaskedApiKey());

        var serverRow = Clone(cycleRowTemplate, container, "StatsServer");
        SetText(serverRow.transform, "Title", "Stats server");

        var replayRow = Clone(cycleRowTemplate, container, "SkipGoalReplays");
        SetText(replayRow.transform, "Title", "Skip goal replays");

        LinkNeighbors(container, keyRow, serverRow, replayRow, where);

        MelonCoroutines.Start(WireRowsWhenActive(container, keyRow, serverRow, replayRow, where));
    }

    /// <summary>
    /// Controller navigation follows hand-authored m_Neighbors links. Clones keep their template's links (to the
    /// template's siblings, possibly on another page), so chain our rows below the container's last row instead.
    /// Down from the last row is left empty so the game's wrap-around still reaches the top.
    /// </summary>
    private static void LinkNeighbors(Transform container, GameObject keyRow, GameObject serverRow, GameObject replayRow, string where)
    {
        var keyButton = keyRow.transform.Find("Button").GetComponent<AbstractNavigable>();
        var chain = new[]
        {
            keyButton,
            serverRow.GetComponent<AbstractNavigable>(),
            replayRow.GetComponent<AbstractNavigable>(),
        };
        var previous = FindLastNavigableBefore(container, keyRow.transform.GetSiblingIndex());

        foreach (var navigable in chain)
        {
            navigable.SetNeighbor(AbstractNavigable.EDirection.Up, null);
            navigable.SetNeighbor(AbstractNavigable.EDirection.Right, null);
            navigable.SetNeighbor(AbstractNavigable.EDirection.Down, null);
            navigable.SetNeighbor(AbstractNavigable.EDirection.Left, null);
        }

        if (previous != null)
        {
            previous.SetNeighbor(AbstractNavigable.EDirection.Down, keyButton);
            keyButton.SetNeighbor(AbstractNavigable.EDirection.Up, previous);
        }
        else
        {
            // Empty page: the cloned default pointed at a row we destroyed.
            var parent = container.GetComponent<NavigableParent>();

            if (parent != null)
            {
                parent.m_DefaultNavigable = keyButton;
            }
        }

        for (var i = 1; i < chain.Length; i++)
        {
            chain[i - 1].SetNeighbor(AbstractNavigable.EDirection.Down, chain[i]);
            chain[i].SetNeighbor(AbstractNavigable.EDirection.Up, chain[i - 1]);
        }

        Log.Msg($"{LogPrefix} {where}: linked below {(previous != null ? previous.transform.parent.name + "/" + previous.name : "nothing (set as default)")}");
    }

    private static AbstractNavigable? FindLastNavigableBefore(Transform container, int siblingIndex)
    {
        for (var i = siblingIndex - 1; i >= 0; i--)
        {
            var row = container.GetChild(i);

            if (!row.gameObject.activeSelf)
            {
                continue;
            }

            var navigable = row.GetComponentInChildren<AbstractNavigable>();

            if (navigable != null)
            {
                return navigable;
            }
        }

        return null;
    }

    /// <summary>
    /// Game components finish setting up in Awake/Start, which only run once the page is shown, so wire
    /// events after the rows have been active for a frame.
    /// </summary>
    private static IEnumerator WireRowsWhenActive(
        Transform container,
        GameObject keyRow,
        GameObject serverRow,
        GameObject replayRow,
        string where)
    {
        while (keyRow != null && !keyRow.activeInHierarchy)
        {
            yield return null;
        }

        yield return null;

        if (keyRow == null || serverRow == null || replayRow == null)
        {
            yield break;
        }

        try
        {
            var keyButton = keyRow.transform.Find("Button").GetComponent<AnimatedButton>();
            var keyLabel = keyRow.transform.Find("Button/Text (TMP)").GetComponent<TMP_Text>();
            WireApiKeyButton(keyButton, keyLabel);

            var serverCycleList = serverRow.GetComponent<CycleList>();
            WireServerCycleList(serverCycleList);

            var replayCycleList = replayRow.GetComponent<CycleList>();
            WireSkipGoalReplaysCycleList(replayCycleList);

            RegisterNavigables(container, where, keyButton, serverCycleList, replayCycleList);
            Log.Msg($"{LogPrefix} Wired rows on {where}.");
        }
        catch (Exception e)
        {
            Log.Error($"{LogPrefix} Wiring rows on {where} failed: {e}");
        }
    }

    private static void WireApiKeyButton(AnimatedButton button, TMP_Text label)
    {
        button.OnClick.AddListener<AbstractNavigable>(new Action<AbstractNavigable>(_ =>
        {
            var clipboard = ReadClipboard().Trim();

            if (clipboard.Length == 0)
            {
                Log.Warning($"{LogPrefix} Clipboard is empty; API key unchanged.");
                return;
            }

            PreferenceService.SetApiKey(clipboard);
            RunReporter.Initialize();
            label.text = MaskedApiKey();
        }));
    }

    private static void WireServerCycleList(CycleList cycleList)
    {
        var urls = ServerPresets.Select(preset => preset.Url).ToList();
        var labels = new Il2CppSystem.Collections.Generic.List<string>();

        foreach (var preset in ServerPresets)
        {
            labels.Add(preset.Label);
        }

        var current = PreferenceService.ApiBaseUrl;
        var index = urls.IndexOf(current);

        if (index < 0)
        {
            urls.Add(current);
            labels.Add($"Custom ({current})");
            index = urls.Count - 1;
        }

        cycleList.Init(labels.Cast<Il2CppSystem.Collections.IList>());
        cycleList.SelectIndex(index, false);

        cycleList.OnSelectionChanged.AddListener<Il2CppSystem.Object>(new Action<Il2CppSystem.Object>(_ =>
        {
            var selected = cycleList.CurrentIndex;

            if (selected < 0 || selected >= urls.Count)
            {
                return;
            }

            PreferenceService.SetApiBaseUrl(urls[selected]);
            RunReporter.Initialize();
        }));
    }

    private static void WireSkipGoalReplaysCycleList(CycleList cycleList)
    {
        var labels = new Il2CppSystem.Collections.Generic.List<string>();
        labels.Add("Off");
        labels.Add("On");

        cycleList.Init(labels.Cast<Il2CppSystem.Collections.IList>());
        cycleList.SelectIndex(PreferenceService.SkipGoalReplays ? 1 : 0, false);

        cycleList.OnSelectionChanged.AddListener<Il2CppSystem.Object>(new Action<Il2CppSystem.Object>(_ =>
        {
            PreferenceService.SetSkipGoalReplays(cycleList.CurrentIndex == 1);
        }));
    }

    private static void RegisterNavigables(Transform container, string where, params AbstractNavigable[] navigables)
    {
        var parent = container.GetComponent<NavigableParent>();

        if (parent == null)
        {
            Log.Warning($"{LogPrefix} {where}: container has no NavigableParent.");
            return;
        }

        var registered = navigables.Select(parent.HasChild).ToArray();
        Log.Msg($"{LogPrefix} {where}: {parent.m_Navigables.Count} navigables, ours already registered: [{string.Join(", ", registered)}], registerOnAwake={parent.m_RegisterChildrenOnAwake}");

        foreach (var navigable in navigables)
        {
            if (!parent.HasChild(navigable))
            {
                parent.RegisterChild(navigable);
            }
        }
    }

    private static void LogMenuState(SettingsMenu menu, string when)
    {
        var subMenus = menu.m_SubMenusInOrder;
        var toggles = menu.m_TabTogglesParent.m_Toggles;
        Log.Msg($"{LogPrefix} {when}: {subMenus?.Length ?? -1} sub menus, {toggles?.Count ?? -1} group toggles, active index {menu.m_ActivePanelIndex}");

        if (subMenus == null)
        {
            return;
        }

        for (var i = 0; i < subMenus.Length; i++)
        {
            var subMenu = subMenus[i];
            var button = subMenu.Button != null ? subMenu.Button.name : "null";
            var panel = subMenu.Panel != null ? subMenu.Panel.name : "null";
            Log.Msg($"{LogPrefix}   [{i}] {button} -> {panel} (cachedAnim={subMenu.CachedAnim != null}, cachedNav={subMenu.CachedNav != null})");
        }
    }

    private static GameObject Clone(GameObject template, Transform parent, string name)
    {
        var clone = Object.Instantiate(template, parent);
        clone.name = name;
        StripLocalization(clone);
        return clone;
    }

    // I2 Localize would overwrite our labels on enable and on language change.
    private static void StripLocalization(GameObject root)
    {
        foreach (var localize in root.GetComponentsInChildren<Localize>(true))
        {
            Object.DestroyImmediate(localize);
        }
    }

    private static void SetText(Transform root, string path, string text)
    {
        var target = root.Find(path);

        if (target == null)
        {
            Log.Warning($"{LogPrefix} No '{path}' under {root.name}.");
            return;
        }

        target.GetComponent<TMP_Text>().text = text;
    }

    private static string MaskedApiKey()
    {
        var apiKey = PreferenceService.ApiKey;
        return apiKey.Length == 0 ? "Paste key" : $"****{apiKey[^4..]}";
    }

    private static string ReadClipboard()
    {
        try
        {
            var text = GUIUtility.systemCopyBuffer ?? "";
            Log.Msg($"{LogPrefix} Read {text.Length} chars from GUIUtility.systemCopyBuffer.");

            if (text.Length > 0)
            {
                return text;
            }
        }
        catch (Exception e)
        {
            Log.Warning($"{LogPrefix} GUIUtility.systemCopyBuffer failed: {e.Message}");
        }

        var fallback = WindowsClipboard.ReadText();
        Log.Msg($"{LogPrefix} Read {fallback.Length} chars from the Win32 clipboard.");
        return fallback;
    }

    private static class WindowsClipboard
    {
        private const uint UnicodeTextFormat = 13;

        public static string ReadText()
        {
            if (!OpenClipboard(IntPtr.Zero))
            {
                return "";
            }

            try
            {
                var handle = GetClipboardData(UnicodeTextFormat);

                if (handle == IntPtr.Zero)
                {
                    return "";
                }

                var pointer = GlobalLock(handle);

                if (pointer == IntPtr.Zero)
                {
                    return "";
                }

                try
                {
                    return Marshal.PtrToStringUni(pointer) ?? "";
                }
                finally
                {
                    GlobalUnlock(handle);
                }
            }
            finally
            {
                CloseClipboard();
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr owner);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetClipboardData(uint format);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr memory);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalUnlock(IntPtr memory);
    }
}
