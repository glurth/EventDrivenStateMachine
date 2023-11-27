using UnityEngine.Events;
using System.Collections.Generic;

namespace EyE.StateMachine
{
    /// <summary>
    /// The `VariableEventDrivenState` class and its descendants are used to represent different states in a system.
    /// All state classes inherit from `VariableEventDrivenState<TEventProviderType>`, providing a simple event-driven and polymorphic approach to state management.
    /// State changes occur in response to events, which will internally call the two abstract functions (subscribe/unsubscribe) defined by the descendant/user of this class.
    ///
    /// The source of the "driving" events, usually a scene object, must be passed to the constructor of every state.
    /// Each state class defined by the user specifies the events it listens to, and how it reacts to each one.
    /// State transitions are accomplished by calling the `ChangeState` method, and passing in a new instance of the desired state class (which will, in turn, require an instance of its TEventProviderType).
    /// The current state is defined by the events to which the state is subscribed, rather than being explicitly stored- this is all ChangeState really does.
    /// State changes are controlled within each state class in response to events, as defined by the user, allowing for dynamic transitions based on events.
    /// The subscribe/unsubscribe functions can implement polymorphic behavior, but the hierarchy must use the same TEventProviderType to do this. 
    /// 
    /// It is recommended that state DATA, particularly data that will need to be saved, be stored in a different class as serialization of states is not handled.
    /// User Defined classes can add a parameter for State Data to their constructor, if desired.
    /// 
    /// The `eventSource` member, which must be passed to the class constructor, provides access to all available events that the state can subscribe to.
    /// Users should implement the `SubscribeToEvents` function, and in there, subscribe to events with the appropriate, user-defined, handler functions.
    /// Cleanup, in the form of unsubscribing, is done in the `UnSubscribeToEvents` function.
    /// These two functions (subscribe/unsubscribe) are automatically called when a state is changed but must be defined by the descendant/user of this class.
    /// 
    /// Descendants will define functions as listeners for specific events found in `eventSource`.
    /// These functions handle the actions triggered by events, which may include changing the state.
    /// Users may choose to modify scene/state values inside this class when events are triggered or implement `IHandleStateChangeEvents` on the `eventSource`.
    /// The purpose of IHandleStateChangeEvents is to allow the event source object to react when the state changes.
    /// </summary>

    // Define the internal Subscribe interface- this interface exists so that VariableEventDrivenStateBase can define SubscribeToEvents as protected, and still allow it to be invokable from inside VariableEventDrivenState<TEventProviderType>
    internal interface ISubscribeToEvents
    {
        void Subscribe();
    }

    /// <typeparam name="TUiEventProviderType"></typeparam>
    public interface IHandleStateChangeEvents
    {
        //        public void HandleUIStateChanged(VariableEventDrivenStateBase newState);
        public void HandleUIStateChanged<TStateType>(TStateType newState) where TStateType : VariableEventDrivenStateBase;

    }

    /// <summary>
    /// The base class for variable event-driven states, implementing the `ISubscribeToEvents` interface.
    /// Descendants must override `SubscribeToEvents` and `UnSubscribeToEvents` functions.
    /// </summary>
    abstract public class VariableEventDrivenStateBase : ISubscribeToEvents
    {
        // Explicitly implement the internal interface
        void ISubscribeToEvents.Subscribe()
        {
            SubscribeToEvents();
        }

        /// <summary>
        /// Override this function to subscribe to events when the state is entered.
        /// </summary>
        abstract protected void SubscribeToEvents();

        /// <summary>
        /// Override this function to unsubscribe from events when the state is exited.
        /// </summary>
        abstract protected void UnSubscribeToEvents();


    }

    /// <summary>
    /// A generic implementation of a variable event-driven state.
    /// </summary>
    /// <typeparam name="TEventProviderType">The type of the event provider/source.</typeparam>
    abstract public class VariableEventDrivenState<TEventProviderType> : VariableEventDrivenStateBase
    {
        /// <summary>
        /// The event provider/source for this state.
        /// </summary>
        protected TEventProviderType eventSource { get; private set; }

        /// <summary>
        /// Initializes a new instance of the `VariableEventDrivenState` class with the specified event source.
        /// Does NOT subscribe to events in here.  This is only done when the state is changed, or when the static CreasteRootState function is invoked.
        /// </summary>
        /// <param name="eventSource">The event provider/source for this state.</param>
        public VariableEventDrivenState(TEventProviderType eventSource)
        {
            this.eventSource = eventSource;
        }
        private VariableEventDrivenState() { }
        public static TStateType CreateRootState<TStateType>(TEventProviderType eventSource) where TStateType : VariableEventDrivenState<TEventProviderType>, new()
        {
            TStateType newRoot = new TStateType();
            newRoot.eventSource = eventSource;
            newRoot.SubscribeToEvents();
            return newRoot;
        }

        /// <summary>
        /// Changes the current state to a new state.
        /// </summary>
        /// <param name="newState">The new state to transition to.</param>
        public void ChangeState(VariableEventDrivenStateBase newState)
        {
            UnSubscribeToEvents();

            if (newState is ISubscribeToEvents subscribeable)
                subscribeable.Subscribe();

            if (eventSource is IHandleStateChangeEvents eventMonitor)
                eventMonitor.HandleUIStateChanged(newState);
        }
    }

