using UnityEngine;
using UnityEngine.UI;
public class MissionHUD : MonoBehaviour {
    public MissionDemolition game;
    public Text levelText, shotsText, hintText, statusText, viewText, trajectoryText, resultsText;
    public GameObject gameOverPanel;
    public Button viewButton, trajectoryButton, retryButton, nextShotButton, playAgainButton;
    public Image powerFill;
    public Button bigManButton;
    public BigBallIcon bigManIcon;
    public Text bigManKey;
    void Start() {
        viewButton.onClick.AddListener(game.ToggleView);
        if (bigManButton) bigManButton.onClick.AddListener(game.ToggleBigManMode);
        trajectoryButton.onClick.AddListener(game.ToggleTrajectory);
        retryButton.onClick.AddListener(game.RetryLevel);
        nextShotButton.onClick.AddListener(() => { if (game.State == MissionDemolition.GameState.Intro) game.SkipIntro(); else game.Reload(); });
        playAgainButton.onClick.AddListener(game.PlayAgain);
        game.Changed += Refresh;
        Refresh();
    }
    void OnDestroy() { if (game) game.Changed -= Refresh; }
    void Update() {
        powerFill.fillAmount = game.slingshot.PullFraction;
        viewButton.interactable = !game.slingshot.IsDragging && game.State != MissionDemolition.GameState.Intro;
    }
    public void Refresh() {
        if (bigManButton) bigManButton.interactable = game.State != MissionDemolition.GameState.GameOver && game.State != MissionDemolition.GameState.LevelComplete;
        if (bigManIcon) bigManIcon.SetActiveMode(game.BigManMode);
        if (bigManKey) bigManKey.color = game.BigManMode ? new Color(1,.78f,.22f) : Color.white;
        levelText.text = "LEVEL " + (game.LevelIndex + 1) + " / " + game.castles.Length + "   " + game.castles[game.LevelIndex].castleName.ToUpper();
        shotsText.text = "SHOTS  " + game.Shots + "     TOTAL  " + game.TotalShots;
        hintText.text = game.castles[game.LevelIndex].hint;
        viewText.text = game.followCam.Overview ? "Launch view  [V]" : "Whole view  [V]";
        trajectoryText.text = "Aim guide: " + (game.slingshot.ShowTrajectory ? "ON  [T]" : "OFF  [T]");
        nextShotButton.interactable = game.State == MissionDemolition.GameState.Flying || game.State == MissionDemolition.GameState.Intro;
        nextShotButton.GetComponentInChildren<Text>().text = game.State == MissionDemolition.GameState.Intro ? "Skip intro  [Space]" : "Next shot  [Space]";
        retryButton.interactable = game.State != MissionDemolition.GameState.LevelComplete && game.State != MissionDemolition.GameState.GameOver;
        statusText.text = game.State == MissionDemolition.GameState.Intro ? "Scout the castle..." : game.State == MissionDemolition.GameState.LevelComplete ? "GOAL REACHED!  " + game.Medal(game.LevelIndex, game.Shots) : "Drag the ball backward, then release. Reach the green goal!";
        gameOverPanel.SetActive(game.State == MissionDemolition.GameState.GameOver);
        if (game.State == MissionDemolition.GameState.GameOver) {
            resultsText.text = "ALL FOUR CASTLES CLEARED\n\n";
            for (int i = 0; i < game.castles.Length; i++) resultsText.text += game.castles[i].castleName + "   " + game.Results[i] + " shots   " + game.Medal(i, game.Results[i]) + "\n";
            resultsText.text += "\nTotal shots (including retries): " + game.TotalShots;
        }
    }
}
