# Miscellaneous Tweaks and Fixes for WotR

A small collection of fixes or tweaks to things that bugged me while playing.
Requires [ModMenu](https://github.com/WittleWolfie/ModMenu/releases)

All features can be enabled or disabled in the Mods menu.

### Natural Attack Stacking

Previously, if you got multiple natural attacks of the same type from different features/buffs/etc.
you would get extra attacks per round. This was 'fixed' by Owlcat at some point so now extra natural 
attacks give no benefit to PCs. With this enabled, vanilla behaviour is replaced with an approximation
of the tabletop rules:

Addtional natural attacks of the same kind gives a stacking increase to the effective size of the 'weapon'
(eg. 2 pairs of Medium claw attacks effectively grant 1 pair of Large claw attacks instead).

You get all 'enchantment' effects (elemental damage/DR penetration/etc.) but multiple enchants of the same type
do not stack.

### Major Strength Blessing Armor Speed Fix

Warpriest's Major Blessing for Strength domain now applies to heavy armor in addition to medium
armor - now matches its description.

### Primalist Bloodline Selection Fix

This is my previously-standalone [Primalist Bloodline Selection](https://github.com/microsoftenator2022/PrimalistBloodlineSelections) fix:

Primalist bloodline selections are now per-bloodline and should function correctly when combined with
Dragon Disciple and/or Second Bloodline (still two rage powers per 4 levels, but you can choose which
bloodline's power to trade).

### Bloodrager Draconic Claws fix
Fixes claw progression for draconic bloodrager bloodlines: 

Correct progression: 1d6, 1d8, 1d8 (Magic), 1d8+1d6 elemental (Magic)

Actual progression (without fix): 1d6, 1d6, 1d6, 1d8 (Magic), 1d8+1d6 elemental (Magic)

### Force Book of Dreams Upgrade

The Book of Dreams item is supposed to upgrade at certain points in the story,
but this has never reliably worked (at least in my experience). Enabling this forces the 
upgrade script to run on every Etude update. Disabled by default.

### Reformed Fiend DR/good

Changes the damage reduction for the Reformed Fiend Bloodrage archetype from DR/evil to DR/good.
Disabled by default.

### Dollroom postprocessing filter toggles

Allows you to disable some postprocess filters in the dollroom (inventory screen, character creation. mythic level up, etc.)

## Acknowledgements

* Special thanks to the modding community on [Discord](https://discord.com/invite/wotr) - in particular, Pheonix99 and Bubbles.
* WittleWolfie for ModMenu
