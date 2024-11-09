using UnityEngine.Events;
using System.Collections.Generic;
using EyE.StateMachine;
namespace EyE.StateMachine.Samples
{
    //sample gameState class- contains data about the state of the game, and has function to load/save/start new
    public class GameStateData
    {
        public bool gameStarted;
        public async void InitForNewGame()
        {
            //await someProcessing()
            gameStarted = true;
        }
        public async void SaveToFile(string filname)
        {
            //await file write stuff...
        }

        public static async System.Threading.Tasks.Task<GameStateData> LoadFromFile(string filname, UnityEvent onComplete)
        {
            GameStateData loadedData = null;
            //stuff..
            //await someProcess()
            onComplete.Invoke();
            return loadedData;
        }
        //other stuff...
    }

    public class MenuState : UnityEventDrivenState
    {
        GameStateData gameData;
        SceneUIObjectReferences sceneObjects;

        public MenuState(GameStateData gameData, SceneUIObjectReferences sceneObjects)
        {
            this.gameData = gameData;
            this.sceneObjects = sceneObjects;

        }
        protected override List<ISubscriber> GetSubscribers()
        {
            return new List<ISubscriber>()
            {
                new UnityEventSubscription(
                    trigger: sceneObjects.menuWindow.newGame.onClick,
                    handler: HandleNewGameClick),
                new UnityEventSubscription(
                    sceneObjects.menuWindow.saveGame.onClick,
                    HandleSaveGameClick),
                new UnityEventSubscription(
                    sceneObjects.menuWindow.quitGame.onClick,
                    HandleQuitGameClick),
            };
        }
        protected override void HandleActivateState()
        {
            sceneObjects.menuWindow.gameObject.SetActive(true);
            sceneObjects.menuWindow.saveGame.interactable = gameData.gameStarted;
        }
        protected override void HandleDeActivateState()
        {
            sceneObjects.menuWindow.gameObject.SetActive(false);
        }
        void HandleNewGameClick()
        {
            UnityEvent initCompleteTrigger = new UnityEvent();
            ChangeState(new WaitScreenForProcess(sceneObjects.waitDisplay, this, initCompleteTrigger));
            System.Threading.Tasks.Task.Run(gameData.InitForNewGame).ContinueWith((o) => { initCompleteTrigger.Invoke(); });
            //run task will not be awaited.  instead the initCompleteTrigger will be invoked when done.
        }
        void HandleSaveGameClick()
        {
            ChangeState(new SaveGameMenuState(gameData, sceneObjects));
        }
        void HandleQuitGameClick()
        {
            UnityEngine.Application.Quit();
        }
    }

    public class SaveGameMenuState : UnityEventDrivenState
    {
        SceneUIObjectReferences sceneObjects;
        GameStateData gameData;

        public SaveGameMenuState(GameStateData gameData, SceneUIObjectReferences sceneObjects)
        {
            this.gameData = gameData;
            this.sceneObjects = sceneObjects;

        }
        protected override List<ISubscriber> GetSubscribers()
        {
            return new List<ISubscriber>()
            {
                new UnityDataEventSubscription<string>(sceneObjects.fileNameWindow.fileNameSelectedEvent, HandleSaveFileString),
                new UnityEventSubscription(sceneObjects.fileNameWindow.cancelEvent,Cancel),
            };
        }
        protected override void HandleActivateState()
        {
            sceneObjects.fileNameWindow.gameObject.SetActive(true);
        }
        protected override void HandleDeActivateState()
        {
            sceneObjects.fileNameWindow.gameObject.SetActive(false);
        }
        void Cancel()
        {
            ChangeState(new MenuState(gameData, sceneObjects)); //return to the main menu state
        }

        void HandleSaveFileString(string filename)
        {
            UnityEvent saveCompleteCallback = new UnityEvent();// this will be passed to the new WaitScreenForProcess state (which will listen to it), and to the SaveToFile function (which will trigger it when done).
            ChangeState(new WaitScreenForProcess(sceneObjects.waitDisplay, new MenuState(gameData, sceneObjects), saveCompleteCallback));
            System.Threading.Tasks.Task
                .Run(() => { gameData.SaveToFile((string)filename); })
                .ContinueWith((o) => { saveCompleteCallback.Invoke(); });

        }
    }

    public class WaitScreenForProcess : UnityRevertibleEventDrivenState
    {

        WaitThingy waitDisplay;
        UnityEvent processIsCompleteTrigger;
        public WaitScreenForProcess(WaitThingy waitDisplay, UnityEventDrivenState stateToChangeToWhenComplete, UnityEvent processIsCompleteTrigger) : base(stateToChangeToWhenComplete)
        {
            this.waitDisplay = waitDisplay;
            this.processIsCompleteTrigger = processIsCompleteTrigger;
        }
        protected override List<ISubscriber> GetSubscribers()
        {
            return new List<ISubscriber>()
            {
                 new UnityEventSubscription(processIsCompleteTrigger,()=>{Revert();}),
            };
        }
        protected override void HandleActivateState()
        {
            waitDisplay.gameObject.SetActive(true);
        }
        protected override void HandleDeActivateState()
        {
            waitDisplay.gameObject.SetActive(false);
        }

    }

    public class UserConfirm : UnityEventDrivenState
    {
        YesNoWindow yesnoDisplay;
        UnityAction yesHandler;
        UnityAction noHandler;
        public UserConfirm(YesNoWindow yesnoDisplay, UnityAction yesHandler, UnityAction noHandler)
        {
            this.yesnoDisplay = yesnoDisplay;
            this.yesHandler = yesHandler;
            this.noHandler = noHandler;
        }
        protected override List<ISubscriber> GetSubscribers()
        {
            return new List<ISubscriber>()
            {
                new UnityEventSubscription(yesnoDisplay.yesEvent,yesHandler),
                new UnityEventSubscription(yesnoDisplay.noEvent, noHandler),
            };
        }
        protected override void HandleActivateState()
        {
            yesnoDisplay.gameObject.SetActive(true);
        }
        protected override void HandleDeActivateState()
        {
            yesnoDisplay.gameObject.SetActive(false);
        }
    }

    // below are example UI component classes that are used by the example states above
    public class SelectFileNameWindow : UnityEngine.MonoBehaviour
    {
        public UnityEvent<string> fileNameSelectedEvent;
        public UnityEvent cancelEvent;
        //UI stuff...
    }
    public class WaitThingy : UnityEngine.MonoBehaviour
    {
        //UI stuff..
    }
    public class YesNoWindow : UnityEngine.MonoBehaviour
    {
        public UnityEvent yesEvent;
        public UnityEvent noEvent;
        //UI stuff..
    }
    public class MainMenuWindow : UnityEngine.MonoBehaviour
    {
        public UnityEngine.UI.Button newGame;
        public UnityEngine.UI.Button saveGame;
        public UnityEngine.UI.Button quitGame;
    }

    //object that will hold references to all the various UI components needed by the state machine
    public class SceneUIObjectReferences : UnityEngine.MonoBehaviour
    {
        public MainMenuWindow menuWindow;
        public SelectFileNameWindow fileNameWindow;
        public WaitThingy waitDisplay;
    }
}