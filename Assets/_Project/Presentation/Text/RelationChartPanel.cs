using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SocialStim.Presentation.Effects;
using SocialStim.Rules.Cast;
using SocialStim.Rules.Dialogue;
using SocialStim.Rules.Reasoning;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SocialStim.Presentation.Text
{
    /// <summary>Read-only, paged native uGUI relation chart with one observer at the centre.</summary>
    public sealed class RelationChartPanel : MonoBehaviour
    {
        private TMP_FontAsset _font;
        private GameText _words;
        private TextPalette _palette;
        private IReadOnlyList<SocialDebugRecord> _records;
        private CharacterId[] _observers;
        private int _observerIndex, _page;
        /// <summary>Captures an immutable diagnostics snapshot without requesting gameplay commands.</summary>
        public void Initialize(TMP_FontAsset font, GameText words, TextPalette palette, IReadOnlyList<SocialDebugRecord> records)
        {
            _font = font; _words = words; _palette = palette; _records = records;
            _observers = records.Select(r => r.Observer).Distinct().OrderBy(id => id.Value).ToArray();
            Rebuild();
        }
        private void Rebuild()
        {
            foreach (Transform child in transform) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            if (_observers.Length == 0) return;
            var all = _records.Where(r => r.Observer == _observers[_observerIndex]).OrderBy(r => r.Subject.Value).ToArray();
            int pages = Mathf.Max(1, (all.Length + 3) / 4); _page = Mathf.Clamp(_page, 0, pages - 1);
            var records = all.Skip(_page * 4).Take(4).ToArray();
            var help = TextUiFactory.Text("Relation chart help", transform, _font, 14);
            help.text = _words.Format("ui.relationsHelp"); help.color = _palette.Foreground;
            help.textWrappingMode = TextWrappingModes.Normal;
            TextUiFactory.Fill(help.rectTransform, new Vector2(0, 0.88f), Vector2.one, Vector2.zero, Vector2.zero);
            Button previous = Control("ui.previousNpc", new Vector2(0, 0.80f), new Vector2(0.28f, 0.87f), () => SelectObserver(-1));
            Control("ui.nextNpc", new Vector2(0.72f, 0.80f), new Vector2(1, 0.87f), () => SelectObserver(1));
            var heading = TextUiFactory.Text("Observer", transform, _font, 14); heading.color = _palette.Foreground;
            heading.text = _words.Format("ui.relationsObserver", ("name", all[0].ObserverName)) + (all[0].ObserverAlive ? "" : " · " + _words.Format("ui.dead"));
            heading.alignment = TextAlignmentOptions.Center;
            TextUiFactory.Fill(heading.rectTransform, new Vector2(0.29f, 0.80f), new Vector2(0.71f, 0.87f), Vector2.zero, Vector2.zero);
            var graph = TextUiFactory.Rect("Directed relations", transform);
            TextUiFactory.Fill(graph, new Vector2(0, 0.12f), new Vector2(1, 0.79f), Vector2.zero, Vector2.zero);
            Vector2[] slots = { new Vector2(-0.32f, 0.29f), new Vector2(0.32f, 0.29f), new Vector2(-0.32f, -0.29f), new Vector2(0.32f, -0.29f) };
            Vector2[] targets = slots.Take(records.Length).ToArray();
            var arrows = graph.gameObject.AddComponent<RelationArrowGraphic>();
            arrows.Configure(targets, records.Select(r => _palette.Muted).ToArray(), 80, 116);
            var centreRoot = TextUiFactory.Rect("Observer card", graph);
            centreRoot.anchorMin = centreRoot.anchorMax = new Vector2(0.5f, 0.5f); centreRoot.sizeDelta = new Vector2(150, 74); centreRoot.anchoredPosition = Vector2.zero;
            centreRoot.gameObject.AddComponent<Image>().color = _palette.FocusBackground; TextUiFactory.Border(centreRoot);
            centreRoot.GetComponent<Outline>().effectColor = _palette.Character(all[0].Observer.Value);
            var centre = TextUiFactory.Text("Central observer", centreRoot, _font, 16);
            centre.text = _words.Format("ui.relationsCentre", ("name", all[0].ObserverName));
            centre.alignment = TextAlignmentOptions.Center; centre.color = _palette.Character(all[0].Observer.Value);
            TextUiFactory.Fill(centre.rectTransform, Vector2.zero, Vector2.one, new Vector2(6, 4), new Vector2(-6, -4));
            for (int i = 0; i < records.Length; i++)
            {
                SocialDebugRecord edge = records[i];
                string name = edge.SubjectName + (edge.Subject.Value == 0 ? " " + _words.Format("ui.you") : "")
                    + (edge.SubjectAlive ? "" : " · " + _words.Format("ui.dead"));
                Button node = TextUiFactory.Button(graph, _font, name, 58);
                node.gameObject.name = "Relation " + edge.Subject.Value;
                node.GetComponent<LayoutElement>().ignoreLayout = true;
                var rect = (RectTransform)node.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f) + targets[i];
                rect.sizeDelta = new Vector2(190, 118); rect.anchoredPosition = Vector2.zero;
                var label = node.GetComponentsInChildren<TextMeshProUGUI>().Single();
                label.fontSize = 14; label.alignment = TextAlignmentOptions.MidlineLeft; label.overflowMode = TextOverflowModes.Ellipsis;
                TextUiFactory.Fill(label.rectTransform, new Vector2(0, 0.73f), Vector2.one, new Vector2(10, 0), new Vector2(-10, -3));
                node.GetComponent<ChoiceFocusIndicator>().SetText(name);
                ScoreBar(rect, "Trust", "ui.relationsTrust", edge.TrustValue, 0.40f,
                    _palette.Night ? new Color(0.25f, 0.8f, 0.85f) : new Color(0.02f, 0.40f, 0.44f));
                ScoreBar(rect, "Suspicion", "ui.relationsSuspicion", edge.SuspicionValue, 0.08f,
                    _palette.Night ? new Color(1, 0.72f, 0.3f) : new Color(0.55f, 0.27f, 0.04f));
                label.color = edge.SubjectAlive ? _palette.Character(edge.Subject.Value) : _palette.Muted;
                node.GetComponent<ChoiceFocusIndicator>().SetSurfaceColors(_palette.ButtonBackground, _palette.FocusBackground);
                node.interactable = edge.Subject.Value != 0;
                node.onClick.AddListener(() => { _observerIndex = System.Array.IndexOf(_observers, edge.Subject); _page = 0; Rebuild(); });
            }
            Control("ui.previousTargets", new Vector2(0, 0), new Vector2(0.30f, 0.09f), () => { _page--; Rebuild(); }).interactable = _page > 0;
            Control("ui.nextTargets", new Vector2(0.70f, 0), new Vector2(1, 0.09f), () => { _page++; Rebuild(); }).interactable = _page + 1 < pages;
            var page = TextUiFactory.Text("Relation page", transform, _font, 14); page.color = _palette.Foreground;
            page.text = _words.Format("ui.relationsPage", ("page", (_page + 1).ToString()), ("pages", pages.ToString())); page.alignment = TextAlignmentOptions.Center;
            TextUiFactory.Fill(page.rectTransform, new Vector2(0.31f, 0), new Vector2(0.69f, 0.09f), Vector2.zero, Vector2.zero);
            GetComponentInParent<InformationPanel>().ConfineNavigation();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(previous.gameObject);
        }
        private void ScoreBar(RectTransform card, string name, string key, float value, float bottom, Color tint)
        {
            var label = TextUiFactory.Text(name + " score", card, _font, 12); label.color = _palette.Foreground;
            label.text = _words.Format(key, ("value", value.ToString("0.0", CultureInfo.InvariantCulture)));
            TextUiFactory.Fill(label.rectTransform, new Vector2(0.06f, bottom + 0.10f), new Vector2(0.94f, bottom + 0.29f), Vector2.zero, Vector2.zero);
            var track = TextUiFactory.Rect(name + " track", card);
            TextUiFactory.Fill(track, new Vector2(0.06f, bottom), new Vector2(0.94f, bottom + 0.07f), Vector2.zero, Vector2.zero);
            var background = track.gameObject.AddComponent<Image>(); background.color = Color.Lerp(_palette.ButtonBackground, _palette.Foreground, 0.18f); background.raycastTarget = false;
            var fill = TextUiFactory.Rect(name + " fill", track); TextUiFactory.Fill(fill, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var image = fill.gameObject.AddComponent<Image>(); image.color = tint; image.raycastTarget = false;
            image.sprite = card.GetComponent<Image>().sprite;
            image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Horizontal; image.fillOrigin = 0; image.fillAmount = Mathf.Clamp01(value / 10f);
        }
        private void SelectObserver(int direction) { _observerIndex = (_observerIndex + direction + _observers.Length) % _observers.Length; _page = 0; Rebuild(); }
        private Button Control(string key, Vector2 minimum, Vector2 maximum, UnityEngine.Events.UnityAction action)
        {
            Button button = TextUiFactory.Button(transform, _font, _words.Format(key), 40);
            TextUiFactory.Fill((RectTransform)button.transform, minimum, maximum, Vector2.zero, Vector2.zero);
            button.GetComponent<ChoiceFocusIndicator>().SetSurfaceColors(_palette.ButtonBackground, _palette.FocusBackground);
            foreach (var text in button.GetComponentsInChildren<TextMeshProUGUI>()) text.color = _palette.Foreground;
            button.onClick.AddListener(action); return button;
        }
    }
}
