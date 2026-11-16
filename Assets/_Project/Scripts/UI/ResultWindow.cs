using SwordArt.Tournament;
using UnityEngine;
using UnityEngine.UI;

namespace SwordArt.UI
{
    /// <summary>Victory / defeat screen with placement, kills and reward breakdown.</summary>
    public sealed class ResultWindow : UIWindow
    {
        private Text _title;
        private Text _subtitle;
        private Text _details;
        private TournamentManager _tournament;

        public override bool CloseOnEscape => false;

        /// <summary>Shows <paramref name="result"/>.</summary>
        public void Show(MatchResult result, TournamentManager tournament)
        {
            _tournament = tournament;
            Open();
            var t = Theme;
            _title.text = result.Won ? "ПОБЕДА" : "ПОРАЖЕНИЕ";
            _title.color = result.Won ? t.Accent : t.Negative;
            _subtitle.text = result.Won ? "Вы — чемпион Bullet of Bullets!"
                : !string.IsNullOrEmpty(result.KillerName) ? $"Место #{result.Placement} из {result.Participants} · вас устранил: {result.KillerName}"
                : $"Место #{result.Placement} из {result.Participants} · победитель: {result.WinnerName}";
            string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);
            _details.text =
                $"Место: <b>#{result.Placement}</b> из {result.Participants}\n" +
                $"Убийств: <b>{result.Kills}</b>\n" +
                $"Время матча: {(int)(result.Duration / 60):00}:{(int)(result.Duration % 60):00}\n\n" +
                $"Участие: +{result.ParticipationReward} кр.\n" +
                $"Убийства: +{result.KillReward} кр.\n" +
                $"{(result.Won ? "Победа" : "Призовое место")}: +{result.PlacementReward} кр.\n" +
                $"<color=#{Hex(new Color(1f, 0.84f, 0.3f))}><b>ИТОГО: +{result.Total} кр.</b></color>";
        }

        protected override void Build(UITheme theme)
        {
            var panel = CreateDialogFrame(theme, new Vector2(760, 680), null);
            _title = UIKit.Text(panel, "Title", "", 84, theme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            _title.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(0, 110));
            _subtitle = UIKit.Text(panel, "Subtitle", "", 24, theme.TextDim, TextAnchor.MiddleCenter);
            _subtitle.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -140), new Vector2(-60, 40));
            _details = UIKit.Text(panel, "Details", "", 26, theme.Text, TextAnchor.UpperCenter);
            _details.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(-80, -340));

            var lobby = UIKit.Button(panel, "В лобби (новый матч)", theme, () => { Close(); _tournament.ReturnToLobby(); });
            ((RectTransform)lobby.transform).Place(new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 34), new Vector2(-50, 60));
            var exit = UIKit.Button(panel, "Выйти из GGO", theme, () => { Close(); _tournament.Logout(); });
            ((RectTransform)exit.transform).Place(new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 34), new Vector2(-50, 60));
        }
    }
}
