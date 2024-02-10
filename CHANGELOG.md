# Changelog

### v1.1.0 - Cleanup and rewrite
* Rewrote most of the code since it was getting "bandaidy".
	* This also fixed a bug with laser pointers showing double beams in some cases.
* Removed DLLs from source control (just copy what you need manually from the LethalCompany directory if you build it yourself)

### v1.0.7 - Quick fix for the keybind spam
* Fixed a spammy error message that occurred when ToggleShortcut was set to "None"

### v1.0.6 - Fixing laser pointer and adding toggle
* Fixed the laser pointer acting as a flashlight (it technically is one in the code) when equipping it.
* Stopped active pocketed flashlights from turning off when activating a laser poiner.
* Adding a configurable keybind for toggling the flashlight at any time
* Fixed a bug where discarding an extra active flashlight would not keep the helmet lamp on if you had another flashlight in your inventory.

### v1.0.5 - Fixing compatibility with ReservedFlashlightSlot
* Fixed a minor bug that prevented any flashlights from turning on when holding multiple if the ReservedFlashlightSlot mod was installed.
	* Note that ReservedFlashlightSlot (as of its v1.5.1) still has a bug that turns the 2nd+ flashlight back off when switching back to it. This is their bug, not mine :)
	
### v1.0.4 - Fixing the helmet light and bulb
* Found and fixed a bug where the helmet light would be on at the same time as the flashlight, but only for non-owners.
* Found and fixed a bug with the flashlight glass material being set incorrectly upon load.
* Organizing my code a bit and separating the patch classes into their own files
	
### v1.0.3 - Fixing more of my fixes
* Using the inverse teleporter while holding an active flashlight kept the beam on the player after being teleported.
* Other players could not see a secondary flashlight turn on when picking one up for the first time, if the already-held flashlight was active.
* Updated the logging calls to be debug level instead of info.
	
### v1.0.2 - Fixing my fixes
* Attempting to fix some networking bugs I introduced in the minor fixes from 1.0.1.
* Consolidated the previous three config settings into one - it was too confusing to mix them.
	
### v1.0.1 - More fixes
* Fixed the helmet light (pocketed flashlight) turning off when picking up an additional inactive flashlight.
* Fixed (modified?) the game allowing multiple flashlights being on at once in the player's inventory.
* Added a config file for disabling the behavior of the two new fixes.
	
### v1.0.0 - Initial Release
* Fixed the main bug where the flashlight would toggle on or off when placing it or interacting with certain items.