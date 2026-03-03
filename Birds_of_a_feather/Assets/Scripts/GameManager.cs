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

    [Header("Selection")]
    [SerializeField] private GameObject[] playerVariants; // 6 player sprite GOs (children of gameplayContainer)
    public int SelectedIndex { get; private set; } = 0;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        if (_skipStartScreenOnce)
        {
            _skipStartScreenOnce = false;

            startScreen.SetActive(false);
            gameOverScreen.SetActive(false);
            gameplayContainer.SetActive(true);

            ApplyPlayerVariant(); // ensure correct character is active

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

        ApplyPlayerVariant(); // ensure correct character is active

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

    // Hook each UI button to this and pass 0..5
    public void SelectCharacter(int index)
    {
        if (playerVariants == null || playerVariants.Length == 0) return;

        SelectedIndex = Mathf.Clamp(index, 0, playerVariants.Length - 1);

        // If gameplay is already visible, switch immediately
        ApplyPlayerVariant();
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