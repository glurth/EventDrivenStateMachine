# EventDrivenStateMachine

This package provides a simple, event-driven state machine implementation for Unity. It allows you to manage different states and switch between them based on events.

All state classes inherit from `EventDrivenState<TEventProviderType>`, providing a simple event-driven and polymorphic approach to state management.
State changes occur in response to events, which will internally call the two abstract functions (subscribe/unsubscribe) defined by the descendant/user of this class.

Extensions of this base class allow for "layered" states, which can be used to keep several states active at the same time.  In addition, a "Revertable" state class can be used which  
has the ability to record and restore the previous state.
 

## Installation

You can install this package in Unity via GitHub using the Unity Package Manager.

1. Open Unity and navigate to the **Package Manager** (Window > Package Manager).
2. In the Package Manager window, click on the `+` button in the top-left corner.
3. Select **Add package from Git URL...**.
4. Paste the following GitHub URL into the dialog:
   https://github.com/glurth/EventDrivenStateMachine.git
5. Click **Add**. The package will be installed into your Unity project.

## Features

- Easily define states: which events they listen to, and what actions to take when those events are triggered.
- LayeredStates can be applied to any existing state, adding to the events being listened/responded to. These layers can be terminated individually, and will be activated/deactivated  
will the state they are applied to.
- Revertible States allow the user to specify an existing state, that will be automatically activated when the revertible state is ready to "revert".  This feature can be use used to create a "stack" of states.

## Usage

The user will derive classes from the abstract state classes in this package.   Each state class defined by the user specifies the events it listens to, and how it reacts to each one.  The source of the "driving" events, usually a scene object, must be passed to the constructor of every state. 
The events and what function to call when they are triggered is specified by the ISubscriber list return by the user-defined abstract function `GetSubscribers()`
The *current* state is defined by the events are currently subscribed to, rather than being explicitly stored- changing event subscriptions (and reactions) is all that changing state really does. A static function (`ActivateRootState`) is provided to create the initial state.
State transitions are accomplished by calling the `ChangeState` method, and passing in a new instance of the desired state class (which will, in turn, require an instance of its TEventProviderType).  State changes are controlled within each state class in response to events, as defined by the user, allowing for dynamic transitions based on events.
Normal `ChangeState` calls will deactivate (unsubscribe) the current state, and activate the new state.  Optionally, one may instead use `LayerNewState` (if the new state is derived  
from `EventDrivenStateLayer`), in which case the current state will not be deactivated, and the new state will be activated.


### Unity
Unity specific variants of the base classes use a concrete class to implement ``ISubscriber`, called ``UnityEventSubscription``.  
Non-generic, but still abstract classes using this subscriber exist for each base class yielding: 
- `UnityEventDrivenState` 
- `UnityEventDrivenStateLayer`
- `UnityRevertibleEventDrivenState` 

### Important condiserations
- The subscribe/unsubscribe functions can implement polymorphic behavior, but the hierarchy must use the same TEventProviderType to do this. 
- It is recommended that state DATA, particularly data that will need to be saved, be stored in a different class as serialization of states is not handled.  User Defined classes can  
add a parameter for State Data to their constructor, if desired.
- One can derive from any existing state, to ADD to the set of events and responses provided by overriding `GetSubscribers()`, starting the returned list with `base.GetSubscribers()`'. (This would, effectively, function like a single layered state)

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

No License without written permission, usually given.
