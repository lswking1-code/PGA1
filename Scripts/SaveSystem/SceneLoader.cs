using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour, ISaveable
{
    public Transform playerTrans;
    public Vector3 firstPosition;
    public Vector3 menuPosition;

    [Header("EventListeners")]
    public SceneLoadEventSO loadEventSO;
    public VoidEventSO newGameEvent;
    public VoidEventSO backToMenuEvent;

    [Header("EventRaise")]
    public VoidEventSO afterSceneLoadedEvent;
    public FadeEventSO fadeEvent;
    public SceneLoadEventSO unloadedSceneEvent;
 

    [Header("Scene")]
    public GameSceneSO firstLoadScene;
    public GameSceneSO menuScene;
    private GameSceneSO currentLoadedScene;
    private GameSceneSO sceneToLoad;
    private Vector3 positionToGo;
    private bool fadeScreen;
    private bool isLoading;
    public float fadeDuration;

    private void Awake()
    {
        // Addressables.LoadSceneAsync(firstLoadScene.sceneReference, LoadSceneMode.Additive);
        // currentLoadedScene = firstLoadScene;
        // currentLoadedScene.sceneReference.LoadSceneAsync(LoadSceneMode.Additive);
    }

    //TODO:做完MainMenu之后更改
    private void Start()
    {
        loadEventSO.RaiseLoadRequestEvent(menuScene, menuPosition, true);
        // NewGame();
    }

    private void OnEnable()
    {
            loadEventSO.LoadRequestEvent += OnLoadRequestEvent;
            newGameEvent.OnEventRaised += NewGame;
            backToMenuEvent.OnEventRaised += OnBackToMenuEvent;

        ISaveable saveable = this;
        saveable.RegisterSaveData();
    }

    private void OnDisable()
    {
            loadEventSO.LoadRequestEvent -= OnLoadRequestEvent;
            newGameEvent.OnEventRaised -= NewGame;
            backToMenuEvent.OnEventRaised -= OnBackToMenuEvent;

        ISaveable saveable = this;
        saveable.UnregisterSaveData();
    }

    private void OnBackToMenuEvent()
    {
        sceneToLoad = menuScene;
        loadEventSO.RaiseLoadRequestEvent(sceneToLoad, menuPosition, true);
    }

    private void NewGame()
    {
        sceneToLoad = firstLoadScene;
        // OnLoadRequestEvent(sceneToLoad, firstPosition, true);
        loadEventSO.RaiseLoadRequestEvent(sceneToLoad, firstPosition, true);
    }

    /// <summary>
    /// 场景加载事件请求
    /// </summary>
    /// <param name="locationToLoad"></param>
    /// <param name="posToGo"></param>
    /// <param name="fadeScreen"></param>
    private void OnLoadRequestEvent(GameSceneSO locationToLoad, Vector3 posToGo, bool fadeScreen)
    {
        if (isLoading)
            return;

        isLoading = true;
        sceneToLoad = locationToLoad;
        positionToGo = posToGo;
        this.fadeScreen = fadeScreen;
        StartCoroutine(UnLoadPreviousScene());
    }

    private IEnumerator UnLoadPreviousScene()
    {
        if (fadeScreen)
        {
            //TODO:变黑
            fadeEvent.FadeIn(fadeDuration);
        }

        yield return new WaitForSeconds(fadeDuration);

        //广播事件调整血条显示
        unloadedSceneEvent.RaiseLoadRequestEvent(sceneToLoad, positionToGo, true);

        // 检查是否有已加载的场景需要卸载
        if (currentLoadedScene != null && currentLoadedScene.sceneReference != null)
        {
            yield return currentLoadedScene.sceneReference.UnLoadScene();
            Debug.Log("UnLoaded Previous Scene");
        }
        
        //关闭人物
        playerTrans.gameObject.SetActive(false);

        //加载新场景
        LoadNewScene();
    }

    private void LoadNewScene()
    {
        var loadingOption = sceneToLoad.sceneReference.LoadSceneAsync(LoadSceneMode.Additive, true);
        Debug.Log("Loading New Scene");
        loadingOption.Completed += OnLoadCompleted;
    }

    /// <summary>
    /// 场景加载完成后
    /// </summary>
    /// <param name="obj"></param>
    private void OnLoadCompleted(AsyncOperationHandle<SceneInstance> obj)
    {
        currentLoadedScene = sceneToLoad;

        playerTrans.position = positionToGo;
        playerTrans.gameObject.SetActive(true);

        if (fadeScreen)
        {
            //TODO:
            fadeEvent.FadeOut(fadeDuration);
        }

        isLoading = false;

        if (currentLoadedScene.sceneType == SceneType.Loaction)
            //场景加载完成后事件
            afterSceneLoadedEvent.RaiseEvent();
    }

    public DataDefination GetDataID()
    {
        return GetComponent<DataDefination>();
    }

    public void GetSaveData(Data data)
    {
        data.SaveGameScene(currentLoadedScene);
    }

    public void LoadSaveData(Data data)
    {
        var playerID = playerTrans.GetComponent<DataDefination>();
        if (data.characterPosDict.ContainsKey(playerID.ID))
        {
            positionToGo = data.characterPosDict[playerID.ID].ToVector3();
            sceneToLoad = data.GetSavedScene();

            OnLoadRequestEvent(sceneToLoad, positionToGo, true);
        }
    }
}
