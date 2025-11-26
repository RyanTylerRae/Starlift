#nullable enable

using UnityEngine;
using System;
using System.Threading.Tasks;

public class Entity : MonoBehaviour
{
    private DamageEventHandler? damageEventHandler = null;
    public event Action<DamageEvent> OnKilled = delegate { };
    private bool isKilled = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TryGetComponent(out damageEventHandler);
    }

    public void SendDamageEvent(GameObject source, int damage, DamageType damageType)
    {
        if (damageEventHandler != null)
        {
            DamageEvent damageEvent = new();
            damageEvent.damageTarget = gameObject;
            damageEvent.damageSource = source;
            damageEvent.damageAmount = damage;
            damageEvent.damageType = damageType;

            damageEventHandler.HandleDamageEvent(damageEvent);
        }
    }

    public async void Kill(DamageEvent damageEvent)
    {
        if (!isKilled)
        {
            isKilled = true;

            if (TryGetComponent(out FirstPersonController controller))
            {
                controller.enabled = false;
            }

            var context = System.Threading.SynchronizationContext.Current;

            await Task.Run(async () =>
            {
                var tcs = new TaskCompletionSource<bool>();

                context?.Post(async _ =>
                {
                    if (TryGetComponent(out ScreenFader screenFader))
                    {
                        await screenFader.FadeToOpacity(targetOpacity: 1.0f, 2.0f);
                        await Task.Delay(2000);
                    }

                    OnKilled.Invoke(damageEvent);
                    Destroy(gameObject);
                    tcs.SetResult(true);
                }, null);

                await tcs.Task;
            });
        }
    }
}
