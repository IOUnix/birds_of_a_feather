using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [SerializeField] private GameObject _gameOverCanvas;
    [SerializeField] private GameObject startScreen;
    [SerializeField] private GameObject gameplayContainer;
    [SerializeField] private GameObject gameOverScreen;

    private static bool _skipStartScreenOnce = false;

    private void Start()
    {
        if (_skipStartScreenOnce)
        {
            _skipStartScreenOnce = false;

            startScreen.SetActive(false);
            gameOverScreen.SetActive(false);
            gameplayContainer.SetActive(true);

            Time.timeScale = 1f;
        }
        else
        {
            Time.timeScale = 0f;
            startScreen.SetActive(true);
            gameplayContainer.SetActive(false);
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public void Play()
    {
        startScreen.SetActive(false);
        gameOverScreen.SetActive(false);
        gameplayContainer.SetActive(true);
        Time.timeScale = 1f;
    }

    public void GameOver()
    {
        _gameOverCanvas.SetActive(true);

        Time.timeScale = 0f;
    }
    public void RestartGame()
    {
        _skipStartScreenOnce = true;
        Time.timeScale = 1f; // important
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
