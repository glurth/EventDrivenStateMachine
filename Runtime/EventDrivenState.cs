using UnityEngine.Events;
using System.Collections.Generic;

namespace EyE.StateMachine
{

    // The `EventDrivenState` class and its descendants are used to represent different states in a system.
    // All state classes inherit from `EventDrivenState<TEventProviderType>`, providing a simple event-driven and polymorphic approach to state management.
    // State changes occur in response to events, which will internally call the two abstract functions (subscribe/unsubscribe) defined by the descendant/user of this class.
    //
    // The source of the "driving" events, usually a scene object, must be passed to the constructor of every state.
    // Each state class defined by the user specifies the events it listens to, and how it reacts to each one.
    // State transitions are accomplished by calling the `ChangeState` method, and passing in a new instance of the desired state class (which will, in turn, require an instance of its TEventProviderType).
    // The current state is defined by the events to which the state is subscribed, rather than being explicitly stored- this is all changing state really does.
    // State changes are controlled within each state class in response to events, as defined by the user, allowing for dynamic transitions based on events.
    // The subscribe/unsubscribe functions can implement polymorphic behavior, but the hierarchy must use the same TEventProviderType to do this. 
    // 
    // It is recommended that state DATA, particularly data that will need to be saved, be stored in a different class as serialization of states is not handled.
    // User Defined classes can add a parameter for State Data to their constructor, if desired.
    // 
    // The `eventSource` member, which must be passed to the class constructor, provides access to all available events that the state can subscribe to.
    // Users should implement the `SubscribeToEvents` function, and in there, subscribe to events with the appropriate, user-defined, handler functions.
    // Cleanup, in the form of unsubscribing, is done in the `UnSubscribeToEvents` function.
    // These two functions (subscribe/unsubscribe) are automatically called when a state is changed but must be defined by the descendant/user of this class.
    // 
    // Descendants will define functions as listeners for specific events found in `eventSource`.
    // These functions handle the actions triggered by events, which may include changing the state.
    // Users may choose to modify scene/state values inside this class when events are triggered or implement `IHandleStateChangeEvents` on the `eventSource`.
    // The purpose of IHandleStateChangeEvents is to allow the event source object to react when the state changes.

    /// <summary>
    /// This interface specify two functions Subscribe() and Unsubscribe(), which for state machines below will apply to events.
    /// </summary>
    public interface ISubscriber
    {
        public void Subscribe();
        public void Unsubscribe();
    }

    /// <summary>
    /// This abstract class handles subscribing and unsubscribing to a list of ISubscribers.
    /// The EventDrivenState class, is derived from and builds upon this class.
    /// </summary>
    abstract public class SubscriptionSetManager
    {
        /// <summary>
        /// This is the primary method, concrete descendants of this class must override it.
        /// For state machines, this will specify a list of which events to subscribe to, and what to do when each is triggered.
        /// </summary>
        /// <returns>the list specified by descendants</returns>
        abstract protected List<ISubscriber> GetSubscribers();

        private List<ISubscriber> _subscribers=null;

        /// <summary>
        /// It invokes GetSubscribers once, and caches the result.
        /// </summary>
        private List<ISubscriber> eventsAndHandlers
        {
            get
            {
                if (_subscribers == null)
                    _subscribers = GetSubscribers();
                return _subscribers;
            }
        }

        /// <summary>
        /// Loops through all subscribers and subscribes to them.
        /// </summary>
        protected void SubscribeToAll()
        {
            foreach (ISubscriber pair in eventsAndHandlers)
                pair.Subscribe();
        }
        /// <summary>
        /// Loops through all subscribers and unsubscribes from them.
        /// </summary>
        protected void UnSubscribeFromAll()
        {
            foreach (ISubscriber pair in eventsAndHandlers)
                pair.Unsubscribe();
        }
    }

