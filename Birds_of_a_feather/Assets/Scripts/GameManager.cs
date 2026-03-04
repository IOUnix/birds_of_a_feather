using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;


public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [SerializeField] private GameObject _gameOverCanvas;
    [SerializeField] private GameObject startScreen;
    [SerializeField] private GameObject gameplayContainer;
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private GameObject playerSelection;
    [SerializeField] private GameObject exitScreen;

    private static bool _skipStartScreenOnce = false;

    // Persist selection across scene reloads
    private static int _selectedIndexPersist = 0;

    [Header("Selection")]
    [SerializeField] private GameObject[] playerVariants; // 6 player sprite GOs
    public int SelectedIndex { get; private set; } = 0;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        // Restore persisted selection on new scene load
        SelectedIndex = _selectedIndexPersist;
    }

    private void Start()
    {
        // Default visibility
        if (playerSelection != null) playerSelection.SetActive(false);
        if (gameplayContainer != null) gameplayContainer.SetActive(false);
        if (startScreen != null) startScreen.SetActive(true);

        // Ensure correct character is active immediately (even before Play)
        ApplyPlayerVariant();

        if (_skipStartScreenOnce)
        {
            _skipStartScreenOnce = false;

            if (startScreen != null) startScreen.SetActive(false);
            if (gameOverScreen != null) gameOverScreen.SetActive(false);
            if (_gameOverCanvas != null) _gameOverCanvas.SetActive(false);

            if (gameplayContainer != null) gameplayContainer.SetActive(true);

            Time.timeScale = 1f;
        }
        else
        {
            Time.timeScale = 0f;
            if (startScreen != null) startScreen.SetActive(true);
            if (gameplayContainer != null) gameplayContainer.SetActive(false);
        }
    }

    public void CharacterBoard()
    {
        // Show selection board from start screen
        if (startScreen != null) startScreen.SetActive(false);
        if (gameOverScreen != null) gameOverScreen.SetActive(false);
        if (_gameOverCanvas != null) _gameOverCanvas.SetActive(false);

        if (playerSelection != null) playerSelection.SetActive(true);

        Time.timeScale = 0f;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (exitScreen.activeSelf)
            {
                UnityEngine.Debug.Log("Back/Escape pressed, quitting...");
                if (playerSelection != null) playerSelection.SetActive(false);
                if (startScreen != null) startScreen.SetActive(false);
                if (gameOverScreen != null) gameOverScreen.SetActive(true);
                if (exitScreen != null) exitScreen.SetActive(false);

            }
            else
            {
                UnityEngine.Debug.Log("Back/Escape pressed, quitting...");
                if (playerSelection != null) playerSelection.SetActive(false);
                if (startScreen != null) startScreen.SetActive(false);
                if (gameOverScreen != null) gameOverScreen.SetActive(false);
                if (exitScreen != null) exitScreen.SetActive(true);
            }
                

        }
    }
    public void Quit()
    {
        UnityEditor.EditorApplication.isPlaying = false;
        UnityEngine.Application.Quit();
    }

    public void Play()
    {
        if (startScreen != null) startScreen.SetActive(false);
        if (gameOverScreen != null) gameOverScreen.SetActive(false);
        if (_gameOverCanvas != null) _gameOverCanvas.SetActive(false);

        if (playerSelection != null) playerSelection.SetActive(false);
        if (gameplayContainer != null) gameplayContainer.SetActive(true);

        ApplyPlayerVariant();

        Time.timeScale = 1f;
    }

    public void GameOver()
    {
        if (gameOverScreen != null) gameOverScreen.SetActive(true);
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        _skipStartScreenOnce = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void SelectCharacter(int index)
    {
        if (playerVariants == null || playerVariants.Length == 0) return;

        SelectedIndex = Mathf.Clamp(index, 0, playerVariants.Length - 1);
        _selectedIndexPersist = SelectedIndex;

        // Hide selection UI immediately
        if (playerSelection != null) playerSelection.SetActive(false);

        // Restart so pipes/tokens reset too
        RestartGame();
    }

    private void ApplyPlayerVariant()
    {
        if (playerVariants == null || playerVariants.Length == 0) return;

        for (int i = 0; i < playerVariants.Length; i++)
        {
            if (playerVariants[i] != null)
                playerVariants[i].SetActive(i == SelectedIndex);
        }
    }
}