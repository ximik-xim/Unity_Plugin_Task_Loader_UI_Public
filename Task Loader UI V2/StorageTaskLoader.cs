using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StorageTaskLoader : MonoBehaviour
{
    public event Action<DKOKeyAndTargetAction> OnAddTask;
    public event Action<DKOKeyAndTargetAction> OnRemoveTask;
    
    public event Action<TypeStatusTaskLoad> OnUpdateGeneralStatuse;
    public event Action<float> OnUpdateGeneralLoadPercentage;

    /// <summary>
    /// была ли инициализация скрипта
    /// </summary>
    public event Action OnInit;
    private bool _isInit = false;
    public bool IsInit => _isInit;
    
    /// <summary>
    /// Происходит ли заргрузка Task
    /// </summary>
    public event Action OnCompleted;
    private bool _isStartLoadTask = false;
    public bool IsStartLoadTask => _isStartLoadTask;
    
    /// <summary>
    /// Происходит ли поиск и выгрузка главных Task сцены 
    /// </summary>
    
    public event Action<bool> OnSceneLoadTask;
    private bool _isSceneLoadTask = false;
    public bool IsSceneLoadTask => _isSceneLoadTask;

    private TypeStatusTaskLoad _currentGeneralStatus = TypeStatusTaskLoad.None;
    
    /// <summary>
    /// Продолжить ли загрузку остальных задач, если хотя бы одна из них вернула FatalError
    /// </summary>
    [SerializeField] 
    private bool _isBreackLoadTaskFatalError = false;

    private SceneStartTask _tlCallBackAddDataTaskScene;
    
    private List<TaskLoaderData> _listTask = new List<TaskLoaderData>();

    private void Awake()
    {
        OnUpdateGeneralStatuse += LoadFinish;

        _isSceneLoadTask = true;
        OnSceneLoadTask?.Invoke(_isSceneLoadTask);
        
        SceneManager.sceneLoaded += FindCallBackAddTask;
        SceneManager.sceneUnloaded += OnEnableFindCallBackAddTask;

        _isInit = true;
        OnInit?.Invoke();
    }

    /// <summary>
    /// Оповещает об начале поиска Task на следующей сцене
    /// </summary>
    /// <param name="arg0"></param>
    private void OnEnableFindCallBackAddTask(Scene arg0)
    {
        _isSceneLoadTask = true;
        OnSceneLoadTask?.Invoke(_isSceneLoadTask);
    }
    
    /// <summary>
    /// Ищет и выгружает Task на сцене
    /// </summary>
    private void FindCallBackAddTask(Scene arg0, LoadSceneMode arg1)
    {
        //КАНЦЕПЦИЯ ПОИСКА ТАСОК ДЛЯ ЗАГРУЗКИ ПРИ ПЕРЕХОДЕ НА НОВУЮ СЦЕНУ НРАВИТЬСЯ
        SceneStartTask callBackAddDataTaskScene = FindObjectOfType<SceneStartTask>();
        _tlCallBackAddDataTaskScene = callBackAddDataTaskScene;
            
        if (callBackAddDataTaskScene != null)
        {
                callBackAddDataTaskScene.SetListTask(this);
                var data= callBackAddDataTaskScene.GetTask();

                foreach (var VARIABLE in data)
                {
                    AddTaskData(VARIABLE);
                }
                
                _isSceneLoadTask = false;
                OnSceneLoadTask?.Invoke(_isSceneLoadTask);

                if (callBackAddDataTaskScene.StartTaskLoadOnInit == true) 
                {
                    StartLoad();
                }
        }
        else
        {
            _isSceneLoadTask = false;
            OnSceneLoadTask?.Invoke(_isSceneLoadTask);
        }
    }
    

    /// <summary>
    /// Очищает список задач после завершения
    /// </summary>
    /// <param name="obj"></param>
    private void LoadFinish(TypeStatusTaskLoad obj)
    {
        if (obj == TypeStatusTaskLoad.Comlite || obj == TypeStatusTaskLoad.FatalError) 
        {
            foreach (var VARIABLE in _listTask)
            {
                VARIABLE.ResetStatusTask();
            }
            
            _isStartLoadTask = false;

            foreach (var VARIABLE in _listTask)
            {
                RemoveSubscription(VARIABLE);
            }

            if (_tlCallBackAddDataTaskScene != null)
            {
                _tlCallBackAddDataTaskScene = null;
            } 
            
            _listTask.Clear();
            OnCompleted?.Invoke();
        }
    }
    

    /// <summary>
    /// Добавит задачу в список
    /// </summary>
    public void AddTaskData(TaskLoaderData taskData)
    {
        if (_isStartLoadTask == false)
        {
            AddSubscription(taskData);
            _listTask.Add(taskData);
            OnAddTask.Invoke(taskData.GetObjectDKO());
        }
        
    }

    /// <summary>
    /// Уберет задачу из список
    /// </summary>
    public void RemoveTaskData(TaskLoaderData taskData)
    {
        if (_isStartLoadTask == false)
        {
            RemoveSubscription(taskData);
            _listTask.Remove(taskData);
            OnRemoveTask.Invoke(taskData.GetObjectDKO());
        }

    }
    
    private void AddSubscription(TaskLoaderData loaderTask)
    {
        loaderTask.OnUpdateStatus += OnGeneralUpdateStatus;
        loaderTask.OnUpdatePercentComplited += OnGeneralUpdateLoadPercentageElementUpdate;
    }
    
    private void RemoveSubscription(TaskLoaderData loaderTask)
    {
        loaderTask.OnUpdateStatus -= OnGeneralUpdateStatus;
        loaderTask.OnUpdatePercentComplited -= OnGeneralUpdateLoadPercentageElementUpdate;
    }
    
    /// <summary>
    /// Обновляет общий статус задач
    /// </summary>
    private void OnGeneralUpdateStatus(TypeStatusTaskLoad status)
    {
        if (status == TypeStatusTaskLoad.FatalError)
        {
            if (_isBreackLoadTaskFatalError == true) 
            {
                foreach (var VARIABLE in _listTask)
                {
                    VARIABLE.BreakTask();
                }
                
                _currentGeneralStatus = TypeStatusTaskLoad.FatalError;
                OnUpdateGeneralStatuse?.Invoke(_currentGeneralStatus);
                return;
            }
            
        }
        
        CheckGeneralUpdateStatus();
    }

    private void CheckGeneralUpdateStatus()
    {
        int countComlite = 0;
        bool isFatalError = false;
        foreach (var VARIABLE in _listTask)
        {
            if (VARIABLE.StatusLoad == TypeStatusTaskLoad.Comlite)
            {
                countComlite++;
                continue;
            }
        }
        
        
        if (countComlite == _listTask.Count)
        {
            _currentGeneralStatus = TypeStatusTaskLoad.Comlite;
            OnUpdateGeneralStatuse?.Invoke(_currentGeneralStatus);
        }
    }

    /// <summary>
    /// Вызывает расчет общего процента выполнения всех задач
    /// </summary>
    /// <param name="keyTask"></param>
    /// <param name="loadPercentage"></param>
    private void OnGeneralUpdateLoadPercentageElementUpdate(float loadPercentage)
    {
        OnGeneralUpdateLoadPercentage();
    }

    /// <summary>
    /// Производит расчет общего процента выполнения всех задач
    /// </summary>
    private void OnGeneralUpdateLoadPercentage()
    {
        float countElement = _listTask.Count;
        float loadGeneralPercentage = 0f;
        foreach (var VARIABLE in _listTask)
        {
            loadGeneralPercentage += VARIABLE.PercentComplited;
        }

        loadGeneralPercentage /= countElement;
        
        OnUpdateGeneralLoadPercentage?.Invoke(loadGeneralPercentage);
    }


    /// <summary>
    /// Запустит загрузку задач
    /// </summary>
    /// <param name="openPanel"></param>
    public void StartLoad()
    {
        if (_isStartLoadTask == false  && _isSceneLoadTask == false)   
        {
            foreach (var VARIABLE in _listTask)
            {
                VARIABLE.StartLogic();
            }

            _isStartLoadTask = true;

            _currentGeneralStatus = TypeStatusTaskLoad.Start;
            OnUpdateGeneralStatuse?.Invoke(TypeStatusTaskLoad.Start);
            
            _currentGeneralStatus = TypeStatusTaskLoad.Load;
            OnUpdateGeneralStatuse?.Invoke(TypeStatusTaskLoad.Load);
            
            OnGeneralUpdateLoadPercentage();
            CheckGeneralUpdateStatus();
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= FindCallBackAddTask;
        SceneManager.sceneUnloaded -= OnEnableFindCallBackAddTask;
    }
}

public enum TypeStatusTaskLoad
{
    None,
    Comlite,
    FatalError,
    Load,
    Start
}