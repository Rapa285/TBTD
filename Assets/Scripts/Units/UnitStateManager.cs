// will contain the data of Units/towers

// The plan is that the unit prefab will be plain

// Since units will be able to level up and upgrade their stats
// A way to save this information is required

// My idea is that an object (this) exists to keep track of their upgrades and information n stuff
// Such that if the unit is to be retreated and re deployed in the future
// This object will "help reapply" all upgrades it has received

// This way upgrade offers and application can be managed in a more centralized manner
// as oposed to each units handling their own upgrade cycle
// Top-down mindset type shit

// made with facilitating upgrades and UI information in mind

// ADDITIONAL CONTEXT :
/* 

In this game, all of the players units (towers) start plain
After defeating a few enemies, it will level up

Upon level up the player may choose one out of three upgrades

Whilst other supporting gimmicks are still in the works
the idea is that players are to frequently swap out their units
to develop their entire army instead of having a single super unit

which is why the storing and application of upgrades need
to be streamlined? or atleast defined

*/