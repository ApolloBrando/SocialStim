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
            int pages = Mathf.Max(1, (all.Length + 7) / 8); _page = Mathf.Clamp(_page, 0, pages - 1);
            var records = all.Skip(_page * 8).Take(8).ToArray();
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
            Vector2[] targets = Enumerable.Range(0, records.Length).Select(i =>
            {
                float angle = Mathf.PI * 0.5f - i * 2 * Mathf.PI / records.Length;
                return new Vector2(Mathf.Cos(angle) * 0.35f, Mathf.Sin(angle) * 0.36f);
            }).ToArray();
            var arrows = graph.gameObject.AddComponent<RelationArrowGraphic>();
            arrows.Configure(targets, records.Select(r => _palette.Muted).ToArray());
            var centre = TextUiFactory.Text("Central observer", graph, _font, 16);
            centre.text = _words.Format("ui.relationsCentre", ("name", all[0].ObserverName));
            centre.alignment = TextAlignmentOptions.Center; centre.color = _palette.Character(all[0].Observer.Value);
            centre.rectTransform.anchorMin = centre.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            centre.rectTransform.sizeDelta = new Vector2(124, 58); centre.rectTransform.anchoredPosition = Vector2.zero;
            for (int i = 0; i < records.Length; i++)
            {
                SocialDebugRecord edge = records[i];
                string name = edge.SubjectName + (edge.SubjectAlive ? "" : " · " + _words.Format("ui.dead"));
                Button node = TextUiFactory.Button(graph, _font, name, 58);
                node.gameObject.name = "Relation " + edge.Subject.Value;
                node.GetComponent<LayoutElement>().ignoreLayout = true;
                var rect = (RectTransform)node.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f) + targets[i];
                rect.sizeDelta = new Vector2(136, 78); rect.anchoredPosition = Vector2.zero;
                var label = node.GetComponentsInChildren<TextMeshProUGUI>().Single();
                label.fontSize = 12;
                string details = _words.Format("ui.relationsScores", ("suspicion", edge.SuspicionValue.ToString("0.0", CultureInfo.InvariantCulture)),
                    ("trust", edge.TrustValue.ToString("0.0", CultureInfo.InvariantCulture)));
                node.GetComponent<ChoiceFocusIndicator>().SetText(name + "\n" + details);
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
