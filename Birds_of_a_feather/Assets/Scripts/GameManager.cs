using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [SerializeField] private GameObject _gameOverCanvas;
    [SerializeField] private GameObject startScreen;
    [SerializeField] private GameObject gameplayContainer;
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private GameObject playerSelection;

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
        playerSelection.SetActive(false);
        gameplayContainer.SetActive(false);
        startScreen.SetActive(true);

        if (_skipStartScreenOnce)
        {
            _skipStartScreenOnce = false;

            startScreen.SetActive(false);
            gameOverScreen.SetActive(false);
            gameplayContainer.SetActive(true);

            ApplyPlayerVariant();
            Time.timeScale = 1f;
        }
        else
        {
            Time.timeScale = 0f;
            startScreen.SetActive(true);
            gameplayContainer.SetActive(false);
        }
    }

    public void CharacterBoard()
    {
        startScreen.SetActive(false);
        playerSelection.SetActive(true);
    }

    public void Play()
    {
        startScreen.SetActive(false);
        gameOverScreen.SetActive(false);
        gameplayContainer.SetActive(true);

        ApplyPlayerVariant();
        Time.timeScale = 1f;
    }

    public void GameOver()
    {
        gameOverScreen.SetActive(true);
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
        _selectedIndexPersist = SelectedIndex; // <-- this is the key line

        RestartGame();
        ApplyPlayerVariant();

        playerSelection.SetActive(false);
    }

    private void ApplyPlayerVariant()
    {
        if (playerVariants == null || playerVariants.Length == 0) return;

        for (int i = 0; i < playerVariants.Length; i++)
            if (playerVariants[i] != null)
                playerVariants[i].SetActive(i == SelectedIndex);
    }
}