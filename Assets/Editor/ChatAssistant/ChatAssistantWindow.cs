using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

public class ChatAssistantWindow : EditorWindow
{
    private string _input = "";
    private Vector2 _scroll;
    private readonly List<string> _messages = new List<string>();

    [MenuItem("Tools/Chat Assistant")]
    public static void Open()
    {
        var win = GetWindow<ChatAssistantWindow>("Chat Assistant");
        win.minSize = new Vector2(420, 320);
        win.Show();
    }

    private void OnEnable()
    {
        if (_messages.Count == 0)
        {
            AddAssistantMessage("Hazırım ✅ Snapshot ZIP’e bas, zip’i buraya yükle, ben projeyi okuyayım.");
        }
    }

    private void OnGUI()
    {
        DrawToolbar();
        GUILayout.Space(6);
        DrawChatArea();
        GUILayout.Space(6);
        DrawInputArea();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("Snapshot ZIP", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                string zipPath = ChatSnapshotExporter.ExportSnapshotZip();
                if (!string.IsNullOrEmpty(zipPath))
                {
                    AddAssistantMessage($"Snapshot oluşturuldu ✅\n{zipPath}\nZip’i buraya yükle.");
                }
            }

            if (GUILayout.Button("Open Desktop", EditorStyles.toolbarButton, GUILayout.Width(90)))
            {
                EditorUtility.RevealInFinder(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                _messages.Clear();
                _input = "";
                GUI.FocusControl(null);
            }
        }
    }

    private void DrawChatArea()
    {
        using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll))
        {
            _scroll = scroll.scrollPosition;

            var style = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                richText = true,
                fontSize = 12
            };

            foreach (var msg in _messages)
            {
                GUILayout.Label(msg, style);
                GUILayout.Space(6);
            }
        }
    }

    private void DrawInputArea()
    {
        GUI.SetNextControlName("ChatInput");

        using (new EditorGUILayout.HorizontalScope())
        {
            _input = EditorGUILayout.TextField(_input);

            bool send = GUILayout.Button("Send", GUILayout.Width(80));
            bool enterPressed = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return;

            if ((send || enterPressed) && !string.IsNullOrWhiteSpace(_input))
            {
                AddUserMessage(_input.Trim());
                _input = "";
                GUI.FocusControl("ChatInput");

                // Demo cevap. (API entegrasyonunu sonra buraya bağlarız.)
                AddAssistantMessage("Mesajını aldım. (Demo) İstersen bir sonraki adımda bunu gerçek ChatGPT API’ye bağlayalım.");
                Repaint();

                // Enter event'ini tüket
                Event.current.Use();
            }
        }
    }

    private void AddUserMessage(string text)
    {
        _messages.Add($"<b><color=#6AA84F>Sen:</color></b> {Escape(text)}");
        ScrollToBottom();
    }

    private void AddAssistantMessage(string text)
    {
        _messages.Add($"<b><color=#3C78D8>Asistan:</color></b> {Escape(text)}");
        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        _scroll.y = float.MaxValue;
    }

    private static string Escape(string s)
    {
        return s.Replace("<", "&lt;").Replace(">", "&gt;");
    }
}
