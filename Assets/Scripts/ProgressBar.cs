using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    public FeedUIManager feed;
    public Slider progressSlider;
    public int progressPerFullFeed = 10;
    public int progressToLevel = 100;

    private int currentProgress = 0;
    private bool wasFull = false;

    void Update() {
        if (!feed) return;

        if (feed.IsFull() && !wasFull) {
            wasFull = true;
            GainProgress();
        } else if (!feed.IsFull()) {
            wasFull = false;
        }

        if (progressSlider)
            progressSlider.value = Mathf.Clamp01((float)currentProgress / progressToLevel);
    }

    void GainProgress() {
        currentProgress += progressPerFullFeed;
        Debug.Log("Progress gained! Current Progress = " + currentProgress);
    }
}
