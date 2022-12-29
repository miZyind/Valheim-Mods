using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Reflection.Emit;
using System.Collections;

namespace Aquaman
{
    [BepInPlugin("miZyind.Aquaman", "Aquaman", "2022.12.30")]
    class Aquaman : BaseUnityPlugin
    {
        void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(Aquaman));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Game), "UpdateRespawn")]
        static void UpdateRespawn(ref Game __instance)
        {
            __instance.m_firstSpawn = false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnvMan), "SetEnv")]
        static void SetEnv(ref EnvSetup env)
        {
            env.m_fogDensityDay = 0f;
            env.m_fogDensityNight = 0f;
            env.m_fogDensityMorning = 0f;
            env.m_fogDensityEvening = 0f;
            env.m_fogColorNight = Color.white;
            env.m_ambColorNight = Color.white;
            env.m_sunColorNight = Color.white;
            env.m_fogColorSunNight = Color.white;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Attack), "UseAmmo")]
        static bool UseAmmo(
            Humanoid ___m_character,
            ItemDrop.ItemData ___m_weapon,
            ref ItemDrop.ItemData ___m_ammoItem,
            ref bool __result
        )
        {
            var target = ___m_character as Player;
            if (target && target.IsPlayer())
            {
                ___m_ammoItem = target.GetInventory().GetAmmoItem(___m_weapon.m_shared.m_ammoType);
                __result =
                    string.IsNullOrEmpty(___m_weapon.m_shared.m_ammoType) || ___m_ammoItem != null;
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharacterAnimEvent), "UpdateFreezeFrame")]
        static bool UpdateFreezeFrame(Character ___m_character, ref Animator ___m_animator)
        {
            var target = ___m_character as Player;
            if (target && target.IsPlayer() && target.InAttack() && ___m_animator != null)
            {
                var weapon = target.GetCurrentWeapon();
                if (weapon != null)
                {
                    switch (weapon.m_shared.m_skillType)
                    {
                        case Skills.SkillType.Bows:
                        case Skills.SkillType.Crossbows:
                        case Skills.SkillType.Pickaxes:
                            ___m_animator.speed = 10;
                            return false;
                        default:
                            break;
                    }
                }
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ItemDrop), "Awake")]
        static void ItemDropAwake(ref ItemDrop __instance)
        {
            __instance.m_itemData.m_shared.m_weight = 0;
            __instance.m_itemData.m_shared.m_teleportable = true;
            __instance.m_itemData.m_shared.m_useDurabilityDrain = 0;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), "OnDeath")]
        static bool OnDeath()
        {
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), "Awake")]
        static void PlayerAwake(ref Player __instance)
        {
            __instance.m_baseHP = 400;
            __instance.m_placeDelay = 0;
            __instance.m_removeDelay = 0;
            __instance.m_baseCameraShake = 0;
            __instance.m_staminaRegenDelay = 0;
            __instance.m_maxCarryWeight = 1000;
            __instance.m_maxPlaceDistance = 100;
            __instance.m_noPlacementCost = true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), "UseStamina")]
        static bool UseStamina()
        {
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
        static void UpdatePlacementGhost(ref Player __instance)
        {
            if (__instance.m_placementGhost != null)
            {
                __instance.m_placementStatus = Player.PlacementStatus.Valid;
                __instance.m_placementGhost
                    .GetComponent<Piece>()
                    .SetInvalidPlacementHeightlight(false);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), "PlayerAttackInput")]
        static bool PlayerAttackInput(
            Player __instance,
            bool ___m_attackHold,
            ref Attack ___m_currentAttack
        )
        {
            var weapon = __instance.GetCurrentWeapon();
            if (weapon != null)
            {
                if (
                    ___m_attackHold
                    && (
                        weapon.m_shared.m_skillType == Skills.SkillType.Bows
                        || weapon.m_shared.m_skillType == Skills.SkillType.Crossbows
                        || weapon.m_shared.m_skillType == Skills.SkillType.Pickaxes
                    )
                )
                {
                    weapon.m_shared.m_attack.m_projectileVel = 100;
                    weapon.m_shared.m_attack.m_projectileVelMin = 100;
                    weapon.m_shared.m_attack.m_projectileAccuracy = 0;
                    weapon.m_shared.m_attack.m_projectileAccuracyMin = 0;
                    weapon.m_shared.m_attack.m_requiresReload = false;
                    weapon.m_shared.m_attack.m_recoilPushback = 0;
                    weapon.m_shared.m_attack.m_reloadTime = 0;
                    var attack = weapon.m_shared.m_attack.Clone();
                    attack.Start(
                        __instance,
                        __instance.m_body,
                        __instance.m_zanim,
                        __instance.m_animEvent,
                        __instance.m_visEquipment,
                        weapon,
                        null,
                        0,
                        1
                    );
                    __instance.m_lastCombatTimer = 0f;
                    __instance.m_currentAttack = attack;
                    return false;
                }
            }
            return true;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Character), "UpdateGroundContact")]
        static IEnumerable<CodeInstruction> UpdateGroundContact(IEnumerable<CodeInstruction> ins)
        {
            var insl = new List<CodeInstruction>(ins);
            for (var i = 0; i < insl.Count; i++)
            {
                if (insl[i].opcode == OpCodes.Ldc_R4 && object.Equals(insl[i].operand, 100f))
                {
                    insl[i].operand = 0f;
                    break;
                }
            }
            return insl.AsEnumerable();
        }
    }
}
