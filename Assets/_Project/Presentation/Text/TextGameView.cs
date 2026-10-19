using System;
using System.Collections.Generic;
using System.Linq;
using SocialStim.Application.Input;
using SocialStim.Application.PresentationCues;
using SocialStim.Rules.Roles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SocialStim.Presentation.Text
{
    /// <summary>Plain-text uGUI view and input adapter; never receives authoritative cast state.</summary>
    public sealed class TextGameView : MonoBehaviour, IGameView, IPlayerInputProvider
    {
        private TMP_FontAsset _font;
        private TextMeshProUGUI _status;
        private TextMeshProUGUI _transcript;
        private RectTransform _choices;
        private ScrollRect _transcriptScroll;
        private ScrollRect _choiceScroll;
        private readonly List<Button> _buttons = new List<Button>();

        /// <inheritdoc />
        public event Action<string> OptionChosen;
        /// <summary>Requests the setup menu; no run data persists.</summary>
        public event Action RestartRequested;

        /// <summary>Builds the plain-text interface once at scene startup.</summary>
        public void Initialize(TMP_FontAsset font)
        {
            _font = font;
            var canvas = new GameObject("Text Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.transform.SetParent(transform, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 800); scaler.matchWidthOrHeight = 0.5f;
            var background = canvas.AddComponent<Image>(); background.color = new Color(0.035f, 0.04f, 0.05f);
            _status = TextUiFactory.Text("Status", canvas.transform, font, 17);
            TextUiFactory.Fill(_status.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(20, -128), new Vector2(-150, -12));
            Button restart = TextUiFactory.Button(canvas.transform, font, "New run", 42);
            TextUiFactory.Fill((RectTransform)restart.transform, Vector2.one, Vector2.one, new Vector2(-140, -55), new Vector2(-20, -12));
            restart.onClick.AddListener(() => RestartRequested?.Invoke());

            _transcriptScroll = TextUiFactory.Scroll("Transcript", canvas.transform, out RectTransform transcriptContent);
            TextUiFactory.Fill((RectTransform)_transcriptScroll.transform, Vector2.zero, new Vector2(0.64f, 1), new Vector2(20, 20), new Vector2(-8, -140));
            _transcript = TextUiFactory.Text("Transcript Text", transcriptContent, font, 19);
            var transcriptLayout = transcriptContent.gameObject.AddComponent<VerticalLayoutGroup>();
            transcriptLayout.childControlHeight = true; transcriptLayout.childControlWidth = true;
            transcriptLayout.childForceExpandHeight = false;
            transcriptContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _choiceScroll = TextUiFactory.Scroll("Choices", canvas.transform, out _choices);
            TextUiFactory.Fill((RectTransform)_choiceScroll.transform, new Vector2(0.64f, 0), Vector2.one, new Vector2(8, 20), new Vector2(-20, -140));
            var choicesLayout = _choices.gameObject.AddComponent<VerticalLayoutGroup>();
            choicesLayout.spacing = 8; choicesLayout.childControlWidth = true; choicesLayout.childControlHeight = true;
            choicesLayout.childForceExpandHeight = false;
            _choices.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        /// <summary>Offers random or chosen role assignment without exposing the generated cast.</summary>
        public void ShowSetup(IReadOnlyList<RoleSpec> roles, string seedDescription, Action<RoleId?> start)
        {
            ClearChoices();
            _status.text = "SocialStim | New run | " + seedDescription;
            _transcript.text = "You belong to the cast. Speak, claim, lie, nominate, judge, and choose a night action.\n\n"
                + "Every spoken line in this engineering slice is marked PLACEHOLDER.\n\n"
                + "Choose a role or accept random assignment. The seed is recorded in the transcript. "
                + "A dead player may spectate but cannot act.\n\nScroll either panel with the mouse wheel. "
                + "Tab and Enter navigate/select standard UI buttons. No text or screen effects are enabled.";
            AddButton("Random role", "Generate a fresh cast and role assignment.", () => start(null));
            foreach (RoleSpec role in roles)
            {
                RoleId id = role.Id;
                AddButton("Play " + role.Name, role.Faction + " | Night: " + role.Ability, () => start(id));
            }
            _choiceScroll.verticalNormalizedPosition = 1;
        }

        /// <inheritdoc />
        public void Render(PlayerInputRequest request)
        {
            var view = request.View;
            string cast = string.Join(" | ", view.Characters.Select(c => c.Name + (c.Id == view.PlayerId ? " (you)" : "")
                + (!c.Alive ? " [dead]" : "") + (c.IsTeammate ? " [teammate]" : "")
                + (c.RevealedRole.HasValue ? " [" + c.RevealedRole.Value + "]" : "")));
            _status.text = $"Day {view.Day} | {view.Phase} | {view.OwnRole.Name} ({view.OwnRole.Faction}) | {(view.Alive ? "Alive" : "Spectator")}"
                + $" | Uses: {(view.Charges < 0 ? "unlimited" : view.Charges.ToString())}\nSeed {view.Seed} | Trials {view.TrialsUsed}/{view.TrialsAllowed}\n{cast}";
            _transcript.text = string.Join("\n\n", view.Transcript.Select(o => o.Text));
            ClearChoices();
            foreach (PlayerOption option in request.Options)
            {
                string id = option.Id;
                AddButton(option.Label, option.Interpretation, () => OptionChosen?.Invoke(id));
            }
            Canvas.ForceUpdateCanvases();
            _transcriptScroll.verticalNormalizedPosition = 0;
            _choiceScroll.verticalNormalizedPosition = 1;
        }

        /// <inheritdoc />
        public void ShowInputError(string message) => _status.text = message + "\n" + _status.text;

        private void AddButton(string label, string detail, Action selected)
        {
            Button button = TextUiFactory.Button(_choices, _font, label + "\n" + detail, 124);
            button.onClick.AddListener(() => selected());
            _buttons.Add(button);
        }

        private void ClearChoices()
        {
            foreach (Button button in _buttons)
            { button.interactable = false; button.gameObject.SetActive(false); Destroy(button.gameObject); }
            _buttons.Clear();
        }
    }
}
