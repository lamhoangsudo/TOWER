using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace AssetInventory
{
    public sealed class TagsUI : BasicEditorUI
    {
        private List<Tag> _tags;
        private string _searchTerm;
        private Vector2 _scrollPos;
        private SearchField SearchField => _searchField = _searchField ?? new SearchField();
        private SearchField _searchField;

        public static TagsUI ShowWindow()
        {
            return GetWindow<TagsUI>("Tag Management");
        }

        public void Init()
        {
            _tags = Tagging.LoadTags();
        }

        public void OnEnable()
        {
            Tagging.OnTagsChanged += Init;
        }

        public void OnDisable()
        {
            Tagging.OnTagsChanged -= Init;
        }

        public override void OnGUI()
        {
            if (Event.current.isKey && Event.current.keyCode == KeyCode.Return)
            {
                Tagging.AddTag(_searchTerm);
                _searchTerm = "";
            }
            _searchTerm = SearchField.OnGUI(_searchTerm, GUILayout.ExpandWidth(true));
            if (_tags != null)
            {
                EditorGUILayout.Space();
                if (_tags.Count == 0)
                {
                    EditorGUILayout.HelpBox("No tags created yet. Use the textfield above to create the first tag.", MessageType.Info);
                }
                else
                {
                    _scrollPos = GUILayout.BeginScrollView(_scrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.ExpandWidth(true));
                    foreach (Tag tag in _tags)
                    {
                        // filter
                        if (!string.IsNullOrWhiteSpace(_searchTerm) && !tag.Name.ToLowerInvariant().Contains(_searchTerm.ToLowerInvariant())) continue;

                        GUILayout.BeginHorizontal();
                        EditorGUI.BeginChangeCheck();
                        tag.Color = "#" + ColorUtility.ToHtmlStringRGB(EditorGUILayout.ColorField(GUIContent.none, tag.GetColor(), false, false, false, GUILayout.Width(20)));
                        if (EditorGUI.EndChangeCheck()) Tagging.SaveTag(tag);
                        EditorGUILayout.LabelField(new GUIContent(tag.Name, tag.FromAssetStore ? "From Asset Store" : "Local Tag"));
                        if (GUILayout.Button(EditorGUIUtility.IconContent("editicon.sml", "|Rename tag"), GUILayout.Width(30)))
                        {
                            NameUI nameUI = new NameUI();
                            nameUI.Init(tag.Name, newName => RenameTag(tag, newName));
                            PopupWindow.Show(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 0, 0), nameUI);
                        }
                        if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Trash", "|Remove tag completely"), GUILayout.Width(30)))
                        {
                            Tagging.DeleteTag(tag);
                        }
                        GUILayout.EndHorizontal();
                    }
                    if (!string.IsNullOrWhiteSpace(_searchTerm))
                    {
                        EditorGUILayout.HelpBox("Press RETURN to create a new tag", MessageType.Info);
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox("Temporary limitation: Actual tag colors will appear darker than selected here.", MessageType.Info);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Delete All")) _tags.ForEach(Tagging.DeleteTag);
                    GUILayout.EndScrollView();
                }
            }
        }

        private void RenameTag(Tag tag, string newName)
        {
            if (string.IsNullOrEmpty(newName) || tag.Name == newName) return;

            Tag existingTag = DBAdapter.DB.Find<Tag>(t => t.Name.ToLower() == newName.ToLower());
            if (existingTag != null)
            {
                EditorUtility.DisplayDialog("Error", "A tag with that name already exists (and merging tags is not yet supported).", "OK");
                return;
            }

            Tagging.RenameTag(tag, newName);
        }
    }
}