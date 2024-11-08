using UnityEngine.Events;
using System.Collections.Generic;

namespace EyE.StateMachine
{

    /// The `EventDrivenState` class and its descendants are used to represent different states in a system.
    /// All state classes inherit from `EventDrivenState<TEventProviderType>`, providing a simple event-driven and polymorphic approach to state management.
    /// State changes occur in response to events, which will internally call the two abstract functions (subscribe/unsubscribe) defined by the descendant/user of this class.
    ///
    /// The source of the "driving" events, usually a scene object, must be passed to the constructor of every state.
    /// Each state class defined by the user specifies the events it listens to, and how it reacts to each one.
    /// State transitions are accomplished by calling the `ChangeState` method, and passing in a new instance of the desired state class (which will, in turn, require an instance of its TEventProviderType).
    /// The current state is defined by the events to which the state is subscribed, rather than being explicitly stored- this is all changing state really does.
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


    public interface ISubscriber
    {
        public void Subscribe();
        public void Unsubscribe();
    }

    /// <summary>
    /// This abstract class handles subscribing and unsubscribing to a list of Events(or whatever), each with a particular UnityAction handler.
    /// The EventDrivenState class, is derived from and builds upon this class.
    /// </summary>
    abstract public class SubscriptionSetManager<TSubscriber> where TSubscriber : ISubscriber
    {
        // this member's GET accessor must be overridden by a concrete descendant classes.
        // It should return a non-changing list of EventHandlerPair's - each of which will provide a UnityEvent to subscribe to, and a UnityAction to run when the event triggers.
        abstract protected List<TSubscriber> GetSubscribers();
        List<TSubscriber> _subscribers;
        protected List<TSubscriber> eventsAndHandlers
        {
            get
            {
                if (_subscribers == null)
                    _subscribers = GetSubscribers();
                return _subscribers;
            }
        }


        protected void SubscribeToAll()
        {
            foreach (TSubscriber pair in eventsAndHandlers)
                pair.Subscribe();
        }
        protected void UnSubscribeFromAll()
        {
            foreach (TSubscriber pair in eventsAndHandlers)
                pair.Unsubscribe();
        }
    }

    /// <summary>
    /// Derive from this class to create a "State" in the state machine.  All the different, derived variants define the different states of the statemachine.
    /// Override the GetSubscribers method to define what events to listen to, and what action to take when they are triggered, for a given state.
    /// It is expected that derived variants will define a constructor with a(some) parameter(s) that will provide access to scene objects that will generate the events it needs to subscribe to.
    /// When a given state is activated, it's events are subscribed to, while the now-previous state's events are unsubscribed from.
    /// The current state is defined by which events are currently subscribed to, it is not actually stored anywhere specific- though the user may choose to do this themselves.
    /// It is expected that some events will trigger a change in state, which is done by derived classes calling "ChangeState" and passing in a new Instance of the state that should be switched to.
    /// Polymorphism may be used to define super-states and sub-states using the eventsAndHandlers member, with descendants adding more pairs to the list (just make sure you call your base version also.)
    /// </summary>
    abstract public class EventDrivenState<TEventSubscriber> : SubscriptionSetManager<TEventSubscriber> where TEventSubscriber : ISubscriber
    {
        public static void ActivateRootState(EventDrivenState<TEventSubscriber> newState)
        {
            newState.HandleActivateState();
            newState.SubscribeToAll();
        }
        //call this function to change from one state to another
        protected void ChangeState(EventDrivenState<TEventSubscriber> newState)
        {
            TerminateLayerStates();
            HandleDeActivateState();
            UnSubscribeFromAll();

            newState.HandleActivateState();
            newState.SubscribeToAll();
            newState.ActivateLayerStates();
        }
        #region layeredStates
        private List<EventDrivenStateLayer<TEventSubscriber>> layeredStates = new List<EventDrivenStateLayer<TEventSubscriber>>();
        //call this function to add activate an additional state(set of event listeners) to this one
        // does NOT deactivate this state
        protected void LayerNewState(EventDrivenStateLayer<TEventSubscriber> newState)
        {
            newState.HandleActivateState();
            newState.SubscribeToAll();
            layeredStates.Add(newState);
        }
        void ActivateLayerStates()
        {
            foreach (EventDrivenStateLayer<TEventSubscriber> stateLayer in layeredStates)
            {
                stateLayer.HandleActivateState();
                stateLayer.SubscribeToAll();
            }
        }
        void TerminateLayerStates()
        {
            foreach (EventDrivenStateLayer<TEventSubscriber> stateLayer in layeredStates)
            {
                stateLayer.Terminate();
            }
        }
        #endregion
        protected virtual void HandleActivateState() { }
        protected virtual void HandleDeActivateState() { }
    }

    //Can be activated via LayerNewState, which will NOT deactivate the calling state, like ChangeState would.
    //These states can be deactivated without having to initiate a new state.
    //These states will be activated and deactivated when the state they are layered upon is
    abstract public class EventDrivenStateLayer<TEventSubscriber> : EventDrivenState<TEventSubscriber> where TEventSubscriber : ISubscriber
    {
        public void Terminate()
        {
            HandleDeActivateState();
            UnSubscribeFromAll();
        }
    }
    
    //has special constructor.  stores ref to the state that will be changed to when Revert() is called.
    //Can used like a stack by passing "this" to constuctor.
    abstract public class RevertibleEventDrivenState<TEventSubscriber> : EventDrivenState<TEventSubscriber> where TEventSubscriber : ISubscriber
    {
        EventDrivenState<TEventSubscriber> stateToRevertTo;

        protected RevertibleEventDrivenState(EventDrivenState<TEventSubscriber> stateToRevertTo)
        {
            this.stateToRevertTo = stateToRevertTo;
        }
        public void Revert()
        {
            ChangeState(stateToRevertTo);
        }
    }

    //examples
    //class used to store an Event and a function to invoke when the event is triggered
    public class UnityEventSubscription : ISubscriber
    {
        //basic trigger and handler pair- no data is passed
        public UnityEngine.Events.UnityEvent trigger;
        public UnityEngine.Events.UnityAction handler;
        //trigger and handler pair that allow an object to be passed as data
        public UnityEngine.Events.UnityEvent<object> triggerWithData;
        public UnityEngine.Events.UnityAction<object> handlerWithData;

        public UnityEventSubscription(UnityEvent trigger, UnityAction handler)
        {
            this.trigger = trigger;
            this.handler = handler;
        }
        public UnityEventSubscription(UnityEvent<object> trigger, UnityAction<object> handler)
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

    abstract public class UnityEventDrivenState : EventDrivenState<UnityEventSubscription> { }
    abstract public class UnityEventDrivenStateLayer : EventDrivenStateLayer<UnityEventSubscription> { }
    abstract public class UnityRevertibleEventDrivenState : RevertibleEventDrivenState<UnityEventSubscription>
    {
        protected UnityRevertibleEventDrivenState(EventDrivenState<UnityEventSubscription> stateToRevertTo) : base(stateToRevertTo)
        {
        }
    }




}

