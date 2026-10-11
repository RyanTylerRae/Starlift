#nullable enable

using UnityEngine;

public abstract class StateMachineComponent : MonoBehaviour
{
    public bool startAutomatically = false;

    protected StateMachine stateMachine = new();

    private bool isBuilt = false;
    private bool isRunning = false;

    public bool IsRunning { get { return isRunning; } }

    protected virtual void Start()
    {
        EnsureBuilt();

        if (startAutomatically)
        {
            StartStateMachine();
        }
    }

    protected virtual void Update()
    {
        stateMachine.Update();
    }

    public void StartStateMachine()
    {
        if (isRunning)
        {
            return;
        }

        EnsureBuilt();
        isRunning = true;
        stateMachine.Start();
    }

    protected abstract void BuildStateMachine();

    private void EnsureBuilt()
    {
        if (isBuilt)
        {
            return;
        }

        isBuilt = true;
        BuildStateMachine();
    }
}
