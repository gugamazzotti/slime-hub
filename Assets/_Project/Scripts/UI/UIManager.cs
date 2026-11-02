using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SlimeHub.Core;
using SlimeHub.Data;
using SlimeHub.Gameplay;

namespace SlimeHub.UI
{
    public sealed class UIManager : MonoBehaviour
    {
        public static bool ModalOpen { get; private set; }

        Text biomassText;
        Text muteLabel;
        Text hapticLabel;
        Image muteIcon;
        Image hapticIcon;
        Text hint;
        RectTransform safe;
        GameObject modalRoot;
        RectTransform modal;
        CanvasGroup modalGroup;
        Text modalTitle;
        RectTransform modalBody;
        int width;
        int height;
        InputField friendField;
        string page;

        SlimeCustomizer customizer;
        IDataService save;
        MissionService missions;
        IFusionService fusion;
        ILeaderboardService board;

        void OnEnable()
        {
            GameEvents.BiomassChanged += OnBiomass;
            GameEvents.MissionsChanged += OnMissions;
        }

        void OnDisable()
        {
            GameEvents.BiomassChanged -= OnBiomass;
            GameEvents.MissionsChanged -= OnMissions;
            ModalOpen = false;
        }

        public void Build()
        {
            customizer = ServiceLocator.Get<SlimeCustomizer>();
            save = ServiceLocator.Get<IDataService>();
            missions = ServiceLocator.Get<MissionService>();
            fusion = ServiceLocator.Get<IFusionService>();
            board = ServiceLocator.Get<ILeaderboardService>();

            Canvas canvas = UiFactory.CreateCanvas("HubUI", 20);
            canvas.transform.SetParent(transform, false);
            safe = UiFactory.SafeArea(canvas.transform);
            width = Screen.width;
            height = Screen.height;

            Image topWash = UiFactory.Block(safe, new Color(0.04f, 0.1f, 0.2f, 0.08f));
            topWash.raycastTarget = false;
            Pin(topWash.rectTransform, true, 230f);
            Image bottomWash = UiFactory.Block(safe, new Color(0.03f, 0.08f, 0.16f, 0.2f));
            bottomWash.raycastTarget = false;
            Pin(bottomWash.rectTransform, false, 470f);

            Image badge = UiFactory.Picture(safe, "badge_biomass", false);
            var badgeRect = badge.rectTransform;
            badgeRect.anchorMin = new Vector2(0f, 1f);
            badgeRect.anchorMax = new Vector2(0f, 1f);
            badgeRect.pivot = new Vector2(0f, 1f);
            badgeRect.sizeDelta = new Vector2(520f, 215f);
            badgeRect.anchoredPosition = new Vector2(8f, -6f);
            biomassText = UiFactory.Label(badge.transform, save.Data.bioMassCurrency.ToString(), 42, TextAnchor.MiddleCenter, Color.white);
            var bioRect = biomassText.rectTransform;
            bioRect.anchorMin = new Vector2(0.49f, 0.42f);
            bioRect.anchorMax = new Vector2(0.73f, 0.68f);
            bioRect.offsetMin = Vector2.zero;
            bioRect.offsetMax = Vector2.zero;
            biomassText.resizeTextForBestFit = true;
            biomassText.resizeTextMinSize = 22;
            biomassText.resizeTextMaxSize = 46;
            biomassText.verticalOverflow = VerticalWrapMode.Truncate;
            UiFactory.Outline(biomassText);

            CornerToggle("btn_pause", save.Data.haptics ? Loc.Get("hub.haptic.on") : Loc.Get("hub.haptic.off"), ToggleHaptics, 210f, out hapticIcon, out hapticLabel);
            CornerToggle("btn_sound", save.Data.muted ? Loc.Get("hub.sound.off") : Loc.Get("hub.sound.on"), ToggleMute, 12f, out muteIcon, out muteLabel);
            PaintToggle(hapticIcon, save.Data.haptics);
            PaintToggle(muteIcon, !save.Data.muted);

            Image plaque = UiFactory.Picture(safe, "plate", true);
            var plaqueRect = plaque.rectTransform;
            plaqueRect.anchorMin = new Vector2(0.5f, 0f);
            plaqueRect.anchorMax = new Vector2(0.5f, 0f);
            plaqueRect.pivot = new Vector2(0.5f, 0f);
            plaqueRect.sizeDelta = new Vector2(980f, 122f);
            plaqueRect.anchoredPosition = new Vector2(0f, 284f);
            hint = UiFactory.Label(plaque.transform, Loc.Get("hub.hint"), 30, TextAnchor.MiddleCenter, new Color(0.08f, 0.15f, 0.26f, 1f));
            UiFactory.Stretch(hint.rectTransform);
            hint.rectTransform.offsetMin = new Vector2(48f, 16f);
            hint.rectTransform.offsetMax = new Vector2(-48f, -16f);
            hint.resizeTextForBestFit = true;
            hint.resizeTextMinSize = 18;
            hint.resizeTextMaxSize = 32;
            hint.verticalOverflow = VerticalWrapMode.Truncate;

            var dock = UiFactory.Rect("Dock", safe);
            dock.anchorMin = new Vector2(0f, 0f);
            dock.anchorMax = new Vector2(1f, 0f);
            dock.pivot = new Vector2(0.5f, 0f);
            dock.sizeDelta = new Vector2(-24f, 252f);
            dock.anchoredPosition = new Vector2(0f, 16f);
            var dockLayout = dock.gameObject.AddComponent<HorizontalLayoutGroup>();
            dockLayout.spacing = 14f;
            dockLayout.padding = new RectOffset(4, 4, 0, 0);
            dockLayout.childControlWidth = true;
            dockLayout.childControlHeight = true;
            dockLayout.childForceExpandWidth = true;
            dockLayout.childForceExpandHeight = true;
            UiFactory.ArtButton(dock, "btn_customize", Loc.Get("hub.customize"), () => Open("hub.customize", BuildShop));
            UiFactory.ArtButton(dock, "btn_play", Loc.Get("hub.play"), () => Open("hub.games", BuildGames));
            UiFactory.ArtButton(dock, "btn_missions", Loc.Get("hub.missions"), () => Open("hub.missions", BuildMissions));
            UiFactory.ArtButton(dock, "btn_fusion", Loc.Get("hub.fusion"), () => Open("hub.fusion", BuildFusion));

            var blocker = UiFactory.Block(safe, new Color(0.03f, 0.06f, 0.12f, 0.62f));
            UiFactory.Stretch(blocker.rectTransform);
            var blockerButton = blocker.gameObject.AddComponent<Button>();
            blockerButton.targetGraphic = blocker;
            blockerButton.transition = Selectable.Transition.None;
            blockerButton.onClick.AddListener(Close);
            modalRoot = blocker.gameObject;
            modalRoot.SetActive(false);

            var window = UiFactory.Picture(blocker.transform, "panel_monitor", true);
            window.raycastTarget = true;
            modal = window.rectTransform;
            modal.anchorMin = new Vector2(0.04f, 0.05f);
            modal.anchorMax = new Vector2(0.96f, 0.95f);
            modal.offsetMin = Vector2.zero;
            modal.offsetMax = Vector2.zero;
            modalGroup = modal.gameObject.AddComponent<CanvasGroup>();
            var windowLayout = modal.gameObject.AddComponent<VerticalLayoutGroup>();
            windowLayout.padding = new RectOffset(96, 96, 108, 84);
            windowLayout.spacing = 14f;
            windowLayout.childControlWidth = true;
            windowLayout.childControlHeight = true;
            windowLayout.childForceExpandWidth = true;
            windowLayout.childForceExpandHeight = false;

            modalTitle = UiFactory.Label(modal, "", 50, TextAnchor.MiddleCenter, Color.white);
            var titleElement = modalTitle.gameObject.AddComponent<LayoutElement>();
            titleElement.minHeight = 78f;
            titleElement.preferredHeight = 78f;
            UiFactory.Outline(modalTitle);

            UiFactory.Scroll(modal, out modalBody);

            Button close = UiFactory.Icon(modal, "btn_close", Close);
            var closeRect = close.GetComponent<RectTransform>();
            var ignore = close.gameObject.AddComponent<LayoutElement>();
            ignore.ignoreLayout = true;
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.sizeDelta = new Vector2(124f, 124f);
            closeRect.anchoredPosition = new Vector2(8f, 16f);

            var guard = modal.gameObject.AddComponent<Button>();
            guard.targetGraphic = window;
            guard.transition = Selectable.Transition.None;
        }