    /// <summary>
    /// Derive from this class to create a "State" in the state machine.  All the different, concrete derived variants of this class define the different states of the state machine.
    /// Override the GetSubscribers method to define what events to listen to, and what action to take when they are triggered, for a given state.
    /// It is expected that derived variants will define a constructor with a(some) parameter(s) that will provide access to scene objects that will generate the events it needs to subscribe to.
    /// When a given state is activated, it's events are subscribed to, after the now-previous state's events are unsubscribed from.
    /// The current state is defined by which events are currently subscribed to, it is not actually stored anywhere specific- though the user may choose to do this themselves.
    /// It is expected that some events will trigger a change in state, which is done by derived classes calling "ChangeState" and passing in a new Instance of the state that should be switched to.
    /// Polymorphism may be used to define super-states and sub-states using the GetSubscribers member, with descendants adding more pairs to the list (just make sure you call your base version also.)
    /// </summary>
    abstract public class EventDrivenState : SubscriptionSetManager
    {
        /// <summary>
        /// Static function used to create a state that does not comes from another state.  Usually this is a "root" state, which will when appropriate/eventually generate all other states.
        /// </summary>
        /// <param name="newState">Instance of a root state.</param>
        public static void ActivateRootState(EventDrivenState newState)
        {
            newState.HandleActivateState();
            newState.SubscribeToAll();
        }
        
        /// <summary>
        /// Main function used to change from this state to another.  
        /// It is expected that some form(s) of a call to this function will be in some/one of the event handlers.
        /// Terminates any states layered on this one, deactivates this state, and unsubscribes it from all events.
        /// Activates the specified state, subscribes to events it specifies, and activate any layered states it may have.
        /// </summary>
        /// <param name="newState">the new state to be activated</param>
        protected void ChangeState(EventDrivenState newState)
        {
            TerminateLayerStates();
            HandleDeActivateState();
            UnSubscribeFromAll();

            newState.HandleActivateState();
            newState.SubscribeToAll();
            newState.ActivateLayerStates();
        }
        #region layeredStates

        /// <summary>
        /// Stores the List of states layered on this one.
        /// </summary>
        private List<EventDrivenStateLayer> layeredStates = new List<EventDrivenStateLayer>();

        /// <summary>
        /// Call this function to add activate an additional EventDrivenStateLayer(set of event listeners) to this one.
        /// Does NOT deactivate this state, only activates layered state
        /// Does not check for duplicates, will throw an exception if provided instance is already layered on this state.
        /// </summary>
        /// <param name="newState">layered state instance to put on `this` state</param>
        protected void LayerNewState(EventDrivenStateLayer newState)
        {
            newState.HandleActivateState();
            newState.SubscribeToAll();
            layeredStates.Add(newState);
        }
        public void NotifyLayerTerminated(EventDrivenStateLayer layerState)
        {
            bool ignore=layeredStates.Remove(layerState);
        }
        /// <summary>
        /// This function is called when the current state is activated, in case it has any layered states.
        /// </summary>
        void ActivateLayerStates()
        {
            foreach (EventDrivenStateLayer stateLayer in layeredStates)
            {
                stateLayer.HandleActivateState();
                stateLayer.SubscribeToAll();
            }
        }
        /// <summary>
        /// This function is called when the current state is deactivated, in case it has any layered states.
        /// </summary>
        void TerminateLayerStates()
        {
            foreach (EventDrivenStateLayer stateLayer in layeredStates)
            {
                stateLayer.Terminate();
            }
        }
        #endregion
        /// <summary>
        /// Users should override this function to specify non-state machine stuff that should happen when this state is activated, like changing a rigid body velocity, or displaying a window.
        /// </summary>
        protected virtual void HandleActivateState() { }
        /// <summary>
        /// Users should override this function to specify non-state machine stuff that should happen when this state is deactivated, like changing a rigid body velocity, or hiding a window.
        /// </summary>
        protected virtual void HandleDeActivateState() { }
    }

    /// <summary>
    /// OPTIONAL  Special kind of state that can be activated via LayerNewState, which will NOT deactivate the calling state, like ChangeState would.
    /// These states can therefore be deactivated without having to initiate a new state, via the Terminate function.
    /// The state it is layered upon needs to be notified when this state is terminated, and so the constructor requires it as a parameter.
    /// </summary>
    /// <typeparam name="TEventSubscriber"></typeparam>
    abstract public class EventDrivenStateLayer : EventDrivenState 
    {
        EventDrivenState nofityOnTerminate;

