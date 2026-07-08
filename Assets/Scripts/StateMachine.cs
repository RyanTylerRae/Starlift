#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

public class StateEdge
{
    public int FromStateIndex = -1;
    public int ToStateIndex = -1;
    public Func<bool>? Condition;
}

public class State
{
    public List<StateEdge> Edges = new();

    public event Action? OnEnterState;
    public event Action? OnExitState;

    public void RaiseEnterState()
    {
        OnEnterState?.Invoke();
    }

    public void RaiseExitState()
    {
        OnExitState?.Invoke();
    }
}

public class StateMachine
{
    public List<string> RegisteredStateNames = new();
    public List<State> RegisteredStates = new();

    private int startingStateIndex = -1;
    private int currentStateIndex = -1;

    public bool EnableLogging = true;

    public void RegisterState(string stateName, bool isStartingState = false)
    {
        if (!RegisteredStateNames.Contains(stateName))
        {
            int index = RegisteredStateNames.Count;
            RegisteredStateNames.Add(stateName);
            RegisteredStates.Add(new State());

            if (isStartingState)
            {
                startingStateIndex = index;
            }
        }
        else
        {
            Debug.LogError($"StateMachine.RegisterState - State '{stateName}' already registered.");
        }
    }

    public void AddEdge(string fromStateName, string toStateName, Func<bool>? condition)
    {
        if (!RegisteredStateNames.Contains(fromStateName))
        {
            Debug.LogError($"StateMachine.AddEdge - FromStateName '{fromStateName}' does not exist.");
            return;
        }

        if (!RegisteredStateNames.Contains(toStateName))
        {
            Debug.LogError($"StateMachine.AddEdge - ToStateName '{toStateName}' does not exist.");
            return;
        }

        if (condition == null)
        {
            Debug.LogError($"StateMachine.AddEdge - No condition passed in for edge '{fromStateName}' to '{toStateName}'. Adding a default edge instead.");
            AddDefaultEdge(fromStateName, toStateName);
            return;
        }

        StateEdge newEdge = new();
        newEdge.Condition = condition;
        newEdge.FromStateIndex = RegisteredStateNames.IndexOf(fromStateName);
        newEdge.ToStateIndex = RegisteredStateNames.IndexOf(toStateName);

        RegisteredStates[newEdge.FromStateIndex].Edges.Add(newEdge);
    }

    public void AddDefaultEdge(string fromStateName, string toStateName)
    {
        AddEdge(fromStateName, toStateName, () => true);
    }

    public void Start()
    {
        if (startingStateIndex < 0)
        {
            Debug.LogError("StateMachine.Start - No starting state registered.");
            return;
        }

        currentStateIndex = startingStateIndex;
        State currentState = RegisteredStates[currentStateIndex];

        currentState.RaiseEnterState();
    }

    public void Update()
    {
        if (currentStateIndex < 0)
        {
            return;
        }

        State currentState = RegisteredStates[currentStateIndex];

        for (int i = 0; i < currentState.Edges.Count; i++)
        {
            if (currentState.Edges[i].Condition != null && currentState.Edges[i].Condition!())
            {
                LogEdgeTransition(currentState.Edges[i]);

                currentState.RaiseExitState();

                currentStateIndex = currentState.Edges[i].ToStateIndex;
                currentState = RegisteredStates[currentStateIndex];

                currentState.RaiseEnterState();
            }
        }
    }

    public string GetCurrentState()
    {
        if (currentStateIndex < 0)
        {
            return string.Empty;
        }

        return RegisteredStateNames[currentStateIndex];
    }

    public bool HasState(string stateName)
    {
        return RegisteredStateNames.Contains(stateName);
    }

    public State GetState(string stateName)
    {
        int index = RegisteredStateNames.IndexOf(stateName);
        return RegisteredStates[index];
    }

    private void LogEdgeTransition(StateEdge edge)
    {
        if (EnableLogging)
        {
            Debug.Log($"StateMachine.LogEdgeTransition - [{RegisteredStateNames[edge.FromStateIndex]}] -> [{RegisteredStateNames[edge.ToStateIndex]}]");
        }
    }
}