        static void Pin(RectTransform rect, bool top, float height)
        {
            rect.anchorMin = new Vector2(0f, top ? 1f : 0f);
            rect.anchorMax = new Vector2(1f, top ? 1f : 0f);
            rect.pivot = new Vector2(0.5f, top ? 1f : 0f);
            rect.sizeDelta = new Vector2(0f, height);
            rect.anchoredPosition = Vector2.zero;
        }

        void CornerToggle(string spriteName, string caption, System.Action action, float right, out Image icon, out Text label)
        {
            Button button = UiFactory.Icon(safe, spriteName, action);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(186f, 122f);
            rect.anchoredPosition = new Vector2(-right, -12f);
            icon = button.targetGraphic as Image;
            label = UiFactory.Label(button.transform, caption, 24, TextAnchor.MiddleCenter, Color.white);
            var labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = new Vector2(0f, -2f);
            labelRect.sizeDelta = new Vector2(48f, 48f);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 14;
            label.resizeTextMaxSize = 26;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            UiFactory.Outline(label);
        }

        static void PaintToggle(Image image, bool on)
        {
            if (image != null)
                image.color = on ? Color.white : new Color(0.55f, 0.6f, 0.64f, 1f);
        }

        void Update()
        {
            if (safe != null && (Screen.width != width || Screen.height != height))
            {
                width = Screen.width;
                height = Screen.height;
                UiFactory.ApplySafe(safe);
            }
        }

