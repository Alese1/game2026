using System;
using System.Collections.Generic;
using SwordArt.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SwordArt.UI
{
    /// <summary>
    /// Bottom-screen dialogue box with speaker name and typewriter text.
    /// E / Space / LMB advances; Esc closes.
    /// </summary>
    public sealed class DialogueWindow : UIWindow
    {
        private const float CharsPerSecond = 55f;

        private Text _speaker;
        private Text _body;
        private Text _hint;
        private readonly List<string> _lines = new List<string>();
        private int _index;
        private float _visibleChars;
        private Action _onFinished;

        /// <summary>Opens the window with <paramref name="lines"/> spoken by <paramref name="speaker"/>.</summary>
        public void Show(string speaker, IList<string> lines, Action onFinished = null)
        {
            Open();
            _lines.Clear();
            if (lines != null) _lines.AddRange(lines);
            if (_lines.Count == 0) _lines.Add("...");
            _onFinished = onFinished;
            _speaker.text = speaker;
            _index = 0;
            _visibleChars = 0;
            Refresh();
        }

        protected override void Build(UITheme theme)
        {
            var panel = UIKit.Panel(transform, "Panel", theme, true);
            panel.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(1100, 230));

            var namePlate = UIKit.Image(panel.transform, "NamePlate", theme.Accent, UIKit.Rounded);
            namePlate.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(36, 0), new Vector2(300, 46));
            _speaker = UIKit.Text(namePlate.transform, "Speaker", "", 24, theme.PanelStrong, TextAnchor.MiddleCenter, FontStyle.Bold);
            _speaker.rectTransform.Stretch();

            _body = UIKit.Text(panel.transform, "Body", "", 27, theme.Text, TextAnchor.UpperLeft);
            _body.rectTransform.Stretch();
            _body.rectTransform.offsetMin = new Vector2(40, 50);
            _body.rectTransform.offsetMax = new Vector2(-40, -40);

            _hint = UIKit.Text(panel.transform, "Hint", "", 18, theme.TextDim, TextAnchor.LowerRight);
            _hint.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-28, 14), new Vector2(-40, 30));
        }

        protected override void Update()
        {
            base.Update();
            if (!IsOpen) return;
            string line = _lines[_index];
            if (_visibleChars < line.Length)
            {
                _visibleChars = Mathf.Min(line.Length, _visibleChars + Time.unscaledDeltaTime * CharsPerSecond);
                Refresh();
            }

            if (!InputFresh) return;
            var input = GameManager.Instance.Input;
            bool advance = input.Interact.WasPressedThisFrame() || input.Jump.WasPressedThisFrame() ||
                           (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                           (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame);
            if (!advance) return;

            if (_visibleChars < line.Length)
            {
                _visibleChars = line.Length;
                Refresh();
                return;
            }
            _index++;
            _visibleChars = 0;
            if (_index >= _lines.Count)
            {
                Close();
                return;
            }
            Refresh();
        }

        protected override void OnClosed()
        {
            var cb = _onFinished;
            _onFinished = null;
            cb?.Invoke();
        }

        private void Refresh()
        {
            string line = _lines[_index];
            _body.text = line.Substring(0, Mathf.Clamp((int)_visibleChars, 0, line.Length));
            bool last = _index >= _lines.Count - 1;
            _hint.text = $"{_index + 1}/{_lines.Count}   [E] {(last ? "Закрыть" : "Далее")}   [Esc] Выйти";
        }
    }
}