    //ALTERNATIVE version-
    ////rather than the user definging the subscribe/unsubscribe functions with a bunch of lines that all say ``event.AddListener(handler)``, a list of pair/handlers is defined by the user, and adding/remving listeners to subscribe/unsubscibe is done automatically.

    //class used to store a 
    public class EventHandlerPair
    {
        //basic trigger and handler pair- no data is passed
        public UnityEngine.Events.UnityEvent trigger;
        public UnityEngine.Events.UnityAction handler;
        //trigger and handler pair that allow an object to be passed as data
        public UnityEngine.Events.UnityEvent<object> triggerWithData;
        public UnityEngine.Events.UnityAction<object> handlerWithData;

        public EventHandlerPair(UnityEvent trigger, UnityAction handler)
        {
            this.trigger = trigger;
            this.handler = handler;
        }
        public EventHandlerPair(UnityEvent<object> trigger, UnityAction<object> handler)
        {
            this.triggerWithData = trigger;
            this.handlerWithData = handler;
        }
        public void Subscribe()
        {
            if (trigger != null)
                trigger.AddListener(handler);
            if (triggerWithData != null)
                triggerWithData.AddListener(handlerWithData);
        }
        public void Unsubscribe()
        {
            if (trigger != null)
                trigger.RemoveListener(handler);
            if (triggerWithData != null)
                triggerWithData.RemoveListener(handlerWithData);
        }
    }


    /// <summary>
    /// This abstract class handles subscribing and unsubscribing to a list of UnityEvents, each with a particular UnityAction handler.
    /// </summary>
    abstract public class EventAndHandlerSubscriber
    {
        // this member's GET accessor must be overridden by a concrete descendant classes.
        // It should return a non-changing list of EventHandlerPair's - each of which will provide a UnityEvent to subscribe to, and a UnityAction to run when the event triggers.
        abstract protected List<EventHandlerPair> eventsAndHandlers { get; }


        protected void SubscribeToEvents()
        {
            foreach (EventHandlerPair pair in eventsAndHandlers)
                pair.Subscribe();
        }
        protected void UnSubscribeFromEvents()
        {
            foreach (EventHandlerPair pair in eventsAndHandlers)
                pair.Unsubscribe();
        }
    }

    /// <summary>
    /// Derive from this class to create a "State" in the statemachine.  All the different, derived variants define the different states of the statemachine.
    /// Override the eventsAndHandlers get accessor to define what events to listen to, and what action to take when they are triggered, for a given state.
    /// It is expected that derived variants will define a constructor with a(some) parameter(s) that will provide access to scene objects that will generate the events it needs to subscribe to.
    /// When a given state is activated, it's events are subscribed to, while the now-previous state's events are unsubscribed from.
    /// The current state is defined by which events are currently subscribed to, it is not actually stored anywhere specific- though the user may choose to do this themselves.
    /// It is expected that some events will trigger a change in state, which is done by derived classes calling "ChangeState" and passing in a new Instance of the state that should be switched to.
    /// Polymorphism may be used to define super-states and sub-states using the eventsAndHandlers member, with descendants adding more pairs to the list (just make sure you call your base version also.)
    /// </summary>
    abstract public class EventAndHandlerState : EventAndHandlerSubscriber
    {
        public static void ActivateRootState(EventAndHandlerState newState)
        {
            newState.HandleActivateState();
            newState.SubscribeToEvents();
        }
        //call this function to change from one state to another
        protected void ChangeState(EventAndHandlerState newState)
        {
            HandleDeActivateState();
            UnSubscribeFromEvents();

            newState.HandleActivateState();
            newState.SubscribeToEvents();
        }
        protected virtual void HandleActivateState() { }
        protected virtual void HandleDeActivateState() { }
    }

    /// <summary>
    /// Derive from this class to create a "State" in the stack-based state machine.  All the derived/variants classes will define the different states of the statemachine.
    /// Override the eventsAndHandlers get accessor to define what events to listen to, and what action to take when they are triggered, for a given state.
    /// It is expected that derived variants will define a constructor with a(some) parameter(s) that will provide access to scene objects that generate the events it needs to subscribe to.
    /// When a given state is activated, it's events are subscribed to, but previous states on the stack ALSO remain subscribed.
    /// The current state is defined by which events are currently subscribed to, it is not actually stored anywhere specific- the stack only contains previous/super states.
    /// Rather than using polymorphism, this state machine will use the stack itself to define it's hierarchy of super/sub states by keeping all states, that are on the stack, subscribed to their events (until popped).
    /// </summary>
    abstract public class EventAndHandlerStackState : EventAndHandlerSubscriber
    {
        protected Stack<EventAndHandlerStackState> stateStack;// stores previous states- the current state is not kept on the stack

        public static void ActivateRootState(EventAndHandlerStackState newState)
        {
            newState.SubscribeToEvents();
            newState.stateStack = new Stack<EventAndHandlerStackState>();
            //newState.stateStack.Push(newState);
        }