        void OnBiomass(int value)
        {
            if (biomassText != null)
                biomassText.text = value.ToString();
        }

        void OnMissions()
        {
            if (ModalOpen && page == "hub.missions")
                BuildMissions();
        }

        void ToggleMute()
        {
            bool muted = !save.Data.muted;
            save.Data.muted = muted;
            save.SaveSoon();
            if (ServiceLocator.TryGet<IAudioService>(out var audio))
                audio.SetMuted(muted);
            if (muteLabel != null)
                muteLabel.text = muted ? Loc.Get("hub.sound.off") : Loc.Get("hub.sound.on");
            PaintToggle(muteIcon, !muted);
        }

        void ToggleHaptics()
        {
            bool enabled = !save.Data.haptics;
            save.Data.haptics = enabled;
            save.SaveSoon();
            if (ServiceLocator.TryGet<IHapticsService>(out var haptics))
                haptics.Enabled = enabled;
            if (enabled)
                hapticsLight();
            if (hapticLabel != null)
                hapticLabel.text = enabled ? Loc.Get("hub.haptic.on") : Loc.Get("hub.haptic.off");
            PaintToggle(hapticIcon, enabled);
        }

        static void hapticsLight()
        {
            if (ServiceLocator.TryGet<IHapticsService>(out var haptics))
                haptics.Light();
        }

        void Open(string key, System.Action fill)
        {
            page = key;
            Clear(modalBody);
            modalTitle.text = Loc.Get(key);
            fill();
            modalRoot.SetActive(true);
            ModalOpen = true;
            modal.localScale = Vector3.one * 0.84f;
            modalGroup.alpha = 0f;
            JuiceTween.Scale(modal, Vector3.one, 0.34f, Ease.OutBack);
            JuiceTween.Fade(modalGroup, 1f, 0.16f);
        }

        public void Close()
        {
            page = null;
            ModalOpen = false;
            if (modalRoot != null)
                modalRoot.SetActive(false);
        }

        void BuildShop()
        {
            Clear(modalBody);
            Section(Loc.Get("shop.color"));
            AddCosmetics(CosmeticSlot.Color);
            Section(Loc.Get("shop.texture"));
            AddCosmetics(CosmeticSlot.Texture);
            Section(Loc.Get("shop.core"));
            AddCosmetics(CosmeticSlot.Core);
            UiFactory.Slider(modalBody, Loc.Get("shop.viscosity"), save.Data.viscosity, value => customizer.SetViscosity(value));
            Note(Loc.Get("shop.viscosity.note"));
        }

        void AddCosmetics(CosmeticSlot slot)
        {
            IReadOnlyList<SlimeCosmeticSO> items = customizer.Catalog;
            for (int i = 0; i < items.Count; i++)
            {
                SlimeCosmeticSO item = items[i];
                if (item.slot != slot)
                    continue;
                bool owned = customizer.IsOwned(item);
                bool selected = customizer.IsSelected(item);
                string name = Loc.Or("item." + item.id, item.displayName);
                string prefix = selected ? "● " : owned ? "" : item.cost + "  ";
                string suffix = selected ? Loc.Get("shop.using") : owned ? "" : Loc.Get("shop.price");
                SlimeCosmeticSO captured = item;
                UiFactory.Button(modalBody, prefix + name + suffix, () =>
                {
                    if (customizer.TrySelectOrBuy(captured))
                        BuildShop();
                    else if (ServiceLocator.TryGet<IAudioService>(out var audio))
                        audio.Play(Sfx.Schlupp, 1.4f, 0.5f);
                }, 112f, selected ? UiFactory.ButtonTone.Solid : UiFactory.ButtonTone.Soft);
            }
        }

