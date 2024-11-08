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

You can define a state machine that controls the behavior of an object (like a character) with the following example.

```
using UnityEngine;
using System;

public class CharacterController : MonoBehaviour
{
 private StateMachine _stateMachine;

 void Start()
 {
     _stateMachine = new StateMachine("Idle");

     // Define states with actions
     _stateMachine.AddState("Idle", () => { Debug.Log("Character is idle"); });
     _stateMachine.AddState("Walking", () => { Debug.Log("Character is walking"); });
     _stateMachine.AddState("Running", () => { Debug.Log("Character is running"); });

     // Execute the initial state
     _stateMachine.ExecuteCurrentState();
 }

 void Update()
 {
     // Switch states based on some conditions (e.g., user input)
     if (Input.GetKeyDown(KeyCode.W))
     {
         _stateMachine.SwitchState("Walking");
         _stateMachine.ExecuteCurrentState();
     }
     else if (Input.GetKeyDown(KeyCode.R))
     {
         _stateMachine.SwitchState("Running");
         _stateMachine.ExecuteCurrentState();
     }
 }
}
```

### StateMachine Class

```
public class StateMachine
{
 private readonly Dictionary<string, Action> _states = new Dictionary<string, Action>();
 private string _currentState;

 public StateMachine(string initialState)
 {
     _currentState = initialState;
 }

 public void AddState(string stateName, Action stateAction)
 {
     if (!_states.ContainsKey(stateName))
     {
         _states.Add(stateName, stateAction);
     }
     else
     {
         throw new InvalidOperationException($"State '{stateName}' already exists.");
     }
 }

 public void SwitchState(string newState)
 {
     if (!_states.ContainsKey(newState))
     {
         throw new InvalidOperationException($"State '{newState}' does not exist.");
     }

     _currentState = newState;
 }

 public void ExecuteCurrentState()
 {
     if (_states.ContainsKey(_currentState))
     {
         _states[_currentState].Invoke();
     }
     else
     {
         throw new InvalidOperationException($"State '{_currentState}' not found in state machine.");
     }
 }
}
```

### State Definitions

- **AddState**: Add a state with a corresponding action to be executed.
- **SwitchState**: Switch to another state by name.
- **ExecuteCurrentState**: Invoke the action of the current state.

## API Reference

### StateMachine Class

- `StateMachine(string initialState)`: Initializes the state machine with an initial state.
- `AddState(string stateName, Action stateAction)`: Adds a new state with the specified name and action.
- `SwitchState(string newState)`: Switches to a new state by name.
- `ExecuteCurrentState()`: Executes the action of the current state.

### Example of Adding States:

```
stateMachine.AddState("Running", () => { Debug.Log("The character is now running."); });
stateMachine.AddState("Jumping", () => { Debug.Log("The character is jumping."); });
```

## Contributing

Feel free to fork this repository, submit pull requests, or open issues. Contributions are welcome!

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
