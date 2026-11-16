using System.Collections;
using System.Collections.Generic;
using SwordArt.Audio;
using SwordArt.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SwordArt.UI
{
    /// <summary>
    /// Full-screen loading overlay. Real-world loads show a calm progress screen; connecting to
    /// GGO shows an animated "Link Start" tunnel of expanding coloured rings.
    /// </summary>
    public sealed class LoadingScreen : MonoBehaviour
    {
        private static readonly string[] Tips =
        {
            "Совет: в офисе можно подработать и заработать кредиты.",
            "Совет: купленное в магазине оружие становится доступным в GGO.",
            "Совет: в GGO красная линия показывает, куда целится противник.",
            "Совет: следите за безопасной зоной — снаружи она наносит урон.",
            "Совет: H — использовать аптечку во время боя.",
            "Совет: игра сохраняется при каждом переходе между мирами."
        };

        private static readonly Color[] RingColors =
        {
            new Color(1f, 0.3f, 0.3f), new Color(1f, 0.7f, 0.2f), new Color(0.4f, 1f, 0.5f),
            new Color(0.3f, 0.8f, 1f), new Color(0.6f, 0.4f, 1f), new Color(1f, 1f, 1f)
        };

        private CanvasGroup _group;
        private Image _background;
        private Text _title;
        private Text _subtitle;
        private Text _tip;
        private Image _barFill;
        private RectTransform _ringRoot;
        private readonly List<Image> _rings = new List<Image>();
        private bool _virtual;
        private float _ringTimer;

        private void Awake()
        {
            _group = gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = true;

            _background = UIKit.Image(transform, "Background", new Color(0.02f, 0.03f, 0.05f, 1f));
            _background.rectTransform.Stretch();
            _background.raycastTarget = true;

            _ringRoot = UIKit.Rect(transform, "Rings");
            _ringRoot.Stretch();

            _title = UIKit.Text(transform, "Title", "", 72, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            _title.rectTransform.PlaceAt(new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1400, 110));
            _subtitle = UIKit.Text(transform, "Subtitle", "", 26, new Color(0.7f, 0.8f, 0.9f), TextAnchor.MiddleCenter);
            _subtitle.rectTransform.PlaceAt(new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(1400, 40));

            var bar = UIKit.Image(transform, "Bar", new Color(1f, 1f, 1f, 0.12f), UIKit.Rounded);
            bar.rectTransform.PlaceAt(new Vector2(0.5f, 0f), new Vector2(0, 120), new Vector2(720, 6));
            _barFill = UIKit.Image(bar.transform, "Fill", new Color(0.36f, 0.78f, 1f), UIKit.Rounded);
            _barFill.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);

            _tip = UIKit.Text(transform, "Tip", "", 22, new Color(0.65f, 0.72f, 0.8f), TextAnchor.MiddleCenter);
            _tip.rectTransform.PlaceAt(new Vector2(0.5f, 0f), new Vector2(0, 70), new Vector2(1400, 36));

            for (int i = 0; i < 14; i++)
            {
                var ring = UIKit.Image(_ringRoot, "Ring" + i, Color.white, UIKit.Ring, false);
                ring.rectTransform.PlaceAt(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 200));
                ring.gameObject.SetActive(false);
                _rings.Add(ring);
            }

            _group.alpha = 0f;
            gameObject.SetActive(false);
        }

        /// <summary>Fades the screen in. <paramref name="virtualWorld"/> selects the Link Start style.</summary>
        public IEnumerator Show(string title, bool virtualWorld)
        {
            _virtual = virtualWorld;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            SetProgress(0f);
            _tip.text = Tips[Random.Range(0, Tips.Length)];

            if (virtualWorld)
            {
                _background.color = Color.black;
                _title.text = "LINK START";
                _title.color = Color.white;
                _subtitle.text = "Gun Gale Online — синхронизация FullDive...";
                _barFill.color = UITheme.Virtual.Accent;
                GameManager.Instance?.Audio?.Play2D(Sfx.LinkStart, 0.7f);
            }
            else
            {
                _background.color = new Color(0.02f, 0.03f, 0.05f, 1f);
                _title.text = string.IsNullOrEmpty(title) ? "Загрузка" : title;
                _title.color = new Color(0.9f, 0.95f, 1f);
                _subtitle.text = "Пожалуйста, подождите...";
                _barFill.color = UITheme.Real.Accent;
            }
            foreach (var r in _rings) r.gameObject.SetActive(false);

            while (_group.alpha < 1f)
            {
                _group.alpha = Mathf.MoveTowards(_group.alpha, 1f, Time.unscaledDeltaTime * 4f);
                yield return null;
            }
        }

        /// <summary>Fades the screen out.</summary>
        public IEnumerator Hide()
        {
            while (_group.alpha > 0f)
            {
                _group.alpha = Mathf.MoveTowards(_group.alpha, 0f, Time.unscaledDeltaTime * 3f);
                yield return null;
            }
            gameObject.SetActive(false);
        }

        /// <summary>Updates the progress bar (0..1).</summary>
        public void SetProgress(float progress)
        {
            _barFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
        }

        private void Update()
        {
            if (!_virtual) return;
            _ringTimer -= Time.unscaledDeltaTime;
            if (_ringTimer <= 0f)
            {
                _ringTimer = 0.09f;
                foreach (var ring in _rings)
                {
                    if (ring.gameObject.activeSelf) continue;
                    ring.gameObject.SetActive(true);
                    ring.rectTransform.localScale = Vector3.one * 0.05f;
                    ring.color = RingColors[Random.Range(0, RingColors.Length)];
                    break;
                }
            }
            foreach (var ring in _rings)
            {
                if (!ring.gameObject.activeSelf) continue;
                float s = ring.rectTransform.localScale.x + Time.unscaledDeltaTime * (1.2f + ring.rectTransform.localScale.x * 3f);
                ring.rectTransform.localScale = Vector3.one * s;
                var c = ring.color;
                c.a = Mathf.Clamp01(1f - s / 12f) * 0.8f;
                ring.color = c;
                if (s > 12f) ring.gameObject.SetActive(false);
            }
        }
    }
}
