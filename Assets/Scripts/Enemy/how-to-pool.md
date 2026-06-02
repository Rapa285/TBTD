# Cara buat pool
## PooledObject sekarang ada di scripts/wave/PooledObject
1. Tarik dari pool
```
GameObject newEnemy = enemyPool.Get().gameObject;
```

2. Wajib: Cuci bersih dan siapkan status baru
```
newEnemy.GetComponent<EnemyMover>().Initialize(baseSpeed);
newEnemy.GetComponent<HealthComponent>().Initialize(maxHealth, startingShield);
```