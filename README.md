# FlashlightFix

This mod <b>definitely</b> fixes the following issues:

<ul>
	<li>The flashlight toggling on or off when being placed in the ship cupboard (and certain other "E" interactions.)</li>
	<li>A flashlight/helmet light turning off when picking up an additional inactive flashlight.</li>
	<ul>
		<li>It will now turn on the new flashlight, and turn off the old one. If the new one is out of batteries, the old one will stay on. This behavior may be disabled in the config.</li>
	</ul>
	<li>More than one flashlight could have been active at the same time, leading to confusion and out of sync helmet lights when switching between them.</li>
	<ul>
		<li>This behavior may be disabled in the config.</li>
	</ul>
</ul>

And <i>probably</i> fixes other, similar problems, like:
<ul>
	<li>The flashlight toggling itself off when using/switching items and picking things up</li>
	<li>The flashlight draining battery when it is not in use</li>
	<li>And more?</li>
</ul>

If this mod does <i>not</i> fix the extra described issues, I will do my best to troubleshoot the problem(s) and update it.

# Changelog

<ul>
	<li><b>v1.0.5</b> - Fixing compatibility with ReservedFlashlightSlot</li>
	<ul>
		<li>Fixed a minor bug that prevented any flashlights from turning on when holding multiple if the ReservedFlashlightSlot mod was installed.</li>
		<ul>
			<li>Note that ReservedFlashlightSlot (as of its v1.5.1) still has a bug that turns the 2nd+ flashlight back off when switching back to it. This is their bug, not mine :)</li>
		</ul>
	</ul>
	&nbsp;
	<li><b>v1.0.4</b> - Fixing the helmet light and bulb</li>
	<ul>
		<li>Found and fixed a bug where the helmet light would be on at the same time as the flashlight, but only for non-owners.</li>
		<li>Found and fixed a bug with the flashlight glass material being set incorrectly upon load.</li>
		<li>Organizing my code a bit and separating the patch classes into their own files</li>
	</ul>
	&nbsp;
	<li><b>v1.0.3</b> - Fixing more of my fixes</li>
	<ul>
		<li>Using the inverse teleporter while holding an active flashlight kept the beam on the player after being teleported.</li>
		<li>Other players could not see a secondary flashlight turn on when picking one up for the first time, if the already-held flashlight was active.</li>
		<li>Updated the logging calls to be debug level instead of info.</li>
	</ul>
	&nbsp;
	<li><b>v1.0.2</b> - Fixing my fixes</li>
	<ul>
		<li>Attempting to fix some networking bugs I introduced in the minor fixes from 1.0.1.</li>
		<li>Consolidated the previous three config settings into one - it was too confusing to mix them.</li>
	</ul>
	&nbsp;
	<li><b>v1.0.1</b> - More fixes</li>
	<ul>
		<li>Fixed the helmet light (pocketed flashlight) turning off when picking up an additional inactive flashlight.</li>
		<li>Fixed (modified?) the game allowing multiple flashlights being on at once in the player's inventory.</li>
		<li>Added a config file for disabling the behavior of the two new fixes.</li>
	</ul>
	&nbsp;
	<li><b>v1.0.0</b> - Initial Release</li>
	<ul>
		<li>Fixed the main bug where the flashlight would toggle on or off when placing it or interacting with certain items.</li>
	</ul>
</ul>
