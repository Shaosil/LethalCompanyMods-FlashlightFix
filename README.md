# FlashlightFix

This mod fixes the following issues:

* The flashlight toggling on or off when being placed in the ship cupboard (and certain other "E" interactions.)
* A flashlight/helmet light turning off when picking up an additional inactive flashlight.
	* It will now turn on the new flashlight, and turn off the old one. If the new one is out of batteries, the old one will stay on. This behavior may be disabled in the config.
* More than one flashlight could have been active at the same time, leading to confusion and out of sync helmet lights when switching between them. This behavior may be disabled in the config.
* Laser pointers are no longer treated as flashlights, so you may keep a flashlight helmet light on while using laser pointers, and lasers will no longer deactivate pocketed flashlights, etc. This behavior may be disabled in the config.
* Flashlightss will no longer toggle themselves when using/switching items and picking things up
	* As a result, they will not drain battery randomly while not truly on.
* And more?

If I find more bugs with them, I'll fix as needed.

### SMALL WARNING:

If playing with people who do not use this mod, in some cases they *may* not always see your flashlights/helmet lamp in the same state as they are on your machine.

Also, I do plan on merging this into [GeneralImprovements](https://thunderstore.io/c/lethal-company/p/ShaosilGaming/GeneralImprovements/) at some point and deprecating it as a standalone mod.