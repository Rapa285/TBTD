- It seems that the newest refactoring of UnitEventBus to use ServiceLocator hasnt been reflected in the other scripts (other than ammo related scripts?)

- range indicator

- pooling behaviour in the future (no destroy work flow) for visual things
- ie. when loading, before entering gameplay, instantiate an undeployed and disabled copy all units to resolve its stats and more importantly visual models/flair
- deployment preview simply enables the object, and deployment deploys it
- recall triggers the "undeployment workflow" whatever that is and disables the unit

- double down on ServiceLocator pattern and use it for UnitDeploymentManager / UnitStateManager
- point being to replace all instance of Findgameobjectbytype