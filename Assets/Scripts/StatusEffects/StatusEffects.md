# how to use status effect

## di skrip tower
```
StatusEffectManager manager = target.GetComponent<StatusEffectManager>();
if (manager != null)
{
    // send dot (5 dmg/tick, 4tick, 1s)
    DotEffect poison = new DotEffect(5f, 4, 1f);
    manager.AddEffect(poison);

    // Slow 40% (0.4f) selama 3 detik
    SlowEffect iceDebuff = new SlowEffect(0.4f, 3f);
    manager.AddEffect(iceDebuff);
}

```