using Stratum;
using VContainer;

public sealed partial class HealthComponentData
{
    public string Key;
    public float MaxHealth = 100;
}

public sealed partial class HealthComponent
{
    private readonly HealthComponentData _componentData;
    [Inject] private readonly IPrefabManager _prefabManager;
    private float _currentHp;
    
    protected override void OnAdd() => _currentHp = _componentData.MaxHealth;

    public void TakeDamage(float damage)
    {
        _currentHp -= damage;
        if(_currentHp <= 0)
        {
            _currentHp = 0;
            if(_prefabManager.TryGetHandle(GameObject, out var handle))
                _prefabManager.ReleasePrefabAsync(handle);
        }
    }
}
