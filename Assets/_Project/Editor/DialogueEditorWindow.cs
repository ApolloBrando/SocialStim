using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SocialStim.Application.PresentationCues;
using SocialStim.Data.Dialogue;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SocialStim.Editor
{
    /// <summary>Native searchable dialogue/copy editor with Undo, validated saves and external-edit protection.</summary>
    public sealed class DialogueEditorWindow : EditorWindow
    {
        [SerializeField] private TextAsset _asset;
        [SerializeField] private string _baseline, _selectedId;
        [SerializeField] private DialogueEditBuffer _buffer;
        private bool _refreshing;
        private string WorkingJson { get => _buffer == null ? "" : _buffer.Json; set { EnsureBuffer(); _buffer.SetText(value); } }
        private string _search = "";
        private EditableTextDocument _document;
        private List<string> _filtered = new List<string>();
        private ListView _list;
        private TextField _prose, _details;
        private Label _status;
        private ObjectField _assetField;
        /// <summary>Opens the native editor for selected or starter dialogue.</summary>
        [MenuItem("Tools/SocialStim/Dialogue and Text Editor")]
        public static void Open() => OpenAsset(Selection.activeObject as TextAsset);
        /// <summary>Opens an explicitly selected text asset, or the starter dialogue when no asset is selected.</summary>
        public static void OpenAsset(TextAsset asset)
        {
            var window = GetWindow<DialogueEditorWindow>("SocialStim Dialogue");
            window.minSize = new Vector2(720, 480);
            if (asset == null) asset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Dialogue/StarterDialogue.json");
            if (asset != window._asset && window.ResolveUnsaved()) window.Load(asset);
            window.Show();
        }
        private void OnEnable()
        {
            EnsureBuffer();
            saveChangesMessage = "Save edited SocialStim dialogue before closing?";
            Undo.undoRedoPerformed += OnUndoRedo;
        }
        private void OnDisable() => Undo.undoRedoPerformed -= OnUndoRedo;
        /// <summary>Builds native UI Toolkit controls; no custom editor-window polling is required.</summary>
        public void CreateGUI()
        {
            rootVisualElement.Clear();
            var toolbar = new Toolbar(); rootVisualElement.Add(toolbar);
            _assetField = new ObjectField("Source") { objectType = typeof(TextAsset), allowSceneObjects = false, value = _asset };
            _assetField.style.flexGrow = 1; toolbar.Add(_assetField);
            _assetField.RegisterValueChangedCallback(evt => {
                var next = evt.newValue as TextAsset;
                if (next == _asset) return;
                if (ResolveUnsaved()) Load(next); else _assetField.SetValueWithoutNotify(_asset);
            });
            toolbar.Add(new ToolbarButton(() => Attempt(SaveChanges)) { text = "Save" });
            toolbar.Add(new ToolbarButton(() => Attempt(ValidateWorking)) { text = "Validate" });
            toolbar.Add(new ToolbarButton(() => { if (ResolveUnsaved()) Load(_asset); }) { text = "Reload" });
            toolbar.Add(new ToolbarButton(() => { if (_asset != null) AssetDatabase.OpenAsset(_asset); }) { text = "Open JSON source" });
            var search = new ToolbarSearchField(); search.style.flexGrow = 1; rootVisualElement.Add(search);
            search.RegisterValueChangedCallback(evt => { _search = evt.newValue; Filter(); });
            var split = new TwoPaneSplitView(0, 300, TwoPaneSplitViewOrientation.Horizontal); split.style.flexGrow = 1; rootVisualElement.Add(split);
            _list = new ListView { fixedItemHeight = 24, selectionType = SelectionType.Single, makeItem = () => new Label() };
            _list.bindItem = (element, index) => ((Label)element).text = _document.Group(_filtered[index]) + " · " + _filtered[index];
            _list.selectionChanged += values => { if (!_refreshing) Select(values.OfType<string>().FirstOrDefault()); }; split.Add(_list);
            var pane = new ScrollView(); split.Add(pane);
            _prose = new TextField("Authored text") { multiline = true, isDelayed = true };
            _prose.style.minHeight = 130; _prose.style.whiteSpace = WhiteSpace.Normal; pane.Add(_prose);
            _prose.RegisterValueChangedCallback(evt => Attempt(() => {
                if (_document == null || _selectedId == null || _document.Text(_selectedId) == evt.newValue) return;
                string updated = _document.Reword(_selectedId, evt.newValue);
                Undo.RecordObject(_buffer, "Edit SocialStim dialogue"); WorkingJson = updated; EditorUtility.SetDirty(_buffer);
                Refresh(); _status.text = "Text staged. Save validates the full asset. Other fields are preserved.";
            }));
            _details = new TextField("Complete selected entry (read-only)") { multiline = true, isReadOnly = true }; pane.Add(_details);
            _status = new Label(); _status.style.whiteSpace = WhiteSpace.Normal; rootVisualElement.Add(_status);
            Refresh();
        }
        private void Load(TextAsset asset)
        {
            if (_buffer != null) DestroyImmediate(_buffer);
            _buffer = null; EnsureBuffer();
            _asset = asset; _selectedId = null;
            WorkingJson = _baseline = asset == null ? "" : File.ReadAllText(AssetDatabase.GetAssetPath(asset));
            _assetField?.SetValueWithoutNotify(asset); Refresh();
        }
        private void Refresh()
        {
            hasUnsavedChanges = WorkingJson != _baseline;
            if (_list == null) return;
            try { _document = string.IsNullOrEmpty(WorkingJson) ? null : new EditableTextDocument(WorkingJson); Filter(); }
            catch (ArgumentException exception) { _document = null; _filtered.Clear(); _list.itemsSource = _filtered; _list.Rebuild(); _status.text = exception.Message + " Open JSON source to repair its structure."; }
            if (_document != null) Select(_selectedId ?? _filtered.FirstOrDefault());
            else { _prose.SetValueWithoutNotify(""); _details.SetValueWithoutNotify(""); }
        }
        private void Filter()
        {
            if (_list == null) return;
            _filtered = _document == null ? new List<string>() : _document.Ids.Where(id => string.IsNullOrEmpty(_search)
                || (id + " " + _document.Group(id) + " " + _document.Text(id)).IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            _refreshing = true;
            try { _list.itemsSource = _filtered; _list.Rebuild(); }
            finally { _refreshing = false; }
            if (_document != null)
            {
                Select(_filtered.Contains(_selectedId) ? _selectedId : _filtered.FirstOrDefault());
                int index = _filtered.IndexOf(_selectedId);
                _list.SetSelectionWithoutNotify(index < 0 ? Array.Empty<int>() : new[] { index });
            }
            _status.text = _filtered.Count + " entries · " + (hasUnsavedChanges ? "unsaved edits" : "source unchanged");
        }
        private void Select(string id)
        {
            _selectedId = id;
            if (_prose == null) return;
            _prose.SetEnabled(_document != null && id != null);
            _prose.SetValueWithoutNotify(_document != null && id != null ? _document.Text(id) : "");
            _details.SetValueWithoutNotify(_document != null && id != null ? _document.Details(id) : "");
        }
        private void ValidateWorking()
        {
            if (_document == null) throw new InvalidOperationException("Select a readable dialogue or copy JSON asset.");
            _document.Validate();
            if (!_document.IsCopy) foreach (var line in DialogueDocument.Parse(WorkingJson))
                try { SemanticMarkup.Parse(line.Text); }
                catch (ArgumentException exception) { throw new ArgumentException(line.Id + ": " + exception.Message, exception); }
            _status.text = "Full asset schema, content bindings and markup valid.";
        }
        /// <summary>Validates and atomically replaces only the selected source, preserving its meta GUID.</summary>
        public override void SaveChanges()
        {
            ValidateWorking();
            if (_asset == null) throw new InvalidOperationException("Select an existing source asset.");
            string path = AssetDatabase.GetAssetPath(_asset);
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || Path.GetExtension(path) != ".json")
                throw new InvalidOperationException("Only project JSON assets can be saved here.");
            if (File.ReadAllText(path) != _baseline) throw new IOException("Source changed outside this window. Preserve your edits and reload before saving.");
            if (!AssetDatabase.MakeEditable(path)) throw new IOException("The source is read-only or could not be checked out.");
            string temporary = path + ".tmp~";
            try { File.WriteAllText(temporary, WorkingJson, new System.Text.UTF8Encoding(false)); File.Replace(temporary, path, null); }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
            _baseline = WorkingJson; base.SaveChanges(); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            DialogueCatalogueExport.RefreshFor(path);
            _status.text = "Saved and reimported. Start a new run to load the edited content.";
        }
        /// <summary>Discards only staged window edits; the source file is untouched.</summary>
        public override void DiscardChanges() { WorkingJson = _baseline; base.DiscardChanges(); Refresh(); }
        private bool ResolveUnsaved()
        {
            if (!hasUnsavedChanges) return true;
            int choice = EditorUtility.DisplayDialogComplex("Unsaved dialogue", "Save edits before changing source?", "Save", "Cancel", "Discard");
            if (choice == 1) return false;
            if (choice == 2) { DiscardChanges(); return true; }
            try { SaveChanges(); return true; } catch (Exception exception) { _status.text = exception.Message; return false; }
        }
        private void Attempt(Action action)
        { try { action(); } catch (Exception exception) { _status.text = exception.Message; Debug.LogError(exception.Message, _asset); } }
        private void EnsureBuffer()
        { if (_buffer == null) { _buffer = CreateInstance<DialogueEditBuffer>(); _buffer.hideFlags = HideFlags.HideAndDontSave; } }
        private void OnUndoRedo() => Refresh();
        private void OnDestroy() { if (_buffer != null) DestroyImmediate(_buffer); }
    }
}
