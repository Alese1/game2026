using SwordArt.Audio;
using SwordArt.Core;
using SwordArt.Economy;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SwordArt.UI
{
    /// <summary>
    /// Office job mini-game "data entry": type the shown 4-key codes before the shift timer
    /// runs out. Pay depends on completed codes and accuracy (see <see cref="EconomyConfig"/>).
    /// </summary>
    public sealed class JobWindow : UIWindow
    {
        private static readonly Key[] Keys = { Key.A, Key.S, Key.D, Key.F, Key.J, Key.K, Key.L };
        private const int CodeLength = 4;

        private enum Phase { Intro, Working, Result }

        private Phase _phase;
        private Text _header;
        private Text _code;
        private Text _info;
        private Text _timer;
        private Image _timerFill;
        private Button _primary;

        private EconomyConfig _config;
        private readonly Key[] _current = new Key[CodeLength];
        private int _typed;
        private int _completed;
        private int _mistakes;
        private float _timeLeft;
        private int _reward;
        private float _shake;

        /// <summary>Opens the job terminal.</summary>
        public void Show()
        {
            _config = GameManager.Instance.Config.economy;
            Open();
            SetPhase(Phase.Intro);
        }

        protected override void Build(UITheme theme)
        {
            var panel = CreateDialogFrame(theme, new Vector2(900, 560), "ОФИС «NISHIKAWA DATA» — ВВОД ДАННЫХ");

            _header = UIKit.Text(panel, "Header", "", 24, theme.TextDim, TextAnchor.MiddleCenter);
            _header.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -88), new Vector2(-80, 80));

            _code = UIKit.Text(panel, "Code", "", 84, theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            _code.rectTransform.PlaceAt(new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(800, 120));

            _info = UIKit.Text(panel, "Info", "", 22, theme.TextDim, TextAnchor.MiddleCenter);
            _info.rectTransform.PlaceAt(new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(800, 60));

            var bar = UIKit.Image(panel, "TimerBar", new Color(1, 1, 1, 0.1f), UIKit.Rounded);
            bar.rectTransform.PlaceAt(new Vector2(0.5f, 0f), new Vector2(0, 110), new Vector2(780, 10));
            _timerFill = UIKit.Image(bar.transform, "Fill", theme.Accent, UIKit.Rounded);
            _timerFill.rectTransform.Place(Vector2.zero, Vector2.one, new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            _timer = UIKit.Text(panel, "Timer", "", 20, theme.TextDim, TextAnchor.MiddleRight);
            _timer.rectTransform.PlaceAt(new Vector2(0.5f, 0f), new Vector2(0, 135), new Vector2(780, 30));

            _primary = UIKit.Button(panel, "Начать смену", theme, OnPrimary);
            ((RectTransform)_primary.transform).PlaceAt(new Vector2(0.5f, 0f), new Vector2(0, 50), new Vector2(340, 56));
        }

        private void OnPrimary()
        {
            if (_phase == Phase.Intro) SetPhase(Phase.Working);
            else if (_phase == Phase.Result) Close();
        }

        private void SetPhase(Phase phase)
        {
            _phase = phase;
            var theme = Theme;
            switch (phase)
            {
                case Phase.Intro:
                    _header.text = "Вводите показанные коды клавишами A S D F J K L.\nОшибка сбрасывает текущий код.";
                    _code.text = "— — — —";
                    _code.color = theme.TextDim;
                    _info.text = $"Смена: {_config.jobTasksPerShift} кодов за {_config.jobShiftDuration:0} с · {_config.jobRewardPerTask} кр. за код + бонусы";
                    _timer.text = "";
                    _timerFill.rectTransform.anchorMax = Vector2.one;
                    _primary.gameObject.SetActive(true);
                    UIKit.SetButtonLabel(_primary, "Начать смену");
                    break;
                case Phase.Working:
                    _completed = 0;
                    _mistakes = 0;
                    _timeLeft = _config.jobShiftDuration;
                    _primary.gameObject.SetActive(false);
                    NewCode();
                    break;
                case Phase.Result:
                    _primary.gameObject.SetActive(true);
                    UIKit.SetButtonLabel(_primary, "Получить оплату");
                    Payout();
                    break;
            }
        }

        private void NewCode()
        {
            for (int i = 0; i < CodeLength; i++) _current[i] = Keys[Random.Range(0, Keys.Length)];
            _typed = 0;
            RenderCode();
            _header.text = $"Код {_completed + 1} из {_config.jobTasksPerShift}";
            _info.text = _mistakes > 0 ? $"Ошибок: {_mistakes}" : "Без ошибок";
        }

        private void RenderCode()
        {
            var theme = Theme;
            string done = ColorUtility.ToHtmlStringRGB(theme.Positive);
            string todo = ColorUtility.ToHtmlStringRGB(theme.Text);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < CodeLength; i++)
            {
                sb.Append(i < _typed ? $"<color=#{done}>" : $"<color=#{todo}>");
                sb.Append(_current[i].ToString());
                sb.Append("</color>");
                if (i < CodeLength - 1) sb.Append("  ");
            }
            _code.text = sb.ToString();
            _code.color = Color.white;
        }

        protected override void Update()
        {
            base.Update();
            if (!IsOpen) return;

            _shake = Mathf.MoveTowards(_shake, 0f, Time.unscaledDeltaTime * 4f);
            _code.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(Time.unscaledTime * 80f) * _shake * 12f, 0f);

            if (_phase != Phase.Working) return;

            _timeLeft -= Time.unscaledDeltaTime;
            _timerFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(_timeLeft / _config.jobShiftDuration), 1f);
            _timer.text = $"{Mathf.Max(0f, _timeLeft):0.0} с";
            if (_timeLeft <= 0f)
            {
                SetPhase(Phase.Result);
                return;
            }

            var kb = Keyboard.current;
            if (kb == null || !InputFresh) return;
            foreach (var key in Keys)
            {
                if (!kb[key].wasPressedThisFrame) continue;
                if (key == _current[_typed])
                {
                    _typed++;
                    GameManager.Instance.Audio.Play2D(Sfx.UIClick, 0.5f, 1.2f);
                    if (_typed >= CodeLength)
                    {
                        _completed++;
                        if (_completed >= _config.jobTasksPerShift)
                        {
                            SetPhase(Phase.Result);
                            return;
                        }
                        NewCode();
                    }
                    else RenderCode();
                }
                else
                {
                    _mistakes++;
                    _typed = 0;
                    _shake = 1f;
                    GameManager.Instance.Audio.Play2D(Sfx.UIError, 0.5f);
                    RenderCode();
                    _info.text = $"Ошибок: {_mistakes}";
                }
                break;
            }
        }

        private void Payout()
        {
            bool all = _completed >= _config.jobTasksPerShift;
            _reward = _completed * _config.jobRewardPerTask;
            if (all) _reward += _config.jobCompletionBonus;
            if (all && _mistakes == 0) _reward += _config.jobPerfectBonus;

            _header.text = all ? "Смена выполнена!" : "Время вышло";
            _code.text = $"+{_reward} кр.";
            _code.color = new Color(1f, 0.84f, 0.3f);
            _info.text = $"Кодов: {_completed}/{_config.jobTasksPerShift} · ошибок: {_mistakes}" +
                         (all ? $" · бонус +{_config.jobCompletionBonus}" : "") +
                         (all && _mistakes == 0 ? $" · идеально +{_config.jobPerfectBonus}" : "");

            var gm = GameManager.Instance;
            if (_reward > 0)
            {
                gm.Currency.Add(_reward, "job");
                gm.UI.Notify($"Заработано за смену: +{_reward} кр.", NotificationKind.Credits);
            }
            gm.Data.progress.jobsCompleted++;
            gm.SaveGame("job");
        }
    }
}