        protected EventDrivenStateLayer(EventDrivenState nofityOnTerminate)
        {
            this.nofityOnTerminate = nofityOnTerminate;
        }

        public void Terminate()
        {
            HandleDeActivateState();
            UnSubscribeFromAll();
            nofityOnTerminate.NotifyLayerTerminated(this);
        }
    }

    /// <summary>
    /// OPTIONAL  This variant has a special constructor that takes and stores a reference to a state instance.
    /// The state that will be changed to this passed in reference when Revert() is called.
    /// RevertibleEventDrivenState can used like a (linked) stack by passing "this" to the constructor, to Push it onto the stack, and using Revert to Pop it off.
    /// </summary>
    /// <typeparam name="TEventSubscriber"></typeparam>
    abstract public class RevertibleEventDrivenState : EventDrivenState
    {
        EventDrivenState stateToRevertTo;

        protected RevertibleEventDrivenState(EventDrivenState stateToRevertTo)
        {
            this.stateToRevertTo = stateToRevertTo;
        }
        /// <summary>
        /// Changes state back to the one initially provided to the constructor.
        /// </summary>
        public void Revert()
        {
            ChangeState(stateToRevertTo);
        }
    }

    #region UnityISubscribers
    /// <summary>
    /// Class used to store a `UnityEvent` and `UnityAction` (that are passed to the constructor) to invoke when the event is triggered
    /// The ISubscriber required functions, Subscribe and Unsubscribe, add/remove the actions (if not null) as listeners to the events (if not null).
    /// </summary>
    public class UnityEventSubscription : ISubscriber
    {
        //basic trigger and handler pair- no data is passed
        public UnityEngine.Events.UnityEvent trigger;
        public UnityEngine.Events.UnityAction handler;

        /// <summary>
        /// Constructor for a UnityEventSubscription.  The parameters are stored internally.
        /// </summary>
        /// <param name="trigger">the event that will be listened for</param>
        /// <param name="handler">the action that will be invoked when the vent is triggered</param>
        public UnityEventSubscription(UnityEvent trigger, UnityAction handler)
        {
            this.trigger = trigger;
            this.handler = handler;
        }

        public void Subscribe()
        {
            if (trigger != null && handler !=null)
                trigger.AddListener(handler);
        }
        public void Unsubscribe()
        {
            if (trigger != null && handler != null)
                trigger.RemoveListener(handler);
        }
    }
    /// <summary>
    /// Class used to store a `UnityEvent<Tdata>` and `UnityAction<Tdata>` (that are passed to the constructor) to invoke when the event is triggered.
    /// This generic version passes data of type Tdata from the trigger to handler.
    /// The ISubscriber required functions, Subscribe and Unsubscribe, add/remove the actions (if not null) as listeners to the events (if not null).
    /// </summary>
    public class UnityDataEventSubscription<Tdata> : ISubscriber
    {
        //trigger and handler pair that allow an object to be passed as data
        public UnityEngine.Events.UnityEvent<Tdata> triggerWithData;
        public UnityEngine.Events.UnityAction<Tdata> handlerWithData;

        /// <summary>
        /// Constructor for a UnityEventSubscription that passes data from the trigger to the handler.  The parameters are stored internally.
        /// Note: type safety is NOT ensured
        /// </summary>
        /// <param name="trigger">the event that will be listened for.  This trigger will pass a data `object` to the handler.</param>
        /// <param name="handler">the action that will be invoked when the vent is triggered.  This handler action will take a data `object` as a parameter.</param>
        public UnityDataEventSubscription(UnityEvent<Tdata> trigger, UnityAction<Tdata> handler)
        {
            this.triggerWithData = trigger;
            this.handlerWithData = handler;
        }
        public void Subscribe()
        {
            if (triggerWithData != null && handlerWithData != null)
                triggerWithData.AddListener(handlerWithData);
        }
        public void Unsubscribe()
        {
            if (triggerWithData != null && handlerWithData != null)
                triggerWithData.RemoveListener(handlerWithData);
        }
    }
   
    #endregion
}