        void BuildGames()
        {
            Clear(modalBody);
            GameCard(Loc.Get("game.hop"), Loc.Get("game.hop.body"), BoardIds.Hop, SceneNames.Hop);
            GameCard(Loc.Get("game.merge"), Loc.Get("game.merge.body"), BoardIds.Merge, SceneNames.Merge);
            GameCard(Loc.Get("game.gulp"), Loc.Get("game.gulp.body"), BoardIds.Gulp, SceneNames.Gulp);
        }

        void GameCard(string title, string body, string board, string scene)
        {
            Section(title);
            Note(body);
            int best = boardService().GetBest(board);
            string shown = best.ToString();
            Note(Loc.Format("games.record", shown));
            UiFactory.Button(modalBody, Loc.Get("games.enter"), () => SceneFlow.Load(scene), 112f, UiFactory.ButtonTone.Solid);
        }

        ILeaderboardService boardService() => board;

        void BuildMissions()
        {
            Clear(modalBody);
            missions.EnsureToday();
            List<MissionSave> list = save.Data.missions;
            if (list == null || list.Count == 0)
            {
                Note(Loc.Get("missions.empty"));
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                MissionSave mission = list[i];
                string progress = mission.progress + " / " + mission.target;
                string description = Loc.Or("mission." + mission.id, mission.description);
                Note(description + "\n" + progress + (mission.claimed ? Loc.Get("missions.claimed") : ""));
                float goal = mission.target <= 0 ? 0f : (float)mission.progress / mission.target;
                UiFactory.Meter(modalBody, goal);
                if (!mission.claimed && mission.progress >= mission.target)
                {
                    string id = mission.id;
                    UiFactory.Button(modalBody, Loc.Format("missions.claim", mission.reward), () =>
                    {
                        missions.TryClaim(id);
                        BuildMissions();
                    }, 112f, UiFactory.ButtonTone.Solid);
                }
            }
        }

        void BuildFusion()
        {
            Clear(modalBody);
            Note(Loc.Get("fusion.note"));
            friendField = UiFactory.Field(modalBody, Loc.Get("fusion.placeholder"));
            UiFactory.Button(modalBody, Loc.Format("fusion.button", fusion.Cost), () => Fuse(), 112f, UiFactory.ButtonTone.Solid);
            if (save.Data.fusions == null || save.Data.fusions.Count == 0)
            {
                Note(Loc.Get("fusion.empty"));
                return;
            }

            Section(Loc.Get("fusion.yours"));
            for (int i = save.Data.fusions.Count - 1; i >= 0; i--)
            {
                FusionSave entry = save.Data.fusions[i];
                string id = entry.id;
                bool selected = save.Data.activeFusionID == id;
                UiFactory.Button(modalBody, (selected ? "● " : "") + FusionCaption(entry.label), () =>
                {
                    customizer.SelectFusion(id);
                    BuildFusion();
                }, 112f, selected ? UiFactory.ButtonTone.Solid : UiFactory.ButtonTone.Soft);
            }
        }

        async void Fuse()
        {
            string code = friendField != null ? friendField.text : "";
            FusionResult result = await fusion.FuseAsync(code);
            if (!result.Success)
            {
                Note(result.Reason);
                return;
            }
            if (ServiceLocator.TryGet<IAudioService>(out var audio))
                audio.Play(Sfx.Merge, 0.9f);
            if (ServiceLocator.TryGet<IHapticsService>(out var haptics))
                haptics.Medium();
            BuildFusion();
        }

        void Section(string title)
        {
            Text text = UiFactory.Label(modalBody, title, 36, TextAnchor.MiddleLeft, Color.white);
            UiFactory.Outline(text);
            var layout = text.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 48f;
            layout.preferredHeight = 48f;
        }

        void Note(string body)
        {
            Text text = UiFactory.Label(modalBody, body, 28, TextAnchor.MiddleLeft, new Color(0.88f, 0.95f, 1f, 1f));
            int lines = 1;
            if (!string.IsNullOrEmpty(body))
                lines += (body.Length / 36) + (body.Split('\n').Length - 1);
            float height = Mathf.Clamp(lines * 38f, 72f, 220f);
            var layout = text.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
        }

        static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }

        static string FusionCaption(string label)
        {
            if (string.IsNullOrWhiteSpace(label) || label == "Fusão anônima" || label == "Fusión anónima" || label == "Anonymous fusion")
                return Loc.Get("fusion.anon");
            if (label.StartsWith("Fusão "))
                label = label.Substring("Fusão ".Length);
            else if (label.StartsWith("Fusión "))
                label = label.Substring("Fusión ".Length);
            else if (label.StartsWith("Fusion "))
                label = label.Substring("Fusion ".Length);
            return Loc.Format("fusion.with", label);
        }
    }
}
