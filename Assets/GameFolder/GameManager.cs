using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameManager : MonoBehaviour //GameManager 클래스 생성
{
    public enum ScreenRole { Start, Loading } //두개의 씬을 관리하기 위해 enum을 사용해 스크린의 역할을 정의했습니다.

    [Header("Scene Flow")]
    [SerializeField] private ScreenRole screenRole; //SerializdField를 사용하여, unity inspector에서 screenRole을 설정할 수 있게 만들었음.
    [SerializeField] private string loadingScene = "LoadingScene";
    [SerializeField] private string mainGameScene = "MainGameScene";

    [Header("Loading Screen")]
    [SerializeField] private Slider loadingBar; //로딩바를 위한 변수 만들기
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField, Min(0f)] private float minimumLoadingSeconds = 1f; //최소시간, 화면이 너무 빨리 바뀌는 것을 방지함

    private bool isTransitioning; //화면이 바뀌고 있는 중인지 아닌지 확인하기 위한 변수임.(중독 방지용)

    private void Start()
    {
        if (screenRole == ScreenRole.Loading) //Loading 화면일 경우, isTransitioning을 true로 설정하고, LoadMainGame 코루틴을 시작함(코루틴은 로딩하면을 먼저 불러오고 비동기 로딩을 진행하기 위해 사용함)
        {
            isTransitioning = true;
            StartCoroutine(LoadMainGame());
        }
    }

    private void Update()
    {
        if (screenRole != ScreenRole.Start || isTransitioning) return;
        bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame; //키보드 연결을 확인하고, 키보드 spacekey가 눌렸는지 확인함.(&& = and 연산자)
        bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame; //똑같은 논리로 마우스 왼쪽 버튼이 눌렸는지 확인함
        if (spacePressed || mouseClicked) StartGame(); //둘 중 하나가 눌렸으면 startgame() 함수 호출(게임 시작)
    }

    // Can also be connected to a UI Button's OnClick event.
    public void StartGame()
    {
        if (screenRole != ScreenRole.Start || isTransitioning) return;
        if (!CanLoadScene(loadingScene) || !CanLoadScene(mainGameScene)) return; //loadinscene을 불러올 수 없거나, maingamescene을 불러올 수 없으면 코드가 실행되지 않음
        isTransitioning = true; //화면이 바뀌고 있는 중임을 확인하기 위해 isTransitioning을 true로 설정함
        SceneManager.LoadSceneAsync(loadingScene, LoadSceneMode.Single); //비동기적으로 loadingScene을 불러옴. LoadSceneMode.Single은 현재 '씬'을 제거하고 다른 씬으로 대체하는 것을 의미함. 즉, 현재 씬을 제거하고 loadingScene으로 바꾸는 것임
    }

    private IEnumerator LoadMainGame()
    {
        SetProgress(0f); //로딩바를 0으로 초기화함
        yield return null; //코루틴이 한 프레임 동안 기다리도록 함(loading bar가 0으로 초기화되고, 로딩씬으로 넘어가는 과정을 먼저 보여주기 위해 사용함)
        if (!CanLoadScene(mainGameScene)) //화면을 바꾸기 전에 mainGameScene을 불러올 수 있는지 확인함. 불러올 수 없으면, loadingText에 "Unable to load game scene."을 표시하고 코루틴을 종료함
        {
            if (loadingText != null) loadingText.text = "Unable to load game scene.";
            yield break;
        }

        float startedAt = Time.realtimeSinceStartup; //로딩 시간을 측정을 하기 위해서 실제 시간을 저장함.
        AsyncOperation operation = SceneManager.LoadSceneAsync(mainGameScene, LoadSceneMode.Single); //비동기적으로 mainGameScene을 불러옴. LoadSceneMode.Single은 현재 '씬'을 제거하고 다른 씬으로 대체하는 것을 의미함. 즉, 현재 씬을 제거하고 mainGameScene으로 바꾸는 것임
        if (operation == null)
        {
            if (loadingText != null) loadingText.text = "Unable to load game scene.";
            yield break;
        }
        operation.allowSceneActivation = false; //씬이 완전히 로드되기 전까지는 씬 전환을 허용하지 않음. 즉, 로딩바가 100%가 되더라도 바로 씬이 바뀌지 않도록 함
        while (operation.progress < 0.9f || Time.realtimeSinceStartup - startedAt < minimumLoadingSeconds)
        {
            SetProgress(Mathf.Clamp01(operation.progress / 0.9f));
            yield return null;
        }
        SetProgress(1f);
        yield return null;
        operation.allowSceneActivation = true;
    }

    private bool CanLoadScene(string sceneName)
    {
        if (!string.IsNullOrWhiteSpace(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName))
            return true;
        Debug.LogError($"Scene '{sceneName}' is missing from the enabled Build Settings scenes.", this);
        return false;
    }

    private void SetProgress(float progress)
    {
        if (loadingBar != null) loadingBar.normalizedValue = progress;
        if (loadingText != null) loadingText.text = $"Loading... {Mathf.RoundToInt(progress * 100f)}%";
    }
}