        //change state to the sub-state provided
        public void Push(EventAndHandlerStackState newState)
        {
            stateStack.Push(this);
            newState.stateStack = stateStack;
            newState.HandleActivateState();
            newState.SubscribeToEvents();
        }
        //Reverts to the previous state and returns it.
        public EventAndHandlerStackState Pop()
        {
            UnSubscribeFromEvents();
            HandleDeActivateState();
            return stateStack.Pop();
        }
        protected virtual void HandleActivateState() { }
        protected virtual void HandleDeActivateState() { }
    }

    //examples

    //sample gameState class- contains data about the state of the game, and has function to load/save/start new
    public class GameStateData
    {
        public bool gameStarted;
        public void InitForNewGame() { gameStarted = true; }
        public async System.Threading.Tasks.Task SaveToFile(string filname, UnityEvent onComplete)
        {
            //stuff...
            onComplete.Invoke();
        }
        public static async System.Threading.Tasks.Task<GameStateData> LoadFromFile(string filname, UnityEvent onComplete)
        {
            //stuff..
            onComplete.Invoke();
            return new GameStateData(); // <= bad example
        }
        //other stuff...
    }


    public class MenuState : EventAndHandlerState
    {
        List<EventHandlerPair> _eventsAndHandlers;
        protected override List<EventHandlerPair> eventsAndHandlers { get => _eventsAndHandlers; }

        GameStateData gameData;
        SceneUIObjects sceneObjects;

        public MenuState(GameStateData gameData, SceneUIObjects sceneObjects)
        {
            this.gameData = gameData;
            this.sceneObjects = sceneObjects;

            _eventsAndHandlers = new List<EventHandlerPair>()
            {
                new EventHandlerPair(sceneObjects.menuWindow.newGame.onClick,HandleNewGameClick),
                new EventHandlerPair(sceneObjects.menuWindow.saveGame.onClick,HandleSaveGameClick),
                new EventHandlerPair(sceneObjects.menuWindow.quitGame.onClick,HandleQuitGameClick),
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
            gameData.InitForNewGame();
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



    public class SaveGameMenuState : EventAndHandlerState
    {
        List<EventHandlerPair> _eventsAndHandlers;
        protected override List<EventHandlerPair> eventsAndHandlers { get => _eventsAndHandlers; }

        SceneUIObjects sceneObjects;
        GameStateData gameData;
        
        public SaveGameMenuState(GameStateData gameData, SceneUIObjects sceneObjects)
        {
            this.gameData = gameData;
            this.sceneObjects = sceneObjects;
            _eventsAndHandlers = new List<EventHandlerPair>()
            {
                new EventHandlerPair(sceneObjects.fileNameWindow.fileNameSelectedEvent,HandleSaveFile),
                new EventHandlerPair(sceneObjects.fileNameWindow.cancelEvent,Cancel),
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
        void HandleSaveFile(object filename)
        {
            UnityEvent saveCompleteCallback= new UnityEvent();// this will be passed to the new WaitScreenForProcess state (which will listen to it), and to the SaveToFile function (which will trigger it when done).
            ChangeState(new WaitScreenForProcess(sceneObjects.waitDisplay, new MenuState(gameData, sceneObjects), saveCompleteCallback));
            gameData.SaveToFile((string)filename, saveCompleteCallback);//Because this call is not awaited, execution of the current method continues before the call is completed.

        }
    }



    public class WaitScreenForProcess : EventAndHandlerState
    {
        List<EventHandlerPair> _eventsAndHandlers;
        protected override List<EventHandlerPair> eventsAndHandlers { get => _eventsAndHandlers; }
        WaitThingy waitDisplay;
        public WaitScreenForProcess(WaitThingy waitDisplay, EventAndHandlerState stateToChangeToWhenComplete, UnityEvent processCompletionCallback)
        {
            this.waitDisplay = waitDisplay;
            _eventsAndHandlers = new List<EventHandlerPair>()
            {
                new EventHandlerPair(processCompletionCallback,()=>{ChangeState(stateToChangeToWhenComplete);}),
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

    public class UserConfirm : EventAndHandlerState
    {
        List<EventHandlerPair> _eventsAndHandlers;
        protected override List<EventHandlerPair> eventsAndHandlers { get => _eventsAndHandlers; }
        YesNoWindow yesnoDisplay;
        public UserConfirm(YesNoWindow yesnoDisplay, UnityAction yesHandler, UnityAction noHandler)
        {
            this.yesnoDisplay = yesnoDisplay;
            _eventsAndHandlers = new List<EventHandlerPair>()
            {
                new EventHandlerPair(yesnoDisplay.yesEvent,yesHandler),
                new EventHandlerPair(yesnoDisplay.noEvent, noHandler),
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
        public UnityEvent<object> fileNameSelectedEvent;
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
    public class SceneUIObjects : UnityEngine.MonoBehaviour
    {
        public MainMenuWindow menuWindow;
        public SelectFileNameWindow fileNameWindow;
        public WaitThingy waitDisplay;
    }
}