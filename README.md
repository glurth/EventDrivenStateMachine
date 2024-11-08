# EventDrivenStateMachine

This package provides a simple, event-driven state machine implementation for Unity. It allows you to manage different states and switch between them based on events, perfect for implementing game behaviors like character states (Idle, Walking, Running, etc.).

## Installation

You can install this package in Unity via GitHub using the Unity Package Manager.

1. Open Unity and navigate to the **Package Manager** (Window > Package Manager).
2. In the Package Manager window, click on the `+` button in the top-left corner.
3. Select **Add package from Git URL...**.
4. Paste the following GitHub URL into the dialog:
   https://github.com/glurth/EventDrivenStateMachine.git

5. Click **Add**. The package will be installed into your Unity project.

## Features

- Easily define and manage states.
- Switch between states with custom actions.
- Execute state-specific behavior by invoking state actions.
- Simple interface for adding and switching states.

## Example Usage

### Setting up a State Machine

You can define a state machine that controls the behavior of an object with the following example.

```
    public class MenuState : UnityEventDrivenState
    {
        GameStateData gameData;
        SceneUIObjects sceneObjects;

        public MenuState(GameStateData gameData, SceneUIObjects sceneObjects)
        {
            this.gameData = gameData;
            this.sceneObjects = sceneObjects;

        }
        protected override List<UnityEventSubscription> GetSubscribers()
        {
            return new List<UnityEventSubscription>()
            {
                new UnityEventSubscription(sceneObjects.menuWindow.newGame.onClick,HandleNewGameClick),
                new UnityEventSubscription(sceneObjects.menuWindow.saveGame.onClick,HandleSaveGameClick),
                new UnityEventSubscription(sceneObjects.menuWindow.quitGame.onClick,HandleQuitGameClick),
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
            ChangeState(new WaitScreenForProcess(sceneObjects.waitDisplay,this, initCompleteTrigger));
            System.Threading.Tasks.Task.Run(gameData.InitForNewGame).ContinueWith((o)=> { initCompleteTrigger.Invoke(); });
            //will not be awaited.  instead the initCompleteTrigger will be invoked when done.
        }
        void HandleSaveGameClick()
        {
            ChangeState(new SaveGameMenuState(gameData, sceneObjects));
        }
        void HandleQuitGameClick()
        {
            UnityEngine.Application.Quit();
        }
    }``

    public class WaitScreenForProcess : UnityRevertibleEventDrivenState
    {

        WaitThingy waitDisplay;
        UnityEvent processIsCompleteTrigger;
        public WaitScreenForProcess(WaitThingy waitDisplay, UnityEventDrivenState stateToChangeToWhenComplete, UnityEvent processIsCompleteTrigger):base(stateToChangeToWhenComplete)
        {
            this.waitDisplay = waitDisplay;
            this.processIsCompleteTrigger = processIsCompleteTrigger;
        }
        protected override List<UnityEventSubscription> GetSubscribers()
        {
            return new List<UnityEventSubscription>()
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
```

## Contributing

Feel free to fork this repository, submit pull requests, or open issues. Contributions are welcome!

## License

No Lincense without written permission, usually given.
