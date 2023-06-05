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

### Force Book of Dreams Upgrade

The Book of Dreams item is supposed to upgrade at certain points in the story,
but this has never reliably worked (at least in my experience). Enabling this forces the 
upgrade script to run on every Etude update. Disabled by default.

### Identify natural weapons

In some cases, "enchanted" natural weapons will appear as Unidentified, requiring a Knowledge (Arcana) check
to identify. This force identifies natural weapons on equip

(This was already included in previous versions of the mod and enabled at all times,
but has now been given its own setting)

### Fix empty hand weapon equip

When multiple features or buffs grant "empty hand replacement" weapons (eg. claws) the game is supposed to equip the "best" set
but sometimes it just chooses the newest (which may be worse than the claws they're replacing).
This adds an additional check after equip to try to ensure the "best" weapon was selected.

### Reformed Fiend DR/good

Changes the damage reduction for the Reformed Fiend Bloodrage archetype from DR/evil to DR/good.
Disabled by default.

### Dollroom postprocessing filter toggles

Allows you to disable some postprocess filters in the dollroom (inventory screen, character creation. mythic level up, etc.)

## Now includes fix for the "Kingmaker.Localization.SharedStringAsset must be instantiated using the ScriptableObject.CreateInstance method instead of new SharedStringAsset." log spam

## Acknowledgements

* Special thanks to the modding community on [Discord](https://discord.com/invite/wotr) - in particular, Pheonix99 and Bubbles.
* WittleWolfie for ModMenu
